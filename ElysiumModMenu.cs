#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using ElysiumModMenu;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using RewiredConsts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using static ElysiumModMenu.ElysiumModMenuGUI;
using static Rewired.UI.ControlMapper.ControlMapper;
using Color = UnityEngine.Color;
using Object = UnityEngine.Object;
using Vector3 = UnityEngine.Vector3;
namespace ElysiumModMenu
{
    [BepInPlugin("com.elysiummodmenu.menu", "ElysiumModMenu", "1.3.5.1")]
    public class Plugin : BasePlugin
    {
        public static ModPlayer modClass;

        public static Plugin Instance { get; private set; } = null!;
        public static string ElysiumFolder = "";
        public static ConfigFile MenuConfig;
        public static ConfigEntry<float> RpcSpoofDelayConfig;
        public static ConfigEntry<KeyCode> MenuKeybind;
        public static ConfigEntry<string> SpoofedLevel;
        public static ConfigEntry<bool> EnableFriendCodeSpoofConfig;
        public static ConfigEntry<string> SpoofFriendCodeConfig;
        public static ConfigEntry<bool> EnablePlatformSpoof;
        public static ConfigEntry<bool> AutoBanBrokenFriendCodeConfig;
        public static ConfigEntry<int> PlatformIndex;
        public static ConfigEntry<bool> ShowWatermarkConfig;
        public static ConfigEntry<int> MenuColorIndexConfig;
        public static ConfigEntry<bool> RgbMenuModeConfig;
        public static ConfigEntry<bool> RgbMenuTextConfig;
        public static ConfigEntry<bool> UnlockCosmeticsConfig;
        public static ConfigEntry<bool> MoreLobbyInfoConfig;
        public static ConfigEntry<bool> EnableChatDarkModeConfig;
        public static ConfigEntry<string> GhostChatColorConfig;
        public static ConfigEntry<bool> EnableAnomalyLogReportsConfig;
        public static ConfigEntry<bool> ShowEspFriendCodeConfig;

        public override void Load()
        {
            Instance = this;

            ElysiumFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "ElysiumModMenu");
            if (!System.IO.Directory.Exists(ElysiumFolder))
            {
                System.IO.Directory.CreateDirectory(ElysiumFolder);
            }

            string banFile = System.IO.Path.Combine(ElysiumFolder, "ElysiumModMenuBanList.txt");
            if (!System.IO.File.Exists(banFile))
            {
                System.IO.File.Create(banFile).Dispose();
            }

            string platformBanFile = System.IO.Path.Combine(ElysiumFolder, "ElysiumPlatformBanList.txt");
            if (!System.IO.File.Exists(platformBanFile))
            {
                System.IO.File.WriteAllText(platformBanFile, "# One custom platform token per line. Matching PlatformName values are host-banned when enabled.\n# Example: github\n");
            }

            string friendEspFile = System.IO.Path.Combine(ElysiumFolder, "ElysiumFriendEspIgnore.txt");
            if (!System.IO.File.Exists(friendEspFile))
            {
                System.IO.File.WriteAllText(friendEspFile, "# One nickname, Friend Code, or PUID per line. Matching players will not show ESP info.\n");
            }

            string botBanFile = System.IO.Path.Combine(ElysiumFolder, "ElysiumBotBanList.txt");
            if (!System.IO.File.Exists(botBanFile))
            {
                System.IO.File.WriteAllText(botBanFile, "# Auto bot ban list. Format: FriendCode|PUID|Nickname|Date|Reason\n# You can also add one nickname, Friend Code, or PUID per line to always ban matching players.\n");
            }

            string configPath = System.IO.Path.Combine(ElysiumFolder, "ElysiumModMenu.cfg");
            RemoveLegacyPlaintextWebhookConfig(configPath);
            MenuConfig = new ConfigFile(configPath, true);
            RpcSpoofDelayConfig = MenuConfig.Bind("ElysiumModMenu.Spoofing", "RpcDelay", 4f, "");
            MenuKeybind = MenuConfig.Bind("ElysiumModMenu.GUI", "Keybind", KeyCode.Insert, "");
            SpoofedLevel = MenuConfig.Bind("ElysiumModMenu.Spoofing", "Level", "100", "");
            EnableFriendCodeSpoofConfig = MenuConfig.Bind("ElysiumModMenu.Spoofing", "EnableFriendCodeSpoof", false, "");
            SpoofFriendCodeConfig = MenuConfig.Bind("ElysiumModMenu.Spoofing", "FriendCode", "crewmate01", "");
            EnablePlatformSpoof = MenuConfig.Bind("ElysiumModMenu.Spoofing", "EnablePlatformSpoof", true, "");
            AutoBanBrokenFriendCodeConfig = MenuConfig.Bind("ElysiumModMenu.Anticheat", "AutoBanBrokenFriendCode", false, "");
            PlatformIndex = MenuConfig.Bind("ElysiumModMenu.Spoofing", "PlatformIndex", 1, "");
            ShowWatermarkConfig = MenuConfig.Bind("ElysiumModMenu.GUI", "ShowWatermark", true, "");
            MenuColorIndexConfig = MenuConfig.Bind("ElysiumModMenu.GUI", "MenuColorIndex", 10, "");
            RgbMenuModeConfig = MenuConfig.Bind("ElysiumModMenu.GUI", "RgbMenuMode", false, "");
            RgbMenuTextConfig = MenuConfig.Bind("ElysiumModMenu.GUI", "RgbMenuText", false, "When true, RGB Menu Mode also recolors menu text.");
            UnlockCosmeticsConfig = MenuConfig.Bind("ElysiumModMenu.General", "UnlockCosmetics", true, "");
            MoreLobbyInfoConfig = MenuConfig.Bind("ElysiumModMenu.Visuals", "MoreLobbyInfo", true, "");
            EnableChatDarkModeConfig = MenuConfig.Bind("ElysiumModMenu.Chat", "EnableChatDarkMode", true, "Turns the custom dark chat input and bubble colors on/off.");
            GhostChatColorConfig = MenuConfig.Bind("ElysiumModMenu.Chat", "GhostChatColor", "#D7B8FF", "Hex color for visible ghost chat messages.");
            EnableAnomalyLogReportsConfig = MenuConfig.Bind("ElysiumModMenu.Diagnostics", "EnableAnomalyLogReports", true, "Yes/No: sending freeze/overload logs to the mod author. Note: this does not affect your performance, nor does it steal your data or anything like that. It is strictly needed for quick anti-cheat fixes.");
            ShowEspFriendCodeConfig = MenuConfig.Bind("ElysiumModMenu.Visuals", "ShowEspFriendCode", true, "Show Friend Code in ESP player info.");
            ClassInjector.RegisterTypeInIl2Cpp<ElysiumModMenuGUI>();
            ClassInjector.RegisterTypeInIl2Cpp<ModPlayer>();

            var guiObject = new GameObject("ElysiumModMenu_Object");
            UnityEngine.Object.DontDestroyOnLoad(guiObject);
            guiObject.hideFlags = HideFlags.HideAndDontSave;
            guiObject.AddComponent<ElysiumModMenuGUI>();

            modClass = AddComponent<ModPlayer>();

            var harmony = new Harmony("com.elysiummodmenu.harmony");
            harmony.PatchAll();
        }

        private static void RemoveLegacyPlaintextWebhookConfig(string configPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(configPath) || !System.IO.File.Exists(configPath)) return;

                List<string> lines = System.IO.File.ReadAllLines(configPath).ToList();
                string[] legacyKeys = { "AnomalyLogWebhookUrl", "EnableDiagnostics", "EndpointUrl", "AuthKey", "IncludePuid" };
                bool changed = false;

                for (int i = lines.Count - 1; i >= 0; i--)
                {
                    string trimmed = lines[i].TrimStart();
                    if (!legacyKeys.Any(key => trimmed.StartsWith(key, StringComparison.OrdinalIgnoreCase))) continue;

                    int start = i;
                    while (start > 0)
                    {
                        string previous = lines[start - 1].TrimStart();
                        if (!previous.StartsWith("#") && previous.Length != 0) break;
                        start--;
                        if (previous.Length == 0) break;
                    }

                    lines.RemoveRange(start, i - start + 1);
                    changed = true;
                }

                if (!changed) return;
                System.IO.File.WriteAllLines(configPath, lines.ToArray());
            }
            catch { }
        }
    }



    public class ElysiumModMenuGUI : MonoBehaviour
    {
        public static string[] spoofMenuNames = { "ElysiumModMenu", "HostGuard/TOH", "Polar", "BanMod", "Better Among Us", "Sicko Menu", "GNC", "KillNetwork (V1)", "KillNetwork (V2)", "KNM" };
        public static byte[] spoofMenuRPCs = { 89, 176, 204, 212, 151, 164, 154, 85, 150, 162 };
        public static float rpcSpoofDelay = 4f;
        public static readonly string[] menuLanguageNames = { "Auto", "English", "Русский", "Deutsch", "Français", "Español", "Italiano", "Português", "Polski", "Nederlands", "Türkçe", "Čeština", "Română", "Magyar", "Svenska", "Dansk", "Suomi", "Norsk", "Українська", "Ελληνικά", "中文", "日本語", "한국어" };
        public static readonly string[] menuLanguageCodes = { "auto", "en", "ru", "de", "fr", "es", "it", "pt", "pl", "nl", "tr", "cs", "ro", "hu", "sv", "da", "fi", "no", "uk", "el", "zh", "ja", "ko" };
        public static int currentMenuLanguageIndex = 0;
        private static readonly Dictionary<string, Dictionary<string, string>> menuTranslations = new Dictionary<string, Dictionary<string, string>>
        {
            ["de"] = new Dictionary<string, string> { ["GENERAL"]="ALLGEMEIN",["SELF"]="SPIELER",["VISUALS"]="VISUELL",["PLAYERS"]="SPIELER",["SABOTAGES"]="SABOTAGEN",["HOST ONLY"]="NUR HOST",["OUTFITS"]="OUTFITS",["VOTEKICK"]="ABSTIMMUNG",["MENU"]="MENÜ",["MAPS"]="KARTEN",["ANIMATIONS"]="ANIMATIONEN",["INFORMATION"]="INFORMATION",["KEYBINDS"]="TASTEN",["WELCOME"]="WILLKOMMEN",["CREDITS"]="CREDITS",["Menu language:"]="Menüsprache:",["FPS Limit"]="FPS-Limit",["Chat History"]="Chat-Verlauf",["History:"]="Verlauf:",["History size:"]="Verlaufgröße:",["CHAT UTILITY"]="CHAT-TOOLS",["Always Show Chat"]="Chat immer anzeigen",["Read Ghost Chat"]="Geisterchat lesen",["Extended Chat"]="Erweiterter Chat",["Fast Chat"]="Schneller Chat",["Unlock Extra Characters"]="Alle Zeichen erlauben",["Spell Check"]="Rechtschreibprüfung",["Clipboard"]="Zwischenablage",["Save Chat Log"]="Chatlog speichern",["Dark Chat Theme"]="Dunkles Chat-Thema",["Enable /color"]="Aktiviere /color",["Block Fortegreen"]="Fortegreen blockieren",["Allow Duplicate Colors"]="Doppelte Farben erlauben",["Auto Ghost After Start"]="Auto-Geist nach Start",["FAVORITE OUTFITS"]="FAVORITEN-OUTFITS",["Slot"]="Slot",["Empty"]="Leer",["Apply"]="Anwenden",["Save Mine"]="Meins speichern",["Save Selected"]="Auswahl speichern",["Saved slot"]="Slot gespeichert",["Applied slot"]="Slot angewendet",["Cleared slot"]="Slot gelöscht",["Auto-Ban Platform Spoof (Host)"]="Auto-Ban Plattform-Spoof (Host)",["Ban Custom Platforms From TXT"]="Custom-Plattformen aus TXT bannen",["RPC Anti-Cheat"]="RPC-Anti-Cheat",["RPC limit:"]="RPC-Limit:",["RPC Local Drop"]="RPC lokal droppen",["RPC Host Ban"]="RPC Host-Ban" },
            ["fr"] = new Dictionary<string, string> { ["GENERAL"]="GÉNÉRAL",["SELF"]="JOUEUR",["VISUALS"]="VISUELS",["PLAYERS"]="JOUEURS",["SABOTAGES"]="SABOTAGES",["HOST ONLY"]="HÔTE",["OUTFITS"]="TENUES",["VOTEKICK"]="VOTEKICK",["MENU"]="MENU",["MAPS"]="CARTES",["ANIMATIONS"]="ANIMATIONS",["INFORMATION"]="INFORMATION",["KEYBINDS"]="TOUCHES",["WELCOME"]="ACCUEIL",["CREDITS"]="CRÉDITS",["Menu language:"]="Langue du menu :",["FPS Limit"]="Limite FPS",["Chat History"]="Historique du chat",["History:"]="Historique :",["History size:"]="Taille historique :",["CHAT UTILITY"]="OUTILS CHAT",["Always Show Chat"]="Toujours afficher le chat",["Read Ghost Chat"]="Lire le chat fantôme",["Extended Chat"]="Chat étendu",["Fast Chat"]="Chat rapide",["Unlock Extra Characters"]="Autoriser tous les caractères",["Spell Check"]="Correction",["Clipboard"]="Presse-papiers",["Save Chat Log"]="Sauver le log chat",["Dark Chat Theme"]="Thème chat sombre",["Enable /color"]="Activer /color",["Block Fortegreen"]="Bloquer Fortegreen",["Allow Duplicate Colors"]="Autoriser les couleurs doubles",["Auto Ghost After Start"]="Fantôme auto après départ",["FAVORITE OUTFITS"]="TENUES FAVORITES",["Slot"]="Empl.",["Empty"]="Vide",["Apply"]="Appliquer",["Save Mine"]="Sauver mien",["Save Selected"]="Sauver sélection",["Saved slot"]="Emplacement sauvé",["Applied slot"]="Emplacement appliqué",["Cleared slot"]="Emplacement vidé",["Auto-Ban Platform Spoof (Host)"]="Auto-ban spoof plateforme (Hôte)",["Ban Custom Platforms From TXT"]="Ban plateformes custom TXT",["RPC Anti-Cheat"]="Anti-cheat RPC",["RPC limit:"]="Limite RPC :",["RPC Local Drop"]="Drop RPC local",["RPC Host Ban"]="Ban RPC hôte" },
            ["es"] = new Dictionary<string, string> { ["GENERAL"]="GENERAL",["SELF"]="JUGADOR",["VISUALS"]="VISUALES",["PLAYERS"]="JUGADORES",["SABOTAGES"]="SABOTAJES",["HOST ONLY"]="HOST",["OUTFITS"]="ATUENDOS",["VOTEKICK"]="VOTOKICK",["MENU"]="MENÚ",["MAPS"]="MAPAS",["ANIMATIONS"]="ANIMACIONES",["INFORMATION"]="INFORMACIÓN",["KEYBINDS"]="TECLAS",["WELCOME"]="BIENVENIDA",["CREDITS"]="CRÉDITOS",["Menu language:"]="Idioma del menú:",["FPS Limit"]="Límite FPS",["Chat History"]="Historial de chat",["History:"]="Historial:",["History size:"]="Tamaño historial:",["CHAT UTILITY"]="UTILIDAD CHAT",["Always Show Chat"]="Mostrar chat siempre",["Read Ghost Chat"]="Leer chat fantasma",["Extended Chat"]="Chat extendido",["Fast Chat"]="Chat rápido",["Unlock Extra Characters"]="Permitir caracteres extra",["Spell Check"]="Ortografía",["Clipboard"]="Portapapeles",["Save Chat Log"]="Guardar log chat",["Dark Chat Theme"]="Tema chat oscuro",["Enable /color"]="Activar /color",["Block Fortegreen"]="Bloquear Fortegreen",["Allow Duplicate Colors"]="Permitir colores duplicados",["Auto Ghost After Start"]="Fantasma auto al iniciar",["FAVORITE OUTFITS"]="ATUENDOS FAVORITOS",["Slot"]="Ranura",["Empty"]="Vacío",["Apply"]="Aplicar",["Save Mine"]="Guardar mío",["Save Selected"]="Guardar selección",["Saved slot"]="Ranura guardada",["Applied slot"]="Ranura aplicada",["Cleared slot"]="Ranura borrada",["Auto-Ban Platform Spoof (Host)"]="Auto-ban spoof plataforma (Host)",["Ban Custom Platforms From TXT"]="Ban plataformas TXT",["RPC Anti-Cheat"]="Anti-cheat RPC",["RPC limit:"]="Límite RPC:",["RPC Local Drop"]="Drop RPC local",["RPC Host Ban"]="Ban RPC host" },
            ["it"] = new Dictionary<string, string> { ["GENERAL"]="GENERALE",["SELF"]="GIOCATORE",["VISUALS"]="VISIVI",["PLAYERS"]="GIOCATORI",["SABOTAGES"]="SABOTAGGI",["HOST ONLY"]="HOST",["OUTFITS"]="OUTFIT",["VOTEKICK"]="VOTEKICK",["MENU"]="MENU",["MAPS"]="MAPPE",["ANIMATIONS"]="ANIMAZIONI",["INFORMATION"]="INFO",["KEYBINDS"]="TASTI",["WELCOME"]="BENVENUTO",["CREDITS"]="CREDITI",["Menu language:"]="Lingua menu:",["FPS Limit"]="Limite FPS",["Chat History"]="Cronologia chat",["History:"]="Cronologia:",["History size:"]="Dim. cronologia:",["CHAT UTILITY"]="UTILITÀ CHAT",["Always Show Chat"]="Mostra sempre chat",["Read Ghost Chat"]="Leggi chat fantasmi",["Extended Chat"]="Chat estesa",["Fast Chat"]="Chat veloce",["Unlock Extra Characters"]="Sblocca caratteri extra",["Spell Check"]="Correttore",["Clipboard"]="Appunti",["Save Chat Log"]="Salva log chat",["Dark Chat Theme"]="Tema chat scuro",["Enable /color"]="Abilita /color",["Block Fortegreen"]="Blocca Fortegreen",["Allow Duplicate Colors"]="Consenti colori doppi",["Auto Ghost After Start"]="Fantasma auto dopo start",["FAVORITE OUTFITS"]="OUTFIT PREFERITI",["Slot"]="Slot",["Empty"]="Vuoto",["Apply"]="Applica",["Save Mine"]="Salva mio",["Save Selected"]="Salva selez.",["Saved slot"]="Slot salvato",["Applied slot"]="Slot applicato",["Cleared slot"]="Slot pulito",["Auto-Ban Platform Spoof (Host)"]="Auto-ban spoof piattaforma",["Ban Custom Platforms From TXT"]="Ban piattaforme custom TXT",["RPC Anti-Cheat"]="Anti-cheat RPC",["RPC limit:"]="Limite RPC:",["RPC Local Drop"]="Drop RPC locale",["RPC Host Ban"]="Ban RPC host" },
            ["pt"] = new Dictionary<string, string> { ["GENERAL"]="GERAL",["SELF"]="JOGADOR",["VISUALS"]="VISUAIS",["PLAYERS"]="JOGADORES",["SABOTAGES"]="SABOTAGENS",["HOST ONLY"]="HOST",["OUTFITS"]="VISUAIS",["VOTEKICK"]="VOTEKICK",["MENU"]="MENU",["MAPS"]="MAPAS",["ANIMATIONS"]="ANIMAÇÕES",["INFORMATION"]="INFORMAÇÃO",["KEYBINDS"]="TECLAS",["WELCOME"]="BOAS-VINDAS",["CREDITS"]="CRÉDITOS",["Menu language:"]="Idioma do menu:",["FPS Limit"]="Limite FPS",["Chat History"]="Histórico do chat",["History:"]="Histórico:",["History size:"]="Tamanho histórico:",["CHAT UTILITY"]="UTILIDADE CHAT",["Always Show Chat"]="Sempre mostrar chat",["Read Ghost Chat"]="Ler chat fantasma",["Extended Chat"]="Chat estendido",["Fast Chat"]="Chat rápido",["Unlock Extra Characters"]="Liberar caracteres extra",["Spell Check"]="Ortografia",["Clipboard"]="Área de transferência",["Save Chat Log"]="Salvar log chat",["Dark Chat Theme"]="Tema chat escuro",["Enable /color"]="Ativar /color",["Block Fortegreen"]="Bloquear Fortegreen",["Allow Duplicate Colors"]="Permitir cores duplicadas",["Auto Ghost After Start"]="Fantasma auto após iniciar",["FAVORITE OUTFITS"]="VISUAIS FAVORITOS",["Slot"]="Slot",["Empty"]="Vazio",["Apply"]="Aplicar",["Save Mine"]="Salvar meu",["Save Selected"]="Salvar seleção",["Saved slot"]="Slot salvo",["Applied slot"]="Slot aplicado",["Cleared slot"]="Slot limpo",["Auto-Ban Platform Spoof (Host)"]="Auto-ban spoof plataforma",["Ban Custom Platforms From TXT"]="Ban plataformas TXT",["RPC Anti-Cheat"]="Anti-cheat RPC",["RPC limit:"]="Limite RPC:",["RPC Local Drop"]="Drop RPC local",["RPC Host Ban"]="Ban RPC host" },
            ["pl"] = new Dictionary<string, string> { ["GENERAL"]="OGÓLNE",["SELF"]="GRACZ",["VISUALS"]="WIZUALNE",["PLAYERS"]="GRACZE",["SABOTAGES"]="SABOTAŻE",["HOST ONLY"]="HOST",["OUTFITS"]="STROJE",["VOTEKICK"]="VOTEKICK",["MENU"]="MENU",["MAPS"]="MAPY",["ANIMATIONS"]="ANIMACJE",["INFORMATION"]="INFORMACJE",["KEYBINDS"]="KLAWISZE",["WELCOME"]="WITAJ",["CREDITS"]="AUTORZY",["Menu language:"]="Język menu:",["FPS Limit"]="Limit FPS",["Chat History"]="Historia czatu",["History:"]="Historia:",["History size:"]="Rozmiar historii:",["CHAT UTILITY"]="NARZĘDZIA CZATU",["Always Show Chat"]="Zawsze pokazuj czat",["Read Ghost Chat"]="Czytaj czat duchów",["Extended Chat"]="Rozszerzony czat",["Fast Chat"]="Szybki czat",["Unlock Extra Characters"]="Odblokuj znaki",["Spell Check"]="Pisownia",["Clipboard"]="Schowek",["Save Chat Log"]="Zapisz log czatu",["Dark Chat Theme"]="Ciemny czat",["Enable /color"]="Włącz /color",["Block Fortegreen"]="Blokuj Fortegreen",["Allow Duplicate Colors"]="Zezwól na duplikaty kolorów",["Auto Ghost After Start"]="Auto duch po starcie",["FAVORITE OUTFITS"]="ULUBIONE STROJE",["Slot"]="Slot",["Empty"]="Pusty",["Apply"]="Zastosuj",["Save Mine"]="Zapisz mój",["Save Selected"]="Zapisz wybrany",["Saved slot"]="Slot zapisany",["Applied slot"]="Slot użyty",["Cleared slot"]="Slot wyczyszczony",["Auto-Ban Platform Spoof (Host)"]="Auto-ban spoof platformy",["Ban Custom Platforms From TXT"]="Ban platform z TXT",["RPC Anti-Cheat"]="Anti-cheat RPC",["RPC limit:"]="Limit RPC:",["RPC Local Drop"]="Lokalny drop RPC",["RPC Host Ban"]="Ban RPC hosta" },
            ["nl"] = new Dictionary<string, string> { ["GENERAL"]="ALGEMEEN",["SELF"]="SPELER",["VISUALS"]="VISUEEL",["PLAYERS"]="SPELERS",["SABOTAGES"]="SABOTAGES",["HOST ONLY"]="HOST",["OUTFITS"]="OUTFITS",["VOTEKICK"]="VOTEKICK",["MENU"]="MENU",["MAPS"]="KAARTEN",["ANIMATIONS"]="ANIMATIES",["INFORMATION"]="INFORMATIE",["KEYBINDS"]="TOETSEN",["WELCOME"]="WELKOM",["CREDITS"]="CREDITS",["Menu language:"]="Menutaal:",["FPS Limit"]="FPS-limiet",["Chat History"]="Chatgeschiedenis",["History:"]="Geschiedenis:",["History size:"]="Geschiedenisgrootte:",["CHAT UTILITY"]="CHAT-HULP",["Always Show Chat"]="Chat altijd tonen",["Read Ghost Chat"]="Geestenchat lezen",["Extended Chat"]="Uitgebreide chat",["Fast Chat"]="Snelle chat",["Unlock Extra Characters"]="Extra tekens toestaan",["Spell Check"]="Spelling",["Clipboard"]="Klembord",["Save Chat Log"]="Chatlog opslaan",["Dark Chat Theme"]="Donker chatthema",["Enable /color"]="/color inschakelen",["Block Fortegreen"]="Fortegreen blokkeren",["Allow Duplicate Colors"]="Dubbele kleuren toestaan",["Auto Ghost After Start"]="Auto-geest na start",["FAVORITE OUTFITS"]="FAVORIETE OUTFITS",["Slot"]="Slot",["Empty"]="Leeg",["Apply"]="Toepassen",["Save Mine"]="Mijn opslaan",["Save Selected"]="Selectie opslaan",["Saved slot"]="Slot opgeslagen",["Applied slot"]="Slot toegepast",["Cleared slot"]="Slot gewist",["Auto-Ban Platform Spoof (Host)"]="Auto-ban platform-spoof",["Ban Custom Platforms From TXT"]="Ban custom platforms uit TXT",["RPC Anti-Cheat"]="RPC anti-cheat",["RPC limit:"]="RPC-limiet:",["RPC Local Drop"]="RPC lokale drop",["RPC Host Ban"]="RPC host-ban" },
            ["tr"] = new Dictionary<string, string> { ["GENERAL"]="GENEL",["SELF"]="OYUNCU",["VISUALS"]="GÖRSEL",["PLAYERS"]="OYUNCULAR",["SABOTAGES"]="SABOTAJLAR",["HOST ONLY"]="HOST",["OUTFITS"]="KIYAFETLER",["VOTEKICK"]="VOTEKICK",["MENU"]="MENÜ",["MAPS"]="HARİTALAR",["ANIMATIONS"]="ANİMASYONLAR",["INFORMATION"]="BİLGİ",["KEYBINDS"]="TUŞLAR",["WELCOME"]="HOŞ GELDİN",["CREDITS"]="KREDİLER",["Menu language:"]="Menü dili:",["FPS Limit"]="FPS sınırı",["Chat History"]="Sohbet geçmişi",["History:"]="Geçmiş:",["History size:"]="Geçmiş boyutu:",["CHAT UTILITY"]="SOHBET ARAÇLARI",["Always Show Chat"]="Sohbeti hep göster",["Read Ghost Chat"]="Hayalet sohbetini oku",["Extended Chat"]="Geniş sohbet",["Fast Chat"]="Hızlı sohbet",["Unlock Extra Characters"]="Ek karakterleri aç",["Spell Check"]="Yazım denetimi",["Clipboard"]="Pano",["Save Chat Log"]="Sohbet kaydını sakla",["Dark Chat Theme"]="Koyu sohbet teması",["Enable /color"]="/color aç",["Block Fortegreen"]="Fortegreen engelle",["Allow Duplicate Colors"]="Aynı renklere izin ver",["Auto Ghost After Start"]="Başlangıçtan sonra oto hayalet",["FAVORITE OUTFITS"]="FAVORİ KIYAFETLER",["Slot"]="Slot",["Empty"]="Boş",["Apply"]="Uygula",["Save Mine"]="Benimkini kaydet",["Save Selected"]="Seçileni kaydet",["Saved slot"]="Slot kaydedildi",["Applied slot"]="Slot uygulandı",["Cleared slot"]="Slot temizlendi",["Auto-Ban Platform Spoof (Host)"]="Platform spoof oto-ban",["Ban Custom Platforms From TXT"]="TXT özel platform ban",["RPC Anti-Cheat"]="RPC anti-cheat",["RPC limit:"]="RPC sınırı:",["RPC Local Drop"]="RPC yerel drop",["RPC Host Ban"]="RPC host ban" },
            ["cs"] = new Dictionary<string, string> { ["GENERAL"]="OBECNÉ",["SELF"]="HRÁČ",["VISUALS"]="VIZUÁLY",["PLAYERS"]="HRÁČI",["SABOTAGES"]="SABOTÁŽE",["HOST ONLY"]="HOST",["OUTFITS"]="OUTFITY",["VOTEKICK"]="VOTEKICK",["MENU"]="MENU",["MAPS"]="MAPY",["ANIMATIONS"]="ANIMACE",["INFORMATION"]="INFORMACE",["KEYBINDS"]="KLÁVESY",["WELCOME"]="VÍTEJ",["CREDITS"]="AUTOŘI",["Menu language:"]="Jazyk menu:",["FPS Limit"]="Limit FPS",["Chat History"]="Historie chatu",["History:"]="Historie:",["History size:"]="Velikost historie:",["CHAT UTILITY"]="NÁSTROJE CHATU",["Always Show Chat"]="Vždy zobrazit chat",["Read Ghost Chat"]="Číst chat duchů",["Extended Chat"]="Rozšířený chat",["Fast Chat"]="Rychlý chat",["Unlock Extra Characters"]="Povolit další znaky",["Spell Check"]="Kontrola pravopisu",["Clipboard"]="Schránka",["Save Chat Log"]="Uložit log chatu",["Dark Chat Theme"]="Tmavý chat",["Enable /color"]="Zapnout /color",["Block Fortegreen"]="Blokovat Fortegreen",["Allow Duplicate Colors"]="Povolit duplicitní barvy",["Auto Ghost After Start"]="Auto duch po startu",["FAVORITE OUTFITS"]="OBLÍBENÉ OUTFITY",["Slot"]="Slot",["Empty"]="Prázdné",["Apply"]="Použít",["Save Mine"]="Uložit můj",["Save Selected"]="Uložit vybraný",["Saved slot"]="Slot uložen",["Applied slot"]="Slot použit",["Cleared slot"]="Slot vymazán",["Auto-Ban Platform Spoof (Host)"]="Auto-ban spoof platformy",["Ban Custom Platforms From TXT"]="Ban platforem z TXT",["RPC Anti-Cheat"]="RPC anti-cheat",["RPC limit:"]="Limit RPC:",["RPC Local Drop"]="Místní drop RPC",["RPC Host Ban"]="RPC host ban" },
            ["ro"] = new Dictionary<string, string> { ["GENERAL"]="GENERAL",["SELF"]="JUCĂTOR",["VISUALS"]="VIZUAL",["PLAYERS"]="JUCĂTORI",["SABOTAGES"]="SABOTAJE",["HOST ONLY"]="HOST",["OUTFITS"]="ȚINUTE",["VOTEKICK"]="VOTEKICK",["MENU"]="MENIU",["MAPS"]="HĂRȚI",["ANIMATIONS"]="ANIMAȚII",["INFORMATION"]="INFORMAȚII",["KEYBINDS"]="TASTE",["WELCOME"]="BUN VENIT",["CREDITS"]="CREDITE",["Menu language:"]="Limba meniului:",["FPS Limit"]="Limită FPS",["Chat History"]="Istoric chat",["History:"]="Istoric:",["History size:"]="Mărime istoric:",["CHAT UTILITY"]="UTILITARE CHAT",["Always Show Chat"]="Arată chat mereu",["Read Ghost Chat"]="Citește chat fantome",["Extended Chat"]="Chat extins",["Fast Chat"]="Chat rapid",["Unlock Extra Characters"]="Permite caractere extra",["Spell Check"]="Ortografie",["Clipboard"]="Clipboard",["Save Chat Log"]="Salvează log chat",["Dark Chat Theme"]="Temă chat întunecată",["Enable /color"]="Activează /color",["Block Fortegreen"]="Blochează Fortegreen",["Allow Duplicate Colors"]="Permite culori duplicate",["Auto Ghost After Start"]="Fantoma auto după start",["FAVORITE OUTFITS"]="ȚINUTE FAVORITE",["Slot"]="Slot",["Empty"]="Gol",["Apply"]="Aplică",["Save Mine"]="Salvează al meu",["Save Selected"]="Salvează selectat",["Saved slot"]="Slot salvat",["Applied slot"]="Slot aplicat",["Cleared slot"]="Slot golit",["Auto-Ban Platform Spoof (Host)"]="Auto-ban spoof platformă",["Ban Custom Platforms From TXT"]="Ban platforme din TXT",["RPC Anti-Cheat"]="Anti-cheat RPC",["RPC limit:"]="Limită RPC:",["RPC Local Drop"]="Drop RPC local",["RPC Host Ban"]="Ban RPC host" },
            ["hu"] = new Dictionary<string, string> { ["GENERAL"]="ÁLTALÁNOS",["SELF"]="JÁTÉKOS",["VISUALS"]="LÁTVÁNY",["PLAYERS"]="JÁTÉKOSOK",["SABOTAGES"]="SZABOTÁZS",["HOST ONLY"]="HOST",["OUTFITS"]="RUHÁK",["VOTEKICK"]="VOTEKICK",["MENU"]="MENÜ",["MAPS"]="PÁLYÁK",["ANIMATIONS"]="ANIMÁCIÓK",["INFORMATION"]="INFORMÁCIÓ",["KEYBINDS"]="BILLENTYŰK",["WELCOME"]="ÜDV",["CREDITS"]="KÉSZÍTŐK",["Menu language:"]="Menü nyelve:",["FPS Limit"]="FPS limit",["Chat History"]="Chat előzmények",["History:"]="Előzmény:",["History size:"]="Előzmény méret:",["CHAT UTILITY"]="CHAT ESZKÖZÖK",["Always Show Chat"]="Chat mindig látszik",["Read Ghost Chat"]="Szellem chat olvasás",["Extended Chat"]="Bővített chat",["Fast Chat"]="Gyors chat",["Unlock Extra Characters"]="Extra karakterek engedélyezése",["Spell Check"]="Helyesírás",["Clipboard"]="Vágólap",["Save Chat Log"]="Chat log mentése",["Dark Chat Theme"]="Sötét chat téma",["Enable /color"]="/color bekapcsolása",["Block Fortegreen"]="Fortegreen tiltása",["Allow Duplicate Colors"]="Dupla színek engedése",["Auto Ghost After Start"]="Auto szellem start után",["FAVORITE OUTFITS"]="KEDVENC RUHÁK",["Slot"]="Slot",["Empty"]="Üres",["Apply"]="Alkalmaz",["Save Mine"]="Saját mentése",["Save Selected"]="Kiválasztott mentése",["Saved slot"]="Slot mentve",["Applied slot"]="Slot alkalmazva",["Cleared slot"]="Slot törölve",["Auto-Ban Platform Spoof (Host)"]="Platform spoof auto-ban",["Ban Custom Platforms From TXT"]="Platform ban TXT-ből",["RPC Anti-Cheat"]="RPC anti-cheat",["RPC limit:"]="RPC limit:",["RPC Local Drop"]="RPC helyi drop",["RPC Host Ban"]="RPC host ban" },
            ["sv"] = new Dictionary<string, string> { ["GENERAL"]="ALLMÄNT",["SELF"]="SPELARE",["VISUALS"]="VISUELLT",["PLAYERS"]="SPELARE",["SABOTAGES"]="SABOTAGE",["HOST ONLY"]="HOST",["OUTFITS"]="OUTFITS",["VOTEKICK"]="VOTEKICK",["MENU"]="MENY",["MAPS"]="KARTOR",["ANIMATIONS"]="ANIMATIONER",["INFORMATION"]="INFO",["KEYBINDS"]="TANGENTER",["WELCOME"]="VÄLKOMMEN",["CREDITS"]="CREDITS",["Menu language:"]="Menyspråk:",["FPS Limit"]="FPS-gräns",["Chat History"]="Chatthistorik",["History:"]="Historik:",["History size:"]="Historikstorlek:",["CHAT UTILITY"]="CHATTVERKTYG",["Always Show Chat"]="Visa alltid chatt",["Read Ghost Chat"]="Läs spökchatt",["Extended Chat"]="Utökad chatt",["Fast Chat"]="Snabb chatt",["Unlock Extra Characters"]="Tillåt extra tecken",["Spell Check"]="Stavning",["Clipboard"]="Urklipp",["Save Chat Log"]="Spara chattlogg",["Dark Chat Theme"]="Mörkt chatt-tema",["Enable /color"]="Aktivera /color",["Block Fortegreen"]="Blockera Fortegreen",["Allow Duplicate Colors"]="Tillåt dubbla färger",["Auto Ghost After Start"]="Auto-spöke efter start",["FAVORITE OUTFITS"]="FAVORITOUTFITS",["Slot"]="Slot",["Empty"]="Tom",["Apply"]="Använd",["Save Mine"]="Spara min",["Save Selected"]="Spara vald",["Saved slot"]="Slot sparad",["Applied slot"]="Slot använd",["Cleared slot"]="Slot rensad",["Auto-Ban Platform Spoof (Host)"]="Auto-ban plattformsspoof",["Ban Custom Platforms From TXT"]="Ban plattformar från TXT",["RPC Anti-Cheat"]="RPC anti-cheat",["RPC limit:"]="RPC-gräns:",["RPC Local Drop"]="RPC lokal drop",["RPC Host Ban"]="RPC host-ban" },
            ["da"] = new Dictionary<string, string> { ["GENERAL"]="GENERELT",["SELF"]="SPILLER",["VISUALS"]="VISUELT",["PLAYERS"]="SPILLERE",["SABOTAGES"]="SABOTAGER",["HOST ONLY"]="HOST",["OUTFITS"]="OUTFITS",["VOTEKICK"]="VOTEKICK",["MENU"]="MENU",["MAPS"]="BANER",["ANIMATIONS"]="ANIMATIONER",["INFORMATION"]="INFO",["KEYBINDS"]="TASTER",["WELCOME"]="VELKOMMEN",["CREDITS"]="CREDITS",["Menu language:"]="Menusprog:",["FPS Limit"]="FPS-grænse",["Chat History"]="Chathistorik",["History:"]="Historik:",["History size:"]="Historikstørrelse:",["CHAT UTILITY"]="CHATVÆRKTØJ",["Always Show Chat"]="Vis altid chat",["Read Ghost Chat"]="Læs spøgelses-chat",["Extended Chat"]="Udvidet chat",["Fast Chat"]="Hurtig chat",["Unlock Extra Characters"]="Tillad ekstra tegn",["Spell Check"]="Stavekontrol",["Clipboard"]="Udklipsholder",["Save Chat Log"]="Gem chatlog",["Dark Chat Theme"]="Mørkt chattema",["Enable /color"]="Aktiver /color",["Block Fortegreen"]="Bloker Fortegreen",["Allow Duplicate Colors"]="Tillad dubletfarver",["Auto Ghost After Start"]="Auto-spøgelse efter start",["FAVORITE OUTFITS"]="FAVORITOUTFITS",["Slot"]="Slot",["Empty"]="Tom",["Apply"]="Anvend",["Save Mine"]="Gem min",["Save Selected"]="Gem valgt",["Saved slot"]="Slot gemt",["Applied slot"]="Slot anvendt",["Cleared slot"]="Slot ryddet",["Auto-Ban Platform Spoof (Host)"]="Auto-ban platform spoof",["Ban Custom Platforms From TXT"]="Ban platforme fra TXT",["RPC Anti-Cheat"]="RPC anti-cheat",["RPC limit:"]="RPC-grænse:",["RPC Local Drop"]="RPC lokal drop",["RPC Host Ban"]="RPC host-ban" },
            ["fi"] = new Dictionary<string, string> { ["GENERAL"]="YLEISET",["SELF"]="PELAAJA",["VISUALS"]="VISUAALIT",["PLAYERS"]="PELAAJAT",["SABOTAGES"]="SABOTAASIT",["HOST ONLY"]="HOST",["OUTFITS"]="ASUT",["VOTEKICK"]="VOTEKICK",["MENU"]="VALIKKO",["MAPS"]="KARTAT",["ANIMATIONS"]="ANIMAATIOT",["INFORMATION"]="TIEDOT",["KEYBINDS"]="NÄPPÄIMET",["WELCOME"]="TERVETULOA",["CREDITS"]="TEKIJÄT",["Menu language:"]="Valikon kieli:",["FPS Limit"]="FPS-raja",["Chat History"]="Chat-historia",["History:"]="Historia:",["History size:"]="Historian koko:",["CHAT UTILITY"]="CHAT-TYÖKALUT",["Always Show Chat"]="Näytä chat aina",["Read Ghost Chat"]="Lue haamuchat",["Extended Chat"]="Laajennettu chat",["Fast Chat"]="Nopea chat",["Unlock Extra Characters"]="Salli lisämerkit",["Spell Check"]="Oikoluku",["Clipboard"]="Leikepöytä",["Save Chat Log"]="Tallenna chat-loki",["Dark Chat Theme"]="Tumma chat-teema",["Enable /color"]="Ota /color käyttöön",["Block Fortegreen"]="Estä Fortegreen",["Allow Duplicate Colors"]="Salli samat värit",["Auto Ghost After Start"]="Auto-haamu alun jälkeen",["FAVORITE OUTFITS"]="SUOSIKKIASUT",["Slot"]="Paikka",["Empty"]="Tyhjä",["Apply"]="Käytä",["Save Mine"]="Tallenna oma",["Save Selected"]="Tallenna valittu",["Saved slot"]="Paikka tallennettu",["Applied slot"]="Paikka käytetty",["Cleared slot"]="Paikka tyhjennetty",["Auto-Ban Platform Spoof (Host)"]="Auto-ban platform spoof",["Ban Custom Platforms From TXT"]="Ban alustat TXT:stä",["RPC Anti-Cheat"]="RPC anti-cheat",["RPC limit:"]="RPC-raja:",["RPC Local Drop"]="RPC paikallinen drop",["RPC Host Ban"]="RPC host-ban" },
            ["no"] = new Dictionary<string, string> { ["GENERAL"]="GENERELT",["SELF"]="SPILLER",["VISUALS"]="VISUELT",["PLAYERS"]="SPILLERE",["SABOTAGES"]="SABOTASJER",["HOST ONLY"]="HOST",["OUTFITS"]="ANTREKK",["VOTEKICK"]="VOTEKICK",["MENU"]="MENY",["MAPS"]="KART",["ANIMATIONS"]="ANIMASJONER",["INFORMATION"]="INFO",["KEYBINDS"]="TASTER",["WELCOME"]="VELKOMMEN",["CREDITS"]="CREDITS",["Menu language:"]="Menyspråk:",["FPS Limit"]="FPS-grense",["Chat History"]="Chat-historikk",["History:"]="Historikk:",["History size:"]="Historikkstørrelse:",["CHAT UTILITY"]="CHAT-VERKTØY",["Always Show Chat"]="Vis alltid chat",["Read Ghost Chat"]="Les spøkelseschat",["Extended Chat"]="Utvidet chat",["Fast Chat"]="Rask chat",["Unlock Extra Characters"]="Tillat ekstra tegn",["Spell Check"]="Stavekontroll",["Clipboard"]="Utklippstavle",["Save Chat Log"]="Lagre chatlogg",["Dark Chat Theme"]="Mørkt chattema",["Enable /color"]="Aktiver /color",["Block Fortegreen"]="Blokker Fortegreen",["Allow Duplicate Colors"]="Tillat like farger",["Auto Ghost After Start"]="Auto-spøkelse etter start",["FAVORITE OUTFITS"]="FAVORITTANTREKK",["Slot"]="Slot",["Empty"]="Tom",["Apply"]="Bruk",["Save Mine"]="Lagre min",["Save Selected"]="Lagre valgt",["Saved slot"]="Slot lagret",["Applied slot"]="Slot brukt",["Cleared slot"]="Slot tømt",["Auto-Ban Platform Spoof (Host)"]="Auto-ban platform spoof",["Ban Custom Platforms From TXT"]="Ban plattformer fra TXT",["RPC Anti-Cheat"]="RPC anti-cheat",["RPC limit:"]="RPC-grense:",["RPC Local Drop"]="RPC lokal drop",["RPC Host Ban"]="RPC host-ban" },
            ["uk"] = new Dictionary<string, string> { ["GENERAL"]="ЗАГАЛЬНЕ",["SELF"]="ГРАВЕЦЬ",["VISUALS"]="ВІЗУАЛ",["PLAYERS"]="ГРАВЦІ",["SABOTAGES"]="САБОТАЖІ",["HOST ONLY"]="ХОСТ",["OUTFITS"]="ОДЯГ",["VOTEKICK"]="КІК",["MENU"]="МЕНЮ",["MAPS"]="КАРТИ",["ANIMATIONS"]="АНІМАЦІЇ",["INFORMATION"]="ІНФОРМАЦІЯ",["KEYBINDS"]="БІНДИ",["WELCOME"]="ВІТАННЯ",["CREDITS"]="АВТОРИ",["Menu language:"]="Мова меню:",["FPS Limit"]="Ліміт FPS",["Chat History"]="Історія чату",["History:"]="Історія:",["History size:"]="Розмір історії:",["CHAT UTILITY"]="УТИЛІТИ ЧАТУ",["Always Show Chat"]="Завжди показувати чат",["Read Ghost Chat"]="Читати чат привидів",["Extended Chat"]="Розширений чат",["Fast Chat"]="Швидкий чат",["Unlock Extra Characters"]="Дозволити всі символи",["Spell Check"]="Перевірка орфографії",["Clipboard"]="Буфер обміну",["Save Chat Log"]="Зберігати лог чату",["Dark Chat Theme"]="Темна тема чату",["Enable /color"]="Увімкнути /color",["Block Fortegreen"]="Блок Fortegreen",["Allow Duplicate Colors"]="Дозволити однакові кольори",["Auto Ghost After Start"]="Авто-привид після старту",["FAVORITE OUTFITS"]="УЛЮБЛЕНІ ОБРАЗИ",["Slot"]="Слот",["Empty"]="Пусто",["Apply"]="Надіти",["Save Mine"]="Зберегти мій",["Save Selected"]="Зберегти вибраний",["Saved slot"]="Слот збережено",["Applied slot"]="Слот застосовано",["Cleared slot"]="Слот очищено",["Auto-Ban Platform Spoof (Host)"]="Авто-бан Platform Spoof",["Ban Custom Platforms From TXT"]="Бан платформ з TXT",["RPC Anti-Cheat"]="RPC античит",["RPC limit:"]="RPC ліміт:",["RPC Local Drop"]="RPC локальний дроп",["RPC Host Ban"]="RPC бан хоста" },
            ["el"] = new Dictionary<string, string> { ["GENERAL"]="ΓΕΝΙΚΑ",["SELF"]="ΠΑΙΚΤΗΣ",["VISUALS"]="ΟΠΤΙΚΑ",["PLAYERS"]="ΠΑΙΚΤΕΣ",["SABOTAGES"]="ΣΑΜΠΟΤΑΖ",["HOST ONLY"]="HOST",["OUTFITS"]="ΣΤΟΛΕΣ",["VOTEKICK"]="VOTEKICK",["MENU"]="ΜΕΝΟΥ",["MAPS"]="ΧΑΡΤΕΣ",["ANIMATIONS"]="ANIMATIONS",["INFORMATION"]="ΠΛΗΡΟΦΟΡΙΕΣ",["KEYBINDS"]="ΠΛΗΚΤΡΑ",["WELCOME"]="ΚΑΛΩΣ ΗΡΘΕΣ",["CREDITS"]="CREDITS",["Menu language:"]="Γλώσσα μενού:",["FPS Limit"]="Όριο FPS",["Chat History"]="Ιστορικό chat",["History:"]="Ιστορικό:",["History size:"]="Μέγεθος ιστορικού:",["CHAT UTILITY"]="ΕΡΓΑΛΕΙΑ CHAT",["Always Show Chat"]="Πάντα εμφάνιση chat",["Read Ghost Chat"]="Ανάγνωση ghost chat",["Extended Chat"]="Εκτεταμένο chat",["Fast Chat"]="Γρήγορο chat",["Unlock Extra Characters"]="Επιπλέον χαρακτήρες",["Spell Check"]="Ορθογραφία",["Clipboard"]="Πρόχειρο",["Save Chat Log"]="Αποθήκευση chat log",["Dark Chat Theme"]="Σκούρο chat",["Enable /color"]="Ενεργοποίηση /color",["Block Fortegreen"]="Block Fortegreen",["Allow Duplicate Colors"]="Να επιτρέπονται ίδια χρώματα",["Auto Ghost After Start"]="Auto ghost μετά την έναρξη",["FAVORITE OUTFITS"]="ΑΓΑΠΗΜΕΝΕΣ ΣΤΟΛΕΣ",["Slot"]="Θέση",["Empty"]="Άδειο",["Apply"]="Εφαρμογή",["Save Mine"]="Αποθ. δικό μου",["Save Selected"]="Αποθ. επιλογής",["Saved slot"]="Θέση αποθηκεύτηκε",["Applied slot"]="Θέση εφαρμόστηκε",["Cleared slot"]="Θέση καθαρίστηκε",["Auto-Ban Platform Spoof (Host)"]="Auto-ban platform spoof",["Ban Custom Platforms From TXT"]="Ban platforms από TXT",["RPC Anti-Cheat"]="RPC anti-cheat",["RPC limit:"]="Όριο RPC:",["RPC Local Drop"]="RPC local drop",["RPC Host Ban"]="RPC host ban" },
            ["zh"] = new Dictionary<string, string> { ["GENERAL"]="常规",["SELF"]="玩家",["VISUALS"]="视觉",["PLAYERS"]="玩家",["SABOTAGES"]="破坏",["HOST ONLY"]="房主",["OUTFITS"]="装扮",["VOTEKICK"]="投票踢人",["MENU"]="菜单",["MAPS"]="地图",["ANIMATIONS"]="动画",["INFORMATION"]="信息",["KEYBINDS"]="按键",["WELCOME"]="欢迎",["CREDITS"]="鸣谢",["Menu language:"]="菜单语言:",["FPS Limit"]="FPS 限制",["Chat History"]="聊天历史",["History:"]="历史:",["History size:"]="历史大小:",["CHAT UTILITY"]="聊天工具",["Always Show Chat"]="始终显示聊天",["Read Ghost Chat"]="读取幽灵聊天",["Extended Chat"]="扩展聊天",["Fast Chat"]="快速聊天",["Unlock Extra Characters"]="允许额外字符",["Spell Check"]="拼写检查",["Clipboard"]="剪贴板",["Save Chat Log"]="保存聊天日志",["Dark Chat Theme"]="深色���天主题",["Enable /color"]="启用 /color",["Block Fortegreen"]="阻止 Fortegreen",["Allow Duplicate Colors"]="允许重复颜色",["Auto Ghost After Start"]="开始后自动幽灵",["FAVORITE OUTFITS"]="收藏装扮",["Slot"]="槽位",["Empty"]="空",["Apply"]="应用",["Save Mine"]="保存自己",["Save Selected"]="保存选中",["Saved slot"]="槽位已保存",["Applied slot"]="槽位已应用",["Cleared slot"]="槽位已清空",["Auto-Ban Platform Spoof (Host)"]="自动封禁平台伪装",["Ban Custom Platforms From TXT"]="从 TXT 封禁自定义平台",["RPC Anti-Cheat"]="RPC 反作弊",["RPC limit:"]="RPC 限制:",["RPC Local Drop"]="RPC 本地丢弃",["RPC Host Ban"]="RPC 房主封禁" },
            ["ja"] = new Dictionary<string, string> { ["GENERAL"]="一般",["SELF"]="プレイヤー",["VISUALS"]="表示",["PLAYERS"]="プレイヤー",["SABOTAGES"]="サボタージュ",["HOST ONLY"]="ホスト",["OUTFITS"]="衣装",["VOTEKICK"]="投票キック",["MENU"]="メニュー",["MAPS"]="マップ",["ANIMATIONS"]="アニメーション",["INFORMATION"]="情報",["KEYBINDS"]="キー設定",["WELCOME"]="ようこそ",["CREDITS"]="クレジット",["Menu language:"]="メニュー言語:",["FPS Limit"]="FPS制限",["Chat History"]="チャット履歴",["History:"]="履歴:",["History size:"]="履歴サイズ:",["CHAT UTILITY"]="チャット機能",["Always Show Chat"]="常にチャット表示",["Read Ghost Chat"]="ゴーストチャット読む",["Extended Chat"]="拡張チャット",["Fast Chat"]="高速チャット",["Unlock Extra Characters"]="追加文字を許可",["Spell Check"]="スペルチェック",["Clipboard"]="クリップボード",["Save Chat Log"]="チャットログ保存",["Dark Chat Theme"]="ダークチャット",["Enable /color"]="/color 有効",["Block Fortegreen"]="Fortegreen ブロック",["Allow Duplicate Colors"]="同じ色を許可",["Auto Ghost After Start"]="開始後に自動ゴースト",["FAVORITE OUTFITS"]="お気に入り衣装",["Slot"]="スロット",["Empty"]="空",["Apply"]="適用",["Save Mine"]="自分を保存",["Save Selected"]="選択を保存",["Saved slot"]="スロット保存",["Applied slot"]="スロット適用",["Cleared slot"]="スロット消去",["Auto-Ban Platform Spoof (Host)"]="平台偽装を自動BAN",["Ban Custom Platforms From TXT"]="TXTから平台BAN",["RPC Anti-Cheat"]="RPCアンチチート",["RPC limit:"]="RPC制限:",["RPC Local Drop"]="RPCローカル破棄",["RPC Host Ban"]="RPCホストBAN" },
            ["ko"] = new Dictionary<string, string> { ["GENERAL"]="일반",["SELF"]="플레이어",["VISUALS"]="비주얼",["PLAYERS"]="플레이어",["SABOTAGES"]="사보타주",["HOST ONLY"]="호스트",["OUTFITS"]="의상",["VOTEKICK"]="투표킥",["MENU"]="메뉴",["MAPS"]="맵",["ANIMATIONS"]="애니메이션",["INFORMATION"]="정보",["KEYBINDS"]="키 설정",["WELCOME"]="환영",["CREDITS"]="크레딧",["Menu language:"]="메뉴 언어:",["FPS Limit"]="FPS 제한",["Chat History"]="채팅 기록",["History:"]="기록:",["History size:"]="기록 크기:",["CHAT UTILITY"]="채팅 도구",["Always Show Chat"]="항상 채팅 표시",["Read Ghost Chat"]="유령 채팅 읽기",["Extended Chat"]="확장 채팅",["Fast Chat"]="빠른 채팅",["Unlock Extra Characters"]="추가 문자 허용",["Spell Check"]="맞춤법 검사",["Clipboard"]="클립보드",["Save Chat Log"]="채팅 로그 저장",["Dark Chat Theme"]="어두운 채팅 테마",["Enable /color"]="/color 활성화",["Block Fortegreen"]="Fortegreen 차단",["Allow Duplicate Colors"]="중복 색상 허용",["Auto Ghost After Start"]="시작 후 자동 유령",["FAVORITE OUTFITS"]="즐겨찾기 의상",["Slot"]="슬롯",["Empty"]="비어 있음",["Apply"]="적용",["Save Mine"]="내 것 저장",["Save Selected"]="선택 저장",["Saved slot"]="슬롯 저장됨",["Applied slot"]="슬롯 적용됨",["Cleared slot"]="슬롯 삭제됨",["Auto-Ban Platform Spoof (Host)"]="플랫폼 위장 자동 밴",["Ban Custom Platforms From TXT"]="TXT 커스텀 플랫폼 밴",["RPC Anti-Cheat"]="RPC 안티치트",["RPC limit:"]="RPC 제한:",["RPC Local Drop"]="RPC 로컬 드롭",["RPC Host Ban"]="RPC 호스트 밴" }
        };

        private static readonly string[] menuTranslationFixKeys =
        {
            "ANTI CHEAT", "AUTO HOST", "LOBBY CONTROLS", "ROLE MANAGER", "PUNISHMENT SYSTEM", "Mode:",
            "RPC PROTECTIONS", "Block Spoof RPC", "Block Sabotage & Meetings", "Block Game RPC in Lobby",
            "Auto-Ban Platform Spoof (Host)", "Ban Custom Platforms From TXT", "Block Meeting RPC Flood",
            "Block Chat RPC Flood", "OTHER PROTECTIONS", "Disable Vote Kicks (Host)", "Auto-Kick Fortegreen",
            "Auto-Ban Broken FriendCode (Host)", "BAN LIST", "Auto-Ban Blacklisted Players", "Enter Friend Code",
            "ADD", "Ban list is empty."
        };

        private static readonly Dictionary<string, string[]> menuTranslationFixes = new Dictionary<string, string[]>
        {
            ["de"] = new[] { "ANTI-CHEAT", "AUTO-HOST", "LOBBY-STEUERUNG", "ROLLENMANAGER", "STRAFSYSTEM", "Modus:", "RPC-SCHUTZ", "Spoof-RPC blockieren", "Sabotage & Meetings blockieren", "Spiel-RPC in Lobby blockieren", "Plattform-Spoof auto-bannen (Host)", "Custom-Plattformen aus TXT bannen", "Meeting-RPC-Flood blockieren", "Chat-RPC-Flood blockieren", "WEITERER SCHUTZ", "Vote-Kicks deaktivieren (Host)", "Fortegreen automatisch kicken", "Defekten FriendCode auto-bannen (Host)", "BAN-LISTE", "Spieler aus Ban-Liste auto-bannen", "Friend Code eingeben", "HINZUFÜGEN", "Ban-Liste ist leer." },
            ["fr"] = new[] { "ANTI-TRICHE", "HÔTE AUTO", "CONTRÔLES LOBBY", "GESTION DES RÔLES", "SYSTÈME DE SANCTIONS", "Mode :", "PROTECTIONS RPC", "Bloquer RPC spoof", "Bloquer sabotages et meetings", "Bloquer RPC de jeu en lobby", "Auto-ban spoof plateforme (Hôte)", "Ban plateformes custom TXT", "Bloquer flood RPC meeting", "Bloquer flood RPC chat", "AUTRES PROTECTIONS", "Désactiver vote-kicks (Hôte)", "Auto-kick Fortegreen", "Auto-ban FriendCode cassé (Hôte)", "LISTE DE BAN", "Auto-ban joueurs listés", "Entrer Friend Code", "AJOUTER", "La liste de ban est vide." },
            ["es"] = new[] { "ANTI-CHEAT", "HOST AUTO", "CONTROLES DE LOBBY", "GESTOR DE ROLES", "SISTEMA DE SANCIONES", "Modo:", "PROTECCIONES RPC", "Bloquear RPC spoof", "Bloquear sabotajes y reuniones", "Bloquear RPC de juego en lobby", "Auto-ban spoof de plataforma (Host)", "Ban de plataformas custom desde TXT", "Bloquear flood RPC de reunión", "Bloquear flood RPC de chat", "OTRAS PROTECCIONES", "Desactivar vote-kicks (Host)", "Auto-kick Fortegreen", "Auto-ban FriendCode roto (Host)", "LISTA DE BAN", "Auto-ban jugadores en lista", "Introducir Friend Code", "AÑADIR", "La lista de ban está vacía." },
            ["it"] = new[] { "ANTI-CHEAT", "HOST AUTO", "CONTROLLI LOBBY", "GESTORE RUOLI", "SISTEMA PUNIZIONI", "Modalità:", "PROTEZIONI RPC", "Blocca RPC spoof", "Blocca sabotaggi e meeting", "Blocca RPC di gioco in lobby", "Auto-ban spoof piattaforma (Host)", "Ban piattaforme custom da TXT", "Blocca flood RPC meeting", "Blocca flood RPC chat", "ALTRE PROTEZIONI", "Disattiva vote-kick (Host)", "Auto-kick Fortegreen", "Auto-ban FriendCode rotto (Host)", "LISTA BAN", "Auto-ban giocatori in lista", "Inserisci Friend Code", "AGGIUNGI", "La lista ban è vuota." },
            ["pt"] = new[] { "ANTI-CHEAT", "HOST AUTO", "CONTROLES DO LOBBY", "GERENCIADOR DE FUNÇÕES", "SISTEMA DE PUNIÇÕES", "Modo:", "PROTEÇÕES RPC", "Bloquear RPC spoof", "Bloquear sabotagens e reuniões", "Bloquear RPC de jogo no lobby", "Auto-ban spoof de plataforma (Host)", "Ban plataformas custom do TXT", "Bloquear flood RPC de reunião", "Bloquear flood RPC de chat", "OUTRAS PROTEÇÕES", "Desativar vote-kicks (Host)", "Auto-kick Fortegreen", "Auto-ban FriendCode quebrado (Host)", "LISTA DE BAN", "Auto-ban jogadores listados", "Inserir Friend Code", "ADICIONAR", "A lista de ban está vazia." },
            ["pl"] = new[] { "ANTI-CHEAT", "AUTO HOST", "KONTROLA LOBBY", "MENEDŻER RÓL", "SYSTEM KAR", "Tryb:", "OCHRONA RPC", "Blokuj spoof RPC", "Blokuj sabotaże i spotkania", "Blokuj RPC gry w lobby", "Auto-ban spoof platformy (Host)", "Ban platform custom z TXT", "Blokuj flood RPC spotkania", "Blokuj flood RPC czatu", "INNA OCHRONA", "Wyłącz vote-kicki (Host)", "Auto-kick Fortegreen", "Auto-ban uszkodzony FriendCode (Host)", "LISTA BANÓW", "Auto-ban graczy z listy", "Wpisz Friend Code", "DODAJ", "Lista banów jest pusta." },
            ["nl"] = new[] { "ANTI-CHEAT", "AUTO-HOST", "LOBBYBEDIENING", "ROLLENBEHEER", "STRAFSYSTEEM", "Modus:", "RPC-BESCHERMING", "Spoof-RPC blokkeren", "Sabotage & meetings blokkeren", "Game-RPC in lobby blokkeren", "Platform-spoof auto-bannen (Host)", "Custom platforms uit TXT bannen", "Meeting-RPC-flood blokkeren", "Chat-RPC-flood blokkeren", "ANDERE BESCHERMING", "Vote-kicks uitschakelen (Host)", "Fortegreen automatisch kicken", "Kapotte FriendCode auto-bannen (Host)", "BANLIJST", "Spelers op banlijst auto-bannen", "Friend Code invoeren", "TOEVOEGEN", "Banlijst is leeg." },
            ["tr"] = new[] { "ANTI-CHEAT", "OTO HOST", "LOBI KONTROLLERİ", "ROL YÖNETİCİSİ", "CEZA SİSTEMİ", "Mod:", "RPC KORUMALARI", "Spoof RPC engelle", "Sabotaj ve toplantıları engelle", "Lobide oyun RPC engelle", "Platform spoof oto-ban (Host)", "TXT özel platform ban", "Toplantı RPC flood engelle", "Sohbet RPC flood engelle", "DİĞER KORUMALAR", "Vote-kick kapat (Host)", "Fortegreen oto-kick", "Bozuk FriendCode oto-ban (Host)", "BAN LİSTESİ", "Listedeki oyuncuları oto-ban", "Friend Code gir", "EKLE", "Ban listesi boş." },
            ["cs"] = new[] { "ANTI-CHEAT", "AUTO HOST", "OVLÁDÁNÍ LOBBY", "SPRÁVCE ROLÍ", "SYSTÉM TRESTŮ", "Režim:", "OCHRANA RPC", "Blokovat spoof RPC", "Blokovat sabotáže a meetingy", "Blokovat herní RPC v lobby", "Auto-ban spoof platformy (Host)", "Ban custom platforem z TXT", "Blokovat meeting RPC flood", "Blokovat chat RPC flood", "DALŠÍ OCHRANA", "Vypnout vote-kicky (Host)", "Auto-kick Fortegreen", "Auto-ban rozbitý FriendCode (Host)", "BAN LIST", "Auto-ban hráčů z listu", "Zadej Friend Code", "PŘIDAT", "Ban list je prázdný." },
            ["ro"] = new[] { "ANTI-CHEAT", "HOST AUTO", "CONTROALE LOBBY", "MANAGER ROLURI", "SISTEM DE PEDEPSE", "Mod:", "PROTECȚII RPC", "Blochează RPC spoof", "Blochează sabotaje și meetinguri", "Blochează RPC de joc în lobby", "Auto-ban spoof platformă (Host)", "Ban platforme custom din TXT", "Blochează flood RPC meeting", "Blochează flood RPC chat", "ALTE PROTECȚII", "Dezactivează vote-kick (Host)", "Auto-kick Fortegreen", "Auto-ban FriendCode stricat (Host)", "LISTĂ BAN", "Auto-ban jucători listați", "Introdu Friend Code", "ADAUGĂ", "Lista de ban este goală." },
            ["hu"] = new[] { "ANTI-CHEAT", "AUTO HOST", "LOBBI VEZÉRLÉS", "SZEREPKEZELŐ", "BÜNTETÉSI RENDSZER", "Mód:", "RPC VÉDELEM", "Spoof RPC blokkolása", "Szabotázsok és meetingek blokkolása", "Játék RPC blokkolása lobbyban", "Platform spoof auto-ban (Host)", "Custom platformok bannolása TXT-ből", "Meeting RPC flood blokkolása", "Chat RPC flood blokkolása", "EGYÉB VÉDELEM", "Vote-kick tiltása (Host)", "Fortegreen auto-kick", "Hibás FriendCode auto-ban (Host)", "BAN LISTA", "Listás játékosok auto-banja", "Friend Code megadása", "HOZZÁAD", "A ban lista üres." },
            ["sv"] = new[] { "ANTI-CHEAT", "AUTO HOST", "LOBBYKONTROLLER", "ROLLHANTERARE", "STRAFFSYSTEM", "Läge:", "RPC-SKYDD", "Blockera spoof-RPC", "Blockera sabotage och möten", "Blockera spel-RPC i lobby", "Auto-ban plattformsspoof (Host)", "Ban custom-plattformar från TXT", "Blockera meeting RPC-flood", "Blockera chat RPC-flood", "ANNAT SKYDD", "Inaktivera vote-kicks (Host)", "Auto-kick Fortegreen", "Auto-ban trasig FriendCode (Host)", "BANLISTA", "Auto-ban spelare på lista", "Ange Friend Code", "LÄGG TILL", "Banlistan är tom." },
            ["da"] = new[] { "ANTI-CHEAT", "AUTO HOST", "LOBBYKONTROL", "ROLLEMANAGER", "STRAFSYSTEM", "Tilstand:", "RPC-BESKYTTELSE", "Bloker spoof-RPC", "Bloker sabotager og møder", "Bloker spil-RPC i lobby", "Auto-ban platform spoof (Host)", "Ban custom-platforme fra TXT", "Bloker meeting RPC-flood", "Bloker chat RPC-flood", "ANDEN BESKYTTELSE", "Deaktiver vote-kicks (Host)", "Auto-kick Fortegreen", "Auto-ban defekt FriendCode (Host)", "BANLISTE", "Auto-ban spillere på liste", "Indtast Friend Code", "TILFØJ", "Banlisten er tom." },
            ["fi"] = new[] { "ANTI-CHEAT", "AUTO HOST", "LOBBYN HALLINTA", "ROOLIEN HALLINTA", "RANGAISTUSJÄRJESTELMÄ", "Tila:", "RPC-SUOJAUKSET", "Estä spoof RPC", "Estä sabotaasit ja kokoukset", "Estä peli-RPC lobbyssa", "Auto-ban platform spoof (Host)", "Ban custom-alustat TXT:stä", "Estä meeting RPC flood", "Estä chat RPC flood", "MUUT SUOJAUKSET", "Poista vote-kickit käytöstä (Host)", "Auto-kick Fortegreen", "Auto-ban rikkinäinen FriendCode (Host)", "BAN-LISTA", "Auto-ban listatut pelaajat", "Syötä Friend Code", "LISÄÄ", "Ban-lista on tyhjä." },
            ["no"] = new[] { "ANTI-CHEAT", "AUTO HOST", "LOBBYKONTROLLER", "ROLLEBEHANDLER", "STRAFFESYSTEM", "Modus:", "RPC-BESKYTTELSE", "Blokker spoof-RPC", "Blokker sabotasje og møter", "Blokker spill-RPC i lobby", "Auto-ban platform spoof (Host)", "Ban custom-plattformer fra TXT", "Blokker meeting RPC-flood", "Blokker chat RPC-flood", "ANNEN BESKYTTELSE", "Deaktiver vote-kicks (Host)", "Auto-kick Fortegreen", "Auto-ban ødelagt FriendCode (Host)", "BANLISTE", "Auto-ban spillere på liste", "Skriv Friend Code", "LEGG TIL", "Banlisten er tom." },
            ["uk"] = new[] { "АНТИЧИТ", "АВТО ХОСТ", "КЕРУВАННЯ ЛОБІ", "МЕНЕДЖЕР РОЛЕЙ", "СИСТЕМА ПОКАРАНЬ", "Режим:", "ЗАХИСТ RPC", "Блокувати spoof RPC", "Блокувати саботажі та зустрічі", "Блокувати ігрові RPC у лобі", "Авто-бан spoof платформи (Хост)", "Бан кастомних платформ з TXT", "Блокувати flood RPC зустрічі", "Блокувати flood RPC чату", "ІНШИЙ ЗАХИСТ", "Вимкнути vote-kick (Хост)", "Авто-кік Fortegreen", "Авто-бан зламаного FriendCode (Хост)", "БАН-ЛИСТ", "Авто-бан гравців зі списку", "Введіть Friend Code", "ДОДАТИ", "Бан-лист порожній." },
            ["el"] = new[] { "ANTI-CHEAT", "AUTO HOST", "ΕΛΕΓΧΟΙ LOBBY", "ΔΙΑΧΕΙΡΙΣΗ ΡΟΛΩΝ", "ΣΥΣΤΗΜΑ ΠΟΙΝΩΝ", "Λειτουργία:", "ΠΡΟΣΤΑΣΙΕΣ RPC", "Μπλοκ spoof RPC", "Μπλοκ σαμποτάζ και meetings", "Μπλοκ game RPC στο lobby", "Auto-ban platform spoof (Host)", "Ban custom platforms από TXT", "Μπλοκ meeting RPC flood", "Μπλοκ chat RPC flood", "ΑΛΛΕΣ ΠΡΟΣΤΑΣΙΕΣ", "Απενεργοποίηση vote-kicks (Host)", "Auto-kick Fortegreen", "Auto-ban χαλασμένο FriendCode (Host)", "ΛΙΣΤΑ BAN", "Auto-ban παικτών στη λίστα", "Εισαγωγή Friend Code", "ΠΡΟΣΘΗΚΗ", "Η λίστα ban είναι άδεια." },
            ["zh"] = new[] { "反作弊", "自动房主", "大厅控制", "身份管理", "处罚系统", "模式:", "RPC 防护", "阻止 Spoof RPC", "阻止破坏和会议", "阻止大厅内游戏 RPC", "自动封禁平台伪装 (房主)", "从 TXT 封禁自定义平台", "阻止会议 RPC 洪泛", "阻止聊天 RPC 洪泛", "其他防护", "禁用投票踢人 (房主)", "自动踢出 Fortegreen", "自动封禁损坏 FriendCode (房主)", "封禁列表", "自动封禁列表玩家", "输入 Friend Code", "添加", "封禁列表为空。" },
            ["ja"] = new[] { "アンチチート", "自動ホスト", "ロビー制御", "����ル管理", "処罰システム", "モード:", "RPC保護", "Spoof RPCをブロック", "サボタージュと会議をブロック", "ロビー中のゲームRPCをブロック", "プラットフォーム偽装を自動BAN (ホスト)", "TXTのカスタムプラットフォームをBAN", "会議RPCフラッドをブロック", "チャットRPCフラッドをブロック", "その他の保護", "投票キックを無効化 (ホスト)", "Fortegreenを自動キック", "壊れたFriendCodeを自動BAN (ホスト)", "BANリスト", "BANリストのプレイヤーを自動BAN", "Friend Codeを入力", "追加", "BANリストは空です。" },
            ["ko"] = new[] { "안티치트", "자동 호스트", "로비 컨트롤", "역할 관리자", "처벌 시스템", "모드:", "RPC 보호", "Spoof RPC 차단", "사보타주와 회의 차단", "로비에서 게임 RPC 차단", "플랫폼 위장 자동 밴 (호스트)", "TXT 커스텀 플랫폼 밴", "회의 RPC 플러드 차단", "채팅 RPC 플러드 차단", "기타 보호", "투표 킥 비활성화 (호스트)", "Fortegreen 자동 킥", "손상된 FriendCode 자동 밴 (호스트)", "밴 목록", "목록의 플레이어 자동 밴", "Friend Code 입력", "추가", "밴 목록이 비어 있습니다." }
        };

        //silly thing
        public static float resetingDataLimit;

        public static byte selectedMorphTargetId = 255;
        public static bool unlockCosmetics = true;
        public static bool moreLobbyInfo = true;

        public static Dictionary<string, KeyCode> keyBinds = new Dictionary<string, KeyCode>();
        public static string bindingAction = "";

        public static string L(string eng, string rus)
        {
            try
            {
                string configuredLanguage = CurrentMenuLanguageCode();
                if (configuredLanguage == "ru" || configuredLanguage == "uk")
                    return TryTranslateMenuText(configuredLanguage, eng, configuredLanguage == "ru" ? rus : null);
                if (configuredLanguage != "auto")
                    return TryTranslateMenuText(configuredLanguage, eng, eng);

                if (DestroyableSingleton<TranslationController>.InstanceExists)
                {
                    string currentLang = DestroyableSingleton<TranslationController>.Instance.currentLanguage.ToString().ToLower();
                    if (currentLang.Contains("russian") || currentLang.Contains("ru"))
                        return rus;
                }
            }
            catch { }
            return eng;
        }

        private static string TryTranslateMenuText(string languageCode, string englishText, string fallback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(languageCode) || string.IsNullOrEmpty(englishText))
                    return fallback ?? englishText;

                if (languageCode == "en")
                    return englishText;

                if (menuTranslationFixes.TryGetValue(languageCode, out string[] fixedTranslations))
                {
                    int fixedIndex = Array.IndexOf(menuTranslationFixKeys, englishText);
                    if (fixedIndex >= 0 && fixedIndex < fixedTranslations.Length && !string.IsNullOrWhiteSpace(fixedTranslations[fixedIndex]))
                        return fixedTranslations[fixedIndex];
                }

                if (menuTranslations.TryGetValue(languageCode, out Dictionary<string, string> translations) &&
                    translations.TryGetValue(englishText, out string translated) &&
                    !string.IsNullOrWhiteSpace(translated))
                    return translated;
            }
            catch { }

            return fallback ?? englishText;
        }

        public static string CurrentMenuLanguageCode()
        {
            try
            {
                int index = Mathf.Clamp(currentMenuLanguageIndex, 0, menuLanguageCodes.Length - 1);
                return menuLanguageCodes[index];
            }
            catch { }

            return "auto";
        }

        private int currentGeneralSubTab = 0;
        private int currentGeneralInfoSubTab = 0;
        private string[] generalSubTabs => new string[] { L("INFORMATION", "ИНФОРМАЦИЯ"), L("KEYBINDS", "БИНДЫ") };
        private string[] generalInfoSubTabs => new string[] { L("WELCOME", "WELCOME"), L("CREDITS", "АВТОРЫ") };

        public static KeyCode menuToggleKey = KeyCode.Insert;
        public static KeyCode bindMassMorph = KeyCode.None;
        public static KeyCode bindSpawnLobby = KeyCode.None;
        public static KeyCode bindDespawnLobby = KeyCode.None;
        public static KeyCode bindCloseMeeting = KeyCode.None;
        public static KeyCode bindInstaStart = KeyCode.None;
        public static KeyCode bindEndCrew = KeyCode.None;
        public static KeyCode bindEndImp = KeyCode.None;
        public static KeyCode bindEndImpDC = KeyCode.None;
        public static KeyCode bindEndHnsDC = KeyCode.None;
        public static KeyCode bindToggleTracers = KeyCode.None;
        public static KeyCode bindToggleNoClip = KeyCode.None;
        public static KeyCode bindToggleFreecam = KeyCode.None;
        public static KeyCode bindToggleCameraZoom = KeyCode.None;
        public static KeyCode bindKillAll = KeyCode.None;
        public static KeyCode bindCallMeeting = KeyCode.None;
        public static KeyCode bindTogglePlayerInfo = KeyCode.None;
        public static KeyCode bindToggleSeeRoles = KeyCode.None;
        public static KeyCode bindToggleSeeGhosts = KeyCode.None;
        public static KeyCode bindToggleFullBright = KeyCode.None;
        public static KeyCode bindKickAll = KeyCode.None;
        public static KeyCode bindFixSabotages = KeyCode.None;
        public static KeyCode bindSetAllGhost = KeyCode.None;
        public static KeyCode bindSetAllGhostImp = KeyCode.None;

        public static readonly HashSet<byte> VanillaRpcIds = new HashSet<byte>
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21,
            22, 23, 24, 25, 26, 27, 29, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42,
            43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 60, 61, 62, 63, 64, 65
        };

        private bool isScannerActiveFlag = false;
        private bool isCamsActiveFlag = false;
        public static bool isWaitingForBind = false;
        public static bool isWaitBindMassMorph = false;
        public static bool isWaitBindSpawnLobby = false;
        public static bool isWaitBindDespawnLobby = false;
        public static bool isWaitBindCloseMeeting = false;
        public static bool isWaitBindInstaStart = false;
        public static bool isWaitBindEndCrew = false;
        public static bool isWaitBindEndImp = false;
        public static bool isWaitBindEndImpDC = false;
        public static bool isWaitBindEndHnsDC = false;
        public static bool isWaitBindToggleTracers = false;
        public static bool isWaitBindToggleNoClip = false;
        public static bool isWaitBindToggleFreecam = false;
        public static bool isWaitBindToggleCameraZoom = false;
        public static bool isWaitBindKillAll = false;
        public static bool isWaitBindCallMeeting = false;
        public static bool isWaitBindTogglePlayerInfo = false;
        public static bool isWaitBindToggleSeeRoles = false;
        public static bool isWaitBindToggleSeeGhosts = false;
        public static bool isWaitBindToggleFullBright = false;
        public static bool isWaitBindKickAll = false;
        public static bool isWaitBindFixSabotages = false;
        public static bool isWaitBindSetAllGhost = false;
        public static bool isWaitBindSetAllGhostImp = false;
        public static bool SpoofMenuEnabled = false;
        public static int selectedSpoofMenuIndex = 0;
        private float uiSpoofTimer = 0f;
        public static bool noClip = false;
        public static bool tpToCursor = false;
        public static bool dragToCursor = false;
        public static float walkSpeed = 1f;

        public static bool DetailedJoinInfo = true;
        private static List<byte> lastPlayerIds = new List<byte>();
        private static Dictionary<byte, float> pendingJoinTimers = new Dictionary<byte, float>();
        private static Dictionary<byte, string> playerHistoryKeysById = new Dictionary<byte, string>();
        public class PlayerHistoryEntry
        {
            public string Name;
            public string FriendCode;
            public string Puid;
            public string Platform;
            public string CustomPlatform;
            public int Level;
            public DateTime FirstSeenUtc;
            public DateTime LastSeenUtc;
            public DateTime? LeftUtc;
            public bool IsOnline;
            public List<byte> RpcCalls = new List<byte>();
        }
        private static List<PlayerHistoryEntry> playerHistoryEntries = new List<PlayerHistoryEntry>();
        private Vector2 playersHistoryScroll = Vector2.zero;
        private int currentPlayersSubTab = 0;
        private string[] playersSubTabs = { "ACTIONS", "HISTORY" };

        public static float engineSpeed = 1f;
        public static bool invertControls = false;
        public static bool autoFollowCursor = false;

        public static int fakeRoleIdx = 0;
        public static RoleTypes[] forceRoleOptions = { RoleTypes.Crewmate, RoleTypes.Impostor, RoleTypes.Engineer, RoleTypes.Scientist, RoleTypes.Shapeshifter, RoleTypes.GuardianAngel };
        public static RoleTypes[] roleAssignOptions = {
            RoleTypes.Crewmate, RoleTypes.Impostor, RoleTypes.Engineer, RoleTypes.Scientist, RoleTypes.Shapeshifter, RoleTypes.GuardianAngel,
            (RoleTypes)8, (RoleTypes)9, (RoleTypes)10, (RoleTypes)12, (RoleTypes)18, RoleTypes.Crewmate, RoleTypes.Impostor
        };
        public static string[] roleAssignNames = {
            "Crewmate", "Impostor", "Engineer", "Scientist", "Shapeshifter", "Guardian Angel",
            "Noisemaker", "Phantom", "Tracker", "Detective", "Viper", "Ghost", "Ghost Imp"
        };
        private int targetRoleAssignIdx = 0;
        private int allPlayersRoleAssignIdx = 0;
        public static bool NoShapeshiftAnim = false;
        public static bool EndlessTracking = false;
        public static bool NoTrackingCooldown = false;
        public static bool UnlimitedInterrogateRange = false;
        public static bool noTaskMode = false;
        public static bool killAuraHostOnly = false;
        public static bool noKillCooldownHostOnly = false;
        public static bool spamReportBodies = false;
        private float killAuraTimer = 0f;

        public static bool enableColorCommand = false;
        public static bool hostChatColor = false;
        public static Color hostChatColorValue = new Color32(0, 128, 128, 255);

        public static bool showMenu = false;
        public static Rect windowRect = new Rect(100, 100, 750, 480);
        public static bool freecam = false;
        private static bool _freecamActive = false;
        public static bool cameraZoom = false;
        public static bool RevealVotesEnabled = false;

        public static Color currentAccentColor = new Color(1f, 0.549f, 0f, 1f);
        public static bool rgbMenuMode = false;
        public static bool rgbMenuText = false;
        private static ElysiumModMenuGUI activeGui;
        private float rgbMenuHue = 0f;
        public static bool enableBackground = false;
        public static bool hardMenu = false;
        public static Texture2D customMenuBg = null;
        private bool wasShowMenu = false;
        private int currentMenuColorIndex = 10;
        private string[] menuColorNames = {
            "Elysium Blue", "Dark Forest", "Green", "Sea Green", "Mint", "Chartreuse",
            "Sun Yellow", "Marigold", "Old Gold",
            "Bright Amber", "Vivid Orange", "Dark Orange",
            "Blood Red",
            "Hot Pink", "Pale Mauve", "Lilac",
            "Lavender", "Deep Indigo", "Indigo",
            "Med Slate Blue", "Slate Blue", "Navy", "Slate Grey",
            "Arctic Cyan", "Neon Lime", "Royal Violet", "Crimson Glow", "Ocean Teal",
            "Sunset Orange", "Rose Quartz", "Electric Blue", "Gold Ember", "Emerald Pulse",
            "Midnight Steel", "Soft Lavender"
        };

        private Color[] menuColors = {
            new Color32(51, 51, 255, 255), new Color(0.192f, 0.290f, 0.196f, 1f), new Color(0f, 0.502f, 0f, 1f), new Color(0.235f, 0.702f, 0.443f, 1f), new Color(0.243f, 0.706f, 0.537f, 1f), new Color(0.498f, 1f, 0f, 1f),
            new Color(0.996f, 0.718f, 0.082f, 1f), new Color(0.812f, 0.651f, 0.004f, 1f),
            new Color(0.996f, 0.612f, 0.063f, 1f), new Color(0.957f, 0.455f, 0.004f, 1f), new Color(1f, 0.549f, 0f, 1f),
            new Color(0.871f, 0.071f, 0.149f, 1f),
            new Color(0.992f, 0.529f, 0.859f, 1f), new Color(0.882f, 0.678f, 0.800f, 1f), new Color(0.784f, 0.635f, 0.784f, 1f),
            new Color(0.925f, 0.686f, 0.996f, 1f), new Color(0.314f, 0.267f, 0.675f, 1f), new Color(0.294f, 0f, 0.51f, 1f),
            new Color(0.482f, 0.408f, 0.933f, 1f), new Color(0.416f, 0.353f, 0.804f, 1f), new Color(0f, 0f, 0.502f, 1f), new Color(0.439f, 0.502f, 0.565f, 1f),
            new Color32(72, 219, 251, 255), new Color32(163, 230, 53, 255), new Color32(124, 58, 237, 255), new Color32(239, 68, 68, 255),
            new Color32(20, 184, 166, 255), new Color32(249, 115, 22, 255), new Color32(244, 114, 182, 255), new Color32(59, 130, 246, 255),
            new Color32(245, 158, 11, 255), new Color32(16, 185, 129, 255), new Color32(51, 65, 85, 255), new Color32(196, 181, 253, 255)
        };

        public static float autoChatEveryoneDelay = 2.5f;
        public static string customChatMessage = "test";
        public static bool customChatSpamEnabled = false;
        public static float customChatSpamDelay = 2.1f;
        public static bool customChatInputFocused = false;
        private float customChatSpamTimer = 0f;

        public static float autoMeetingTimer = 0f;
        private string[] tabNames => new string[] { L("GENERAL", "ОБЩИЕ"), L("SELF", "ИГРОК"), L("VISUALS", "ВИЗУАЛ"), L("PLAYERS", "ИГРОКИ"), L("SABOTAGES", "САБОТАЖИ"), L("HOST ONLY", "ХОСТ"), L("VOTEKICK", "КИК"), L("MENU", "МЕНЮ"), L("ANIMATIONS", "АНИМАЦИИ") };
        public static float speedMultiplier = 1f;
        public static bool noSettingLimit = false;
        public static float globalRoomColorId = 0f;

        private int currentHostOnlySubTab = 0;
        private string[] hostOnlySubTabs => new string[] { L("LOBBY CONTROLS", "КОНТРОЛЬ ЛОББИ"), L("ROLE MANAGER", "МЕНЕДЖЕР РОЛЕЙ"), L("ANTI CHEAT", "АНТИ-ЧИТ"), L("AUTO HOST", "АВТО ХОСТ"), L("MAPS", "КАРТЫ") };
        public static bool UseSnapToRPC = true;
        private static bool isSkeldFlipped = false;
        public static float selectedMapSpawnIdx = 0f;
        public static string[] mapSpawnNames = { "The Skeld", "Mira HQ", "Polus", "The Airship", "The Fungle" };

        public static bool FlippedSkeld
        {
            get { return isSkeldFlipped; }
            set
            {
                if (AmongUsClient.Instance == null || isSkeldFlipped == value) return;
                var temp = AmongUsClient.Instance.ShipPrefabs[3];
                AmongUsClient.Instance.ShipPrefabs[3] = AmongUsClient.Instance.ShipPrefabs[0];
                AmongUsClient.Instance.ShipPrefabs[0] = temp;
                isSkeldFlipped = value;
            }
        }

        [HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.Start))]
        public static class AllowSymbols_TextBoxTMP_Start_Patch
        {
            public static void Postfix(TextBoxTMP __instance)
            {
                __instance.allowAllCharacters = ElysiumModMenuGUI.allowLinksAndSymbols;
                __instance.AllowSymbols = ElysiumModMenuGUI.allowLinksAndSymbols;
                __instance.AllowEmail = ElysiumModMenuGUI.allowLinksAndSymbols;
            }
        }
        [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
        public static class ChatJailbreak_ChatController_Update_Postfix
        {
            public static void Postfix(ChatController __instance)
            {
                if (__instance == null || __instance.freeChatField == null || __instance.freeChatField.textArea == null) return;

                if (ElysiumModMenuGUI.enableFastChat && __instance.timeSinceLastMessage < 0.9f)
                {
                    __instance.timeSinceLastMessage = 0.9f;
                }

                __instance.freeChatField.textArea.allowAllCharacters = ElysiumModMenuGUI.allowLinksAndSymbols;
                __instance.freeChatField.textArea.AllowSymbols = ElysiumModMenuGUI.allowLinksAndSymbols;
                __instance.freeChatField.textArea.AllowEmail = ElysiumModMenuGUI.allowLinksAndSymbols;

                __instance.freeChatField.textArea.characterLimit = ElysiumModMenuGUI.enableExtendedChat ? 120 : 100;
            }
        }
        [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendFreeChat))]
        public static class AllowURLS_ChatController_SendFreeChat_Patch
        {
            public static bool Prefix(ChatController __instance)
            {
                if (!ElysiumModMenuGUI.allowLinksAndSymbols) return true;

                string text = __instance.freeChatField.Text;

                if (!string.IsNullOrWhiteSpace(text))
                {
                    PlayerControl.LocalPlayer.RpcSendChat(text);
                    __instance.freeChatField.textArea.SetText(string.Empty, string.Empty);
                }

                return false;
            }
        }
        public static bool autoKickBugs = false;
        public static float autoKickTimer = 5f;
        public static Dictionary<byte, float> fortegreenTimer = new Dictionary<byte, float>();
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
        public static class AutoKickBugs_Patch
        {
            public static void Postfix(PlayerControl __instance, byte bodyColor)
            {
                if (!ElysiumModMenuGUI.autoKickBugs || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

                try
                {
                    if (__instance != null && __instance != PlayerControl.LocalPlayer && __instance.Data != null && !__instance.Data.Disconnected)
                    {
                        byte pid = __instance.PlayerId;
                        string colorName = Palette.GetColorName((int)bodyColor);

                        if (bodyColor == 18 || colorName == "???" || bodyColor >= Palette.PlayerColors.Length)
                        {
                            if (!ElysiumModMenuGUI.fortegreenTimer.ContainsKey(pid))
                            {
                                ElysiumModMenuGUI.fortegreenTimer[pid] = Time.time + ElysiumModMenuGUI.autoKickTimer;
                            }
                        }
                        else
                        {
                            if (ElysiumModMenuGUI.fortegreenTimer.ContainsKey(pid))
                            {
                                ElysiumModMenuGUI.fortegreenTimer.Remove(pid);
                            }
                        }
                    }
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(VoteBanSystem), nameof(VoteBanSystem.HandleRpc))]
        public static class VoteBanSystemPatch
        {
            public static bool Prefix(VoteBanSystem __instance, byte callId, Hazel.MessageReader reader)
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || !ElysiumModMenuGUI.disableVoteKicks)
                    return true;

                if (callId == 26)
                {
                    try
                    {
                        int targetClientId = reader.ReadInt32();
                        int voterClientId = reader.ReadInt32();
                        string targetName = ResolveVoteClientName(targetClientId);
                        string voterName = ResolveVoteClientName(voterClientId);
                        ElysiumModMenuGUI.ShowNotification($"<color=#FFAC1C>[VOTEKICK BLOCK]</color> {voterName} tried to vote-kick {targetName}");
                    }
                    catch
                    {
                        ElysiumModMenuGUI.ShowNotification("<color=#FFAC1C>[VOTEKICK BLOCK]</color> Vote-kick blocked, sender could not be resolved.");
                    }

                    return false;
                }

                return true;
            }

            private static string ResolveVoteClientName(int clientId)
            {
                try
                {
                    if (PlayerControl.AllPlayerControls != null)
                    {
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc == null || pc.Data == null) continue;
                            if (pc.Data.ClientId == clientId || (int)pc.OwnerId == clientId)
                            {
                                string name = string.IsNullOrWhiteSpace(pc.Data.PlayerName) ? "Unknown" : pc.Data.PlayerName;
                                return $"{name} ({clientId})";
                            }
                        }
                    }
                }
                catch { }

                return $"client {clientId}";
            }
        }
        public static bool disableVoteKicks = false;


        [HarmonyPatch(typeof(ShhhBehaviour), nameof(ShhhBehaviour.PlayAnimation))]
        public static class SkipShhh_Perfect_Patch
        {
            public static bool Prefix(ShhhBehaviour __instance, ref Il2CppSystem.Collections.IEnumerator __result)
            {
                if (!ElysiumModMenuGUI.skipShhhAnim || __instance == null) return true;

                __instance.gameObject.SetActive(false);

                __result = FastSkip().WrapToIl2Cpp();
                return false;
            }

            private static System.Collections.IEnumerator FastSkip() { yield break; }
        }
        private void SpawnMap(int mapId)
        {
            try
            {
                if ((UnityEngine.Object)(object)AmongUsClient.Instance == (UnityEngine.Object)null || AmongUsClient.Instance.ShipPrefabs == null)
                    return;

                int realMapId = mapId;
                if (mapId == 3) realMapId = 4;
                if (mapId == 4) realMapId = 5;

                if (realMapId >= AmongUsClient.Instance.ShipPrefabs.Count)
                    return;

                BepInEx.Unity.IL2CPP.Utils.MonoBehaviourExtensions.StartCoroutine(this, CoSpawnMap(realMapId));
            }
            catch { }
        }

        [HideFromIl2Cpp]
        private System.Collections.IEnumerator CoSpawnMap(int mapId)
        {
            AmongUsClient.Instance.ShipLoadingAsyncHandle = AmongUsClient.Instance.ShipPrefabs[mapId].InstantiateAsync((Transform)null, false);
            yield return AmongUsClient.Instance.ShipLoadingAsyncHandle;

            ShipStatus.Instance = AmongUsClient.Instance.ShipLoadingAsyncHandle.Result.GetComponent<ShipStatus>();
            ((InnerNetClient)AmongUsClient.Instance).Spawn(((Component)ShipStatus.Instance).GetComponent<InnerNetObject>(), -2, (SpawnFlags)0);

        }

        private void DespawnMap()
        {
            try
            {
                if (ShipStatus.Instance != null)
                {
                    ShipStatus.Instance.Despawn();
                }
            }
            catch { }
        }

        private void DespawnCurrentMap()
        {
            DespawnMap();
        }

        [HideFromIl2Cpp]
        private System.Collections.IEnumerator CoSpawnOverlappedMap(int mapId)
        {
            yield return CoSpawnMap(mapId);
        }
        public static Dictionary<string, Vector2> skeldTeleportLocations = new Dictionary<string, Vector2>()
{
    { "Cafeteria", new Vector2(-0.78f, 2.48f) },
    { "Weapons", new Vector2(8.04f, 1.24f) },
    { "Navigation", new Vector2(16.59f, -2.33f) },
    { "O2", new Vector2(5.15f, -3.12f) },
    { "Shields", new Vector2(10.15f, -7.64f) },
    { "Communications", new Vector2(3.87f, -11.08f) },
    { "Storage", new Vector2(-1.92f, -6.14f) },
    { "Admin", new Vector2(5.31f, -7.42f) },
    { "Electrical", new Vector2(-3.37f, -4.84f) },
    { "Security", new Vector2(-5.69f, -3.07f) },
    { "Medbay", new Vector2(-8.61f, -4.30f) },
    { "Reactor", new Vector2(-20.19f, -2.48f) },
    { "Upper Engine", new Vector2(-16.84f, 2.47f) },
    { "Lower Engine", new Vector2(-16.48f, -7.53f) }
};

        public static Dictionary<string, Vector2> miraTeleportLocations = new Dictionary<string, Vector2>()
{
    { "Launchpad", new Vector2(0.12f, -1.5f) },
    { "Medbay", new Vector2(10.2f, 15.1f) },
    { "Locker Room", new Vector2(12.5f, 18.5f) },
    { "Decontamination", new Vector2(14.8f, 22.0f) },
    { "Reactor", new Vector2(20.5f, 25.0f) },
    { "Laboratory", new Vector2(26.2f, 22.1f) },
    { "Office", new Vector2(24.5f, 15.2f) },
    { "Greenhouse", new Vector2(22.1f, 8.5f) },
    { "Admin", new Vector2(18.2f, 3.1f) },
    { "Cafeteria", new Vector2(14.5f, -2.1f) },
    { "Storage", new Vector2(9.8f, -6.5f) }
};

        public static Dictionary<string, Vector2> polusTeleportLocations = new Dictionary<string, Vector2>()
{
    { "Dropship", new Vector2(0f, 0f) },
    { "Electrical", new Vector2(5.2f, 12.1f) },
    { "O2", new Vector2(-12.4f, 8.5f) },
    { "Security", new Vector2(-18.5f, 2.2f) },
    { "Decontamination", new Vector2(-25.2f, 1.5f) },
    { "Specimen Room", new Vector2(-30.1f, -5.2f) },
    { "Laboratory", new Vector2(-20.5f, -12.1f) },
    { "Medbay", new Vector2(-8.2f, -15.4f) },
    { "Communications", new Vector2(8.5f, -12.1f) },
    { "Weapons", new Vector2(15.2f, -2.5f) }
};

        public static Dictionary<string, Vector2> airshipTeleportLocations = new Dictionary<string, Vector2>()
{
    { "Cockpit", new Vector2(-30f, 15f) },
    { "Vault", new Vector2(-15f, 15f) },
    { "Brig", new Vector2(-5f, 10f) },
    { "Meeting Room", new Vector2(10f, 12f) },
    { "Records", new Vector2(25f, 12f) },
    { "Lounge", new Vector2(35f, 8f) },
    { "Kitchen", new Vector2(25f, -5f) }
};

        public static Dictionary<string, Vector2> fungleTeleportLocations = new Dictionary<string, Vector2>()
{
    { "Beach", new Vector2(0f, -20f) },
    { "Jungle", new Vector2(15f, 10f) },
    { "Lookout", new Vector2(-10f, 25f) },
    { "Laboratory", new Vector2(-25f, 0f) },
    { "Storage", new Vector2(5f, -5f) }
};
        public static int GetCurrentMapId()
        {
            if (AmongUsClient.Instance == null) return 0;
            if (AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
            {
                return AmongUsClient.Instance.TutorialMapId;
            }
            else
            {
                if (GameOptionsManager.Instance == null || GameOptionsManager.Instance.CurrentGameOptions == null) return 0;
                return GameOptionsManager.Instance.CurrentGameOptions.MapId;
            }
        }
        private Vector2 mapsScrollPos = Vector2.zero;
        public static Dictionary<string, Vector2> GetTeleportLocations()
        {
            switch (GetCurrentMapId())
            {
                case 0: return skeldTeleportLocations;
                case 1: return miraTeleportLocations;
                case 2: return polusTeleportLocations;
                case 3: return skeldTeleportLocations;
                case 4: return airshipTeleportLocations;
                case 5: return fungleTeleportLocations;
                default: return skeldTeleportLocations;
            }
        }

        public static void TeleportTo(Vector2 position)
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.NetTransform == null) return;
            if (UseSnapToRPC)
            {
                PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(position);
            }
            else
            {
                PlayerControl.LocalPlayer.NetTransform.SnapTo(position);
            }
        }

        private int currentTab = 0;
        private int targetTabIndex = 0;
        private float tabTransitionProgress = 1f;
        private Vector2 scrollPosition = Vector2.zero;
        private void DrawAutoHostMainTab()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < autoHostSubTabs.Length; i++)
            {
                string subTabLabel = i < hostOnlySubTabs.Length ? hostOnlySubTabs[i] : autoHostSubTabs[i];
                if (GUILayout.Button(subTabLabel, currentAutoHostSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                {
                    currentAutoHostSubTab = i;
                    scrollPosition = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (currentAutoHostSubTab == 0) DrawLobbyControls();
            else if (currentAutoHostSubTab == 1) DrawPlayersRoles();
            else if (currentAutoHostSubTab == 2) DrawAntiCheatTab();
            else if (currentAutoHostSubTab == 3) DrawAutoHostTab();
        }

        private void DrawMapsTab()
        {
            GUILayout.BeginVertical(menuCardStyle);

            DrawMenuSectionHeader(L("LOBBY CONTROL", "КОНТРОЛЬ ЛОББИ"));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Spawn Lobby", "Создать лобби"), btnStyle, GUILayout.Height(30))) SpawnLobby();
            if (GUILayout.Button(L("Despawn Lobby", "Удалить лобби"), btnStyle, GUILayout.Height(30))) DespawnLobby();
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            DrawMenuSectionHeader(L("MAP CONTROL", "КОНТРОЛЬ КАРТЫ"));
            isManualMapSpawn = DrawToggle(isManualMapSpawn, L("Manual Map Spawn Mode", "Ручной спавн карты"), 250);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Select Map:", "Выбор карты:"), GUILayout.Width(100));
            selectedMapSpawnIdx = (int)GUILayout.HorizontalSlider((int)selectedMapSpawnIdx, 0, mapSpawnNames.Length - 1, sliderStyle, sliderThumbStyle, GUILayout.Width(200));
            GUILayout.Label($"<color=#{GetMenuAccentHex()}>{mapSpawnNames[(int)selectedMapSpawnIdx]}</color>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Spawn Map", "Создать карту"), activeTabStyle, GUILayout.Height(30))) SpawnMap((int)selectedMapSpawnIdx);
            if (GUILayout.Button(L("Despawn Map", "Удалить карту"), btnStyle, GUILayout.Height(30))) DespawnCurrentMap();
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            DrawMenuSectionHeader(L("ROOM TELEPORTS (IN-GAME)", "ТЕЛЕПОРТЫ ПО КОМНАТАМ (В ИГРЕ)"));
            if (ShipStatus.Instance != null && PlayerControl.LocalPlayer != null)
            {
                mapsScrollPos = GUILayout.BeginScrollView(mapsScrollPos, GUILayout.Height(160));
                var locations = GetTeleportLocations();
                int columns = 3;
                int count = 0;

                GUILayout.BeginHorizontal();
                foreach (var loc in locations)
                {
                    if (GUILayout.Button(loc.Key, btnStyle, GUILayout.Width(135), GUILayout.Height(30)))
                    {
                        TeleportTo(loc.Value);
                        ShowNotification($"<color=#00FF00>[TELEPORT]</color> {L("Moved to:", "Перемещен в:")} <b>{loc.Key}</b>");
                    }

                    count++;
                    if (count % columns == 0)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label($"<color=#777777>{L("Teleports are only available when you are on a map.", "Телепорты доступны только когда вы находитесь на карте.")}</color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
            }

            GUILayout.EndVertical();
        }

        private void DrawChatSettingsTab()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label(L("CHAT SETTINGS & LOGS", "НАСТРОЙКИ ЧАТА И ЛОГИ"), headerStyle);
            GUILayout.Space(10);

            string hexColor = GetMenuAccentHex();

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(300));
            GUILayout.Label($"<b><color=#{hexColor}>{L("LOCAL FEATURES", "ЛОКАЛЬНЫЕ ФУНКЦИИ")}</color></b>", toggleLabelStyle);
            GUILayout.Space(6);
            alwaysChat = DrawToggle(alwaysChat, L("Always Show Chat", "Всегда показывать чат"), 280);
            GUILayout.Space(2);
            readGhostChat = DrawToggle(readGhostChat, L("Read Ghost Chat", "Читать чат призраков"), 280);
            GUILayout.Space(4);
            DrawGhostChatColorControl(280f);
            GUILayout.Space(2);
            enableExtendedChat = DrawToggle(enableExtendedChat, L("Extended Chat (120 chars)", "Длинный чат (120 симв.)"), 280);
            GUILayout.Space(2);
            enableFastChat = DrawToggle(enableFastChat, L("Fast Chat (3.1 to 2.1", "Быстрый чат (c 3.1 до 2.1)"), 280);
            GUILayout.Space(2);
            allowLinksAndSymbols = DrawToggle(allowLinksAndSymbols, L("Unlock Extra Characters", "Разрешить все символы"), 280);
            GUILayout.Space(2);
            enableSpellCheck = DrawToggle(enableSpellCheck, L("Spell Check (Basic)", "Проверка орфографии (Базовая)"), 280);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.Label($"<b><color=#{hexColor}>{L("UTILITY OPTIONS", "УТИЛИТЫ")}</color></b>", toggleLabelStyle);
            GUILayout.Space(6);
            enableChatHistory = DrawToggle(enableChatHistory, L("Chat History (Up/Down)", "История чата (Стрелочки)"), 280);
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{L("History size:", "Размер истории:")} <color=#{hexColor}>{chatHistoryLimit}</color>", new GUIStyle(toggleLabelStyle) { richText = true }, GUILayout.Height(22), GUILayout.Width(130));
            chatHistoryLimit = Mathf.Clamp((int)GUILayout.HorizontalSlider(chatHistoryLimit, 5f, 80f, sliderStyle, sliderThumbStyle, GUILayout.Width(145)), 5, 80);
            TrimChatHistoryToLimit();
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            enableClipboard = DrawToggle(enableClipboard, L("Clipboard (Ctrl+C/V)", "Буфер обмена (Ctrl+C/V)"), 280);
            GUILayout.Space(2);
            enableChatLog = DrawToggle(enableChatLog, L("Save Chat Log to File", "Сохранять лог чата в файл"), 280);
            GUILayout.Space(2);
            enableChatDarkMode = DrawToggle(enableChatDarkMode, L("Dark Chat Theme", "Темная тема чата"), 280);
            if (enableChatDarkMode && GUILayout.Button(L("Turn Off Dark Chat", "Выключить темный чат"), btnStyle, GUILayout.Width(180), GUILayout.Height(24)))
            {
                enableChatDarkMode = false;
                SaveConfig();
            }

            GUILayout.Space(8);

            GUILayout.Label($"<b><color=#{hexColor}>{L("HOST LOBBY OPTIONS", "НАСТРОЙКИ ХОСТА")}</color></b>", toggleLabelStyle);
            GUILayout.Space(6);
            enableColorCommand = DrawToggle(enableColorCommand, L("Enable /color command", "Разрешить команду /color"), 280);
            GUILayout.Space(2);
            blockFortegreenChat = DrawToggle(blockFortegreenChat, L("Block Fortegreen Chat", "Запрет чата Fortegreen"), 280);
            GUILayout.Space(2);
            blockRainbowChat = DrawToggle(blockRainbowChat, L("Block Rainbow Chat", "Запрет радужного чата"), 280);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            GUILayout.Label($"<b><color=#{hexColor}>{L("CHAT SENDER", "ОТПРАВКА ЧАТА")}</color></b>", toggleLabelStyle);
            GUILayout.Space(6);

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Space(6);

            GUIStyle macFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };
            macFieldStyle.normal.textColor = whiteMenuTheme ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);
            macFieldStyle.padding = new RectOffset();
            macFieldStyle.padding.left = 12;
            macFieldStyle.padding.right = 12;
            macFieldStyle.padding.top = 8;
            macFieldStyle.padding.bottom = 8;
            macFieldStyle.margin = new RectOffset();
            macFieldStyle.margin.left = 4;
            macFieldStyle.margin.right = 4;
            macFieldStyle.margin.top = 4;
            macFieldStyle.margin.bottom = 4;

            Rect chatInputRect = GUILayoutUtility.GetRect(10f, 34f, GUILayout.ExpandWidth(true), GUILayout.Height(34));
            GUI.Box(chatInputRect, string.Empty, macFieldStyle);

            string drawText = string.IsNullOrEmpty(customChatMessage)
                ? L("Type a message...", "Введите сообщение...")
                : customChatMessage;

            if (customChatInputFocused && (Time.unscaledTime % 1f) < 0.5f)
                drawText += "|";

            GUIStyle chatInputTextStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                richText = false,
                fontSize = 12
            };
            chatInputTextStyle.normal.textColor = whiteMenuTheme ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);

            Rect textRect = new Rect(chatInputRect.x + 12f, chatInputRect.y + 4f, chatInputRect.width - 24f, chatInputRect.height - 8f);
            GUI.Label(textRect, drawText, chatInputTextStyle);

            Event e = Event.current;
            if (e != null)
            {
                if (e.type == EventType.MouseDown)
                {
                    customChatInputFocused = chatInputRect.Contains(e.mousePosition);
                    if (customChatInputFocused) e.Use();
                }
                else if (customChatInputFocused && e.type == EventType.KeyDown)
                {
                    if (HandleClipboardShortcut(e, ref customChatMessage, 120))
                    {
                    }
                    else if (e.keyCode == KeyCode.Backspace)
                    {
                        if (!string.IsNullOrEmpty(customChatMessage))
                            customChatMessage = customChatMessage.Substring(0, customChatMessage.Length - 1);
                        e.Use();
                    }
                    else if (e.keyCode == KeyCode.Escape)
                    {
                        customChatInputFocused = false;
                        e.Use();
                    }
                    else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                    {
                        TrySendCustomChatMessage(customChatMessage);
                        e.Use();
                    }
                    else if (!char.IsControl(e.character))
                    {
                        if (customChatMessage == null) customChatMessage = string.Empty;
                        if (customChatMessage.Length < 120)
                            customChatMessage += e.character;
                        e.Use();
                    }
                }
            }

            GUILayout.Space(10);

            GUILayout.BeginHorizontal(GUILayout.Height(30));
            if (GUILayout.Button(L("Send Chat", "Отправить"), btnStyle, GUILayout.Width(150), GUILayout.Height(30)))
                TrySendCustomChatMessage(customChatMessage);

            GUILayout.Space(10);
            string spamBtnText = customChatSpamEnabled ? L("Spam: ON", "Спам: ВКЛ") : L("Spam: OFF", "Спам: ВЫКЛ");
            if (GUILayout.Button(spamBtnText, customChatSpamEnabled ? activeTabStyle : btnStyle, GUILayout.Width(150), GUILayout.Height(30)))
                customChatSpamEnabled = !customChatSpamEnabled;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(12);

            GUILayout.BeginHorizontal(GUILayout.Height(24));
            GUILayout.Label($"{L("Delay:", "Задержка:")} {Mathf.Round(customChatSpamDelay * 10f) / 10f}s", new GUIStyle(toggleLabelStyle) { fontSize = 11 }, GUILayout.Height(22), GUILayout.Width(122));
            customChatSpamDelay = GUILayout.HorizontalSlider(customChatSpamDelay, 0.5f, 10f, sliderStyle, sliderThumbStyle, GUILayout.Width(300));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.Label($"<b><color=#{hexColor}>{L("COMMANDS & INFO", "КОМАНДЫ И ИНФОРМАЦИЯ")}</color></b>", toggleLabelStyle);
            GUILayout.Space(4);

            GUILayout.Label($"<color=#FFAC1C><b>{L("Whisper:", "Шепот:")}</b></color> /w, /pm, /msg [Name/ID/Color] [Text]", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 });
            GUILayout.Label($"<color=#777777>{L("Sends a private message to a player on your screen only.", "Отправляет личное сообщение выбранному игроку (видит только он и вы).")}</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11, wordWrap = true });

            GUILayout.Space(6);

            GUILayout.Label($"<color=#777777><b>Log Info:</b> {L("ChatLog.txt clears every 3 game restarts.", "Файл ChatLog.txt очищается каждые 3 запуска игры.")}</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11, wordWrap = true });

            GUILayout.EndVertical();
        }

        private void TrySendCustomChatMessage(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return;
            if (PlayerControl.LocalPlayer == null) return;

            try
            {
                PlayerControl.LocalPlayer.RpcSendChat(rawText.Trim());
            }
            catch { }
        }

        private static readonly HashSet<string> BasicSpellDictionary = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "hello","hi","gg","wp","yes","no","ok","pls","please","thanks","thx","go","come","start","skip","vote","report","body","kill","who","where","why",
            "привет","да","нет","ок","пж","пожалуйста","спасибо","го","старт","скип","голос","репорт","труп","килл","кто","где","почему","лол"
        };

        private static void TrySpellCheckNotify(string text)
        {
            if (!enableSpellCheck || string.IsNullOrWhiteSpace(text)) return;
            if (text.StartsWith("/") || text.StartsWith("!")) return;

            try
            {
                var words = Regex.Matches(text.ToLower(), @"[a-zа-яё]{3,}");
                List<string> suspicious = new List<string>();
                foreach (Match m in words)
                {
                    string w = m.Value;
                    if (w.Length < 3) continue;
                    if (BasicSpellDictionary.Contains(w)) continue;
                    if (suspicious.Contains(w)) continue;
                    suspicious.Add(w);
                    if (suspicious.Count >= 4) break;
                }

                if (suspicious.Count > 0)
                {
                    string joined = string.Join(", ", suspicious);
                    ShowNotification($"<color=#FFCC66>[SPELL]</color> Проверь слова: {joined}");
                }
            }
            catch { }
        }

        private static void UpsertPlayerHistory(PlayerControl pc)
        {
            try
            {
                if (pc == null || pc.Data == null || pc.Data.Disconnected) return;
                string name = string.IsNullOrEmpty(pc.Data.PlayerName) ? "Unknown" : pc.Data.PlayerName;
                string fc = GetDisplayedFriendCode(pc.Data);
                string puid = "Unknown";
                string platform = "Unknown";
                string customPlatform = "";
                int level = 1;

                try
                {
                    uint rawLevel = pc.Data.PlayerLevel;
                    if (rawLevel != uint.MaxValue && rawLevel < 10000) level = (int)rawLevel + 1;
                }
                catch { }

                try
                {
                    var client = AmongUsClient.Instance?.GetClientFromCharacter(pc);
                    if (client != null)
                    {
                        platform = GetPlatform(client);
                        customPlatform = GetCustomPlatformName(client);
                        puid = GetClientPuid(client);
                    }
                }
                catch { }

                string key = $"{fc}|{puid}|{name}";
                var item = playerHistoryEntries.FirstOrDefault(x => $"{x.FriendCode}|{x.Puid}|{x.Name}" == key);
                bool changed = false;
                if (item == null)
                {
                    item = new PlayerHistoryEntry
                    {
                        Name = name,
                        FriendCode = fc,
                        Puid = puid,
                        Platform = platform,
                        CustomPlatform = customPlatform,
                        Level = level,
                        FirstSeenUtc = DateTime.UtcNow,
                        LastSeenUtc = DateTime.UtcNow,
                        IsOnline = true
                    };
                    playerHistoryEntries.Add(item);
                    changed = true;
                }
                else
                {
                    changed = item.Name != name ||
                              item.FriendCode != fc ||
                              item.Puid != puid ||
                              item.Platform != platform ||
                              item.CustomPlatform != customPlatform ||
                              item.Level != level ||
                              !item.IsOnline ||
                              item.LeftUtc.HasValue;
                    item.Name = name;
                    item.FriendCode = fc;
                    item.Puid = puid;
                    item.Platform = platform;
                    item.CustomPlatform = customPlatform;
                    item.Level = level;
                    item.LastSeenUtc = DateTime.UtcNow;
                    item.LeftUtc = null;
                    item.IsOnline = true;
                }
                playerHistoryKeysById[pc.PlayerId] = key;
                if (changed) WritePlayerHistoryFile();
            }
            catch { }
        }

        private static string GetCustomPlatformName(ClientData client)
        {
            try
            {
                string value = client?.PlatformData?.PlatformName;
                if (string.IsNullOrWhiteSpace(value)) return "";
                value = Regex.Replace(value, "<.*?>", string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(value)) return "";

                string platform = GetPlatform(client);
                if (value.Equals(platform, StringComparison.OrdinalIgnoreCase)) return "";
                if (value.Equals(client.PlatformData.Platform.ToString(), StringComparison.OrdinalIgnoreCase)) return "";
                return value;
            }
            catch { return ""; }
        }

        public static string GetClientPuid(ClientData client)
        {
            if (client == null) return "Unknown";

            try
            {
                string direct = client.ProductUserId;
                if (!string.IsNullOrWhiteSpace(direct)) return direct.Trim();
            }
            catch { }

            string[] memberNames = { "ProductUserId", "productUserId", "Puid", "PUID", "puid", "EosId", "EOSId", "ProductId", "PlayerId" };
            foreach (string memberName in memberNames)
            {
                try
                {
                    PropertyInfo prop = client.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    object value = prop?.GetValue(client, null);
                    if (value != null && !string.IsNullOrWhiteSpace(value.ToString())) return value.ToString().Trim();
                }
                catch { }

                try
                {
                    FieldInfo field = client.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    object value = field?.GetValue(client);
                    if (value != null && !string.IsNullOrWhiteSpace(value.ToString())) return value.ToString().Trim();
                }
                catch { }
            }

            return "Unknown";
        }

        private static string FormatPlatformHistory(PlayerHistoryEntry entry)
        {
            if (entry == null) return "Unknown";
            return string.IsNullOrWhiteSpace(entry.CustomPlatform)
                ? entry.Platform
                : $"{entry.Platform} + custom: {entry.CustomPlatform}";
        }

        private static string PlayerHistoryFilePath()
        {
            string folder = string.IsNullOrWhiteSpace(Plugin.ElysiumFolder)
                ? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "ElysiumModMenu")
                : Plugin.ElysiumFolder;
            return System.IO.Path.Combine(folder, "ElysiumPlayerHistory.txt");
        }

        private static void MarkPlayerHistoryLeft(byte playerId)
        {
            try
            {
                if (!playerHistoryKeysById.TryGetValue(playerId, out string key)) return;
                var item = playerHistoryEntries.FirstOrDefault(x => $"{x.FriendCode}|{x.Puid}|{x.Name}" == key);
                if (item == null || !item.IsOnline) return;

                item.IsOnline = false;
                item.LeftUtc = DateTime.UtcNow;
                item.LastSeenUtc = item.LeftUtc.Value;
                WritePlayerHistoryFile();
            }
            catch { }
        }

        public static void RecordPlayerRpc(PlayerControl pc, byte callId)
        {
            try
            {
                if (VanillaRpcIds.Contains(callId)) return;
                if (pc == null || pc.Data == null) return;
                UpsertPlayerHistory(pc);

                if (!playerHistoryKeysById.TryGetValue(pc.PlayerId, out string key)) return;
                var item = playerHistoryEntries.FirstOrDefault(x => $"{x.FriendCode}|{x.Puid}|{x.Name}" == key);
                if (item == null) return;

                if (!item.RpcCalls.Contains(callId))
                {
                    item.RpcCalls.Add(callId);
                    item.RpcCalls.Sort();
                    WritePlayerHistoryFile();
                }
            }
            catch { }
        }

        private static string FormatRpcHistory(PlayerHistoryEntry entry)
        {
            if (entry == null || entry.RpcCalls == null || entry.RpcCalls.Count == 0) return "нет";
            byte[] customRpcCalls = entry.RpcCalls.Where(x => !VanillaRpcIds.Contains(x)).Distinct().OrderBy(x => x).ToArray();
            if (customRpcCalls.Length == 0) return "нет";
            return string.Join(", ", customRpcCalls.Select(x => x.ToString()).ToArray());
        }

        private static void WritePlayerHistoryFile()
        {
            try
            {
                string path = PlayerHistoryFilePath();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

                List<string> lines = new List<string>
                {
                    "ElysiumModMenu Player History",
                    $"Updated UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                    ""
                };

                foreach (var e in playerHistoryEntries.OrderByDescending(x => x.LastSeenUtc))
                {
                    string left = e.LeftUtc.HasValue ? e.LeftUtc.Value.ToString("yyyy-MM-dd HH:mm:ss") : "online";
                    lines.Add($"Nick: {e.Name}");
                    lines.Add($"Level: {e.Level}");
                    lines.Add($"FriendCode: {e.FriendCode}");
                    lines.Add($"PUID: {e.Puid}");
                    lines.Add($"Joined UTC: {e.FirstSeenUtc:yyyy-MM-dd HH:mm:ss}");
                    lines.Add($"Left UTC: {left}");
                    lines.Add($"Platform: {FormatPlatformHistory(e)}");
                    lines.Add($"RPC calls: {FormatRpcHistory(e)}");
                    lines.Add(new string('-', 48));
                }

                System.IO.File.WriteAllLines(path, lines.ToArray(), Encoding.UTF8);
            }
            catch { }
        }

        private void TryHostOnlyKillAuraTick()
        {
            if (!killAuraHostOnly)
            {
                killAuraTimer = 0f;
                return;
            }

            if (AmongUsClient.Instance == null) return;
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;
            if (PlayerControl.LocalPlayer.Data.IsDead) return;
            if (!RoleManager.IsImpostorRole(PlayerControl.LocalPlayer.Data.RoleType)) return;
            if (MeetingHud.Instance != null) return;
            if (PlayerControl.LocalPlayer.inVent || PlayerControl.LocalPlayer.onLadder) return;
            if (!noKillCooldownHostOnly && GetRemainingKillCooldown(PlayerControl.LocalPlayer.PlayerId) > 0.05f) return;

            killAuraTimer += Time.deltaTime;
            if (killAuraTimer < 0.05f) return;

            if (PlayerControl.AllPlayerControls == null) return;

            PlayerControl nearestTarget = null;
            float nearestDistance = float.MaxValue;
            Vector3 localPos = PlayerControl.LocalPlayer.transform.position;
            Vector2 localPos2D = new Vector2(localPos.x, localPos.y);

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc == PlayerControl.LocalPlayer || pc.Data == null) continue;
                if (pc.Data.Disconnected || pc.Data.IsDead) continue;
                if (pc.inVent || pc.onLadder) continue;

                Vector3 targetPos = pc.transform.position;
                float dist = Vector2.Distance(localPos2D, new Vector2(targetPos.x, targetPos.y));
                if (dist <= 2.2f && dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearestTarget = pc;
                }
            }

            if (nearestTarget == null) return;

            try
            {
                PlayerControl.LocalPlayer.CmdCheckMurder(nearestTarget);
                PlayerControl.LocalPlayer.RpcMurderPlayer(nearestTarget, true);

                if (AmongUsClient.Instance.AmHost)
                    PlayerControl.LocalPlayer.SetKillTimer(noKillCooldownHostOnly ? 0f : GetConfiguredKillCooldown());

                killAuraTimer = 0f;
            }
            catch { }
        }

        private void DrawAntiCheatTab()
        {
            float antiCheatColumnWidth = (windowRect.width - 186f) / 2f;
            if (antiCheatColumnWidth < 282f) antiCheatColumnWidth = 282f;

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(antiCheatColumnWidth));

            DrawMenuSectionHeader(L("PUNISHMENT SYSTEM", "СИСТЕМА НАКАЗАНИЙ"));
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Mode:", "Режим:"), toggleLabelStyle, GUILayout.Width(60));

            GUIStyle middleLabelStyle = new GUIStyle(btnStyle) { fontStyle = FontStyle.Bold, normal = { background = null, textColor = GetMenuAccentColor() } };

            if (GUILayout.Button("<", btnStyle, GUILayout.Width(25), GUILayout.Height(25)))
            {
                punishmentMode--;
                if (punishmentMode < 0) punishmentMode = punishmentNames.Length - 1;
                settingsDirty = true;
            }

            GUILayout.Label(punishmentNames[punishmentMode], middleLabelStyle, GUILayout.ExpandWidth(true), GUILayout.Height(25));

            if (GUILayout.Button(">", btnStyle, GUILayout.Width(25), GUILayout.Height(25)))
            {
                punishmentMode++;
                if (punishmentMode >= punishmentNames.Length) punishmentMode = 0;
                settingsDirty = true;
            }
            GUILayout.EndHorizontal();

            string modeDesc = punishmentMode switch
            {
                0 => "<color=#777777>Null: Пакеты блокируются без действий.</color>",
                1 => "<color=#FFFF00>Warn: Блокировка + Уведомление на экран.</color>",
                2 => "<color=#FF8800>Kick: Игрок будет исключен из лобби.</color>",
                3 => "<color=#FF0000>Ban: Игрок будет забанен (Host Only).</color>",
                _ => ""
            };
            GUILayout.Label(modeDesc, new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11, wordWrap = true });

            GUILayout.Space(12);
            DrawMenuSectionHeader(L("RPC PROTECTIONS", "ЗАЩИТА RPC"));

            blockSpoofRPC = DrawToggle(blockSpoofRPC, L("Block Spoof RPC", "Блокировать spoof RPC"), 250);
            GUILayout.Space(5);
            blockSabotageRPC = DrawToggle(blockSabotageRPC, L("Block Sabotage & Meetings", "Блокировать саботажи и митинги"), 250);
            GUILayout.Space(5);
            blockGameRpcInLobby = DrawToggle(blockGameRpcInLobby, L("Block Game RPC in Lobby", "Блокировать игровые RPC в лобби"), 250);
            GUILayout.Space(5);

            autoBanPlatformSpoof = DrawToggle(autoBanPlatformSpoof, L("Auto-Ban Platform Spoof (Host)", "Авто-бан Platform Spoof (Хост)"), 250);
            GUILayout.Space(5);
            banCustomPlatformsFromTxt = DrawToggle(banCustomPlatformsFromTxt, L("Ban Custom Platforms From TXT", "Бан кастом платформ из TXT"), 250);
            GUILayout.Space(5);

            blockMeetingFloodRpc = DrawToggle(blockMeetingFloodRpc, L("Block Meeting RPC Flood", "Блокировать флуд RPC митинга"), 250);
            GUILayout.Space(5);
            blockChatFloodRpc = DrawToggle(blockChatFloodRpc, L("Block Chat RPC Flood", "Блокировать флуд RPC чата"), 250);
            GUILayout.Space(5);
            enablePasosLimit = DrawToggle(enablePasosLimit, L("RPC Anti-Cheat", "RPC Античит"), 250);
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            //GUILayout.Label(L("RPC limit:", "Лимит RPC:"), new GUIStyle(toggleLabelStyle), GUILayout.Height(22), GUILayout.Width(76));
           
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            //GUILayout.Space(5);
            //enableLocalPasosBan = DrawToggle(enableLocalPasosBan, L("RPC Local Drop", "RPC локальный дроп"), 250);
            //GUILayout.Space(5);
            ////enableHostPasosBan = DrawToggle(enableHostPasosBan, L("RPC Host Ban", "RPC бан на хосте"), 250);
            //GUILayout.Space(5);
            //enableMalformedPacketGuard = DrawToggle(enableMalformedPacketGuard, L("Anti-Crash (Malformed Packets)", "Анти-краш (кривые пакеты)"), 250);
            GUILayout.Space(5);
            banMalformedPacketSender = DrawToggle(banMalformedPacketSender, L("Ban Malformed Sender (Host)", "Бан за кривые пакеты (Хост)"), 250);
            GUILayout.Space(5);
            enableQuickChatEmptyGuard = DrawToggle(enableQuickChatEmptyGuard, L("QuickChat Anti-Crash", "Анти-краш QuickChat"), 250);
            GUILayout.Space(5);
            banQuickChatEmptySpammer = DrawToggle(banQuickChatEmptySpammer, L("Ban QuickChat Spammer (Host)", "Бан за QuickChat спам (Хост)"), 250);
            GUILayout.Space(5);
            //enableUnownedSpawnGuard = DrawToggle(enableUnownedSpawnGuard, L("Drop Fake Spawns (Host)", "Дроп фейк-спавнов (Хост)"), 250);
            GUILayout.Space(15);
            DrawMenuSectionHeader(L("OTHER PROTECTIONS", "ПРОЧАЯ ЗАЩИТА"));

            disableVoteKicks = DrawToggle(disableVoteKicks, L("Disable Vote Kicks (Host)", "Запрет кика голосованием (Хост)"), 250);
            GUILayout.Space(5);

            autoKickBugs = DrawToggle(autoKickBugs, L("Auto-Kick Fortegreen", "Авто-кик багнутых игроков"), 250);
            if (autoKickBugs)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("Timer:", "Таймер:"), new GUIStyle(toggleLabelStyle), GUILayout.Height(22), GUILayout.Width(62));
                autoKickTimer = GUILayout.HorizontalSlider(autoKickTimer, 1f, 15f, sliderStyle, sliderThumbStyle, GUILayout.Width(112));
                GUILayout.Space(8);
                GUILayout.Label(autoKickTimer.ToString("0.0") + "s", menuBadgeStyle, GUILayout.Width(46), GUILayout.Height(22));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(5);
            autoBanBrokenFriendCode = DrawToggle(autoBanBrokenFriendCode, L("Auto-Ban Broken FriendCode (Host)", "Авто-бан сломанного FriendCode (Хост)"), 250);
            GUILayout.Space(5);
            autoKickLowLevelEnabled = DrawToggle(autoKickLowLevelEnabled, L("Kick Low Level (Host)", "Кик по уровню (Хост)"), 250);
            if (autoKickLowLevelEnabled)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(L("Min level:", "Мин. уровень:"), new GUIStyle(toggleLabelStyle), GUILayout.Height(22), GUILayout.Width(86));
                int oldMinLevel = autoKickMinLevel;
                autoKickMinLevel = Mathf.Clamp((int)GUILayout.HorizontalSlider(autoKickMinLevel, 1f, 300f, sliderStyle, sliderThumbStyle, GUILayout.Width(112)), 1, 300);
                if (oldMinLevel != autoKickMinLevel) settingsDirty = true;
                GUILayout.Space(8);
                GUILayout.Label(autoKickMinLevel.ToString(), menuBadgeStyle, GUILayout.Width(46), GUILayout.Height(22));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(5);
            banBotsEnabled = DrawToggle(banBotsEnabled, L("Ban Bots (Host)", "Бан ботов (Хост)"), 250);

            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(antiCheatColumnWidth), GUILayout.ExpandHeight(true));
            DrawMenuSectionHeader(L("BAN LIST", "БАН ЛИСТ"));
            autoBanEnabled = DrawToggle(autoBanEnabled, L("Auto-Ban Blacklisted Players", "Авто-бан игроков из списка"), 250);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            string defaultBanText = L("Enter Friend Code", "Введите Friend Code");
            string banValue = string.IsNullOrEmpty(banInput) && !isEditingBan ? defaultBanText : banInput;

            if (DrawPseudoInputButton(banValue, isEditingBan, 25f, 46))
            {
                isEditingBan = !isEditingBan;
                isEditingGhostChatColor = false;
                ResetAllBindWaits();
            }

            if (GUILayout.Button(L("ADD", "ДОБАВИТЬ"), btnStyle, GUILayout.Width(75f), GUILayout.Height(25f)))
            {
                if (!string.IsNullOrWhiteSpace(banInput))
                {
                    AddToBanList(banInput.Trim(), "Manual", "Unknown", "Manual ban");
                    banInput = "";
                    isEditingBan = false;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            banListScroll = GUILayout.BeginScrollView(banListScroll);

            if (bannedEntries.Count == 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label($"<color=#777777>{L("Ban list is empty.", "Бан лист пуст.")}</color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
                GUILayout.FlexibleSpace();
            }
            else
            {
                for (int i = 0; i < bannedEntries.Count; i++)
                {
                    string entry = bannedEntries[i];
                    if (string.IsNullOrWhiteSpace(entry)) continue;

                    string[] parts = entry.Split('|');
                    string disp = parts.Length >= 3 ? $"{parts[2]} ({parts[0]})" : entry;

                    GUILayout.BeginHorizontal(boxStyle);
                    GUILayout.Label(disp, new GUIStyle(GUI.skin.label) { fontSize = 12 }, GUILayout.Width(185));
                    GUILayout.FlexibleSpace();

                    GUIStyle redCrossStyle = new GUIStyle(btnStyle);
                    redCrossStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);

                    if (GUILayout.Button("X", redCrossStyle, GUILayout.Width(25), GUILayout.Height(22)))
                    {
                        RemoveFromBanList(entry);
                        break;
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        public static class ElysiumAnticheat
        {
            public static void Flag(PlayerControl player, string reason)
            {
                if (player == null || player.Data == null || player == PlayerControl.LocalPlayer) return;

                string pName = player.Data.PlayerName ?? "Unknown";

                int mode = ElysiumModMenuGUI.punishmentMode;

                if (mode >= 1)
                {
                    ElysiumModMenuGUI.ShowNotification($"<color=#FF0000>[ANTICHEAT]</color> <b>{pName}</b>: {reason}");
                }

                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    if (mode == 2)
                    {
                        AmongUsClient.Instance.KickPlayer(player.OwnerId, false);
                    }
                    else if (mode == 3)
                    {
                        string fc = string.IsNullOrEmpty(player.Data.FriendCode) ? "Unknown" : player.Data.FriendCode;
                        string puid = "Unknown";
                        try
                        {
                            var client = AmongUsClient.Instance.GetClientFromCharacter(player);
                            if (client != null) puid = GetClientPuid(client);
                        }
                        catch { }

                        ElysiumModMenuGUI.AddToBanList(fc, puid, pName, $"Anticheat: {reason}");

                        AmongUsClient.Instance.KickPlayer(player.OwnerId, true);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
        public static class Anticheat_PlayerControl_RPC
        {
            private static readonly Dictionary<byte, Queue<float>> chatRpcTimes = new Dictionary<byte, Queue<float>>();
            private static readonly Dictionary<byte, Queue<float>> meetingRpcTimes = new Dictionary<byte, Queue<float>>();
            private static readonly HashSet<byte> lobbyGameRpcs = new HashSet<byte>
            {
                (byte)RpcCalls.MurderPlayer,
                (byte)RpcCalls.ReportDeadBody,
                (byte)RpcCalls.StartMeeting,
                (byte)RpcCalls.EnterVent,
                (byte)RpcCalls.ExitVent,
                (byte)RpcCalls.Shapeshift,
                (byte)RpcCalls.ProtectPlayer
            };

            private static bool IsFlooded(Dictionary<byte, Queue<float>> map, byte playerId, int maxCalls, float windowSeconds)
            {
                float now = Time.unscaledTime;
                if (!map.TryGetValue(playerId, out Queue<float> times))
                {
                    times = new Queue<float>();
                    map[playerId] = times;
                }

                times.Enqueue(now);
                while (times.Count > 0 && now - times.Peek() > windowSeconds)
                    times.Dequeue();

                return times.Count > maxCalls;
            }

            public static bool Prefix(PlayerControl __instance, byte callId, Hazel.MessageReader reader)
            {
                if (__instance != null && __instance != PlayerControl.LocalPlayer && __instance.Data != null && ElysiumModMenuGUI.enablePasosLimit)
                {
                    //int clientId = Shield_PasosLimit_Patch.GetKickClientId(__instance, -1);
                    //if (Shield_PasosLimit_Patch.RecordDrop(clientId, __instance, $"PlayerControl RPC {callId} spam"))
                    //    return false;
                }

                if (!ElysiumModMenuGUI.blockSpoofRPC &&
                    !ElysiumModMenuGUI.blockSabotageRPC &&
                    !ElysiumModMenuGUI.blockGameRpcInLobby &&
                    !ElysiumModMenuGUI.blockChatFloodRpc &&
                    !ElysiumModMenuGUI.blockMeetingFloodRpc) return true;
                if (__instance == null || __instance == PlayerControl.LocalPlayer || __instance.Data == null) return true;

                int oldPos = reader.Position;
                bool isCheat = false;
                string cheatReason = "";

                try
                {
                    if (ElysiumModMenuGUI.blockGameRpcInLobby &&
                        AmongUsClient.Instance != null &&
                        !AmongUsClient.Instance.IsGameStarted &&
                        lobbyGameRpcs.Contains(callId))
                    {
                        isCheat = true;
                        cheatReason = $"Game RPC in lobby ({((RpcCalls)callId)})";
                    }

                    if (!isCheat && ElysiumModMenuGUI.blockChatFloodRpc &&
                        (callId == (byte)RpcCalls.SendChat || callId == (byte)RpcCalls.SendQuickChat))
                    {
                        if (IsFlooded(chatRpcTimes, __instance.PlayerId, ElysiumModMenuGUI.chatRpcLimit, ElysiumModMenuGUI.chatRpcWindow))
                        {
                            isCheat = true;
                            cheatReason = "Chat RPC flood";
                        }
                    }

       
                    if (!isCheat && ElysiumModMenuGUI.enableQuickChatEmptyGuard &&
                        callId == (byte)RpcCalls.SendQuickChat)
                    {
                        int qcPos = reader.Position;
                        int zeroRun = 0, zeroMax = 0, scanned = 0;
                        while (reader.Position < reader.Length && scanned < 4096)
                        {
                            scanned++;
                            if (reader.ReadByte() == 0) { zeroRun++; if (zeroRun > zeroMax) zeroMax = zeroRun; }
                            else zeroRun = 0;
                        }
                        reader.Position = qcPos;

                        if (zeroMax >= 8)
                        {
                            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost &&
                                __instance != null && __instance != PlayerControl.LocalPlayer &&
                                __instance.OwnerId != AmongUsClient.Instance.HostId)
                            {
                                try
                                {
                                    bool qcBan = ElysiumModMenuGUI.banQuickChatEmptySpammer;
                                    string qcName = (__instance.Data != null && !string.IsNullOrEmpty(__instance.Data.PlayerName))
                                        ? __instance.Data.PlayerName : $"Client {__instance.OwnerId}";
                                    if (qcBan)
                                    {
                                        string qcFc = (__instance.Data != null && !string.IsNullOrEmpty(__instance.Data.FriendCode))
                                            ? __instance.Data.FriendCode : "Unknown";
                                        string qcPuid = "Unknown";
                                        try
                                        {
                                            var qcClient = AmongUsClient.Instance.GetClientFromCharacter(__instance);
                                            if (qcClient != null) qcPuid = ElysiumModMenuGUI.GetClientPuid(qcClient);
                                        }
                                        catch { }
                                        ElysiumModMenuGUI.AddToBanList(qcFc, qcPuid, qcName, "QuickChat Empty spam (anti-crash)");
                                    }
                                    AmongUsClient.Instance.KickPlayer(__instance.OwnerId, qcBan);
                                    ElysiumModMenuGUI.ShowNotification($"<color=#FF4444>[ANTI-CRASH]</color> {qcName} {(qcBan ? "banned" : "kicked")}: QuickChat spam");
                                }
                                catch { }
                            }
                            return false; 
                        }
                    }

                    if (!isCheat && ElysiumModMenuGUI.blockMeetingFloodRpc &&
                        (callId == (byte)RpcCalls.StartMeeting || callId == (byte)RpcCalls.ReportDeadBody))
                    {
                        if (IsFlooded(meetingRpcTimes, __instance.PlayerId, ElysiumModMenuGUI.meetingRpcLimit, ElysiumModMenuGUI.meetingRpcWindow))
                        {
                            isCheat = true;
                            cheatReason = "Meeting RPC flood";
                        }
                    }

                    if (!isCheat && ElysiumModMenuGUI.blockSpoofRPC)
                    {
                        if (callId == (byte)RpcCalls.SetColor)
                        {
                            uint netId = reader.ReadUInt32();
                            byte color = reader.ReadByte();
                            if (color >= Palette.PlayerColors.Length) { isCheat = true; cheatReason = $"Invalid Color ID ({color})"; }
                        }
                        else if (callId == (byte)RpcCalls.SetName || callId == (byte)RpcCalls.CheckName)
                        {
                            uint netId = callId == (byte)RpcCalls.SetName ? reader.ReadUInt32() : 0;
                            string reqName = reader.ReadString();
                            if (reqName.Length > 12) { isCheat = true; cheatReason = "Name length too long"; }
                            if (reqName.Contains("<")) { isCheat = true; cheatReason = "HTML Tags in name"; }
                        }
                        else if (callId == (byte)RpcCalls.SetScanner)
                        {
                            bool scanning = reader.ReadBoolean();
                            if (scanning && RoleManager.IsImpostorRole(__instance.Data.RoleType))
                            { isCheat = true; cheatReason = "Scanner activated as Impostor"; }
                        }
                        else if (callId == (byte)RpcCalls.PlayAnimation)
                        {
                            byte anim = reader.ReadByte();
                            if (RoleManager.IsImpostorRole(__instance.Data.RoleType))
                            { isCheat = true; cheatReason = "Task Animation as Impostor"; }
                        }
                        else if (callId == (byte)RpcCalls.EnterVent || callId == (byte)RpcCalls.ExitVent)
                        {
                            if (!__instance.Data.IsDead && __instance.Data.Role != null && !__instance.Data.Role.CanVent)
                            { isCheat = true; cheatReason = "Vent without vent ability"; }

                            if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek() && RoleManager.IsImpostorRole(__instance.Data.RoleType))
                            { isCheat = true; cheatReason = "Venting as Seeker in H&S"; }
                        }
                    }

                    if (!isCheat && ElysiumModMenuGUI.blockSabotageRPC)
                    {
                        if (callId == (byte)RpcCalls.ReportDeadBody)
                        {
                            if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek())
                            { isCheat = true; cheatReason = "Reported body in H&S"; }
                        }
                        else if (callId == (byte)RpcCalls.SetStartCounter)
                        {
                            reader.ReadPackedInt32();
                            sbyte counter = reader.ReadSByte();

                            if (__instance.OwnerId != AmongUsClient.Instance.HostId && counter != -1)
                            { isCheat = true; cheatReason = "Start counter changed by non-host"; }
                        }
                    }
                }
                catch { }

                reader.Position = oldPos;

                if (isCheat)
                {
                    ElysiumAnticheat.Flag(__instance, cheatReason);
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.HandleRpc))]
        public static class Anticheat_ShipStatus_RPC
        {
            public static bool Prefix(ShipStatus __instance, byte callId, Hazel.MessageReader reader)
            {
                if (!ElysiumModMenuGUI.blockSabotageRPC) return true;

                int oldPos = reader.Position;
                bool isCheat = false;
                string cheatReason = "";
                PlayerControl sender = null;

                try
                {
                    if (callId == (byte)RpcCalls.UpdateSystem)
                    {
                        SystemTypes system = (SystemTypes)reader.ReadByte();
                        sender = reader.ReadNetObject<PlayerControl>();

                        if (sender != null && !sender.AmOwner)
                        {
                            if (system == SystemTypes.Sabotage)
                            {
                                SystemTypes sabSystem = (SystemTypes)reader.ReadByte();
                                if (sender.Data != null && !RoleManager.IsImpostorRole(sender.Data.RoleType))
                                { isCheat = true; cheatReason = "Triggered Sabotage as Crewmate"; }
                            }
                        }
                    }
                    else if (callId == (byte)RpcCalls.CloseDoorsOfType)
                    {
                        if (GameManager.Instance != null && GameManager.Instance.IsHideAndSeek())
                        { isCheat = true; cheatReason = "Closed doors in H&S"; }
                    }
                }
                catch { }

                reader.Position = oldPos;

                if (isCheat && sender != null && sender != PlayerControl.LocalPlayer)
                {
                    ElysiumAnticheat.Flag(sender, cheatReason);
                    return false;
                }

                return true;
            }
        }
        public static bool autoChatEveryone = false;
        public static bool pendingAutoMeeting = false;

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckColor))]
        public static class AllowDuplicateColors_CheckColor_Patch
        {
            private static bool applyingDuplicateColor;

            public static bool Prefix(PlayerControl __instance, byte bodyColor)
            {
                if (applyingDuplicateColor || !ElysiumModMenuGUI.allowDuplicateColors ||
                    __instance == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost ||
                    bodyColor == byte.MaxValue)
                    return true;

                try
                {
                    applyingDuplicateColor = true;
                    __instance.RpcSetColor(bodyColor);
                    return false;
                }
                catch { return true; }
                finally { applyingDuplicateColor = false; }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
        public static class Anticheat_Platform_Check
        {
            public static void Postfix(PlayerControl __instance)
            {
                if ((!ElysiumModMenuGUI.blockSpoofRPC && !ElysiumModMenuGUI.autoBanPlatformSpoof && !ElysiumModMenuGUI.banCustomPlatformsFromTxt) ||
                    __instance == null || __instance == PlayerControl.LocalPlayer) return;

                try
                {
                    var clientData = AmongUsClient.Instance.GetClientFromCharacter(__instance);
                    if (clientData == null || clientData.PlatformData == null) return;

                    if (ElysiumModMenuGUI.banCustomPlatformsFromTxt &&
                        MatchesPlatformBanTxt(clientData, out string customPlatformName, out string token))
                    {
                        HostBanForPlatform(__instance, $"Custom platform TXT match '{token}' ({customPlatformName})");
                        return;
                    }

                    var platform = clientData.PlatformData;
                    string pName = platform.PlatformName;
                    ulong xuid = platform.XboxPlatformId;
                    ulong psid = platform.PsnPlatformId;

                    bool isValid = true;

                    switch (platform.Platform)
                    {
                        case Platforms.StandaloneEpicPC:
                        case Platforms.StandaloneSteamPC:
                        case Platforms.StandaloneMac:
                        case Platforms.StandaloneItch:
                        case Platforms.IPhone:
                        case Platforms.Android:
                            isValid = (pName == "TESTNAME" && xuid == 0 && psid == 0);
                            break;
                        case Platforms.StandaloneWin10:
                            isValid = (pName == "TESTNAME" && xuid != 0 && psid == 0);
                            break;
                        case Platforms.Xbox:
                            isValid = (pName != "TESTNAME" && pName.Length >= 3 && xuid != 0 && psid == 0);
                            break;
                        case Platforms.Playstation:
                            isValid = (pName != "TESTNAME" && xuid == 0 && psid != 0);
                            break;
                        case Platforms.Switch:
                            isValid = (pName != "TESTNAME" && xuid == 0 && psid == 0);
                            break;
                    }

                    if (!isValid)
                    {
                        string reason = $"Platform Spoof detected ({platform.Platform})";
                        if (ElysiumModMenuGUI.autoBanPlatformSpoof)
                            HostBanForPlatform(__instance, reason);
                        else if (ElysiumModMenuGUI.blockSpoofRPC)
                            ElysiumAnticheat.Flag(__instance, reason);
                    }
                }
                catch { }
            }
        }
        public static class ElysiumAutoLobbyReturn
        {
            private const float AutoReturnDelaySeconds = 3f;
            private const float AutoReturnRetrySeconds = 0.4f;
            private const int AutoReturnMaxAttempts = 40;

            private static int trackedEndGameId;
            private static int exhaustedEndGameId;
            private static int attempt;
            private static float nextAttemptAt;
            private static bool pending;

            public static void UpdateLogic()
            {
                if (!ShouldAutoReturn())
                {
                    ResetState();
                    return;
                }
                if (LobbyBehaviour.Instance != null)
                {
                    ResetState();
                    return;
                }

                EndGameManager val = UnityEngine.Object.FindObjectOfType<EndGameManager>();
                if (val != null)
                {
                    int instanceID = val.gameObject.GetInstanceID();
                    if (trackedEndGameId != instanceID)
                    {
                        trackedEndGameId = instanceID;
                        exhaustedEndGameId = 0;
                        attempt = 0;
                        nextAttemptAt = Time.unscaledTime + AutoReturnDelaySeconds;
                        pending = true;
                    }
                }
                else if (trackedEndGameId == 0) return;

                if (!pending || exhaustedEndGameId == trackedEndGameId || Time.unscaledTime < nextAttemptAt)
                    return;

                bool flag = false;
                if (val != null)
                {
                    flag = TryInvokeEndGameAction(val);
                    flag = TryClickEndGameButtons(val) || flag;
                }
                flag = TryClickGlobalReturnButtons() || flag;

                if (LobbyBehaviour.Instance != null)
                {
                    ResetState();
                    return;
                }

                attempt++;
                if (attempt >= AutoReturnMaxAttempts)
                    pending = false;
                else
                    nextAttemptAt = Time.unscaledTime + AutoReturnRetrySeconds;
            }

            public static void ResetState()
            {
                trackedEndGameId = 0;
                exhaustedEndGameId = 0;
                attempt = 0;
                nextAttemptAt = 0f;
                pending = false;
            }

            private static bool ShouldAutoReturn()
            {
                return ElysiumModMenuGUI.AutoReturnLobbyAfterMatch || ElysiumAutoHostService.ShouldReturnAfterMatch;
            }

            private static bool TryInvokeEndGameAction(EndGameManager manager)
            {
                if (manager == null) return false;
                string[] methods = new string[] { "Continue", "NextGame", "PlayAgain" };
                for (int i = 0; i < methods.Length; i++)
                {
                    System.Reflection.MethodInfo methodInfo = FindMethodNoWarn(manager.GetType(), methods[i], Type.EmptyTypes);
                    if (methodInfo != null)
                    {
                        try { methodInfo.Invoke(manager, null); return true; }
                        catch { }
                    }
                }
                return false;
            }

            private static System.Reflection.MethodInfo FindMethodNoWarn(Type type, string name, Type[] parameters)
            {
                if (type == null || string.IsNullOrWhiteSpace(name)) return null;
                Type[] types = parameters ?? Type.EmptyTypes;
                Type t = type;
                while (t != null)
                {
                    System.Reflection.MethodInfo method = t.GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, types, null);
                    if (method != null) return method;
                    t = t.BaseType;
                }
                return null;
            }

            private static bool TryClickEndGameButtons(EndGameManager manager)
            {
                if (manager == null) return false;
                if (TryClickPassiveButtons(manager.GetComponentsInChildren<PassiveButton>(true), true))
                    return true;
                return TryClickUnityButtons(manager.GetComponentsInChildren<UnityEngine.UI.Button>(true), true);
            }

            private static bool TryClickGlobalReturnButtons()
            {
                if (TryClickPassiveButtons(UnityEngine.Object.FindObjectsOfType<PassiveButton>(), true))
                    return true;
                return TryClickUnityButtons(UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Button>(), true);
            }

            private static bool TryClickPassiveButtons(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<PassiveButton> buttons, bool onlyActive)
            {
                if (buttons == null) return false;
                foreach (PassiveButton btn in buttons)
                {
                    if (btn == null) continue;
                    if (onlyActive && (!btn.gameObject.activeInHierarchy || !btn.isActiveAndEnabled))
                        continue;
                    if (!IsLobbyReturnButton(btn.name, btn.GetComponentsInChildren<TMPro.TMP_Text>(true)))
                        continue;
                    try
                    {
                        if (btn.OnClick != null)
                        {
                            btn.OnClick.Invoke();
                            return true;
                        }
                    }
                    catch { }
                }
                return false;
            }

            private static bool TryClickUnityButtons(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<UnityEngine.UI.Button> buttons, bool onlyActive)
            {
                if (buttons == null) return false;
                foreach (UnityEngine.UI.Button btn in buttons)
                {
                    if (btn == null) continue;
                    if (onlyActive && (!btn.gameObject.activeInHierarchy || !btn.isActiveAndEnabled || !btn.interactable))
                        continue;
                    if (!IsLobbyReturnButton(btn.name, btn.GetComponentsInChildren<TMPro.TMP_Text>(true)))
                        continue;
                    try
                    {
                        if (btn.onClick != null)
                        {
                            btn.onClick.Invoke();
                            return true;
                        }
                    }
                    catch { }
                }
                return false;
            }

            private static bool IsLobbyReturnButton(string objectName, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<TMPro.TMP_Text> texts)
            {
                string input = (objectName ?? string.Empty).ToLowerInvariant();
                if (ContainsAny(input, "exit", "quit", "menu", "back", "leave", "вых", "выйт", "назад"))
                    return false;
                if (ContainsAny(input, "continue", "nextgame", "playagain", "returntolobby", "tolobby", "lobby", "again", "продолж", "занов", "снов", "лобби", "играть", "вернут"))
                    return true;
                if (texts == null) return false;
                foreach (TMPro.TMP_Text txt in texts)
                {
                    if (txt == null) continue;
                    string stripped = StripRichText(txt.text).ToLowerInvariant();
                    if (ContainsAny(stripped, "exit", "quit", "menu", "back", "leave", "вых", "выйт", "назад"))
                        return false;
                    if (ContainsAny(stripped, "continue", "next game", "play again", "return to lobby", "lobby", "again", "продолж", "занов", "снов", "лобби", "играть", "вернут"))
                        return true;
                }
                return false;
            }

            private static bool ContainsAny(string input, params string[] tokens)
            {
                if (string.IsNullOrEmpty(input)) return false;
                foreach (string token in tokens)
                    if (!string.IsNullOrWhiteSpace(token) && input.Contains(token))
                        return true;
                return false;
            }

            private static string StripRichText(string input)
            {
                if (string.IsNullOrEmpty(input)) return string.Empty;
                char[] chars = new char[input.Length];
                int length = 0;
                bool inTag = false;
                foreach (char c in input)
                {
                    switch (c)
                    {
                        case '<': inTag = true; continue;
                        case '>': inTag = false; continue;
                    }
                    if (!inTag) chars[length++] = c;
                }
                return new string(chars, 0, length);
            }
        }

        public static class ElysiumAutoHostService
        {
            public sealed class AutoHostStatusSnapshot
            {
                public bool Enabled;
                public bool IsHost;
                public bool IsLobby;
                public bool IsInGame;
                public string State = string.Empty;
                public string LastReason = string.Empty;
                public int ConnectedPlayers;
                public int ReadyPlayers;
                public int RequiredPlayers;
                public float CountdownRemainingSeconds;
                public float BackoffRemainingSeconds;
                public float LobbyAgeSeconds;
                public float LobbyLifeRemainingSeconds = -1f;
                public bool WaitingForLoadedPlayers;
                public bool AutoReturnAfterMatch;
                public bool ForceLastMinute;
                public string StartMode = string.Empty;
                public float EffectiveStartDelaySeconds;
                public float WarmupRemainingSeconds;
                public float LoadGraceRemainingSeconds;
                public bool FastStartActive;
                public bool ForceStartActive;
            }

            private enum AutoHostState
            {
                Disabled, Idle, Warmup, WaitingPlayers, WaitingLoad,
                Countdown, Starting, InGame, Returning, Backoff,
            }

            private const float TickIntervalSeconds = 0.2f;
            private const float StartRequestGraceSeconds = 7f;
            private const float LobbyLifetimeSeconds = 600f;
            private const float LastMinuteStartSeconds = 60f;
            private const float NotificationCooldownSeconds = 0.75f;

            private static AutoHostState state = AutoHostState.Disabled;
            private static string lastReason = "disabled";
            private static float nextTickAt;
            private static float countdownStartedAt = -1f;
            private static float activeCountdownDelay = -1f;
            private static float backoffUntil = -1f;
            private static float lastStartIssuedAt = -1f;
            private static float lobbyOpenedAt = -1f;
            private static float loadWaitStartedAt = -1f;
            private static float lastNotificationAt = -1f;
            private static int lobbyGameId = -1;
            private static int lastCountdownNotice = -1;

            public static void Tick()
            {
                float now = Time.unscaledTime;
                if (now < nextTickAt) return;
                nextTickAt = now + TickIntervalSeconds;

                if (!IsEnabled)
                {
                    ResetLobbyFlow(true);
                    SetState(AutoHostState.Disabled, "Выключен");
                    return;
                }

                InnerNetClient client = TryGetClient();
                if (client == null)
                {
                    ResetLobbyFlow(false);
                    SetState(AutoHostState.Idle, "Клиент недоступен");
                    return;
                }

                if (!client.AmHost)
                {
                    ResetLobbyFlow(false);
                    SetState(AutoHostState.Idle, "Ожидаю хост-контекст");
                    return;
                }

                if (IsEndGameScreen())
                {
                    ResetLobbyFlow(false);
                    SetState(ShouldReturnAfterMatch ? AutoHostState.Returning : AutoHostState.InGame,
                        ShouldReturnAfterMatch ? "Возврат в лобби" : "Матч завершен");
                    return;
                }

                if (IsInMatch())
                {
                    ResetLobbyFlow(true);
                    SetState(AutoHostState.InGame, "Матч идет");
                    return;
                }

                if (LobbyBehaviour.Instance == null)
                {
                    ResetLobbyFlow(false);
                    lobbyOpenedAt = -1f;
                    lobbyGameId = -1;
                    SetState(AutoHostState.Idle, "Вне лобби");
                    return;
                }

                TrackLobby(client, now);
                TickHostedLobby(client, now);
            }

            public static AutoHostStatusSnapshot GetStatusSnapshot()
            {
                AutoHostStatusSnapshot snapshot = new AutoHostStatusSnapshot
                {
                    Enabled = IsEnabled,
                    State = FormatState(state),
                    LastReason = lastReason ?? string.Empty,
                    RequiredPlayers = RequiredPlayers,
                    CountdownRemainingSeconds = CountdownRemaining,
                    BackoffRemainingSeconds = BackoffRemaining,
                    LobbyAgeSeconds = lobbyOpenedAt > 0f ? Mathf.Max(0f, Time.unscaledTime - lobbyOpenedAt) : 0f,
                    LobbyLifeRemainingSeconds = LobbyLifeRemaining,
                    AutoReturnAfterMatch = ShouldReturnAfterMatch,
                    ForceLastMinute = ForceLastMinuteEnabled,
                    StartMode = ElysiumModMenuGUI.AutoHostInstantStart ? "Мгновенный" : "Обычный",
                    EffectiveStartDelaySeconds = EffectiveStartDelay(0),
                    WarmupRemainingSeconds = WarmupRemaining,
                    LoadGraceRemainingSeconds = LoadGraceRemaining,
                };
                InnerNetClient client = TryGetClient();
                if (client != null)
                {
                    snapshot.IsHost = client.AmHost;
                    snapshot.IsLobby = LobbyBehaviour.Instance != null;
                    snapshot.IsInGame = IsInMatch();
                    snapshot.ConnectedPlayers = CountLobbyPlayers(client, out int readyPlayers, out _);
                    snapshot.ReadyPlayers = readyPlayers;
                    snapshot.WaitingForLoadedPlayers = snapshot.ConnectedPlayers > snapshot.ReadyPlayers;
                    snapshot.FastStartActive = IsFastStartActive(snapshot.ConnectedPlayers);
                    snapshot.ForceStartActive = ShouldForceStart(snapshot.ConnectedPlayers, out _);
                    snapshot.EffectiveStartDelaySeconds = EffectiveStartDelay(snapshot.ConnectedPlayers);
                }
                return snapshot;
            }

            public static void ResetTransientState()
            {
                nextTickAt = 0f;
                ResetLobbyFlow(true);
                SetState(IsEnabled ? AutoHostState.Idle : AutoHostState.Disabled, IsEnabled ? "Сброшен" : "Выключен");
            }

            public static string TryStartNow()
            {
                if (!IsEnabled) return "Автохост выключен.";
                InnerNetClient client = TryGetClient();
                if (client == null || !client.AmHost) return "Ручной старт доступен только хосту.";
                if (LobbyBehaviour.Instance == null) return "Ручной старт доступен только в лобби.";
                GameStartManager manager = TryGetGameStartManager();
                if (manager == null) return "Кнопка старта не найдена.";

                if (!TryConfiguredStart(manager))
                {
                    EnterBackoff("Ручной старт отклонен");
                    return "Старт не сработал.";
                }
                lastStartIssuedAt = Time.unscaledTime;
                countdownStartedAt = -1f;
                activeCountdownDelay = -1f;
                backoffUntil = -1f;
                SetState(AutoHostState.Starting, "Ручной старт");
                Notify("Автохост", "Матч запускается вручную.");
                return "Старт отправлен.";
            }

            private static void TickHostedLobby(InnerNetClient client, float now)
            {
                int connectedPlayers = CountLobbyPlayers(client, out int readyPlayers, out string loadingName);
                bool forceStart = ShouldForceStart(connectedPlayers, out string forceReason);
                float warmupRemaining = WarmupRemaining;

                if (!forceStart && warmupRemaining > 0.05f)
                {
                    countdownStartedAt = -1f;
                    activeCountdownDelay = -1f;
                    lastStartIssuedAt = -1f;
                    lastCountdownNotice = -1;
                    SetState(AutoHostState.Warmup, $"Прогрев лобби {Mathf.CeilToInt(warmupRemaining)}с");
                    return;
                }

                bool waitingForLoad = ElysiumModMenuGUI.AutoHostWaitLoadedPlayers && connectedPlayers > readyPlayers;
                if (waitingForLoad && !forceStart && !CanBypassLoadWait(now, readyPlayers, connectedPlayers, loadingName))
                {
                    countdownStartedAt = -1f;
                    activeCountdownDelay = -1f;
                    lastStartIssuedAt = -1f;
                    lastCountdownNotice = -1;
                    SetState(AutoHostState.WaitingLoad, $"Ожидаю прогрузку {readyPlayers}/{connectedPlayers}: {loadingName}");
                    return;
                }
                if (!waitingForLoad) loadWaitStartedAt = -1f;

                if (lastStartIssuedAt > 0f)
                {
                    if (now - lastStartIssuedAt < StartRequestGraceSeconds)
                    {
                        SetState(AutoHostState.Starting, "Старт отправлен");
                        return;
                    }
                    lastStartIssuedAt = -1f;
                    EnterBackoff("Старт не подтвердился");
                    return;
                }

                if (backoffUntil > now)
                {
                    SetState(AutoHostState.Backoff, "Пауза после попытки");
                    return;
                }

                int requiredPlayers = RequiredPlayers;
                bool enoughPlayers = ElysiumModMenuGUI.AutoHostWaitLoadedPlayers ? readyPlayers >= requiredPlayers : connectedPlayers >= requiredPlayers;
                bool continueBelowMin = !ElysiumModMenuGUI.AutoHostCancelBelowMin && countdownStartedAt >= 0f && connectedPlayers >= 2;

                if (!forceStart && !enoughPlayers && !continueBelowMin)
                {
                    if (countdownStartedAt >= 0f)
                        Notify("Автохост", "Отсчет отменен: игроков стало меньше минимума.");
                    countdownStartedAt = -1f;
                    activeCountdownDelay = -1f;
                    lastCountdownNotice = -1;
                    SetState(AutoHostState.WaitingPlayers, $"Игроки {connectedPlayers}/{requiredPlayers}");
                    return;
                }

                float delay = EffectiveStartDelay(connectedPlayers);
                if (!forceStart && countdownStartedAt < 0f)
                {
                    countdownStartedAt = now;
                    activeCountdownDelay = delay;
                    lastCountdownNotice = -1;
                    SetState(AutoHostState.Countdown, IsFastStartActive(connectedPlayers) ? "Быстрый старт" : "Минимум игроков набран");
                    Notify("Автохост", $"Старт через {Mathf.CeilToInt(delay)} с.");
                }

                if (!forceStart && now - countdownStartedAt < delay)
                {
                    AnnounceCountdown(delay - (now - countdownStartedAt));
                    SetState(AutoHostState.Countdown, "Отсчет");
                    return;
                }

                GameStartManager manager = TryGetGameStartManager();
                if (manager == null)
                {
                    EnterBackoff("Кнопка старта не найдена");
                    return;
                }
                if (!TryConfiguredStart(manager))
                {
                    EnterBackoff(forceStart ? "Форс-старт отклонен" : "Старт отклонен");
                    return;
                }

                countdownStartedAt = -1f;
                activeCountdownDelay = -1f;
                backoffUntil = -1f;
                lastStartIssuedAt = now;
                lastCountdownNotice = -1;
                SetState(AutoHostState.Starting, forceStart ? forceReason : "Старт матча");
                Notify("Автохост", forceStart ? forceReason : "Минимум набран, запускаю матч.");
            }

            private static void TrackLobby(InnerNetClient client, float now)
            {
                int gameId;
                try { gameId = client.GameId; } catch { gameId = 0; }
                if (lobbyOpenedAt >= 0f && lobbyGameId == gameId) return;
                lobbyOpenedAt = now;
                lobbyGameId = gameId;
                ResetLobbyFlow(true);
                SetState(AutoHostState.WaitingPlayers, "Новое лобби");
            }

            private static void AnnounceCountdown(float remaining)
            {
                int whole = Mathf.CeilToInt(Mathf.Max(0f, remaining));
                if (whole == lastCountdownNotice) return;
                if (whole == 60 || whole == 30 || whole == 15 || whole == 10 || whole == 5 || whole == 3 || whole == 2 || whole == 1)
                {
                    lastCountdownNotice = whole;
                    Notify("Автохост", $"Старт через {whole} с.");
                }
            }

            private static bool TryConfiguredStart(GameStartManager manager)
            {
                if (manager == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || LobbyBehaviour.Instance == null)
                    return false;
                try
                {
                    manager.MinPlayers = 1;
                    if (ElysiumModMenuGUI.AutoHostInstantStart)
                    {
                        manager.startState = GameStartManager.StartingStates.Countdown;
                        manager.countDownTimer = 0f;
                        return true;
                    }
                    manager.BeginGame();
                    return true;
                }
                catch { return false; }
            }

            private static void EnterBackoff(string reason)
            {
                countdownStartedAt = -1f;
                activeCountdownDelay = -1f;
                lastStartIssuedAt = -1f;
                loadWaitStartedAt = -1f;
                lastCountdownNotice = -1;
                backoffUntil = Time.unscaledTime + BackoffSeconds;
                SetState(AutoHostState.Backoff, reason);
                Notify("Автохост: пауза", reason);
            }

            private static void ResetLobbyFlow(bool clearBackoff)
            {
                countdownStartedAt = -1f;
                lastStartIssuedAt = -1f;
                lastCountdownNotice = -1;
                if (clearBackoff) backoffUntil = -1f;
            }

            private static void SetState(AutoHostState nextState, string reason)
            {
                if (!string.IsNullOrWhiteSpace(reason)) lastReason = reason.Trim();
                state = nextState;
            }

            private static int CountLobbyPlayers(InnerNetClient client, out int readyPlayers, out string loadingName)
            {
                readyPlayers = 0;
                loadingName = "игрок";
                if (client == null || client.allClients == null) return 0;

                int connected = 0;
                try
                {
                    var cursor = client.allClients.GetEnumerator();
                    while (cursor.MoveNext())
                    {
                        ClientData data = cursor.Current;
                        if (data == null || data.Id < 0) continue;
                        if (IsDisconnected(data)) continue;
                        connected++;
                        if (IsReady(data)) readyPlayers++;
                        else loadingName = CleanName(data.PlayerName);
                    }
                }
                catch { return CountReadyPlayerControls(out readyPlayers); }
                return connected;
            }

            private static int CountReadyPlayerControls(out int readyPlayers)
            {
                readyPlayers = 0;
                try
                {
                    if (PlayerControl.AllPlayerControls == null) return 0;
                    int count = 0;
                    var cursor = PlayerControl.AllPlayerControls.GetEnumerator();
                    while (cursor.MoveNext())
                    {
                        PlayerControl player = cursor.Current;
                        if (player == null || player.Data == null || player.Data.Disconnected || player.PlayerId >= 100) continue;
                        count++;
                        readyPlayers++;
                    }
                    return count;
                }
                catch { return 0; }
            }

            private static bool IsReady(ClientData data)
            {
                try
                {
                    PlayerControl character = data.Character;
                    return character != null && character.Data != null && !character.Data.Disconnected && character.PlayerId < 100;
                }
                catch { return false; }
            }

            private static bool IsDisconnected(ClientData data)
            {
                try { return data.Character != null && data.Character.Data != null && data.Character.Data.Disconnected; }
                catch { return false; }
            }

            private static GameStartManager TryGetGameStartManager()
            {
                try { if (DestroyableSingleton<GameStartManager>.InstanceExists) return DestroyableSingleton<GameStartManager>.Instance; } catch { }
                try { return UnityEngine.Object.FindObjectOfType<GameStartManager>(); } catch { return null; }
            }

            private static InnerNetClient TryGetClient()
            {
                try { return AmongUsClient.Instance == null ? null : (InnerNetClient)AmongUsClient.Instance; } catch { return null; }
            }

            private static bool CanBypassLoadWait(float now, int readyPlayers, int connectedPlayers, string loadingName)
            {
                if (readyPlayers < RequiredPlayers) { loadWaitStartedAt = -1f; return false; }
                int grace = Mathf.Clamp((int)ElysiumModMenuGUI.AutoHostLoadGraceSeconds, 0, 90);
                if (grace <= 0) { loadWaitStartedAt = -1f; return false; }
                if (loadWaitStartedAt < 0f) loadWaitStartedAt = now;
                if (now - loadWaitStartedAt < grace)
                {
                    SetState(AutoHostState.WaitingLoad, $"Жду прогрузку {readyPlayers}/{connectedPlayers}: {loadingName}");
                    return false;
                }
                SetState(AutoHostState.Countdown, "Прогрузка задержалась, старт по готовым");
                return true;
            }

            private static bool ShouldForceStart(int connectedPlayers, out string reason)
            {
                int minPlayers = ForceMinPlayers;
                if (ForceLastMinuteEnabled && connectedPlayers >= minPlayers && LobbyLifeRemaining >= 0f && LobbyLifeRemaining <= LastMinuteStartSeconds)
                {
                    reason = "Форс-старт: лобби скоро закроется";
                    return true;
                }
                int forceAfterMinutes = Mathf.Clamp(ElysiumModMenuGUI.AutoHostForceAfterMinutes, 0, 10);
                if (forceAfterMinutes > 0 && connectedPlayers >= minPlayers && lobbyOpenedAt > 0f && Time.unscaledTime - lobbyOpenedAt >= forceAfterMinutes * 60f)
                {
                    reason = $"Форс-старт: ожидание {forceAfterMinutes} мин";
                    return true;
                }
                reason = string.Empty;
                return false;
            }

            private static bool IsFastStartActive(int connectedPlayers)
            {
                int threshold = Mathf.Clamp(ElysiumModMenuGUI.AutoHostFastStartPlayers, 0, 15);
                return threshold > 0 && connectedPlayers >= threshold;
            }

            private static float EffectiveStartDelay(int connectedPlayers)
            {
                float delay = StartDelaySeconds;
                if (IsFastStartActive(connectedPlayers))
                    delay = Mathf.Min(delay, Mathf.Clamp(ElysiumModMenuGUI.AutoHostFastStartDelaySeconds, 0, 60));
                return delay;
            }

            private static bool IsInMatch() => ShipStatus.Instance != null && LobbyBehaviour.Instance == null && !IsEndGameScreen();

            private static bool IsEndGameScreen()
            {
                try { return UnityEngine.Object.FindObjectOfType<EndGameManager>() != null; } catch { return false; }
            }

            private static void Notify(string title, string detail)
            {
                if (!ElysiumModMenuGUI.AutoHostNotifications) return;
                float now = Time.unscaledTime;
                if (lastNotificationAt > 0f && now - lastNotificationAt < NotificationCooldownSeconds) return;
                lastNotificationAt = now;
                ElysiumModMenuGUI.ShowNotification($"<color=#FF00FF>[{title}]</color> {detail}");
            }

            private static string FormatState(AutoHostState value)
            {
                return value switch
                {
                    AutoHostState.Disabled => L("Disabled", "Выключен"),
                    AutoHostState.Idle => L("Idle", "Ожидание"),
                    AutoHostState.Warmup => L("Warmup", "Прогрев"),
                    AutoHostState.WaitingPlayers => L("Waiting for players", "Ждет игроков"),
                    AutoHostState.WaitingLoad => L("Waiting for load", "Ждет прогрузку"),
                    AutoHostState.Countdown => L("Countdown", "Отсчет"),
                    AutoHostState.Starting => L("Starting", "Запуск"),
                    AutoHostState.InGame => L("In Game", "В игре"),
                    AutoHostState.Returning => L("Returning", "Возврат"),
                    AutoHostState.Backoff => L("Backoff", "Пауза"),
                    _ => value.ToString(),
                };
            }

            private static string CleanName(string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return "игрок";
                string clean = value.Replace("\r", " ").Replace("\n", " ").Trim();
                return clean.Length <= 18 ? clean : clean.Substring(0, 17) + "...";
            }

            public static bool IsEnabled => ElysiumModMenuGUI.AutoHostEnabled;
            public static bool ShouldReturnAfterMatch => IsEnabled && ElysiumModMenuGUI.AutoReturnLobbyAfterMatch;
            private static bool ForceLastMinuteEnabled => ElysiumModMenuGUI.AutoHostForceLastMinute;
            private static int RequiredPlayers => Mathf.Clamp(ElysiumModMenuGUI.AutoHostMinPlayers, 1, 15);
            private static int ForceMinPlayers => Mathf.Clamp(ElysiumModMenuGUI.AutoHostForceMinPlayers, 1, 15);
            private static float StartDelaySeconds => Mathf.Clamp(ElysiumModMenuGUI.AutoHostStartDelaySeconds, 0f, 180f);
            private static float BackoffSeconds => Mathf.Clamp(ElysiumModMenuGUI.AutoHostBackoffSeconds, 2f, 60f);
            private static float CountdownRemaining => countdownStartedAt < 0f ? 0f : Mathf.Clamp((activeCountdownDelay >= 0f ? activeCountdownDelay : StartDelaySeconds) - (Time.unscaledTime - countdownStartedAt), 0f, StartDelaySeconds);
            private static float BackoffRemaining => backoffUntil < 0f ? 0f : Mathf.Clamp(backoffUntil - Time.unscaledTime, 0f, BackoffSeconds);
            private static float LobbyLifeRemaining => lobbyOpenedAt < 0f ? -1f : Mathf.Clamp(LobbyLifetimeSeconds - (Time.unscaledTime - lobbyOpenedAt), 0f, LobbyLifetimeSeconds);
            private static float WarmupRemaining => lobbyOpenedAt < 0f ? 0f : Mathf.Clamp(ElysiumModMenuGUI.AutoHostWarmupSeconds - (Time.unscaledTime - lobbyOpenedAt), 0f, 120f);
            private static float LoadGraceRemaining => loadWaitStartedAt < 0f || ElysiumModMenuGUI.AutoHostLoadGraceSeconds <= 0 ? 0f : Mathf.Clamp(ElysiumModMenuGUI.AutoHostLoadGraceSeconds - (Time.unscaledTime - loadWaitStartedAt), 0f, 90f);
        }
        private int currentVisualsSubTab = 0;
        private string[] visualsSubTabs => new string[] { L("IN-GAME", "В ИГРЕ"), L("OUTFITS", "ОДЕЖДА") };
        private int currentSelfSubTab = 0;
        private string[] selfSubTabs = { "SPOOF", "MOVEMENT", "ROLES", "CHAT" };
        private string[] selfOtherTabs = { "MOVEMENT", "ROLES", "CHAT" };
        public static bool fakeStartCounterTroll = false;
        public static bool fakeStartCounterCustom = false;
        public static string fakeStartInput = "69";
        public static bool isEditingFakeStart = false;
        public static float customStartTimer = -1f;

        public static bool localRainbow = false;
        public static List<byte> rainbowPlayers = new List<byte>();
        public static float colorTimer = 0f;
        public static byte currentColorId = 0;
        private Vector2 playerListScrollPos = Vector2.zero;
        private Vector2 playerActionScrollPos = Vector2.zero;
        private byte selectedAntiCheatPlayerId = 255;

        public static string spoofLevelString = "100";
        public static string customNameInput = "хыхых";
        public static string spoofFriendCodeInput = "crewmate01";
        public static string localFriendCodeInput = "Steam#Local";
        public static string ghostChatColorHex = "#D7B8FF";
        public static bool isEditingLevel = false;
        public static bool isEditingName = false;
        public static bool isEditingFriendCode = false;
        public static bool isEditingLocalFriendCode = false;
        public static bool isEditingGhostChatColor = false;
        private static bool discordLaunchStatusSent = false;
        private static bool discordInvalidWebhookNotified = false;
        private static float discordLaunchStatusNextTryAt = 0f;
        private static readonly string relaySessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
        private static readonly Dictionary<string, int> watchedLogLineCounts = new Dictionary<string, int>();
        private static readonly DateTime logMonitorStartedUtc = DateTime.UtcNow;
        private static readonly object anomalyLogMonitorLock = new object();
        private static System.Threading.Timer anomalyLogMonitorTimer;
        private static string anomalyReportDetailsCache = $"sessionId={relaySessionId}\nclientId=Unknown\nnetworkMode=Unknown\nhost=Unknown\nplatform=Unknown\ninGame=Unknown";
        private static float logMonitorNextScanAt = 0f;
        private static float logBurstWindowStartedAt = -1f;
        private static float logBurstCooldownUntil = 0f;
        private static int logBurstLineCount = 0;
        private static bool anomalyLogWatchNotified = false;
        private const int LogBurstLineThreshold = 15;
        private const int InitialLogTailLineLimit = 120;
        private const float LogBurstWindowSeconds = 5f;
        private const float LogBurstScanIntervalSeconds = 1f;
        private const float LogBurstAlertCooldownSeconds = 60f;
        public static bool enableLocalNameSpoof = false;
        public static bool enableLocalFriendCodeSpoof = false;
        public static bool enableFriendCodeSpoof = false;
        public static bool enablePlatformSpoof = true;
        public static bool enableAnomalyLogReports = true;
        public static bool showEspFriendCode = true;
        public static bool allowDuplicateColors = false;
        public static bool autoGhostAfterStart = false;
        public static bool autoBanPlatformSpoof = false;
        public static bool banCustomPlatformsFromTxt = false;
        public static bool autoKickLowLevelEnabled = false;
        public static int autoKickMinLevel = 200;
        public static int fpsLimit = 60;
        public static int chatHistoryLimit = 20;
        public static int currentPlatformIndex = 1;
        private static float localNameRefreshTimer = 0f;
        private static float localFriendCodeRefreshTimer = 0f;
        private static float platformBanScanTimer = 0f;
        private static int lastAppliedFpsLimit = -1;
        private static bool autoGhostAppliedThisGame = false;
        private static bool wasGameStartedForAutoGhost = false;
        private static string originalLocalFriendCode = null;
        private static string originalLocalName = null;
        private static float friendEspIgnoreNextLoadAt = 0f;
        private static readonly HashSet<string> friendEspIgnoreTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static string platformBanListPath = "";
        private static float platformBanListNextLoadAt = 0f;
        private static readonly HashSet<string> customPlatformBanTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<int> platformSpoofPunishedOwners = new HashSet<int>();
        private float lowLevelKickScanTimer = 0f;
        private static readonly HashSet<int> lowLevelKickPunishedOwners = new HashSet<int>();
        private const int FavoriteOutfitSlotCount = 4;
        private static readonly string[] favoriteOutfitSlots = new string[FavoriteOutfitSlotCount];
        private float brokenFcScanTimer = 0f;
        private static readonly HashSet<int> brokenFcPunishedOwners = new HashSet<int>();

        public static string[] platformNames = {
            "Epic", "Steam", "Mac", "Microsoft", "Itch", "iOS",
            "Android", "Switch", "Xbox", "PlayStation", "Starlight"
        };

        public static Platforms[] platformValues = {
            (Platforms)1,
            (Platforms)2,
            (Platforms)3,
            (Platforms)4,
            (Platforms)5,
            (Platforms)6,
            (Platforms)7,
            (Platforms)8,
            (Platforms)9,
            (Platforms)10,
            (Platforms)112
        };

        public static bool unlockFeatures = true;



        public class ElysiumNotification
        {
            public string title;
            public string message;
            public float ttl;
            public float lifetime;
            public bool HasExpired => lifetime > ttl;

            public ElysiumNotification(string title, string message, float ttl)
            {
                this.title = title;
                this.message = message;
                this.ttl = ttl;
                this.lifetime = 0f;
            }
        }
        public static List<string> bannedEntries = new List<string>();
        public static string banListPath = "";
        private Vector2 banListScroll = Vector2.zero;
        public static bool autoBanEnabled = true;
        public static string banInput = "";
        public static bool isEditingBan = false;
        public static List<string> botBannedEntries = new List<string>();
        public static string botBanListPath = "";
        public static bool banBotsEnabled = false;
        public static readonly string[] botNameTokens = new string[] { "UCbot", "bot", "бот", "Ucбот", "sixseven", "лут", "67" };

        public static void LoadBanList()
        {
            try
            {
                banListPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "ElysiumModMenuBanList.txt");
                if (!System.IO.File.Exists(banListPath))
                {
                    System.IO.File.Create(banListPath).Dispose();
                }
                bannedEntries = new List<string>(System.IO.File.ReadAllLines(banListPath));
            }
            catch { }
        }

        public static void AddToBanList(string friendCode, string puid, string name, string reason)
        {
            try
            {
                if (string.IsNullOrEmpty(friendCode)) return;

                bool alreadyBanned = false;
                string fcLower = friendCode.Trim().ToLower();

                foreach (var e in bannedEntries)
                {
                    string[] parts = e.Split('|');
                    if (parts.Length > 0 && parts[0].Trim().ToLower() == fcLower)
                    {
                        alreadyBanned = true;
                        break;
                    }
                }

                if (!alreadyBanned)
                {
                    string date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    string entry = $"{friendCode}|{puid}|{name}|{date}|{reason}";
                    bannedEntries.Add(entry);
                    System.IO.File.AppendAllText(banListPath, entry + Environment.NewLine);
                }
            }
            catch { }
        }

        public static void RemoveFromBanList(string entry)
        {
            try
            {
                bannedEntries.Remove(entry);
                System.IO.File.WriteAllLines(banListPath, bannedEntries.ToArray());
            }
            catch { }
        }

        public static void LoadBotBanList()
        {
            try
            {
                botBanListPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "ElysiumBotBanList.txt");
                if (!System.IO.File.Exists(botBanListPath))
                {
                    System.IO.File.Create(botBanListPath).Dispose();
                }
                botBannedEntries = new List<string>(System.IO.File.ReadAllLines(botBanListPath));
            }
            catch { }
        }

        public static void AddToBotBanList(string friendCode, string puid, string name, string reason)
        {
            try
            {
                string fc = string.IsNullOrWhiteSpace(friendCode) ? "Unknown" : friendCode.Trim();
                string nm = string.IsNullOrWhiteSpace(name) ? "Unknown" : name.Trim();
                string fcLower = fc.ToLower();
                string nameLower = nm.ToLower();

                bool already = false;
                foreach (var e in botBannedEntries)
                {
                    if (string.IsNullOrWhiteSpace(e) || e.TrimStart().StartsWith("#")) continue;
                    string[] parts = e.Split('|');
                    if (parts.Length > 0 && fcLower != "unknown" && parts[0].Trim().ToLower() == fcLower) { already = true; break; }
                    if (fcLower == "unknown" && parts.Length >= 3 && parts[2].Trim().ToLower() == nameLower) { already = true; break; }
                }

                if (!already)
                {
                    if (string.IsNullOrEmpty(botBanListPath)) LoadBotBanList();
                    string date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    string entry = $"{fc}|{puid}|{nm}|{date}|{reason}";
                    botBannedEntries.Add(entry);
                    System.IO.File.AppendAllText(botBanListPath, entry + Environment.NewLine);
                }
            }
            catch { }
        }

        public static bool IsBotName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return false;
                string n = name.Trim().ToLowerInvariant();

                foreach (var token in botNameTokens)
                {
                    if (string.IsNullOrWhiteSpace(token)) continue;
                    if (n.Contains(token.Trim().ToLowerInvariant())) return true;
                }

                foreach (var e in botBannedEntries)
                {
                    if (string.IsNullOrWhiteSpace(e) || e.TrimStart().StartsWith("#")) continue;
                    string[] parts = e.Split('|');
                    string nick = parts.Length >= 3 ? parts[2].Trim().ToLowerInvariant() : e.Trim().ToLowerInvariant();
                    if (!string.IsNullOrWhiteSpace(nick) && nick != "unknown" && n.Contains(nick)) return true;
                }
            }
            catch { }
            return false;
        }

        public static bool IsBotBannedFc(string fc)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fc)) return false;
                string f = fc.Trim().ToLowerInvariant();
                foreach (var e in botBannedEntries)
                {
                    if (string.IsNullOrWhiteSpace(e) || e.TrimStart().StartsWith("#")) continue;
                    string[] parts = e.Split('|');
                    if (parts.Length > 0 && parts[0].Trim().ToLowerInvariant() == f) return true;
                }
            }
            catch { }
            return false;
        }

        public static bool killReach = false, killAnyone = false;
        public static bool endlessSsDuration = false, noVitalsCooldown = false;
        public static bool endlessBattery = false, endlessVentTime = false, noVentCooldown = false, noMapCooldowns = false;
        public static bool reactorSab = false, oxygenSab = false, commsSab = false, elecSab = false;
        public static bool autoOpenDoors = false;
        public static bool moonWalk = false;
        public static bool SeePlayersInVent = false;
        public static bool seeGhosts = false;
        public static bool seeRoles = false;
        public static bool showPlayerInfo = false;
        public static bool revealMeetingRoles = false;
        public static bool showTracers = false;
        public static bool fullBright = false;
        public static bool seeProtections = false;
        public static bool seeKillCooldown = false;
        public static bool extendedLobby = false;
        public static bool DarkModeEnabled = true;
        public static bool enableChatDarkMode = true;
        public static float customLightRadius = 5f;
        private static Dictionary<byte, float> lastKillTimestamps = new Dictionary<byte, float>();

        public static bool alwaysChat = false;
        public static bool readGhostChat = false;
        public static bool enableSpellCheck = false;

        public static bool neverEndGame = false;
        public static void ShowNotification(string text)
        {
            string title = "ElysiumModMenu";
            string msg = text;

            if (text.Contains("[") && text.Contains("]"))
            {
                int start = text.IndexOf("[");
                int end = text.IndexOf("]");
                if (end > start)
                {
                    string rawTitle = text.Substring(start + 1, end - start - 1);
                    title = System.Text.RegularExpressions.Regex.Replace(rawTitle, "<.*?>", string.Empty);
                    msg = System.Text.RegularExpressions.Regex.Replace(msg, @"(<color=#[^>]+>)?\[.*?\](</color>)?\s*", "");
                }
            }
            SendNotification(title, msg.Trim(), 3.5f);
        }

        public static void SendNotification(string title, string message, float ttl = 3.5f)
        {
            if (!EnableCustomNotifs) return;
            screenNotifications.Add(new ElysiumNotification(title, message, ttl));
        }



        public static HashSet<byte> forcedImpostors = new HashSet<byte>();
        public static Dictionary<byte, RoleTypes> forcedPreGameRoles = new Dictionary<byte, RoleTypes>();
        public static bool enablePreGameRoleForce = false;
        private Vector2 preRolesListScrollPos = Vector2.zero;
        private Vector2 preRolesActionScrollPos = Vector2.zero;
        private byte selectedPreRoleId = 255;
        public static List<PlayerControl> lockedPlayersList = new List<PlayerControl>();
        public static bool LogAllRPCs = true;
        public static bool blockRainbowChat = true;
        public static bool blockFortegreenChat = true;

        public static bool EnableCustomNotifs = true;
        public static Vector2 notificationBoxSize = new Vector2(260f, 65f);
        public static List<ElysiumNotification> screenNotifications = new List<ElysiumNotification>();


        private bool stylesInited = false;
        private GUIStyle windowStyle, btnStyle, activeTabStyle, headerStyle, boxStyle;
        private GUIStyle sidebarStyle, sidebarBtnStyle, activeSidebarBtnStyle, titleStyle;
        private GUIStyle toggleOnStyle, toggleOffStyle, toggleLabelStyle, safeLineStyle;
        private GUIStyle sliderStyle, sliderThumbStyle, subTabStyle, activeSubTabStyle;
        public GUIStyle inputBlockStyle;
        private Texture2D texWindowBg, texBoxBg, texBtnBg, texAccent, texSidebarBg;
        private Texture2D texToggleOff, texToggleOn, texSliderBg, texSliderThumb, texInputBg, texColorBtn, texScrollThumb;
        private Texture2D texMenuCard;
        private GUIStyle menuCardStyle, menuSectionTitleStyle, menuDescStyle, menuBadgeStyle, menuAccentBarStyle, menuSwatchStyle;
        private void DrawHostOnlyTab()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < hostOnlySubTabs.Length; i++)
            {
                if (GUILayout.Button(hostOnlySubTabs[i], currentHostOnlySubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                {
                    currentHostOnlySubTab = i;
                    scrollPosition = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (currentHostOnlySubTab == 0) DrawLobbyControls();
            else if (currentHostOnlySubTab == 1) DrawPlayersRoles();
            else if (currentHostOnlySubTab == 2) DrawAntiCheatTab();
            else if (currentHostOnlySubTab == 3) DrawAutoHostTab();
            else if (currentHostOnlySubTab == 4) DrawMapsTab();
        }


        private void DrawVisualsInGame()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("VISIBILITY", "ВИДИМОСТЬ"));

            GUILayout.BeginHorizontal();
            seeGhosts = DrawToggle(seeGhosts, L("See Ghosts", "Видеть призраков"), 210);
            seeRoles = DrawToggle(seeRoles, L("See Roles", "Видеть роли"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            showPlayerInfo = DrawToggle(showPlayerInfo, L("Show Player Info (ESP)", "Инфо об игроке (ESP)"), 210);
            revealMeetingRoles = DrawToggle(revealMeetingRoles, L("Reveal Roles (Meeting)", "Показывать роли на собрании"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            showEspFriendCode = DrawToggle(showEspFriendCode, L("Show FC In ESP", "FriendCode в ESP"), 210);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            removePenalty = DrawToggle(removePenalty, L("No Disconnect Penalty", "Нет штрафа за выход"), 210);
            alwaysShowLobbyTimer = DrawToggle(alwaysShowLobbyTimer, L("Always Show Lobby Timer", "Всегда показывать таймер лобби"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            showTracers = DrawToggle(showTracers, L("Show Tracers", "Показывать линии (Tracer)"), 210);
            fullBright = DrawToggle(fullBright, L("Full Bright (No Shadows)", "Полная яркость (Нет теней)"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            alwaysChat = DrawToggle(alwaysChat, L("Always Show Chat", "Всегда показывать чат"), 210);
            readGhostChat = DrawToggle(readGhostChat, L("Read Ghost Chat", "Читать чат призраков"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            freecam = DrawToggle(freecam, L("Freecam (WASD)", "Свободная камера (WASD)"), 210);
            cameraZoom = DrawToggle(cameraZoom, L("Camera Zoom (Scroll)", "Зум камеры (Колесико)"), 210);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            RevealVotesEnabled = DrawToggle(RevealVotesEnabled, L("Reveal Votes (Meeting)", "Показывать голоса (Собрание)"), 210);
            SeePlayersInVent = DrawToggle(SeePlayersInVent, L("See Players In Vents", "Видеть игроков в люках"), 210);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            seeProtections = DrawToggle(seeProtections, L("See Protections", "Видеть щиты"), 210);
            seeKillCooldown = DrawToggle(seeKillCooldown, L("See Kill Cooldown", "Видеть килл-кд"), 210);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }


        public static bool enableLocalPetSpamDrop = true;
        public static bool enableHostPetSpamBan = false;

        public static bool enableMalformedPacketGuard = true;
        public static bool banMalformedPacketSender = false;
        public static bool enableQuickChatEmptyGuard = true;
        public static bool banQuickChatEmptySpammer = true;
        public static bool enableUnownedSpawnGuard = true;

        // HostGuard's adaptation (thanks to one silly guy :p)

        static class HazelThings
        {
            static bool isShouldProtect => PlayerControl.LocalPlayer && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay;

            static bool isCooling;
            static void GetHazelError(string errorType)
            {
                if (!isCooling)
                {
                    isCooling = true;
                    DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage($"Got Hazel error - <color=#ffff00>{errorType}</color>");
                    if (banMalformedPacketSender)
                        {
                            KeyValuePair<int, float> keyValuePair = HandleMessage.LastJoin.OrderBy((KeyValuePair<int, float> pair) => pair.Value).FirstOrDefault();
		                    AmongUsClient.Instance.KickPlayer(keyValuePair.Key, ban: true);
                        }
                    new LateTask(delegate
                    {
                        isCooling = false;
                    }, 10f);
                }
            }
            [HarmonyPatch(typeof(MessageReader), nameof(MessageReader.ReadPackedUInt32))]
            class SafePackedUInt32
            {
                static bool Prefix(MessageReader __instance, ref uint __result)
                {
                    if (__instance.Length <= __instance.Position && enableMalformedPacketGuard && isShouldProtect)
                    {
                        __result = 0;
                        GetHazelError("ReadPackedUInt32");
                        return false;
                    }

                    return true;
                }
            }

            [HarmonyPatch(typeof(MessageReader), nameof(MessageReader.ReadPackedInt32))]
            class SafePackedInt32
            {
                static bool Prefix(MessageReader __instance, ref int __result)
                {
                    if (__instance.Length <= __instance.Position && enableMalformedPacketGuard && isShouldProtect)
                    {
                        __result = 0;
                        GetHazelError("ReadPackedInt32");
                        return false;
                    }

                    return true;
                }
            }
        }


        internal class LateTask
        {
            public string name;

            public float timer;

            public System.Action action;

            public static List<LateTask> Tasks = new List<LateTask>();

            public bool Run(float deltaTime)
            {
                timer -= deltaTime;
                if (timer <= 0f)
                {
                    action();
                    return true;
                }
                return false;
            }

            public LateTask(System.Action action, float time, string name = "No Name Task")
            {
                this.action = action;
                timer = time;
                this.name = name;
                Tasks.Add(this);
            }

            public static void Stop(string name)
            {
                Tasks.RemoveAll((LateTask task) => task.name == name);
            }

            public static void Stop(LateTask task)
            {
                Tasks.Remove(task);
            }

            public static void Update(float deltaTime)
            {
                List<LateTask> list = new List<LateTask>();
                for (int i = 0; i < Tasks.Count; i++)
                {
                    LateTask lateTask = Tasks[i];
                    try
                    {
                        if (lateTask.Run(deltaTime))
                        {
                            list.Add(lateTask);
                        }
                    }
                    catch (Exception)
                    {
                        list.Add(lateTask);
                    }
                }
                list.ForEach(delegate (LateTask task)
                {
                    Tasks.Remove(task);
                });
            }
        }

        public class ModPlayer : MonoBehaviour

        {
            public PlayerControl player;

            public float LastTask;

            public float JoinTime;

            public bool NameApply = false;

            private Dictionary<byte, Queue<float>> rpcCallTimestamps = new Dictionary<byte, Queue<float>>();

            private const float DefaultTimeWindow = 1f;

            private const int DefaultRpcLimitPerWindow = 10;

            private bool isMarkedAsSpamRpc;

            private bool isMarkedAsModUser;

            public static ModPlayer LocalPlayer => PlayerControl.LocalPlayer.Mod();

            public static readonly HashSet<byte> normalCustomCallId = new HashSet<byte> { 6, 80, 78, 70, 210, 81, 176, 169 };

            public readonly HashSet<byte> SpammableCrashRpc = new HashSet<byte> { 49, 50, 7, 4, 18, 31 };

            public static readonly HashSet<int> excludedCallIdsForTargetClient = new HashSet<int> { 5, 6, 7, 13, 44, 51, 54, 62, 64, 55 };

            public static readonly HashSet<byte> ImmediatelyRPCs = new HashSet<byte>
    {
        51, 54, 5, 7, 14, 47, 48, 12, 52, 53, 54,
        45, 46, 62, 64, 55, 56, 2, 63, 65, 21, 49
    };

            public static readonly HashSet<int> excludedCallIdsForLobby = new HashSet<int>
    {
        5, 6, 7, 9, 10, 13, 17, 18, 21, 33,
        36, 37, 38, 39, 40, 41, 42, 43, 44, 49, 50,
        60, 61, 80, 78, 70, 210, 81, 176
    };

            public static readonly IReadOnlyDictionary<byte, (short, float)> StandartALotRPCs = new Dictionary<byte, (short, float)>
    {
        {
            31,
            (10, 1f)
        },
        {
            18,
            (25, 1f)
        },
        {
            49,
            (50, 0.1f)
        },
        {
            44,
            (50, 0.1f)
        },
        {
            50,
            (100, 0.1f)
        },
        {
            8,
            (30, 0.1f)
        },
        {
            6,
            (30, 0.1f)
        },
        {
        39,
        (30, 0.1f)
        },
        {
            40,
            (30, 0.1f)
        },
        {
            42,
            (30, 0.1f)
        },
        {
            41,
            (30, 0.1f)
        },
        {
            33,
            (1, 1f)
        },
        {
            54,
            (5, 1f)
        },
        {
            7,
            (100, 0.1f)
        }
    };

            static bool RpcCrash;

            public static readonly HashSet<byte> excludedNumMsgCallIds = new HashSet<byte> { 12, 41, 39, 40, 42, 43, 38, 49 };

            public static readonly HashSet<byte> SusRPCs = new HashSet<byte> { 101, 164, 154, 85, 219, 81, 176, 204, 216, 121, 119, 167 };


            public ModPlayer(IntPtr ptr)
                : base(ptr)
            {
            }

            public void Awake()
            {
                player = this.GetComponent<PlayerControl>();
            }

            public void FixedUpdate()
            {
                JoinTime += Time.fixedDeltaTime;
                LateTask.Update(Time.deltaTime);
            }

            public bool RpcCheck(byte callId, int targetClientId, SendOption sendOption, int numData)
            {
                if (PlayerControl.LocalPlayer == null) return true;
                RpcCalls b = (RpcCalls)callId;
                if (!CheckSpam(callId))
                {
                    return false;
                }

                if (AmongUsClient.Instance.AmHost)
                {
                    if (targetClientId >= 0 && !excludedCallIdsForTargetClient.Contains(callId) && !(player.Data.ClientId == AmongUsClient.Instance.ClientId))
                    {

                        if (!RpcCrash)
                        {
                            new LateTask(delegate
                            {
                               DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage($"<color=#FFFF00>Received Rpc from <b>{player.Data.PlayerName}</b>, {b}\nthat shouldn't be got like that way</color>");
                                if (enablePasosLimit)
                                {
                                    AmongUsClient.Instance.KickPlayer(player.OwnerId, true);
                                }
                            }, 2f);
                            _ = new LateTask(delegate
                            {
                                RpcCrash = false;
                            }, 15f);
                            RpcCrash = true;
                        }
                        return false;

                    }
                    if (AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined && !excludedCallIdsForLobby.Contains(callId) && callId < 66 && player != PlayerControl.LocalPlayer)
                    {
                        if (!RpcCrash)
                        {
                            new LateTask(delegate
                            {
                                DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage($"<color=#FFFF00>Received Rpc in lobby from <b>{player.Data.PlayerName}</b>, <color=#00FFFF>{b}</color>\nthat shouldn't be used there</color>");

                                if (enablePasosLimit)
                                {
                                    AmongUsClient.Instance.KickPlayer(player.OwnerId, true);
                                }
                            }, 2f);
                            _ = new LateTask(delegate
                            {
                                RpcCrash = false;
                            }, 10f);
                            RpcCrash = true;
                        }
                        return false;
                    }
                }
                if (callId > 65)
                {
                    if (isMarkedAsModUser) return false;
                    if (!RpcCrash)
                    {
                        new LateTask(delegate
                        {    
                            isMarkedAsModUser = true;
                        }, 2f);
                        _ = new LateTask(delegate
                        {
                            RpcCrash = false;
                        }, 10f);
                        RpcCrash = true;
                    }
                    return true;
                }
                if (!excludedNumMsgCallIds.Contains(callId) && ((numData > 1 && ImmediatelyRPCs.Contains(callId)) && !AmongUsClient.Instance.AmHost || numData > 25) && player != PlayerControl.LocalPlayer)
                {
                    if (!RpcCrash)
                    {
                        new LateTask(delegate
                        {
                            DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage($"<color=#FFFF00>Received Too many Rpc from <b>{player.name}</b>\nfor this type of Rpc - <color=#00FFFF>{b}</color> </color>");
                        }, 2f);
                        _ = new LateTask(delegate
                        {
                            RpcCrash = false;
                        }, 10f);
                        RpcCrash = true;
                    }
                    return targetClientId <= 0;

                }
                if (sendOption == SendOption.None && callId != 0 && player != PlayerControl.LocalPlayer)
                {
                    if (!RpcCrash && !isMarkedAsModUser)
                    {
                        if (isMarkedAsModUser) return targetClientId <= 0;
                        isMarkedAsModUser = true;
                        _ = new LateTask(delegate
                        {
                           DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage($"<color=#FFFF00>Received SendOption None Rpc (Modded) from <b>{player.name}</b>, <color=#00FFFF>{b}</color></color>");
                        }, 2f);
                        _ = new LateTask(delegate
                        {
                            RpcCrash = false;
                        }, 10f);
                        RpcCrash = true;
                    }
                    return targetClientId <= 0;
                }
                return true;
            }

            public bool CheckSpam(byte callId)
            {

                if (!rpcCallTimestamps.ContainsKey(callId))
                {
                    rpcCallTimestamps[callId] = new Queue<float>();
                }
                Queue<float> queue = rpcCallTimestamps[callId];
                float fixedTime = Time.fixedTime;
                float num = 1f;
                int num2 = 10;
                if (StandartALotRPCs.TryGetValue(callId, out var value))
                {
                    num2 = value.Item1;
                    num = value.Item2;
                }
                while (queue.Count > 0 && queue.Peek() < fixedTime - num)
                {
                    queue.Dequeue();
                }
                queue.Enqueue(fixedTime);
                if ((queue.Count > num2) && (byte)RpcCalls.SnapTo != callId)
                {
                    if (!RpcCrash)
                    {
                        new LateTask(delegate
                        {
                            if (enablePasosLimit)
                            {
                                AmongUsClient.Instance.KickPlayer(player.OwnerId, true);
                            }
                            if (!isMarkedAsSpamRpc && player != PlayerControl.LocalPlayer)
                            {     
                                isMarkedAsSpamRpc = true;
                            }
                            DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage($"<color=#FFFF00>Rpc Spam from <b>{player.name}</b>\nwith <color=#00FFFF>{(RpcCalls)callId}</color></color>");
                        }, 2f);
                        new LateTask(delegate
                        {
                            RpcCrash = false;

                        }, 10f);
                        RpcCrash = true;
                    }
                    return false;
                }
                return true;
            }

            public static bool operator ==(ModPlayer a, PlayerControl b)
            {
                return a.player == b;
            }

            public static bool operator ==(PlayerControl a, ModPlayer b)
            {
                return a == b.player;
            }

            public static bool operator !=(ModPlayer a, PlayerControl b)
            {
                return a.player != b;
            }

            public static bool operator !=(PlayerControl a, ModPlayer b)
            {
                return a != b.player;
            }
        }

        [HarmonyPatch(typeof(InnerNetClient), "HandleMessage")]
        internal class HandleMessage
        {
            public static List<uint> blockedSenders = new();
            public static Dictionary<uint, int> msgCount = new();
            public static float blockedtimer;
            public static float timer;

            public static void HandleTimer()
            {
                GoingTimer();
                resetingDataLimit += Time.deltaTime;
                timer -= Time.deltaTime;
                blockedtimer -= Time.deltaTime;

                if (resetingDataLimit >= 1)
                {
                    resetingDataLimit = 0;
                }

                if (timer <= 0)
                {
                    timer = 1;
                    msgCount.Clear();
                }
                if (blockedtimer <= 0)
                {
                    blockedtimer = 15;
                    blockedSenders.Clear();
                }
            }

            public static bool Crashed;
            public static Dictionary<int, float> LastJoin = new Dictionary<int, float>();

            public static void GoingTimer()
            {
                foreach (var item in LastJoin)
                {
                    LastJoin[item.Key] += Time.deltaTime;
                }
            }

            public static void Crash(string reason)
            {
                if (!Crashed)
                {
                    Debug.LogError("WARNING - " + reason);
                    _ = new LateTask(delegate
                    {
                        DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage("<#ffff00>" + L("Got crash attempt - "  + reason, "Произошла попытка краша - " + reason));
                        if (banMalformedPacketSender)
                        {
                            KeyValuePair<int, float> keyValuePair = HandleMessage.LastJoin.OrderBy((KeyValuePair<int, float> pair) => pair.Value).FirstOrDefault();
		                    AmongUsClient.Instance.KickPlayer(keyValuePair.Key, ban: true);
                        }
                    }, 0.1f);
                    _ = new LateTask(delegate
                    {
                        Crashed = false;
                    }, 10f);
                    Crashed = true;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool CheckDataMessage(MessageReader message, int TargetClientId, SendOption sendOption)
            {
                if (PlayerControl.LocalPlayer == null) return true;
                int num = 0;
                InnerNetObject innerNetObject = default;
                while (message.Position < message.Length)
                {

                    if (num > 75)
                    {
                        Crash("Spam of Data");
                        return false;
                    }

                    MessageReader messageReader = message.ReadMessage();
                    if (messageReader.Tag != 207 && messageReader.Tag > 8 || messageReader.Tag == 3 || messageReader.Tag == 0)
                    {
                        Crash("Bad Tag - " + messageReader.Tag);
                        return false;
                    }
                    if (messageReader.Tag == 1)
                    {
                        uint num2 = messageReader.ReadPackedUInt32();
                        try
                        {
                            if (!AmongUsClient.Instance.allObjects.allObjectsFast.ContainsKey(num2) && !AmongUsClient.Instance.DestroyedObjects.Contains(num2) && AmongUsClient.Instance.AmHost || num2 > AmongUsClient.Instance.NetIdCnt + 30 || num2 == 0)
                            {
                                Crash("Null Data - " + num2);
                                return false;
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        if (messageReader.Tag != 2)
                        {
                            continue;
                        }
                        uint num3 = messageReader.ReadPackedUInt32();
                        byte callId = messageReader.ReadByte();
                        AmongUsClient instance = AmongUsClient.Instance;
                        lock (instance.allObjects)
                        {
                            if (instance.allObjects.allObjectsFast.TryGetValue(num3, out innerNetObject))
                            {
                                if (innerNetObject is PlayerControl pc)
                                {
                                    if (!pc.Mod().RpcCheck(callId, TargetClientId, sendOption, num))
                                    {
                                        return false;
                                    }
                                }
                                else if (innerNetObject is PlayerPhysics playerPhysics)
                                {
                                    if (!playerPhysics.myPlayer.Mod().RpcCheck(callId, TargetClientId, sendOption, num))
                                    {
                                        return false;
                                    }
                                }
                                else if (innerNetObject is CustomNetworkTransform customNetworkTransform && !customNetworkTransform.myPlayer.Mod().RpcCheck(callId, TargetClientId, sendOption, num))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
                return true;
            }

            public static void Dispatcher(InnerNetClient __instance, System.Action action)
            {
                __instance.Dispatcher.Add(action);
            }

            public static void PreDispatcher(InnerNetClient __instance, System.Action action)
            {
                __instance.PreSpawnDispatcher.Add(action); 
            }

            public static bool Prefix(InnerNetClient __instance, MessageReader reader, SendOption sendOption)
            {
                if (enableMalformedPacketGuard == false) return true;

                switch (reader.Tag)
                {
                    case 1:
                        {
                            int num2 = reader.ReadInt32();
                            int num3 = 0;
                            ClientData clientData = null;
                            bool flag = false;
                            if (__instance.GameId == num2)
                            {
                                num3 = reader.ReadInt32();
                                flag = __instance.AmHost;
                                __instance.HostId = reader.ReadInt32();
                                string playerName = reader.ReadString();
                                PlatformSpecificData platformSpecificData = new PlatformSpecificData();
                                MessageReader messageReader = reader.ReadMessage();
                                platformSpecificData.Platform = (Platforms)messageReader.Tag;
                                string platformName = messageReader.ReadString();
                                platformSpecificData.PlatformName = platformName;
                                switch (platformSpecificData.Platform)
                                {
                                    case Platforms.StandaloneWin10:
                                    case Platforms.Xbox:
                                        platformSpecificData.XboxPlatformId = messageReader.ReadUInt64();
                                        break;
                                    case Platforms.Playstation:
                                        platformSpecificData.PsnPlatformId = messageReader.ReadUInt64();
                                        break;
                                }
                                uint playerLevel = reader.ReadPackedUInt32();
                                string productUserId = reader.ReadString();
                                string friendCode = reader.ReadString();
                                clientData = new ClientData(num3, playerName, platformSpecificData, playerLevel, productUserId, friendCode);
                                LastJoin[num3] = 0f;
                                ClientData client = __instance.GetOrCreateClient(clientData);
                                Debug.Log($"Player {num3} joined. Name - {clientData.PlayerName} with FC {clientData.FriendCode}");
                                LastJoin[num3] = 0f;
                                lock (__instance.Dispatcher)
                                {
                                    Dispatcher(__instance, delegate
                                    {
                                        __instance.OnPlayerJoined(client);
                                    });
                                }
                                if (!__instance.AmHost || flag)
                                {
                                    break;
                                }
                                lock (__instance.Dispatcher)
                                {
                                    Dispatcher(__instance, delegate
                                    {
                                        __instance.OnBecomeHost();
                                    });
                                }
                            }
                            else
                            {
                                __instance.EnqueueDisconnect(DisconnectReasons.IncorrectGame);
                            }
                            break;
                        }
                    case 5:
                    case 6:
                        {
                            int num = reader.ReadInt32();
                            int TargetClientId = 0;
                            if (__instance.GameId != num)
                            {
                                break;
                            }
                            if (reader.Tag == 6)
                            {
                                try
                                {
                                    TargetClientId = reader.ReadPackedInt32();
                                }
                                catch
                                { 
                                    break;
                                }
                                if (__instance.ClientId != TargetClientId)
                                {
                                    Debug.Log($"Got data meant for {TargetClientId} for some unknown reason");
                                    break;
                                }
                            }
                            else
                            {
                                TargetClientId = -1;
                            }
                            if (__instance.InOnlineScene)
                            {
                                MessageReader subReader2 = MessageReader.Get(reader);
                                lock (__instance.Dispatcher)
                                {
                                    Dispatcher(__instance, delegate
                                    {

                                        int num4 = 0;
                                        num4 = subReader2.Position;
                                        if (!CheckDataMessage(subReader2, TargetClientId, sendOption))
                                        {
                                            subReader2.Recycle();
                                        }
                                        else
                                        {
                                            subReader2.Position = num4;
                                            HandleGameData.HandleData(__instance, subReader2, TargetClientId, sendOption);
                                        }
                                    });
                                }
                            }
                            else
                            {
                                if (sendOption == SendOption.None)
                                {
                                    break;
                                }
                                MessageReader subReader3 = MessageReader.Get(reader);
                                lock (__instance.PreSpawnDispatcher)
                                {
                                    PreDispatcher(__instance, delegate
                                    {
                                        int num4 = 0;
                                        num4 = subReader3.Position;
                                        if (CheckDataMessage(subReader3, TargetClientId, sendOption))
                                        {
                                            subReader3.Position = num4;
                                            HandleGameData.HandleData(__instance, subReader3, TargetClientId, sendOption);
                                        }
                                    });
                                }
                            }
                            break;
                        }
                    default:
                        return true;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(InnerNetClient), "HandleGameData")]
        internal class HandleGameData
        {

            public static bool Prefix(InnerNetClient __instance, MessageReader parentReader)
            {
                return !enableMalformedPacketGuard;
            }

            public static void HandleData(InnerNetClient __instance, MessageReader parentReader, int TargetClientId, SendOption sendOption)
            {
                try
                {
                    int num = 0;
                    while (parentReader.Position < parentReader.Length)
                    {
                        num++;
                        MessageReader reader = parentReader.ReadMessageAsNewBuffer();
                        int msgNum = __instance.msgNum;
                        __instance.msgNum = msgNum + 1;
                        __instance.StartCoroutine(BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions.WrapToIl2Cpp(Handle(__instance, reader, msgNum, TargetClientId, sendOption, num)));
                    }
                }
                finally
                {
                    parentReader.Recycle();
                }
            }

            public static IEnumerator Handle(InnerNetClient __instance, MessageReader reader, int msgNum, int TargetClientId, SendOption sendOption, int numData)
            {
                int cnt = 0;
                reader.Position = 0;

                switch ((GameDataTypes)reader.Tag)
                {
                    case GameDataTypes.SceneChangeFlag:
                        {
                            int num3 = reader.ReadPackedInt32();
                            ClientData clientData2 = __instance.FindClientById(num3);
                            string text = reader.ReadString();
                            if (clientData2 != null && !string.IsNullOrWhiteSpace(text))
                            {
                                MonoBehaviourExtensions.StartCoroutine(__instance, AmongUsClientUtils.CoOnPlayerChangedScene(__instance, clientData2, text));
                                Debug.Log($"SceneChangeFlag for {num3} to {text}");
                                break;
                            }
                            Debug.Log($"(SceneChangeFlag) Couldn't find client {num3} to change scene to {text}");
                            reader.Recycle();
                            break;
                        }
                    case GameDataTypes.RpcFlag:
                        try
                        {
                            InnerNetObject value = default;
                            while (true)
                            {
                                uint num2;
                                try
                                {
                                    num2 = reader.ReadPackedUInt32();
                                }

                                catch (Exception ex)
                                {
                                    
                                    throw;
                                    break;
                                }
                                byte b = reader.ReadByte();
                                RpcCalls rpcCalls = (RpcCalls)b;
                                try
                                {
                                    if (PlayerControl.LocalPlayer) Debug.Log($"RpcFlag ({sendOption}) - ({__instance.FindObjectByNetId<InnerNetObject>(num2).name}({__instance.FindObjectByNetId<InnerNetObject>(num2).NetId})RPC:" + rpcCalls);
                                }
                                catch { if (PlayerControl.LocalPlayer) HandleMessage.Crash("Unknown object sent RPC - " + rpcCalls); }

                                lock (__instance.allObjects)
                                {
                                    if (__instance.allObjects.AllObjectsFast.TryGetValue(num2, out value))
                                    {
                                        value.HandleRpc(b, reader);
                                        goto IL_03b6;
                                    }
                                    if (num2 == uint.MaxValue || __instance.DestroyedObjects.Contains(num2))
                                    {
                                        goto IL_03b6;
                                    }
                                    if (cnt++ <= 10)
                                    {
                                        reader.Position = 0;
                                        yield return Effects.Wait(0.1f);
                                        continue;
                                    }
                                    break;
                                IL_03b6:
                                    value = null;
                                    break;
                                }
                            }
                            break;
                        }
                        finally
                        {
                            reader.Recycle();
                        }
                    default:
                        Debug.Log($"Data Flag: {(GameDataTypes)reader.Tag}");
                        yield return __instance.HandleGameDataInner(reader, __instance.msgNum);
                        break;
                }
            }
        }

        public static class AmongUsClientUtils
        {
            public static IEnumerator CreatePlayer(AmongUsClient __instance, ClientData clientData)
            {
                if (clientData.IsBeingCreated || clientData.Character)
                {
                    yield break;
                }
                if (!__instance.AmHost)
                {
                    __instance.logger.Debug("Waiting for host to make my player", null);
                    yield break;
                }
                clientData.IsBeingCreated = true;
                bool isOwnerOfPlayerData = (__instance.NetworkMode == NetworkModes.LocalGame || __instance.AmModdedHost || (__instance).NetworkMode == NetworkModes.FreePlay);
                sbyte b;
                if (isOwnerOfPlayerData)
                {
                    b = (GameData.Instance.HasPlayer(clientData) ? GameData.Instance.GetPlayerIdFromClient(clientData) : GameData.Instance.GetAvailableId());
                    if (b == -1)
                    {
                        (__instance).SendLateRejection(clientData.Id, DisconnectReasons.GameFull);
                        __instance.logger.Info("Overfilled room.", null);
                        clientData.IsBeingCreated = false;
                        yield break;
                    }
                }
                else
                {
                    yield return new WaitUntil((Func<bool>)(() => GameData.Instance.HasPlayer(clientData)));
                    b = GameData.Instance.GetPlayerIdFromClient(clientData);
                }
                Vector2 vector = Vector2.zero;
                if (DestroyableSingleton<TutorialManager>.InstanceExists)
                {
                    vector = new Vector2(-1.9f, 3.25f);
                }
                PlayerControl pc = Object.Instantiate(__instance.PlayerPrefab, vector, Quaternion.identity);
                pc.PlayerId = (byte)b;
                pc.FriendCode = clientData.FriendCode;
                pc.Puid = clientData.ProductUserId;
                clientData.Character = pc;
                (__instance).UpdateCachedClients(clientData, clientData.Character);
                if (ShipStatus.Instance)
                {
                    ShipStatus.Instance.SpawnPlayer(pc, Palette.PlayerColors.Length, initialSpawn: false);
                }
                if (isOwnerOfPlayerData)
                {
                    NetworkedPlayerInfo netObjParent = GameData.Instance.AddPlayer(pc, clientData);
                    __instance.Spawn(netObjParent);
                }
                else
                {
                    while (GameData.Instance.GetPlayerByClient(clientData) == null)
                    {
                        yield return null;
                    }
                }
                AmongUsClient.Instance.Spawn(pc, clientData.Id, SpawnFlags.IsClientCharacter);
                if (isOwnerOfPlayerData)
                {
                    GameData.Instance.DirtyAllData();
                }
                if (GameManager.Instance.LogicOptions.IsDefaults)
                {
                    GameManager.Instance.LogicOptions.SetRecommendations(GameData.Instance.PlayerCount, (AmongUsClient.Instance).NetworkMode);
                }
                clientData.IsBeingCreated = false;
            }

            public static SpawnGameDataMessage CreateSpawnMessage(InnerNetObject netObjParent, int ownerId, SpawnFlags flags)
            {
                InnerNetObject[] array = netObjParent.GetComponentsInChildren<InnerNetObject>();
                InnerNetObject[] array2 = array;
                foreach (InnerNetObject innerNetObject in array2)
                {
                    if (innerNetObject is CustomNetworkTransform)
                    {
                        innerNetObject.OwnerId = (AmongUsClient.Instance).ClientId;
                    }
                    else
                    {
                        innerNetObject.OwnerId = ownerId;
                    }
                    innerNetObject.SpawnFlags = flags;
                    if (innerNetObject.NetId == 0)
                    {
                        AmongUsClient instance = AmongUsClient.Instance;
                        uint netIdCnt = instance.NetIdCnt;
                        instance.NetIdCnt = netIdCnt + 1;
                        innerNetObject.NetId = netIdCnt;
                        lock (AmongUsClient.Instance.allObjects)
                        {
                            AmongUsClient.Instance.allObjects.TryAddNetObject(innerNetObject);
                        }
                    }
                }
                return new SpawnGameDataMessage(netObjParent, ownerId, flags, array);
            }

            public static SpawnGameDataMessage CreateSpawnMessage(AmongUsClient __instance, InnerNetObject netObjParent, int ownerId, SpawnFlags flags)
            {
                InnerNetObject[] array = netObjParent.GetComponentsInChildren<InnerNetObject>();
                InnerNetObject[] array2 = array;
                foreach (InnerNetObject innerNetObject in array2)
                {
                    innerNetObject.OwnerId = ownerId;
                    innerNetObject.SpawnFlags = flags;
                    if (innerNetObject.NetId == 0)
                    {
                        uint netIdCnt = (__instance).NetIdCnt;
                        (__instance).NetIdCnt = netIdCnt + 1;
                        innerNetObject.NetId = netIdCnt;
                        lock ((__instance).allObjects)
                        {
                            (__instance).allObjects.TryAddNetObject(innerNetObject);
                        }
                    }
                }
                return new SpawnGameDataMessage(netObjParent, ownerId, flags, array);
            }

            public static IEnumerator CoOnPlayerChangedScene(InnerNetClient __instance, ClientData client, string currentScene)
            {
                client.InScene = true;
                if (GameData.Instance == null)
                {
                    GameData.Instance = Object.Instantiate(AmongUsClient.Instance.GameDataPrefab);
                }
                GameData.Instance.RemoveDisconnectedPlayers();
                if (!__instance.AmHost)
                {
                    yield break;
                }
                if (VoteBanSystem.Instance == null)
                {
                    VoteBanSystem.Instance = Object.Instantiate(AmongUsClient.Instance.VoteBanPrefab);
                    __instance.Spawn(VoteBanSystem.Instance);
                }
                if (currentScene.Equals("Tutorial"))
                {
                    GameManager.DestroyInstance();
                    GameManager netObjParent = GameManagerCreator.CreateGameManager(GameOptionsManager.Instance.CurrentGameOptions.GameMode);
                    __instance.Spawn(netObjParent);
                    int index = ((AmongUsClient.Instance.TutorialMapId == 0 && AprilFoolsMode.ShouldFlipSkeld()) ? 3 : AmongUsClient.Instance.TutorialMapId);
                    AmongUsClient.Instance.ShipLoadingAsyncHandle = AmongUsClient.Instance.ShipPrefabs[index].InstantiateAsync(null, false);
                    yield return AmongUsClient.Instance.ShipLoadingAsyncHandle;
                    AsyncOperationHandle<GameObject> test = AmongUsClient.Instance.ShipLoadingAsyncHandle;
                    GameObject result = test.Result;
                    AmongUsClient.Instance.ShipLoadingAsyncHandle = null;
                    __instance.Spawn(result.GetComponent<ShipStatus>());
                    yield return AmongUsClient.Instance.CreatePlayer(client);
                }
                else
                {
                    if (!currentScene.Equals("OnlineGame"))
                    {
                        yield break;
                    }
                    if (client.Id != __instance.ClientId)
                    {
                        __instance.SendInitialData(client.Id);
                    }
                    else
                    {
                        if (__instance.NetworkMode == NetworkModes.LocalGame)
                        {
                            __instance.StartCoroutine(AmongUsClient.Instance.CoBroadcastManager());
                        }
                        GameManager.DestroyInstance();
                        GameManager netObjParent2 = GameManagerCreator.CreateGameManager(GameOptionsManager.Instance.CurrentGameOptions.GameMode);
                        __instance.Spawn(netObjParent2);
                    }
                    yield return CreatePlayer(AmongUsClient.Instance, client);
                }
            }
        }

        // my silly adaptation ends here~

        // Anti-crash: drops malformed GameData / GameDataTo packets before the game
        // builds the closure that later freezes in HandleGameDataInner. On host the
        // sender is resolved by object ownership and kicked (or banned).
        //[HarmonyPatch(typeof(InnerNetClient), "HandleMessage", new Type[] { typeof(MessageReader), typeof(SendOption) })]
        //public static class Elysium_MalformedPacketGuard
        //{
        //    public static bool Prefix(InnerNetClient __instance, [HarmonyArgument(0)] MessageReader reader)
        //    {
        //        try
        //        {
        //            if (!ElysiumModMenuGUI.enableMalformedPacketGuard) return true;
        //            if (__instance == null || reader == null) return true;

        //            byte tag = reader.Tag;
        //            if (tag != 5 && tag != 6) return true; // only GameData / GameDataTo

        //            int startPos = reader.Position;
        //            bool valid;
        //            try { valid = IsFramingValid(reader, tag); }
        //            finally { try { reader.Position = startPos; } catch { } }

        //            if (valid) return true;

        //            if (__instance.AmHost)
        //            {
        //                int senderId = -1;
        //                try { senderId = ResolveSenderClientId(reader, tag); }
        //                finally { try { reader.Position = startPos; } catch { } }
        //                Punish(__instance, senderId);
        //            }

        //            return false; // drop: original never runs -> no freeze
        //        }
        //        catch
        //        {
        //            return true; // never break the game on our own error
        //        }
        //    }

        //    // Re-parse the packet with the same Hazel reader. Any throw / invalid tag
        //    // => malformed (fail-closed). Legit packets parse cleanly and pass.
        //    private static bool IsFramingValid(MessageReader reader, byte tag)
        //    {
        //        try
        //        {
        //            reader.ReadInt32();                     // gameId
        //            if (tag == 6) reader.ReadPackedInt32(); // targetId (GameDataTo)

        //            int parts = 0;
        //            while (reader.Position < reader.Length && parts < 1024)
        //            {
        //                MessageReader part = reader.ReadMessage();
        //                if (part == null) break;
        //                parts++;

        //                byte pt = part.Tag;
        //                if (pt == 0 || pt == 3 || pt > 8) return false; // invalid sub-tag

        //                // Every valid sub-message starts with a packed-int (netId/clientId);
        //                // a too-short packet throws right here.
        //                part.ReadPackedUInt32();
        //                if (pt == 2) part.ReadByte();        // RPC: callId
        //                else if (pt == 6) part.ReadString(); // SceneChange: scene name
        //            }
        //            return true;
        //        }
        //        catch
        //        {
        //            return false; // any parse failure = malformed packet
        //        }
        //    }

        //    // Find the sender by packet content (works on relay/official servers too):
        //    // take the netId of the first sub-message and find who owns that object.
        //    private static int ResolveSenderClientId(MessageReader reader, byte tag)
        //    {
        //        try
        //        {
        //            reader.ReadInt32();
        //            if (tag == 6) reader.ReadPackedInt32();
        //            if (reader.Position >= reader.Length) return -1;

        //            MessageReader sub = reader.ReadMessage();
        //            if (sub == null) return -1;

        //            byte st = sub.Tag;
        //            if (st != 1 && st != 2 && st != 5) return -1; // Data/RPC/Despawn start with netId

        //            uint netId = sub.ReadPackedUInt32();
        //            return OwnerClientIdOfNetId(netId);
        //        }
        //        catch
        //        {
        //            return -1;
        //        }
        //    }

        //    // netId -> owner (PlayerControl / physics / transform) -> its clientId (OwnerId).
        //    private static int OwnerClientIdOfNetId(uint netId)
        //    {
        //        try
        //        {
        //            if (netId == 0 || PlayerControl.AllPlayerControls == null) return -1;

        //            foreach (PlayerControl p in PlayerControl.AllPlayerControls)
        //            {
        //                if (p == null) continue;
        //                try
        //                {
        //                    bool match = p.NetId == netId;
        //                    if (!match && p.MyPhysics != null) match = p.MyPhysics.NetId == netId;
        //                    if (!match && p.NetTransform != null) match = p.NetTransform.NetId == netId;
        //                    if (match) return (int)p.OwnerId; // OwnerId == clientId
        //                }
        //                catch { }
        //            }
        //        }
        //        catch { }

        //        return -1;
        //    }

        //    private static void Punish(InnerNetClient client, int cheaterId)
        //    {
        //        try
        //        {
        //            // Unknown sender / self / host -> do not punish (packet is already dropped).
        //            if (cheaterId < 0 || cheaterId == client.ClientId || cheaterId == client.HostId) return;

        //            bool ban = ElysiumModMenuGUI.banMalformedPacketSender;
        //            PlayerControl player = Shield_PasosLimit_Patch.FindPlayerByClientId(cheaterId);
        //            string pName = player != null && player.Data != null && !string.IsNullOrEmpty(player.Data.PlayerName)
        //                ? player.Data.PlayerName : $"Client {cheaterId}";

        //            if (ban)
        //            {
        //                string fc = (player != null && player.Data != null && !string.IsNullOrEmpty(player.Data.FriendCode))
        //                    ? player.Data.FriendCode : "Unknown";
        //                string puid = cheaterId.ToString();
        //                try
        //                {
        //                    if (player != null && AmongUsClient.Instance != null)
        //                    {
        //                        var cd = AmongUsClient.Instance.GetClientFromCharacter(player);
        //                        if (cd != null) puid = ElysiumModMenuGUI.GetClientPuid(cd);
        //                    }
        //                }
        //                catch { }
        //                ElysiumModMenuGUI.AddToBanList(fc, puid, pName, "Malformed packet (anti-crash)");
        //            }

        //            try { client.KickPlayer(cheaterId, ban); } catch { }
        //            ElysiumModMenuGUI.ShowNotification($"<color=#FF4444>[ANTI-CRASH]</color> {pName} {(ban ? "banned" : "kicked")}: malformed packet");
        //        }
        //        catch { }
        //    }
        //}

        //[HarmonyPatch(typeof(MessageReader), nameof(MessageReader.ReadMessage))]
        //public static class Shield_PasosLimit_Patch
        //{
        //    private const byte DataGameDataTag = 1;
        //    private const byte RpcGameDataTag = 2;
        //    private const byte DroppedGameDataTag = 0;
        //    private const float PasosNotifyCooldown = 2f;
        //    private const float RpcSpamWindow = 1f;
        //    private static readonly Dictionary<int, Queue<float>> rpcSpamTrackers = new Dictionary<int, Queue<float>>();
        //    private static readonly HashSet<int> pasosBlockedClientIds = new HashSet<int>();
        //    private static readonly HashSet<int> pasosHostBannedClientIds = new HashSet<int>();
        //    private static float lastPasosNotify;
        //    private static int currentPasosClientId = -1;

        //    public static void BeginMessageContext(int clientId)
        //    {
        //        currentPasosClientId = IsValidClientId(clientId) ? clientId : -1;
        //    }

        //    public static bool IsClientBlocked(int clientId)
        //    {
        //        return ElysiumModMenuGUI.enableLocalPasosBan && IsValidClientId(clientId) && pasosBlockedClientIds.Contains(clientId);
        //    }

        //    public static bool IsPlayerBlocked(PlayerControl player)
        //    {
        //        return player != null && IsClientBlocked(GetKickClientId(player, -1));
        //    }

        //    public static bool IsEmptyGameDataReader(MessageReader reader)
        //    {
        //        if (!ElysiumModMenuGUI.enablePasosLimit || reader == null) return false;

        //        try
        //        {
        //            return reader.Length <= 0 || (reader.Position <= 0 && reader.BytesRemaining <= 0);
        //        }
        //        catch { }

        //        return false;
        //    }

        //    public static bool IsValidClientId(int clientId)
        //    {
        //        return clientId >= 0 && clientId < 256;
        //    }

        //    private static float lastUnownedSpawnNotify;

        //    // True if clientId is a currently-connected client (exists in allClients).
        //    public static bool IsConnectedClientId(int clientId)
        //    {
        //        try
        //        {
        //            InnerNetClient c = AmongUsClient.Instance;
        //            if (c == null || c.allClients == null) return false;

        //            var cursor = c.allClients.GetEnumerator();
        //            while (cursor.MoveNext())
        //            {
        //                var cd = cursor.Current;
        //                if (cd != null && cd.Id == clientId) return true;
        //            }
        //        }
        //        catch { }
        //        return false;
        //    }

        //    // Detects a Spawn sub-message (GameData sub-tag 4) whose owner clientId does
        //    // not exist. Such spawns get buffered by the game ("Delay spawn for unowned"),
        //    // and a flood of them grows that queue every frame -> freeze. Host only.
        //    // Negative owner ids (e.g. -2 = global/host-owned objects) are always allowed.
        //    public static bool IsUnownedSpawnReader(MessageReader reader)
        //    {
        //        if (!ElysiumModMenuGUI.enableUnownedSpawnGuard || reader == null) return false;
        //        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return false;

        //        try
        //        {
        //            if (reader.Tag != 4) return false; // Spawn sub-message only

        //            int startPos = reader.Position;
        //            bool unowned = false;
        //            try
        //            {
        //                reader.ReadPackedUInt32();              // spawn / prefab id
        //                int ownerId = reader.ReadPackedInt32(); // owner clientId
        //                if (ownerId >= 0 && !IsConnectedClientId(ownerId))
        //                    unowned = true;
        //            }
        //            catch { unowned = false; } // parse failure -> leave to other guards
        //            finally { try { reader.Position = startPos; } catch { } }

        //            return unowned;
        //        }
        //        catch { }
        //        return false;
        //    }

        //    // Rate-limited notification when a fake spawn is dropped (the attack sends many).
        //    public static void NotifyUnownedSpawnDrop()
        //    {
        //        try
        //        {
        //            float now = UnityEngine.Time.time;
        //            if (now - lastUnownedSpawnNotify < 3f) return;
        //            lastUnownedSpawnNotify = now;
        //            ElysiumModMenuGUI.ShowNotification("<color=#FF4444>[ANTI-CRASH]</color> Dropped fake spawn (unowned client)");
        //        }
        //        catch { }
        //    }

        //    public static bool RecordDrop(int clientId = -1, PlayerControl player = null, string reason = "RPC spam")
        //    {
        //        float now = UnityEngine.Time.time;
        //        int resolvedClientId = IsValidClientId(clientId) ? clientId : GetKickClientId(player, -1);
        //        if (!IsValidClientId(resolvedClientId))
        //            resolvedClientId = currentPasosClientId;
        //        if (!IsValidClientId(resolvedClientId))
        //            resolvedClientId = ResolveSingleRemoteClientId();
        //        if (!IsValidClientId(resolvedClientId))
        //            return false;

        //        int clientDropCount = TrackRpcSpam(resolvedClientId, now);
        //        bool overLimit = clientDropCount >= ElysiumModMenuGUI.rpcSpamLimit;
        //        if (overLimit)
        //        {
        //            BlockPasosClient(resolvedClientId, player, clientDropCount, reason);
        //            if (now - lastPasosNotify > PasosNotifyCooldown)
        //                lastPasosNotify = now;
        //        }

        //        return overLimit || (ElysiumModMenuGUI.enableLocalPasosBan && pasosBlockedClientIds.Contains(resolvedClientId));
        //    }

        //    private static int TrackRpcSpam(int clientId, float now)
        //    {
        //        if (!IsValidClientId(clientId)) return 0;

        //        if (!rpcSpamTrackers.TryGetValue(clientId, out Queue<float> drops))
        //        {
        //            drops = new Queue<float>();
        //            rpcSpamTrackers[clientId] = drops;
        //        }

        //        while (drops.Count > 0 && drops.Peek() < now - RpcSpamWindow)
        //            drops.Dequeue();

        //        drops.Enqueue(now);
        //        return drops.Count;
        //    }

        //    private static void BlockPasosClient(int clientId, PlayerControl player, int packetCount, string reason)
        //    {
        //        try
        //        {
        //            if (!IsValidClientId(clientId) || (AmongUsClient.Instance != null && clientId == AmongUsClient.Instance.ClientId)) return;

        //            if (player == null)
        //                player = FindPlayerByClientId(clientId);

        //            string pName = player?.Data?.PlayerName ?? $"Client {clientId}";
        //            int banClientId = GetKickClientId(player, clientId);
        //            string fc = string.IsNullOrEmpty(player?.Data?.FriendCode) ? "Unknown" : player.Data.FriendCode;
        //            string puid = IsValidClientId(banClientId) ? banClientId.ToString() : clientId.ToString();

        //            try
        //            {
        //                if (player != null && AmongUsClient.Instance != null)
        //                {
        //                    var client = AmongUsClient.Instance.GetClientFromCharacter(player);
        //                    if (client != null) puid = GetClientPuid(client);
        //                }
        //            }
        //            catch { }

        //            if (ElysiumModMenuGUI.enableLocalPasosBan && pasosBlockedClientIds.Add(clientId))
        //            {
        //                ElysiumModMenuGUI.AddToBanList(fc, puid, pName, $"Local RPC drop: {reason} after {packetCount} packets/s");
        //            }

        //            if (!ElysiumModMenuGUI.enableHostPasosBan || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
        //            if (!pasosHostBannedClientIds.Add(clientId)) return;
        //            if (!IsValidClientId(banClientId)) return;

        //            ElysiumModMenuGUI.AddToBanList(fc, puid, pName, $"Host RPC auto-ban: {reason} after {packetCount} packets/s");
        //            AmongUsClient.Instance.KickPlayer(banClientId, true);
        //        }
        //        catch { }
        //    }

        //    public static int GetKickClientId(PlayerControl player, int fallbackClientId)
        //    {
        //        try
        //        {
        //            if (player != null)
        //            {
        //                int ownerId = (int)player.OwnerId;
        //                if (IsValidClientId(ownerId)) return ownerId;

        //                if (player.Data != null && IsValidClientId(player.Data.ClientId))
        //                    return player.Data.ClientId;
        //            }
        //        }
        //        catch { }

        //        return IsValidClientId(fallbackClientId) ? fallbackClientId : -1;
        //    }

        //    public static PlayerControl FindPlayerByClientId(int clientId)
        //    {
        //        try
        //        {
        //            if (PlayerControl.AllPlayerControls == null) return null;

        //            foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
        //            {
        //                if (pc == null) continue;
        //                if ((int)pc.OwnerId == clientId) return pc;

        //                try
        //                {
        //                    if (pc.Data != null && pc.Data.ClientId == clientId) return pc;
        //                }
        //                catch { }
        //            }
        //        }
        //        catch { }

        //        return null;
        //    }

        //    public static int ResolveClientId(object source)
        //    {
        //        return ResolveClientId(source, 0);
        //    }

        //    private static int ResolveClientId(object source, int depth)
        //    {
        //        if (source == null || depth > 2 || source is MessageReader || source is SendOption) return -1;

        //        try
        //        {
        //            if (source is PlayerControl pc)
        //                return GetKickClientId(pc, -1);

        //            int direct = ConvertNumericClientId(source);
        //            if (direct >= 0) return direct;

        //            Type type = source.GetType();
        //            foreach (string name in new[] { "ClientId", "OwnerId", "Id", "clientId", "ownerId", "id" })
        //            {
        //                PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //                direct = ConvertNumericClientId(property?.GetValue(source));
        //                if (direct >= 0) return direct;

        //                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //                direct = ConvertNumericClientId(field?.GetValue(source));
        //                if (direct >= 0) return direct;
        //            }

        //            foreach (string name in new[] { "Character", "Object", "Player", "Data", "character", "player" })
        //            {
        //                PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //                direct = ResolveClientId(property?.GetValue(source), depth + 1);
        //                if (direct >= 0) return direct;

        //                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //                direct = ResolveClientId(field?.GetValue(source), depth + 1);
        //                if (direct >= 0) return direct;
        //            }

        //            string typeName = type.FullName ?? type.Name;
        //            if (typeName.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0 ||
        //                typeName.IndexOf("Client", StringComparison.OrdinalIgnoreCase) >= 0)
        //            {
        //                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        //                {
        //                    if (property.GetIndexParameters().Length > 0) continue;

        //                    try
        //                    {
        //                        direct = ConvertNumericClientId(property.GetValue(source));
        //                        if (direct >= 0) return direct;
        //                    }
        //                    catch { }
        //                }

        //                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        //                {
        //                    try
        //                    {
        //                        direct = ConvertNumericClientId(field.GetValue(source));
        //                        if (direct >= 0) return direct;
        //                    }
        //                    catch { }
        //                }
        //            }
        //        }
        //        catch { }

        //        return -1;
        //    }

        //    private static int ResolveSingleRemoteClientId()
        //    {
        //        try
        //        {
        //            if (PlayerControl.AllPlayerControls == null) return -1;

        //            int found = -1;
        //            int count = 0;
        //            foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
        //            {
        //                if (pc == null || pc == PlayerControl.LocalPlayer || pc.Data == null || pc.Data.Disconnected) continue;

        //                int ownerId = (int)pc.OwnerId;
        //                if (!IsValidClientId(ownerId)) continue;

        //                found = ownerId;
        //                count++;
        //                if (count > 1) return -1;
        //            }

        //            return count == 1 ? found : -1;
        //        }
        //        catch { }

        //        return -1;
        //    }

        //    private static int ConvertNumericClientId(object value)
        //    {
        //        if (value == null) return -1;

        //        try
        //        {
        //            TypeCode code = Type.GetTypeCode(value.GetType());
        //            switch (code)
        //            {
        //                case TypeCode.Byte:
        //                case TypeCode.SByte:
        //                case TypeCode.Int16:
        //                case TypeCode.UInt16:
        //                case TypeCode.Int32:
        //                case TypeCode.UInt32:
        //                case TypeCode.Int64:
        //                case TypeCode.UInt64:
        //                    long id = Convert.ToInt64(value);
        //                    return id >= 0 && id < 256 ? (int)id : -1;
        //            }
        //        }
        //        catch { }

        //        return -1;
        //    }

        //    public static void Postfix(MessageReader __result)
        //    {
        //        DropSuspiciousGameDataMessage(__result);
        //    }

        //    public static void DropSuspiciousGameDataMessage(MessageReader reader)
        //    {
        //        if (!ElysiumModMenuGUI.enablePasosLimit || reader == null) return;

        //        try
        //        {
        //            if (reader.BytesRemaining > 0 || reader.Length > 0) return;

        //            string reason = null;
        //            if (reader.Tag == RpcGameDataTag)
        //                reason = "empty StartMessage RPC";
        //            else if (reader.Tag == DataGameDataTag)
        //                reason = "empty SDF/data message";
        //            else if (IsBadGameDataTag(reader.Tag))
        //                reason = $"bad data tag {reader.Tag}";

        //            if (reason == null) return;

        //            reader.Tag = DroppedGameDataTag;
        //            reader.Position = reader.Length;

        //            RecordDrop(-1, null, reason);
        //        }
        //        catch { }
        //    }

        //    private static bool IsBadGameDataTag(byte tag)
        //    {
        //        switch (tag)
        //        {
        //            case DroppedGameDataTag:
        //            case DataGameDataTag:
        //            case RpcGameDataTag:
        //            case 4:
        //            case 5:
        //            case 6:
        //            case 7:
        //            case 8:
        //                return false;
        //            default:
        //                return true;
        //        }
        //    }
        //}

        //public static class Shield_PasosLimit_GameDataGuard
        //{
        //    private static readonly Dictionary<Type, PropertyInfo> coroutineReaderProperties = new Dictionary<Type, PropertyInfo>();

        //    public static bool ShouldDrop(object[] args)
        //    {
        //        try
        //        {
        //            MessageReader spawnReader = FindReader(args);
        //            if (Shield_PasosLimit_Patch.IsUnownedSpawnReader(spawnReader))
        //            {
        //                Shield_PasosLimit_Patch.NotifyUnownedSpawnDrop();
        //                return true;
        //            }
        //        }
        //        catch { }

        //        if (!ElysiumModMenuGUI.enablePasosLimit) return false;

        //        try
        //        {
        //            MessageReader reader = FindReader(args);

        //            if (!Shield_PasosLimit_Patch.IsEmptyGameDataReader(reader))
        //                return false;

        //            Shield_PasosLimit_Patch.RecordDrop();
        //            return true;
        //        }
        //        catch { }

        //        return false;
        //    }

        //    public static bool ShouldDropCoroutine(object instance)
        //    {
        //        try
        //        {
        //            MessageReader spawnReader = GetCoroutineReader(instance);
        //            if (Shield_PasosLimit_Patch.IsUnownedSpawnReader(spawnReader))
        //            {
        //                Shield_PasosLimit_Patch.NotifyUnownedSpawnDrop();
        //                return true;
        //            }
        //        }
        //        catch { }

        //        if (!ElysiumModMenuGUI.enablePasosLimit) return false;

        //        try
        //        {
        //            MessageReader reader = GetCoroutineReader(instance);

        //            if (!Shield_PasosLimit_Patch.IsEmptyGameDataReader(reader))
        //                return false;

        //            Shield_PasosLimit_Patch.RecordDrop();
        //            return true;
        //        }
        //        catch { }

        //        return false;
        //    }

        //    private static MessageReader FindReader(object[] args)
        //    {
        //        if (args == null) return null;

        //        foreach (object arg in args)
        //        {
        //            if (arg is MessageReader reader)
        //                return reader;
        //        }

        //        return null;
        //    }

        //    private static MessageReader GetCoroutineReader(object instance)
        //    {
        //        if (instance == null) return null;

        //        try
        //        {
        //            Type type = instance.GetType();
        //            if (!coroutineReaderProperties.TryGetValue(type, out PropertyInfo property))
        //            {
        //                property = type.GetProperty("reader", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //                coroutineReaderProperties[type] = property;
        //            }

        //            return property?.GetValue(instance) as MessageReader;
        //        }
        //        catch { }

        //        return null;
        //    }

        //    private static System.Collections.IEnumerator EmptyGameDataCoroutine()
        //    {
        //        yield break;
        //    }

        //    public static Il2CppSystem.Collections.IEnumerator EmptyGameDataCoroutineIl2Cpp()
        //    {
        //        return EmptyGameDataCoroutine().WrapToIl2Cpp();
        //    }
        //}

        //[HarmonyPatch]
        //public static class Shield_PasosLimit_HandleGameData_Patch
        //{
        //    public static IEnumerable<MethodBase> TargetMethods()
        //    {
        //        foreach (MethodInfo method in typeof(InnerNetClient).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        //        {
        //            if (method.Name == "HandleGameData" && method.ReturnType == typeof(void) &&
        //                method.GetParameters().Any(p => p.ParameterType == typeof(MessageReader)))
        //            {
        //                yield return method;
        //            }
        //        }
        //    }

        //    public static bool Prefix(object[] __args)
        //    {
        //        return !Shield_PasosLimit_GameDataGuard.ShouldDrop(__args);
        //    }
        //}

        //[HarmonyPatch]
        //public static class Shield_PasosLimit_HandleGameDataInner_Patch
        //{
        //    public static IEnumerable<MethodBase> TargetMethods()
        //    {
        //        foreach (MethodInfo method in typeof(InnerNetClient).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        //        {
        //            if (method.Name == "HandleGameDataInner" &&
        //                method.GetParameters().Any(p => p.ParameterType == typeof(MessageReader)))
        //            {
        //                yield return method;
        //            }
        //        }
        //    }

        //    public static bool Prefix(object[] __args, ref Il2CppSystem.Collections.IEnumerator __result)
        //    {
        //        if (!Shield_PasosLimit_GameDataGuard.ShouldDrop(__args))
        //            return true;

        //        __result = Shield_PasosLimit_GameDataGuard.EmptyGameDataCoroutineIl2Cpp();
        //        return false;
        //    }
        //}

        //[HarmonyPatch]
        //public static class Shield_PasosLimit_GameDataCoroutine_Patch
        //{
        //    public static IEnumerable<MethodBase> TargetMethods()
        //    {
        //        foreach (Type nestedType in typeof(InnerNetClient).GetNestedTypes(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        //        {
        //            if (nestedType.Name.IndexOf("HandleGameDataInner", StringComparison.OrdinalIgnoreCase) < 0) continue;

        //            MethodInfo moveNext = nestedType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //            if (moveNext != null && moveNext.ReturnType == typeof(bool))
        //                yield return moveNext;
        //        }
        //    }

        //    public static bool Prefix(object __instance, ref bool __result)
        //    {
        //        if (!Shield_PasosLimit_GameDataGuard.ShouldDropCoroutine(__instance))
        //            return true;

        //        __result = false;
        //        return false;
        //    }
        //}

        //[HarmonyPatch]
        //public static class Shield_PasosLimit_HandleMessageContext_Patch
        //{
        //    public static IEnumerable<MethodBase> TargetMethods()
        //    {
        //        foreach (MethodInfo method in typeof(InnerNetClient).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        //        {
        //            if (method.Name != "HandleMessage") continue;
        //            if (method.GetParameters().Any(p => p.ParameterType == typeof(MessageReader)))
        //                yield return method;
        //        }
        //    }

        //    public static bool Prefix(object[] __args)
        //    {
        //        if (!ElysiumModMenuGUI.enablePasosLimit) return true;

        //        try
        //        {
        //            int clientId = ExtractClientId(__args);
        //            Shield_PasosLimit_Patch.BeginMessageContext(clientId);
        //        }
        //        catch { }

        //        return true;
        //    }

        //    public static int ExtractClientId(object[] args)
        //    {
        //        if (args == null) return -1;

        //        foreach (object arg in args)
        //        {
        //            int clientId = ExtractClientId(arg);
        //            if (clientId >= 0) return clientId;
        //        }

        //        return -1;
        //    }

        //    private static int ExtractClientId(object source)
        //    {
        //        return Shield_PasosLimit_Patch.ResolveClientId(source);
        //    }
        //}

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
        public static class Shield_PetSpam_Patch
        {
            public static bool Prefix(PlayerPhysics __instance, byte callId, Hazel.MessageReader reader)
            {
                if (!ElysiumModMenuGUI.enablePasosLimit) return true;

                if (callId == 49 || callId == 50)
                {
                    try
                    {
                        if (__instance == null || __instance.myPlayer == null) return true;

                        if (__instance.myPlayer == PlayerControl.LocalPlayer) return true;

                        //if (Shield_PasosLimit_Patch.IsPlayerBlocked(__instance.myPlayer))
                        return false;

                        //int clientId = Shield_PasosLimit_Patch.GetKickClientId(__instance.myPlayer, -1);
                        //if (Shield_PasosLimit_Patch.RecordDrop(clientId, __instance.myPlayer, "pet RPC spam"))
                        return false;
                    }
                    catch { }
                }

                return true;
            }
        }
        public static int GetColorIdByName(string name)
        {
            string[] names = { "red", "blue", "green", "pink", "orange", "yellow", "black", "white", "purple", "brown", "cyan", "lime", "maroon", "rose", "banana", "gray", "tan", "coral", "fortegreen" };
            for (int i = 0; i < names.Length; i++)
                if (names[i] == name.ToLower().Trim()) return i;
            return -1;
        }
        private IEnumerator AttemptShapeshiftFrame(PlayerControl target, PlayerControl morphInto)
        {
            if (target == null || morphInto == null || PlayerControl.LocalPlayer == null || AmongUsClient.Instance == null) yield break;

            bool hasAnticheat = AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame && !Constants.IsVersionModded();

            if (target.Data.RoleType != RoleTypes.Shapeshifter && hasAnticheat)
            {
                RoleTypes currentRole = target.Data.RoleType;
                target.RpcSetRole(RoleTypes.Shapeshifter, true);

                yield return new WaitForSeconds(0.5f);

                target.RpcShapeshift(morphInto, true);

                yield return new WaitForSeconds(0.5f);

                target.RpcSetRole(currentRole, true);
            }
            else
            {
                target.RpcShapeshift(morphInto, true);
            }
            ShowNotification($"<color=#ca08ff>[MORPH]</color> <b>{target.Data.PlayerName}</b> превращен в <b>{morphInto.Data.PlayerName}</b>!");
        }

        private IEnumerator MassMorphCoroutine()
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls == null) yield break;

            bool hasAnticheat = AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame && !Constants.IsVersionModded();

            Dictionary<byte, RoleTypes> originalRoles = new Dictionary<byte, RoleTypes>();

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                {
                    originalRoles[pc.PlayerId] = pc.Data.RoleType;

                    if (hasAnticheat && pc.Data.RoleType != RoleTypes.Shapeshifter)
                    {
                        pc.RpcSetRole(RoleTypes.Shapeshifter, true);
                    }
                }
            }

            if (hasAnticheat) yield return new UnityEngine.WaitForSeconds(0.5f);

            PlayerControl targetToMorphInto = null;
            if (selectedMorphTargetId != 255)
            {
                targetToMorphInto = GameData.Instance.GetPlayerById(selectedMorphTargetId)?.Object;
            }

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                {
                    PlayerControl morphTarget = targetToMorphInto != null ? targetToMorphInto : pc;
                    pc.RpcShapeshift(morphTarget, true);
                }
            }


            if (hasAnticheat) yield return new UnityEngine.WaitForSeconds(0.5f);

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                {
                    if (hasAnticheat && originalRoles.ContainsKey(pc.PlayerId))
                    {
                        pc.RpcSetRole(originalRoles[pc.PlayerId], true);
                    }
                }
            }

            string notifText = targetToMorphInto != null ? targetToMorphInto.Data.PlayerName : "Egg";
            ShowNotification($"<color=#FF00FF>[MASS MORPH]</color> {notifText}");
        }


        private void ForceMeetingAsPlayer(PlayerControl target)
        {
            if (target == null || AmongUsClient.Instance == null) return;
            if (!AmongUsClient.Instance.AmHost) return;

            try
            {
                MeetingRoomManager.Instance.AssignSelf(target, null);
                target.RpcStartMeeting(null);
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(target);
            }
            catch { }
        }

        private void KillAll()
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.AllPlayerControls == null) return;
            Vector3 op = PlayerControl.LocalPlayer.transform.position;
            foreach (var t in PlayerControl.AllPlayerControls)
            {
                if (t != null && t != PlayerControl.LocalPlayer && !t.Data.IsDead && !t.Data.Disconnected)
                {
                    PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(t.transform.position);
                    PlayerControl.LocalPlayer.CmdCheckMurder(t);
                    PlayerControl.LocalPlayer.RpcMurderPlayer(t, true);
                }
            }
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(op);
        }

        private void KickAll()
        {
            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.AllPlayerControls != null)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                    if (pc != null && pc != PlayerControl.LocalPlayer && !pc.Data.Disconnected)
                        AmongUsClient.Instance.KickPlayer((int)pc.OwnerId, false);
            }
        }

        private void DespawnLobby()
        {
            try
            {
                if (LobbyBehaviour.Instance != null && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    LobbyBehaviour.Instance.Cast<InnerNetObject>().Despawn();
                }
            }
            catch { }
        }

        private void SpawnLobby()
        {
            try
            {
                if (GameStartManager.Instance != null && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    LobbyBehaviour newLobby = UnityEngine.Object.Instantiate<LobbyBehaviour>(GameStartManager.Instance.LobbyPrefab);
                    AmongUsClient.Instance.Spawn(newLobby.Cast<InnerNetObject>(), -2, SpawnFlags.None);
                }
            }
            catch { }
        }



        public static void UnlockCosmetics()
        {
            if (HatManager.Instance == null) return;
            try
            {
                foreach (var h in HatManager.Instance.allHats) h.Free = true;
                foreach (var s in HatManager.Instance.allSkins) s.Free = true;
                foreach (var v in HatManager.Instance.allVisors) v.Free = true;
                foreach (var p in HatManager.Instance.allPets) p.Free = true;
                foreach (var n in HatManager.Instance.allNamePlates) n.Free = true;
            }
            catch { }
        }

        public static void ChangeNameGlobalHost(PlayerControl target, string newName)
        {
            if (target == null) return;
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
            try
            {
                target.RpcSetName(newName);
                var netObj = GameData.Instance.GetComponent<InnerNetObject>();
                if (netObj != null) netObj.SetDirtyBit(1U << (int)target.PlayerId);
            }
            catch { }
        }

        private static void ApplyLocalNameSelf(string newName, bool notify = true)
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null)
                {
                    if (notify) ShowNotification("<color=#FF4444>[LOCAL NAME]</color> Local player not found.");
                    return;
                }

                string renderName = BuildLocalNameRenderText(newName);
                if (originalLocalName == null)
                {
                    originalLocalName = local.CurrentOutfit != null && !string.IsNullOrWhiteSpace(local.CurrentOutfit.PlayerName)
                        ? local.CurrentOutfit.PlayerName
                        : local.Data?.PlayerName;
                }

                if (local.cosmetics != null)
                    local.cosmetics.SetName(renderName);

                TrySetPlayerNameObject(local.Data, renderName);
                if (local.Data != null)
                {
                    TrySetPlayerNameObject(local.Data.DefaultOutfit, renderName);
                    TrySetPlayerNameObject(local.CurrentOutfit, renderName);
                }

                if (notify)
                    ShowNotification($"<color=#00FFAA>[LOCAL NAME]</color> {L("Applied locally:", "Локально применен:")} <b>{newName}</b>");
            }
            catch { }
        }

        private static void RestoreLocalNameSelf()
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.cosmetics == null) return;

                string baseName = !string.IsNullOrWhiteSpace(originalLocalName)
                    ? originalLocalName
                    : (local.Data?.PlayerName ?? local.CurrentOutfit?.PlayerName);
                if (!string.IsNullOrWhiteSpace(baseName))
                {
                    local.cosmetics.SetName(baseName);
                    TrySetPlayerNameObject(local.Data, baseName);
                    if (local.Data != null)
                    {
                        TrySetPlayerNameObject(local.Data.DefaultOutfit, baseName);
                        TrySetPlayerNameObject(local.CurrentOutfit, baseName);
                    }
                }

                originalLocalName = null;
            }
            catch { }
        }

        private static void ApplyLocalFriendCodeSelf(string fakeFriendCode, bool notify = true)
        {
            try
            {
                PlayerControl local = PlayerControl.LocalPlayer;
                if (local == null || local.Data == null)
                {
                    if (notify) ShowNotification("<color=#FF4444>[LOCAL FC]</color> Local player data not found.");
                    return;
                }

                fakeFriendCode ??= string.Empty;
                string current = local.Data.FriendCode ?? string.Empty;
                if (originalLocalFriendCode == null && current != fakeFriendCode)
                    originalLocalFriendCode = current;

                TrySetStringMember(local.Data, "FriendCode", fakeFriendCode);

                if (notify)
                    ShowNotification($"<color=#00FFAA>[LOCAL FC]</color> {L("Applied locally:", "Локально применен:")} <b>{fakeFriendCode}</b>");
            }
            catch { }
        }

        private static void RestoreLocalFriendCodeSelf()
        {
            try
            {
                if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null || originalLocalFriendCode == null) return;
                TrySetStringMember(PlayerControl.LocalPlayer.Data, "FriendCode", originalLocalFriendCode);
                originalLocalFriendCode = null;
            }
            catch { }
        }

        private static void TrySetPlayerNameObject(object target, string newName)
        {
            TrySetStringMember(target, "PlayerName", newName);
        }

        private static void TrySetStringMember(object target, string memberName, string value)
        {
            if (target == null || string.IsNullOrEmpty(memberName)) return;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type type = target.GetType();

            try
            {
                PropertyInfo property = type.GetProperty(memberName, flags);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(target, value, null);
                    return;
                }
            }
            catch { }

            try
            {
                FieldInfo field = type.GetField(memberName, flags);
                if (field != null) field.SetValue(target, value);
            }
            catch { }
        }

        private static void TryInvokeStringMethod(object target, string methodName, string value)
        {
            if (target == null) return;

            try
            {
                MethodInfo method = target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string) },
                    null);

                if (method != null)
                    method.Invoke(target, new object[] { value });
            }
            catch { }
        }

        public static bool showWatermark = true;
        public static bool whiteMenuTheme = false;

        private static void SaveBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
        }

        private static bool LoadBool(string key, bool defaultValue)
        {
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) == 1 : defaultValue;
        }

        private static int LoadInt(string key, int defaultValue)
        {
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : defaultValue;
        }

        private static float LoadFloat(string key, float defaultValue)
        {
            return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetFloat(key) : defaultValue;
        }

        private void SaveConfig()
        {
            try
            {
                PlayerPrefs.SetInt("M_BndMagnet", (int)bindMagnetCursor);
                Plugin.SpoofedLevel.Value = spoofLevelString;
                Plugin.EnableFriendCodeSpoofConfig.Value = enableFriendCodeSpoof;
                Plugin.SpoofFriendCodeConfig.Value = spoofFriendCodeInput;
                Plugin.EnablePlatformSpoof.Value = enablePlatformSpoof;
                Plugin.AutoBanBrokenFriendCodeConfig.Value = autoBanBrokenFriendCode;
                Plugin.PlatformIndex.Value = currentPlatformIndex;
                Plugin.ShowWatermarkConfig.Value = showWatermark;
                Plugin.UnlockCosmeticsConfig.Value = unlockCosmetics;
                Plugin.MoreLobbyInfoConfig.Value = moreLobbyInfo;
                Plugin.EnableChatDarkModeConfig.Value = enableChatDarkMode;
                Plugin.GhostChatColorConfig.Value = SanitizeHexColor(ghostChatColorHex, "#D7B8FF");
                Plugin.EnableAnomalyLogReportsConfig.Value = enableAnomalyLogReports;
                Plugin.ShowEspFriendCodeConfig.Value = showEspFriendCode;
                Plugin.RpcSpoofDelayConfig.Value = rpcSpoofDelay;
                Plugin.MenuColorIndexConfig.Value = currentMenuColorIndex;
                Plugin.RgbMenuModeConfig.Value = rgbMenuMode;
                Plugin.RgbMenuTextConfig.Value = rgbMenuText;
                SaveBool("M_RgbMenuText", rgbMenuText);
                if (menuToggleKey == KeyCode.None) menuToggleKey = KeyCode.Insert;
                Plugin.MenuKeybind.Value = menuToggleKey;
                PlayerPrefs.SetInt("M_MenuToggleKey", (int)menuToggleKey);
                SaveBool("M_WhiteTheme", whiteMenuTheme);
                PlayerPrefs.SetInt("M_MenuLanguageIndex", currentMenuLanguageIndex);
                PlayerPrefs.SetInt("M_FpsLimit", fpsLimit);
                SaveBool("M_EnableBackground", enableBackground);
                SaveBool("M_HardMenu", hardMenu);
                SaveBool("M_EnableCustomNotifs", EnableCustomNotifs);
                SaveBool("M_LogAllRPCs", LogAllRPCs);
                PlayerPrefs.SetInt("M_SelectedSpoofMenuIndex", selectedSpoofMenuIndex);
                PlayerPrefs.SetFloat("M_MenuWindowX", windowRect.x);
                PlayerPrefs.SetFloat("M_MenuWindowY", windowRect.y);
                PlayerPrefs.SetFloat("M_MenuWindowW", windowRect.width);
                PlayerPrefs.SetFloat("M_MenuWindowH", windowRect.height);
                PlayerPrefs.SetInt("M_CurrentTab", currentTab);
                PlayerPrefs.SetInt("M_TargetTab", targetTabIndex);
                PlayerPrefs.SetInt("M_CurrentGeneralSubTab", currentGeneralSubTab);
                PlayerPrefs.SetInt("M_CurrentGeneralInfoSubTab", currentGeneralInfoSubTab);
                PlayerPrefs.SetInt("M_CurrentSelfSubTab", currentSelfSubTab);
                PlayerPrefs.SetInt("M_CurrentVisualsSubTab", currentVisualsSubTab);
                PlayerPrefs.SetInt("M_CurrentPlayersSubTab", currentPlayersSubTab);
                PlayerPrefs.SetInt("M_CurrentHostOnlySubTab", currentHostOnlySubTab);
                PlayerPrefs.SetInt("M_CurrentAutoHostSubTab", currentAutoHostSubTab);
                PlayerPrefs.SetInt("M_BndMMorph", (int)bindMassMorph);
                PlayerPrefs.SetInt("M_BndSpawn", (int)bindSpawnLobby);
                PlayerPrefs.SetInt("M_BndDespawn", (int)bindDespawnLobby);
                PlayerPrefs.SetInt("M_BndCloseMtg", (int)bindCloseMeeting);
                PlayerPrefs.SetInt("M_BndInstaStart", (int)bindInstaStart);
                PlayerPrefs.SetInt("M_BndEndCrew", (int)bindEndCrew);
                PlayerPrefs.SetInt("M_BndEndImp", (int)bindEndImp);
                PlayerPrefs.SetInt("M_BndEndImpDC", (int)bindEndImpDC);
                PlayerPrefs.SetInt("M_BndEndHnsDC", (int)bindEndHnsDC);
                PlayerPrefs.SetInt("M_BndToggleTracers", (int)bindToggleTracers);
                PlayerPrefs.SetInt("M_BndToggleNoClip", (int)bindToggleNoClip);
                PlayerPrefs.SetInt("M_BndToggleFreecam", (int)bindToggleFreecam);
                PlayerPrefs.SetInt("M_BndToggleCameraZoom", (int)bindToggleCameraZoom);
                PlayerPrefs.SetInt("M_BndKillAll", (int)bindKillAll);
                PlayerPrefs.SetInt("M_BndCallMeeting", (int)bindCallMeeting);
                PlayerPrefs.SetInt("M_BndTogglePlayerInfo", (int)bindTogglePlayerInfo);
                PlayerPrefs.SetInt("M_BndToggleSeeRoles", (int)bindToggleSeeRoles);
                PlayerPrefs.SetInt("M_BndToggleSeeGhosts", (int)bindToggleSeeGhosts);
                PlayerPrefs.SetInt("M_BndToggleFullBright", (int)bindToggleFullBright);
                PlayerPrefs.SetInt("M_BndKickAll", (int)bindKickAll);
                PlayerPrefs.SetInt("M_BndFixSabotages", (int)bindFixSabotages);
                PlayerPrefs.SetInt("M_BndSetAllGhost", (int)bindSetAllGhost);
                PlayerPrefs.SetInt("M_BndSetAllGhostImp", (int)bindSetAllGhostImp);
                SaveBool("M_AutoKickBugs", autoKickBugs);
                PlayerPrefs.SetFloat("M_AutoKickTimer", autoKickTimer);
                SaveBool("M_DisableVoteKicks", disableVoteKicks);
                SaveBool("M_LocalNameSpoof", enableLocalNameSpoof);
                SaveBool("M_LocalFakeFCEnabled", enableLocalFriendCodeSpoof);
                PlayerPrefs.SetString("M_LocalFakeFC", localFriendCodeInput);

                SaveBool("M_ShowPlayerInfo", showPlayerInfo);
                SaveBool("M_SeeGhosts", seeGhosts);
                SaveBool("M_SeeRoles", seeRoles);
                SaveBool("M_RevealMeetingRoles", revealMeetingRoles);
                SaveBool("M_ShowTracers", showTracers);
                SaveBool("M_FullBright", fullBright);
                SaveBool("M_SeeProtections", seeProtections);
                SaveBool("M_SeeKillCooldown", seeKillCooldown);
                SaveBool("M_ExtendedLobby", extendedLobby);
                SaveBool("M_MoreLobbyInfo", moreLobbyInfo);
                SaveBool("M_AlwaysChat", alwaysChat);
                SaveBool("M_ReadGhostChat", readGhostChat);
                SaveBool("M_EnableExtendedChat", enableExtendedChat);
                SaveBool("M_EnableFastChat", enableFastChat);
                SaveBool("M_AllowLinksAndSymbols", allowLinksAndSymbols);
                SaveBool("M_EnableChatHistory", enableChatHistory);
                PlayerPrefs.SetInt("M_ChatHistoryLimit", chatHistoryLimit);
                SaveBool("M_EnableClipboard", enableClipboard);
                SaveBool("M_EnableChatLog", enableChatLog);
                SaveBool("M_EnableColorCommand", enableColorCommand);
                SaveBool("M_BlockRainbowChat", blockRainbowChat);
                SaveBool("M_BlockFortegreenChat", blockFortegreenChat);
                SaveBool("M_SpoofMenuEnabled", SpoofMenuEnabled);
                SaveBool("M_NoClip", noClip);
                SaveBool("M_TpToCursor", tpToCursor);
                SaveBool("M_DragToCursor", dragToCursor);
                SaveBool("M_AutoFollowCursor", autoFollowCursor);
                SaveBool("M_Freecam", freecam);
                SaveBool("M_CameraZoom", cameraZoom);
                SaveBool("M_RevealVotes", RevealVotesEnabled);
                SaveBool("M_NoTaskMode", noTaskMode);
                SaveBool("M_NoMapCooldowns", noMapCooldowns);
                SaveBool("M_NeverEndGame", neverEndGame);
                SaveBool("M_RemovePenalty", removePenalty);
                SaveBool("M_AlwaysShowLobbyTimer", alwaysShowLobbyTimer);
                SaveBool("M_AutoBanEnabled", autoBanEnabled);
                SaveBool("M_AllowDuplicateColors", allowDuplicateColors);
                SaveBool("M_BlockSpoofRPC", blockSpoofRPC);
                SaveBool("M_AutoBanPlatformSpoof", autoBanPlatformSpoof);
                SaveBool("M_BanCustomPlatformsFromTxt", banCustomPlatformsFromTxt);
                SaveBool("M_AutoKickLowLevel", autoKickLowLevelEnabled);
                PlayerPrefs.SetInt("M_AutoKickMinLevel", Mathf.Clamp(autoKickMinLevel, 1, 300));
                SaveBool("M_BlockSabotageRPC", blockSabotageRPC);
                PlayerPrefs.SetInt("M_PunishmentMode", punishmentMode);
                SaveBool("M_BlockGameRpcInLobby", blockGameRpcInLobby);
                SaveBool("M_BlockChatFloodRpc", blockChatFloodRpc);
                SaveBool("M_BlockMeetingFloodRpc", blockMeetingFloodRpc);
                SaveBool("M_PasosLimit", enablePasosLimit);
                SaveBool("M_AntiPasosLocalBan", enableLocalPasosBan);
                SaveBool("M_AntiPasosHostBan", enableHostPasosBan);
                SaveBool("M_MalformedPacketGuard", enableMalformedPacketGuard);
                SaveBool("M_BanMalformedPacketSender", banMalformedPacketSender);
                SaveBool("M_QuickChatEmptyGuard", enableQuickChatEmptyGuard);
                SaveBool("M_BanQuickChatEmptySpammer", banQuickChatEmptySpammer);
                SaveBool("M_UnownedSpawnGuard", enableUnownedSpawnGuard);
                SaveBool("M_AutoHostEnabled", AutoHostEnabled);
                SaveBool("M_AutoReturnLobbyAfterMatch", AutoReturnLobbyAfterMatch);
                SaveBool("M_AutoHostNotifications", AutoHostNotifications);
                SaveBool("M_AutoHostForceLastMinute", AutoHostForceLastMinute);
                SaveBool("M_AutoHostWaitLoadedPlayers", AutoHostWaitLoadedPlayers);
                SaveBool("M_AutoHostCancelBelowMin", AutoHostCancelBelowMin);
                SaveBool("M_AutoHostInstantStart", AutoHostInstantStart);
                SaveBool("M_AutoGhostAfterStart", autoGhostAfterStart);
                PlayerPrefs.SetInt("M_AutoHostMinPlayers", AutoHostMinPlayers);
                PlayerPrefs.SetFloat("M_AutoHostStartDelaySeconds", AutoHostStartDelaySeconds);
                PlayerPrefs.SetInt("M_AutoHostFastStartPlayers", AutoHostFastStartPlayers);
                PlayerPrefs.SetFloat("M_AutoHostFastStartDelaySeconds", AutoHostFastStartDelaySeconds);
                PlayerPrefs.SetFloat("M_WalkSpeed", walkSpeed);
                PlayerPrefs.SetFloat("M_EngineSpeed", engineSpeed);

                Plugin.MenuConfig.Save();

                PlayerPrefs.SetString("M_SpoofName", customNameInput);
                for (int i = 0; i < favoriteOutfitSlots.Length; i++)
                    PlayerPrefs.SetString($"M_FavoriteOutfit_{i}", favoriteOutfitSlots[i] ?? string.Empty);
                PlayerPrefs.Save();
            }
            catch { }
        }
        private void DrawAutoHostTab()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("AUTO HOST SYSTEM", "СИСТЕМА АВТО-ХОСТА"));

            var snapshot = ElysiumAutoHostService.GetStatusSnapshot();
            GUILayout.Label($"<color=#aaaaaa>{L("Status:", "Статус:")}</color> <color=#FFAC1C>{snapshot.State}</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 });
            GUILayout.Space(10);

            AutoHostEnabled = DrawToggle(AutoHostEnabled, L("Enable Auto Host", "Включить Авто-Хост"), 250);
            GUILayout.Space(5);
            AutoReturnLobbyAfterMatch = DrawToggle(AutoReturnLobbyAfterMatch, L("Auto Return To Lobby", "Авто-возврат в лобби"), 250);
            GUILayout.Space(5);
            AutoHostNotifications = DrawToggle(AutoHostNotifications, L("Show Notifications", "Показывать уведомления"), 250);
            GUILayout.Space(5);
            AutoHostWaitLoadedPlayers = DrawToggle(AutoHostWaitLoadedPlayers, L("Wait For Players To Load", "Ждать прогрузки игроков"), 250);
            GUILayout.Space(5);
            AutoHostCancelBelowMin = DrawToggle(AutoHostCancelBelowMin, L("Cancel Countdown If Player Leaves", "Отмена отсчета, если игрок вышел"), 250);
            GUILayout.Space(5);
            AutoHostInstantStart = DrawToggle(AutoHostInstantStart, L("Instant Start (No 5s Wait)", "Мгновенный старт (Без 5с)"), 250);
            GUILayout.Space(5);
            autoGhostAfterStart = DrawToggle(autoGhostAfterStart, L("Auto Ghost After Start", "Авто-призрак после старта"), 250);
            GUILayout.Space(5);
            AutoHostForceLastMinute = DrawToggle(AutoHostForceLastMinute, L("Force Start Last Minute", "Форс-старт на последней минуте"), 250);

            GUILayout.Space(15);

            string hexColor = GetMenuAccentHex();
            GUIStyle sliderLabelStyle = new GUIStyle(toggleLabelStyle) { richText = true };

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{L("Min Players:", "Мин. игроков:")} <color=#{hexColor}>{AutoHostMinPlayers}</color>", sliderLabelStyle, GUILayout.Width(175));
            AutoHostMinPlayers = (int)GUILayout.HorizontalSlider(AutoHostMinPlayers, 1f, 15f, sliderStyle, sliderThumbStyle, GUILayout.Width(335));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{L("Start Delay:", "Задержка старта:")} <color=#{hexColor}>{Mathf.Round(AutoHostStartDelaySeconds)}s</color>", sliderLabelStyle, GUILayout.Width(175));
            AutoHostStartDelaySeconds = GUILayout.HorizontalSlider(AutoHostStartDelaySeconds, 0f, 180f, sliderStyle, sliderThumbStyle, GUILayout.Width(335));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{L("Fast Start Players:", "Игроков для фаст-старта:")} <color=#{hexColor}>{AutoHostFastStartPlayers}</color>", sliderLabelStyle, GUILayout.Width(175));
            AutoHostFastStartPlayers = (int)GUILayout.HorizontalSlider(AutoHostFastStartPlayers, 0f, 15f, sliderStyle, sliderThumbStyle, GUILayout.Width(335));
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{L("Fast Start Delay:", "Задержка фаст-старта:")} <color=#{hexColor}>{Mathf.Round(AutoHostFastStartDelaySeconds)}s</color>", sliderLabelStyle, GUILayout.Width(175));
            AutoHostFastStartDelaySeconds = GUILayout.HorizontalSlider(AutoHostFastStartDelaySeconds, 0f, 60f, sliderStyle, sliderThumbStyle, GUILayout.Width(335));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        private void LoadConfig()
        {
            try
            {
                spoofLevelString = Plugin.SpoofedLevel.Value;
                enableFriendCodeSpoof = Plugin.EnableFriendCodeSpoofConfig.Value;
                spoofFriendCodeInput = Plugin.SpoofFriendCodeConfig.Value;
                enablePlatformSpoof = Plugin.EnablePlatformSpoof.Value;
                autoBanBrokenFriendCode = Plugin.AutoBanBrokenFriendCodeConfig.Value;
                currentPlatformIndex = Plugin.PlatformIndex.Value;
                showWatermark = Plugin.ShowWatermarkConfig.Value;
                unlockCosmetics = Plugin.UnlockCosmeticsConfig.Value;
                moreLobbyInfo = Plugin.MoreLobbyInfoConfig.Value;
                enableChatDarkMode = Plugin.EnableChatDarkModeConfig.Value;
                ghostChatColorHex = SanitizeHexColor(Plugin.GhostChatColorConfig.Value, "#D7B8FF");
                enableAnomalyLogReports = Plugin.EnableAnomalyLogReportsConfig.Value;
                showEspFriendCode = Plugin.ShowEspFriendCodeConfig.Value;
                rpcSpoofDelay = Plugin.RpcSpoofDelayConfig.Value;
                currentMenuColorIndex = Plugin.MenuColorIndexConfig.Value;
                rgbMenuMode = Plugin.RgbMenuModeConfig.Value;
                rgbMenuText = LoadBool("M_RgbMenuText", Plugin.RgbMenuTextConfig.Value);
                whiteMenuTheme = LoadBool("M_WhiteTheme", whiteMenuTheme);
                currentMenuLanguageIndex = Mathf.Clamp(LoadInt("M_MenuLanguageIndex", currentMenuLanguageIndex), 0, menuLanguageNames.Length - 1);
                fpsLimit = Mathf.Clamp(LoadInt("M_FpsLimit", fpsLimit), 60, 240);
                ApplyFpsLimit();
                autoKickBugs = LoadBool("M_AutoKickBugs", autoKickBugs);
                if (PlayerPrefs.HasKey("M_AutoKickTimer")) autoKickTimer = PlayerPrefs.GetFloat("M_AutoKickTimer");
                disableVoteKicks = LoadBool("M_DisableVoteKicks", disableVoteKicks);
                enableLocalNameSpoof = LoadBool("M_LocalNameSpoof", enableLocalNameSpoof);
                enableLocalFriendCodeSpoof = LoadBool("M_LocalFakeFCEnabled", enableLocalFriendCodeSpoof);
                if (PlayerPrefs.HasKey("M_LocalFakeFC")) localFriendCodeInput = PlayerPrefs.GetString("M_LocalFakeFC");
                if (PlayerPrefs.HasKey("M_BndMagnet")) bindMagnetCursor = (KeyCode)PlayerPrefs.GetInt("M_BndMagnet");
                menuToggleKey = Plugin.MenuKeybind.Value == KeyCode.None ? KeyCode.Insert : Plugin.MenuKeybind.Value;
                if (PlayerPrefs.HasKey("M_MenuToggleKey")) menuToggleKey = (KeyCode)PlayerPrefs.GetInt("M_MenuToggleKey");
                if (menuToggleKey == KeyCode.None) menuToggleKey = KeyCode.Insert;
                if (PlayerPrefs.HasKey("M_BndMMorph")) bindMassMorph = (KeyCode)PlayerPrefs.GetInt("M_BndMMorph");
                if (PlayerPrefs.HasKey("M_BndSpawn")) bindSpawnLobby = (KeyCode)PlayerPrefs.GetInt("M_BndSpawn");
                if (PlayerPrefs.HasKey("M_BndDespawn")) bindDespawnLobby = (KeyCode)PlayerPrefs.GetInt("M_BndDespawn");
                if (PlayerPrefs.HasKey("M_BndCloseMtg")) bindCloseMeeting = (KeyCode)PlayerPrefs.GetInt("M_BndCloseMtg");
                if (PlayerPrefs.HasKey("M_BndInstaStart")) bindInstaStart = (KeyCode)PlayerPrefs.GetInt("M_BndInstaStart");
                if (PlayerPrefs.HasKey("M_BndEndCrew")) bindEndCrew = (KeyCode)PlayerPrefs.GetInt("M_BndEndCrew");
                if (PlayerPrefs.HasKey("M_BndEndImp")) bindEndImp = (KeyCode)PlayerPrefs.GetInt("M_BndEndImp");
                if (PlayerPrefs.HasKey("M_BndEndImpDC")) bindEndImpDC = (KeyCode)PlayerPrefs.GetInt("M_BndEndImpDC");
                if (PlayerPrefs.HasKey("M_BndEndHnsDC")) bindEndHnsDC = (KeyCode)PlayerPrefs.GetInt("M_BndEndHnsDC");
                if (PlayerPrefs.HasKey("M_BndToggleTracers")) bindToggleTracers = (KeyCode)PlayerPrefs.GetInt("M_BndToggleTracers");
                if (PlayerPrefs.HasKey("M_BndToggleNoClip")) bindToggleNoClip = (KeyCode)PlayerPrefs.GetInt("M_BndToggleNoClip");
                if (PlayerPrefs.HasKey("M_BndToggleFreecam")) bindToggleFreecam = (KeyCode)PlayerPrefs.GetInt("M_BndToggleFreecam");
                if (PlayerPrefs.HasKey("M_BndToggleCameraZoom")) bindToggleCameraZoom = (KeyCode)PlayerPrefs.GetInt("M_BndToggleCameraZoom");
                if (PlayerPrefs.HasKey("M_BndKillAll")) bindKillAll = (KeyCode)PlayerPrefs.GetInt("M_BndKillAll");
                if (PlayerPrefs.HasKey("M_BndCallMeeting")) bindCallMeeting = (KeyCode)PlayerPrefs.GetInt("M_BndCallMeeting");
                if (PlayerPrefs.HasKey("M_BndTogglePlayerInfo")) bindTogglePlayerInfo = (KeyCode)PlayerPrefs.GetInt("M_BndTogglePlayerInfo");
                if (PlayerPrefs.HasKey("M_BndToggleSeeRoles")) bindToggleSeeRoles = (KeyCode)PlayerPrefs.GetInt("M_BndToggleSeeRoles");
                if (PlayerPrefs.HasKey("M_BndToggleSeeGhosts")) bindToggleSeeGhosts = (KeyCode)PlayerPrefs.GetInt("M_BndToggleSeeGhosts");
                if (PlayerPrefs.HasKey("M_BndToggleFullBright")) bindToggleFullBright = (KeyCode)PlayerPrefs.GetInt("M_BndToggleFullBright");
                if (PlayerPrefs.HasKey("M_BndKickAll")) bindKickAll = (KeyCode)PlayerPrefs.GetInt("M_BndKickAll");
                if (PlayerPrefs.HasKey("M_BndFixSabotages")) bindFixSabotages = (KeyCode)PlayerPrefs.GetInt("M_BndFixSabotages");
                if (PlayerPrefs.HasKey("M_BndSetAllGhost")) bindSetAllGhost = (KeyCode)PlayerPrefs.GetInt("M_BndSetAllGhost");
                if (PlayerPrefs.HasKey("M_BndSetAllGhostImp")) bindSetAllGhostImp = (KeyCode)PlayerPrefs.GetInt("M_BndSetAllGhostImp");

                if (!rgbMenuMode && currentMenuColorIndex >= 0 && currentMenuColorIndex < menuColors.Length)
                {
                    currentAccentColor = menuColors[currentMenuColorIndex];
                }

                showPlayerInfo = LoadBool("M_ShowPlayerInfo", showPlayerInfo);
                seeGhosts = LoadBool("M_SeeGhosts", seeGhosts);
                seeRoles = LoadBool("M_SeeRoles", seeRoles);
                revealMeetingRoles = LoadBool("M_RevealMeetingRoles", revealMeetingRoles);
                showTracers = LoadBool("M_ShowTracers", showTracers);
                fullBright = LoadBool("M_FullBright", fullBright);
                seeProtections = LoadBool("M_SeeProtections", seeProtections);
                seeKillCooldown = LoadBool("M_SeeKillCooldown", seeKillCooldown);
                extendedLobby = LoadBool("M_ExtendedLobby", extendedLobby);
                moreLobbyInfo = LoadBool("M_MoreLobbyInfo", moreLobbyInfo);
                alwaysChat = LoadBool("M_AlwaysChat", alwaysChat);
                readGhostChat = LoadBool("M_ReadGhostChat", readGhostChat);
                enableExtendedChat = LoadBool("M_EnableExtendedChat", enableExtendedChat);
                enableFastChat = LoadBool("M_EnableFastChat", enableFastChat);
                allowLinksAndSymbols = LoadBool("M_AllowLinksAndSymbols", allowLinksAndSymbols);
                enableChatHistory = LoadBool("M_EnableChatHistory", enableChatHistory);
                chatHistoryLimit = Mathf.Clamp(LoadInt("M_ChatHistoryLimit", chatHistoryLimit), 5, 80);
                enableClipboard = LoadBool("M_EnableClipboard", enableClipboard);
                enableChatLog = LoadBool("M_EnableChatLog", enableChatLog);
                enableColorCommand = LoadBool("M_EnableColorCommand", enableColorCommand);
                blockRainbowChat = LoadBool("M_BlockRainbowChat", blockRainbowChat);
                blockFortegreenChat = LoadBool("M_BlockFortegreenChat", blockFortegreenChat);
                SpoofMenuEnabled = LoadBool("M_SpoofMenuEnabled", SpoofMenuEnabled);
                noClip = LoadBool("M_NoClip", noClip);
                tpToCursor = LoadBool("M_TpToCursor", tpToCursor);
                dragToCursor = LoadBool("M_DragToCursor", dragToCursor);
                autoFollowCursor = LoadBool("M_AutoFollowCursor", autoFollowCursor);
                freecam = LoadBool("M_Freecam", freecam);
                cameraZoom = LoadBool("M_CameraZoom", cameraZoom);
                RevealVotesEnabled = LoadBool("M_RevealVotes", RevealVotesEnabled);
                noTaskMode = LoadBool("M_NoTaskMode", noTaskMode);
                noMapCooldowns = LoadBool("M_NoMapCooldowns", noMapCooldowns);
                neverEndGame = LoadBool("M_NeverEndGame", neverEndGame);
                removePenalty = LoadBool("M_RemovePenalty", removePenalty);
                alwaysShowLobbyTimer = LoadBool("M_AlwaysShowLobbyTimer", alwaysShowLobbyTimer);
                autoBanEnabled = LoadBool("M_AutoBanEnabled", autoBanEnabled);
                allowDuplicateColors = LoadBool("M_AllowDuplicateColors", allowDuplicateColors);
                blockSpoofRPC = LoadBool("M_BlockSpoofRPC", blockSpoofRPC);
                autoBanPlatformSpoof = LoadBool("M_AutoBanPlatformSpoof", autoBanPlatformSpoof);
                banCustomPlatformsFromTxt = LoadBool("M_BanCustomPlatformsFromTxt", banCustomPlatformsFromTxt);
                autoKickLowLevelEnabled = LoadBool("M_AutoKickLowLevel", autoKickLowLevelEnabled);
                autoKickMinLevel = Mathf.Clamp(LoadInt("M_AutoKickMinLevel", autoKickMinLevel), 1, 300);
                blockSabotageRPC = LoadBool("M_BlockSabotageRPC", blockSabotageRPC);
                punishmentMode = Mathf.Clamp(LoadInt("M_PunishmentMode", punishmentMode), 0, punishmentNames.Length - 1);
                blockGameRpcInLobby = LoadBool("M_BlockGameRpcInLobby", blockGameRpcInLobby);
                blockChatFloodRpc = LoadBool("M_BlockChatFloodRpc", blockChatFloodRpc);
                blockMeetingFloodRpc = LoadBool("M_BlockMeetingFloodRpc", blockMeetingFloodRpc);
                enablePasosLimit = LoadBool("M_PasosLimit", enablePasosLimit);
                enableLocalPasosBan = LoadBool("M_AntiPasosLocalBan", enableLocalPasosBan);
                enableHostPasosBan = LoadBool("M_AntiPasosHostBan", enableHostPasosBan);
                enableMalformedPacketGuard = LoadBool("M_MalformedPacketGuard", enableMalformedPacketGuard);
                banMalformedPacketSender = LoadBool("M_BanMalformedPacketSender", banMalformedPacketSender);
                enableQuickChatEmptyGuard = LoadBool("M_QuickChatEmptyGuard", enableQuickChatEmptyGuard);
                banQuickChatEmptySpammer = LoadBool("M_BanQuickChatEmptySpammer", banQuickChatEmptySpammer);
                enableUnownedSpawnGuard = LoadBool("M_UnownedSpawnGuard", enableUnownedSpawnGuard);
                AutoHostEnabled = LoadBool("M_AutoHostEnabled", AutoHostEnabled);
                AutoReturnLobbyAfterMatch = LoadBool("M_AutoReturnLobbyAfterMatch", AutoReturnLobbyAfterMatch);
                AutoHostNotifications = LoadBool("M_AutoHostNotifications", AutoHostNotifications);
                AutoHostForceLastMinute = LoadBool("M_AutoHostForceLastMinute", AutoHostForceLastMinute);
                AutoHostWaitLoadedPlayers = LoadBool("M_AutoHostWaitLoadedPlayers", AutoHostWaitLoadedPlayers);
                AutoHostCancelBelowMin = LoadBool("M_AutoHostCancelBelowMin", AutoHostCancelBelowMin);
                AutoHostInstantStart = LoadBool("M_AutoHostInstantStart", AutoHostInstantStart);
                autoGhostAfterStart = LoadBool("M_AutoGhostAfterStart", autoGhostAfterStart);
                if (PlayerPrefs.HasKey("M_AutoHostMinPlayers")) AutoHostMinPlayers = PlayerPrefs.GetInt("M_AutoHostMinPlayers");
                if (PlayerPrefs.HasKey("M_AutoHostStartDelaySeconds")) AutoHostStartDelaySeconds = PlayerPrefs.GetFloat("M_AutoHostStartDelaySeconds");
                if (PlayerPrefs.HasKey("M_AutoHostFastStartPlayers")) AutoHostFastStartPlayers = PlayerPrefs.GetInt("M_AutoHostFastStartPlayers");
                if (PlayerPrefs.HasKey("M_AutoHostFastStartDelaySeconds")) AutoHostFastStartDelaySeconds = PlayerPrefs.GetFloat("M_AutoHostFastStartDelaySeconds");
                if (PlayerPrefs.HasKey("M_WalkSpeed")) walkSpeed = PlayerPrefs.GetFloat("M_WalkSpeed");
                if (PlayerPrefs.HasKey("M_EngineSpeed")) engineSpeed = PlayerPrefs.GetFloat("M_EngineSpeed");
                for (int i = 0; i < favoriteOutfitSlots.Length; i++)
                    favoriteOutfitSlots[i] = PlayerPrefs.GetString($"M_FavoriteOutfit_{i}", string.Empty);
                enableBackground = LoadBool("M_EnableBackground", enableBackground);
                hardMenu = LoadBool("M_HardMenu", hardMenu);
                EnableCustomNotifs = LoadBool("M_EnableCustomNotifs", EnableCustomNotifs);
                LogAllRPCs = LoadBool("M_LogAllRPCs", LogAllRPCs);
                selectedSpoofMenuIndex = Mathf.Clamp(LoadInt("M_SelectedSpoofMenuIndex", selectedSpoofMenuIndex), 0, spoofMenuNames.Length - 1);
                windowRect = new Rect(
                    LoadFloat("M_MenuWindowX", windowRect.x),
                    LoadFloat("M_MenuWindowY", windowRect.y),
                    Mathf.Clamp(LoadFloat("M_MenuWindowW", windowRect.width), 640f, 1400f),
                    Mathf.Clamp(LoadFloat("M_MenuWindowH", windowRect.height), 420f, 900f));
                currentTab = Mathf.Clamp(LoadInt("M_CurrentTab", currentTab), 0, tabNames.Length - 1);
                targetTabIndex = Mathf.Clamp(LoadInt("M_TargetTab", currentTab), 0, tabNames.Length - 1);
                currentGeneralSubTab = Mathf.Clamp(LoadInt("M_CurrentGeneralSubTab", currentGeneralSubTab), 0, generalSubTabs.Length - 1);
                currentGeneralInfoSubTab = Mathf.Clamp(LoadInt("M_CurrentGeneralInfoSubTab", currentGeneralInfoSubTab), 0, generalInfoSubTabs.Length - 1);
                currentSelfSubTab = Mathf.Clamp(LoadInt("M_CurrentSelfSubTab", currentSelfSubTab), 0, selfSubTabs.Length);
                currentVisualsSubTab = Mathf.Clamp(LoadInt("M_CurrentVisualsSubTab", currentVisualsSubTab), 0, visualsSubTabs.Length - 1);
                currentPlayersSubTab = Mathf.Clamp(LoadInt("M_CurrentPlayersSubTab", currentPlayersSubTab), 0, playersSubTabs.Length - 1);
                currentHostOnlySubTab = Mathf.Clamp(LoadInt("M_CurrentHostOnlySubTab", currentHostOnlySubTab), 0, hostOnlySubTabs.Length - 1);
                currentAutoHostSubTab = Mathf.Clamp(LoadInt("M_CurrentAutoHostSubTab", currentAutoHostSubTab), 0, autoHostSubTabs.Length - 1);
                tabTransitionProgress = 1f;
                SyncKeybindDictionary();
                if (PlayerPrefs.HasKey("M_SpoofName")) customNameInput = PlayerPrefs.GetString("M_SpoofName");
            }
            catch { }
        }

        private static void ApplyFpsLimit()
        {
            try
            {
                fpsLimit = Mathf.Clamp(fpsLimit, 60, 240);
                if (lastAppliedFpsLimit == fpsLimit) return;
                Application.targetFrameRate = fpsLimit;
                QualitySettings.vSyncCount = 0;
                lastAppliedFpsLimit = fpsLimit;
            }
            catch { }
        }

        private static void TrimChatHistoryToLimit()
        {
            try
            {
                chatHistoryLimit = Mathf.Clamp(chatHistoryLimit, 5, 80);
                while (ChatHistory.sentMessages.Count > chatHistoryLimit)
                    ChatHistory.sentMessages.RemoveAt(0);

                ChatHistory.HistoryIndex = Mathf.Clamp(ChatHistory.HistoryIndex, 0, ChatHistory.sentMessages.Count);
            }
            catch { }
        }

        private static void SyncKeybindDictionary()
        {
            try
            {
                keyBinds["Toggle Menu"] = menuToggleKey;
                keyBinds["Magnet Cursor"] = bindMagnetCursor;
                keyBinds["Mass Morph"] = bindMassMorph;
                keyBinds["Spawn Lobby"] = bindSpawnLobby;
                keyBinds["Despawn Lobby"] = bindDespawnLobby;
                keyBinds["Close Meeting"] = bindCloseMeeting;
                keyBinds["Insta Start"] = bindInstaStart;
                keyBinds["End Crew"] = bindEndCrew;
                keyBinds["End Imp"] = bindEndImp;
                keyBinds["End Imp DC"] = bindEndImpDC;
                keyBinds["End H&S DC"] = bindEndHnsDC;
                keyBinds["Toggle Tracers"] = bindToggleTracers;
                keyBinds["Toggle NoClip"] = bindToggleNoClip;
                keyBinds["Toggle Freecam"] = bindToggleFreecam;
                keyBinds["Toggle Camera Zoom"] = bindToggleCameraZoom;
                keyBinds["Toggle Player Info"] = bindTogglePlayerInfo;
                keyBinds["Toggle See Roles"] = bindToggleSeeRoles;
                keyBinds["Toggle See Ghosts"] = bindToggleSeeGhosts;
                keyBinds["Toggle Full Bright"] = bindToggleFullBright;
                keyBinds["Kill All"] = bindKillAll;
                keyBinds["Call Meeting"] = bindCallMeeting;
                keyBinds["Kick All"] = bindKickAll;
                keyBinds["Fix Sabotages"] = bindFixSabotages;
                keyBinds["All Ghost"] = bindSetAllGhost;
                keyBinds["All Ghost Imp"] = bindSetAllGhostImp;
            }
            catch { }
        }
        private Texture2D MakeRoundedTex(int size, Color col, float radius)
        {
            Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
            result.hideFlags = HideFlags.HideAndDontSave;
            Color[] pix = new Color[size * size];
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                    float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    Color c = col;
                    c.a = col.a * alpha;
                    pix[y * size + x] = c;
                }
            }
            result.SetPixels(pix); result.Apply();
            return result;
        }

        private RectOffset CreateRectOffset(int left, int right, int top, int bottom)
        {
            return new RectOffset { left = left, right = right, top = top, bottom = bottom };
        }

        private void UpdateSwitchTex(Texture2D tex, bool isOn, Color accentColor)
        {
            int width = tex.width; int height = tex.height;
            Color transparent = new Color(0, 0, 0, 0);
            Color offBg = new Color(0.23f, 0.23f, 0.23f, 1f);
            Color offKnob = new Color(0.6f, 0.6f, 0.6f, 1f);
            Color bgColor = isOn ? accentColor : offBg;
            Color knobColor = isOn ? Color.white : offKnob;
            float r = height / 2f;
            float cx1 = r; float cx2 = width - r; float cy = r;
            float knobRadius = r - 2f;
            float knobCx = isOn ? cx2 : cx1;
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dLeft = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx1, cy));
                    float dRight = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx2, cy));
                    float dRect = (x + 0.5f >= cx1 && x + 0.5f <= cx2) ? Mathf.Abs((y + 0.5f) - cy) : 9999f;
                    float distBg = Mathf.Min(dLeft, Mathf.Min(dRight, dRect));
                    float alphaBg = Mathf.Clamp01(r - distBg + 0.5f);
                    float distKnob = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(knobCx, cy));
                    float alphaKnob = Mathf.Clamp01(knobRadius - distKnob + 0.5f);
                    if (alphaBg > 0)
                    {
                        Color finalCol = Color.Lerp(bgColor, knobColor, alphaKnob);
                        finalCol.a = alphaBg;
                        pixels[y * width + x] = finalCol;
                    }
                    else pixels[y * width + x] = transparent;
                }
            }
            tex.SetPixels(pixels); tex.Apply();
        }

        private static Color GetThemeAccentColor(Color source)
        {
            if (!whiteMenuTheme) return source;

            Color.RGBToHSV(source, out float h, out float s, out float v);

            if (s < 0.08f)
                return new Color(0.34f, 0.34f, 0.34f, 1f);

            if (h <= 0.04f || h >= 0.96f)
                return new Color(0.50f, 0.14f, 0.18f, 1f);

            if (h >= 0.11f && h <= 0.19f)
                return new Color32(232, 194, 37, 255);

            float targetS = Mathf.Clamp(Mathf.Max(s, 0.55f), 0.55f, 0.95f);
            float targetV = Mathf.Clamp(v * 0.62f, 0.26f, 0.72f);
            Color mapped = Color.HSVToRGB(h, targetS, targetV);
            mapped.a = 1f;
            return mapped;
        }

        public static Color GetMenuAccentColor(bool allowRgbText = true)
        {
            return allowRgbText && RgbMenuTextActive()
                ? GetThemeAccentColor(currentAccentColor)
                : GetThemeAccentColor(GetStableMenuAccentSource());
        }

        public static string GetMenuAccentHex(bool allowRgbText = true)
        {
            return ColorUtility.ToHtmlStringRGB(GetMenuAccentColor(allowRgbText));
        }

        public static Color GetMenuControlAccentColor()
        {
            return rgbMenuMode
                ? GetThemeAccentColor(currentAccentColor)
                : GetThemeAccentColor(GetStableMenuAccentSource());
        }

        private static bool RgbMenuTextActive()
        {
            return rgbMenuMode && rgbMenuText;
        }

        private static Color GetStableMenuAccentSource()
        {
            try
            {
                if (activeGui != null && activeGui.menuColors != null && activeGui.menuColors.Length > 0)
                    return activeGui.menuColors[Mathf.Clamp(activeGui.currentMenuColorIndex, 0, activeGui.menuColors.Length - 1)];
            }
            catch { }

            return currentAccentColor;
        }

        private void UpdateAccentColor(Color color)
        {
            currentAccentColor = color;
            Color effectiveColor = GetThemeAccentColor(color);
            Color controlColor = rgbMenuMode ? effectiveColor : GetMenuAccentColor(false);
            if (texAccent != null)
            {
                int size = texAccent.width;
                Color[] pix = new Color[size * size];
                float center = size / 2f;
                float radius = 6f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                        float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        Color c = controlColor; c.a = alpha;
                        pix[y * size + x] = c;
                    }
                }
                texAccent.SetPixels(pix); texAccent.Apply();
            }
            if (texSliderThumb != null)
            {
                int size = texSliderThumb.width;
                Color[] pix = new Color[size * size];
                float center = size / 2f;
                float radius = 10f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                        float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        Color c = controlColor; c.a = alpha;
                        pix[y * size + x] = c;
                    }
                }
                texSliderThumb.SetPixels(pix); texSliderThumb.Apply();
            }
            if (texScrollThumb != null)
            {
                int size = texScrollThumb.width;
                Color[] pix = new Color[size * size];
                float center = size / 2f;
                float radius = 4f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                        float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        Color c = controlColor; c.a = alpha;
                        pix[y * size + x] = c;
                    }
                }
                texScrollThumb.SetPixels(pix); texScrollThumb.Apply();
            }
            if (texToggleOn != null) UpdateSwitchTex(texToggleOn, true, controlColor);
            bool rgbText = RgbMenuTextActive();
            Color menuHeadingText = rgbText ? effectiveColor : (whiteMenuTheme ? new Color(0.15f, 0.15f, 0.15f, 1f) : GetMenuAccentColor(false));
            if (windowStyle != null) windowStyle.normal.textColor = rgbText ? effectiveColor : (whiteMenuTheme ? new Color(0.16f, 0.16f, 0.16f, 1f) : GetMenuAccentColor(false));
            if (headerStyle != null) headerStyle.normal.textColor = menuHeadingText;
            if (menuSectionTitleStyle != null) menuSectionTitleStyle.normal.textColor = menuHeadingText;
            if (menuBadgeStyle != null) menuBadgeStyle.normal.textColor = menuHeadingText;
            if (activeSidebarBtnStyle != null) { activeSidebarBtnStyle.normal.textColor = effectiveColor; activeSidebarBtnStyle.hover.textColor = effectiveColor; }
            if (activeTabStyle != null) activeTabStyle.normal.background = texAccent;
            if (activeSubTabStyle != null) activeSubTabStyle.normal.background = texAccent;
            if (btnStyle != null) btnStyle.active.background = texAccent;
            if (inputBlockStyle != null) inputBlockStyle.normal.textColor = rgbText ? effectiveColor : (whiteMenuTheme ? new Color(0.15f, 0.15f, 0.15f, 1f) : GetMenuAccentColor(false));
        }

        private void InitStyles()
        {
            bool isLightTheme = whiteMenuTheme;
            Color accent = GetMenuControlAccentColor();
            Color darkBg = isLightTheme ? new Color(0.97f, 0.97f, 0.97f, 0.78f) : new Color(0.12f, 0.12f, 0.12f, 0.90f);
            Color sidebarBg = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            Color boxBg = new Color(0f, 0f, 0f, 0f);
            Color btnCol = isLightTheme ? new Color(0.90f, 0.90f, 0.90f, 0.74f) : new Color(0.23f, 0.23f, 0.23f, 1f);
            Color sliderBgCol = isLightTheme ? new Color(0.78f, 0.78f, 0.78f, 0.68f) : new Color(0.08f, 0.08f, 0.08f, 1f);
            Color textMain = isLightTheme ? new Color(0.18f, 0.18f, 0.18f, 1f) : new Color(0.78f, 0.78f, 0.78f, 1f);
            Color textMuted = isLightTheme ? new Color(0.33f, 0.33f, 0.33f, 1f) : new Color(0.6f, 0.6f, 0.6f, 1f);
            Color textHover = isLightTheme ? new Color(0.06f, 0.06f, 0.06f, 1f) : Color.white;
            Color headerText = RgbMenuTextActive() ? accent : (isLightTheme ? new Color(0.15f, 0.15f, 0.15f, 1f) : GetMenuAccentColor(false));
            Color inputBgCol = isLightTheme ? new Color(1f, 1f, 1f, 0.86f) : new Color(0.08f, 0.08f, 0.08f, 0.85f);

            texWindowBg = MakeRoundedTex(64, darkBg, 12f);
            texSidebarBg = MakeRoundedTex(64, sidebarBg, 0f);
            texBoxBg = MakeRoundedTex(64, boxBg, 0f);
            texBtnBg = MakeRoundedTex(64, btnCol, 6f);
            texAccent = MakeRoundedTex(64, accent, 6f);
            texSliderBg = MakeRoundedTex(64, sliderBgCol, 4f);
            texSliderThumb = MakeRoundedTex(20, accent, 10f);
            texInputBg = MakeRoundedTex(64, inputBgCol, 6f);
            texColorBtn = MakeRoundedTex(64, Color.white, 12f);

            texMenuCard = MakeRoundedTex(64, isLightTheme ? new Color(1f, 1f, 1f, 0.55f) : new Color(1f, 1f, 1f, 0.045f), 12f);

            menuCardStyle = new GUIStyle();
            menuCardStyle.normal.background = texMenuCard;
            menuCardStyle.border = CreateRectOffset(12, 12, 12, 12);
            menuCardStyle.padding = CreateRectOffset(14, 14, 12, 14);
            menuCardStyle.margin = CreateRectOffset(0, 0, 0, 10);

            menuAccentBarStyle = new GUIStyle();
            menuAccentBarStyle.normal.background = texAccent;

            menuSectionTitleStyle = new GUIStyle();
            menuSectionTitleStyle.normal.textColor = headerText;
            menuSectionTitleStyle.fontStyle = FontStyle.Bold;
            menuSectionTitleStyle.fontSize = 13;
            menuSectionTitleStyle.alignment = TextAnchor.MiddleLeft;
            menuSectionTitleStyle.richText = true;

            menuDescStyle = new GUIStyle();
            menuDescStyle.normal.textColor = textMuted;
            menuDescStyle.fontSize = 11;
            menuDescStyle.richText = true;
            menuDescStyle.wordWrap = true;
            menuDescStyle.padding = CreateRectOffset(2, 0, 2, 0);

            menuBadgeStyle = new GUIStyle();
            menuBadgeStyle.normal.background = texInputBg;
            menuBadgeStyle.normal.textColor = headerText;
            menuBadgeStyle.fontStyle = FontStyle.Bold;
            menuBadgeStyle.fontSize = 12;
            menuBadgeStyle.alignment = TextAnchor.MiddleCenter;
            menuBadgeStyle.border = CreateRectOffset(6, 6, 6, 6);
            menuBadgeStyle.padding = CreateRectOffset(8, 8, 3, 3);

            menuSwatchStyle = new GUIStyle();
            menuSwatchStyle.normal.background = texColorBtn;
            menuSwatchStyle.border = CreateRectOffset(8, 8, 8, 8);

            texToggleOff = new Texture2D(30, 16, TextureFormat.RGBA32, false);
            texToggleOff.hideFlags = HideFlags.HideAndDontSave;
            texToggleOn = new Texture2D(30, 16, TextureFormat.RGBA32, false);
            texToggleOn.hideFlags = HideFlags.HideAndDontSave;
            UpdateSwitchTex(texToggleOff, false, Color.white);
            UpdateSwitchTex(texToggleOn, true, accent);

            safeLineStyle = new GUIStyle();
            safeLineStyle.normal.background = MakeRoundedTex(2, isLightTheme ? new Color(0.75f, 0.75f, 0.75f, 1f) : Color.white, 0f);

            windowStyle = new GUIStyle();
            windowStyle.normal.background = texWindowBg;
            windowStyle.normal.textColor = RgbMenuTextActive() ? accent : (isLightTheme ? new Color(0.16f, 0.16f, 0.16f, 1f) : GetMenuAccentColor(false));
            windowStyle.fontStyle = FontStyle.Bold;
            windowStyle.fontSize = 14;
            windowStyle.padding = CreateRectOffset(0, 0, 0, 0);
            windowStyle.border = CreateRectOffset(12, 12, 12, 12);

            boxStyle = new GUIStyle();
            boxStyle.normal.background = texBoxBg;
            boxStyle.padding = CreateRectOffset(0, 0, 0, 0);
            boxStyle.margin = CreateRectOffset(0, 0, 4, 8);

            btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.normal.background = texBtnBg;
            btnStyle.normal.textColor = textMain;
            btnStyle.active.background = texAccent;
            btnStyle.active.textColor = Color.black;
            btnStyle.alignment = TextAnchor.MiddleCenter;
            btnStyle.border = CreateRectOffset(6, 6, 6, 6);
            btnStyle.fontSize = 12;
            btnStyle.fontStyle = FontStyle.Bold;
            btnStyle.clipping = TextClipping.Overflow;
            btnStyle.wordWrap = false;

            activeTabStyle = new GUIStyle(btnStyle);
            activeTabStyle.normal.background = texAccent;
            activeTabStyle.normal.textColor = Color.black;

            subTabStyle = new GUIStyle(btnStyle);
            subTabStyle.padding = CreateRectOffset(8, 8, 2, 2);
            subTabStyle.clipping = TextClipping.Overflow;
            subTabStyle.wordWrap = false;
            activeSubTabStyle = new GUIStyle(activeTabStyle);
            activeSubTabStyle.padding = CreateRectOffset(8, 8, 2, 2);
            activeSubTabStyle.clipping = TextClipping.Overflow;
            activeSubTabStyle.wordWrap = false;

            inputBlockStyle = new GUIStyle(btnStyle);
            inputBlockStyle.normal.background = texInputBg;
            inputBlockStyle.hover.background = texInputBg;
            inputBlockStyle.active.background = texAccent;
            inputBlockStyle.normal.textColor = RgbMenuTextActive() ? accent : (isLightTheme ? new Color(0.15f, 0.15f, 0.15f, 1f) : GetMenuAccentColor(false));
            inputBlockStyle.alignment = TextAnchor.MiddleCenter;
            inputBlockStyle.fontStyle = FontStyle.Bold;

            headerStyle = new GUIStyle();
            headerStyle.normal.background = texBtnBg;
            headerStyle.normal.textColor = headerText;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleLeft;
            headerStyle.padding = CreateRectOffset(6, 6, 4, 4);
            headerStyle.margin = CreateRectOffset(0, 0, 4, 4);
            headerStyle.fontSize = 13;
            headerStyle.clipping = TextClipping.Overflow;
            headerStyle.wordWrap = false;

            sidebarStyle = new GUIStyle();
            sidebarStyle.normal.background = texSidebarBg;
            sidebarStyle.padding = CreateRectOffset(0, 0, 5, 0);

            sidebarBtnStyle = new GUIStyle();
            sidebarBtnStyle.normal.textColor = textMuted;
            sidebarBtnStyle.hover.textColor = textHover;
            sidebarBtnStyle.padding = CreateRectOffset(12, 0, 6, 6);
            sidebarBtnStyle.alignment = TextAnchor.MiddleLeft;
            sidebarBtnStyle.fontSize = 13;
            sidebarBtnStyle.fontStyle = FontStyle.Bold;

            activeSidebarBtnStyle = new GUIStyle(sidebarBtnStyle);
            activeSidebarBtnStyle.normal.textColor = accent;
            activeSidebarBtnStyle.hover.textColor = accent;

            toggleOffStyle = new GUIStyle();
            toggleOffStyle.normal.background = texToggleOff;
            toggleOnStyle = new GUIStyle();
            toggleOnStyle.normal.background = texToggleOn;

            toggleLabelStyle = new GUIStyle();
            toggleLabelStyle.normal.textColor = textMain;
            toggleLabelStyle.alignment = TextAnchor.MiddleLeft;
            toggleLabelStyle.padding = CreateRectOffset(4, 0, 0, 0);
            toggleLabelStyle.fontSize = 12;
            toggleLabelStyle.fontStyle = FontStyle.Bold;
            toggleLabelStyle.clipping = TextClipping.Overflow;
            toggleLabelStyle.wordWrap = false;
            toggleLabelStyle.richText = true;

            sliderStyle = new GUIStyle();
            sliderStyle.normal.background = texSliderBg;
            sliderStyle.border = CreateRectOffset(6, 6, 6, 6);
            sliderStyle.fixedHeight = 10f;
            sliderStyle.margin = CreateRectOffset(0, 0, 8, 8);

            sliderThumbStyle = new GUIStyle();
            sliderThumbStyle.normal.background = texSliderThumb;
            sliderThumbStyle.fixedWidth = 18f;
            sliderThumbStyle.fixedHeight = 18f;
            sliderThumbStyle.margin = CreateRectOffset(0, 0, -4, 0);

            titleStyle = new GUIStyle();
            titleStyle.normal.textColor = accent;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize = 14;
            titleStyle.padding = CreateRectOffset(10, 0, 8, 0);

            Texture2D texScrollBg = MakeRoundedTex(8, new Color(0.1f, 0.1f, 0.1f, 0.2f), 4f);
            texScrollThumb = MakeRoundedTex(8, accent, 4f);

            GUIStyle scrollBarStyle = new GUIStyle(GUI.skin.verticalScrollbar);
            scrollBarStyle.normal.background = texScrollBg;
            scrollBarStyle.fixedWidth = 8f;
            scrollBarStyle.border = CreateRectOffset(0, 0, 4, 4);
            scrollBarStyle.margin = CreateRectOffset(2, 2, 2, 2);

            GUIStyle scrollBarThumbStyle = new GUIStyle(GUI.skin.verticalScrollbarThumb);
            scrollBarThumbStyle.normal.background = texScrollThumb;
            scrollBarThumbStyle.hover.background = texScrollThumb;
            scrollBarThumbStyle.active.background = texScrollThumb;
            scrollBarThumbStyle.fixedWidth = 8f;
            scrollBarThumbStyle.border = CreateRectOffset(0, 0, 4, 4);

            GUI.skin.verticalScrollbar = scrollBarStyle;
            GUI.skin.verticalScrollbarThumb = scrollBarThumbStyle;
            GUI.skin.horizontalScrollbar.normal.background = null;
            GUI.skin.horizontalScrollbarThumb.normal.background = null;
            GUI.skin.label.normal.textColor = textMain;
            GUI.skin.box.normal.textColor = textMain;

            stylesInited = true;
        }
        public static bool autoCopyCodeAndLeave = false;
        public static HashSet<int> votedPlayerIds = new HashSet<int>();

        private void LoadBackgroundImage()
        {
            try
            {
                string bgPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "MenuBG.png");
                if (!System.IO.File.Exists(bgPath)) bgPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "MenuBG.jpg");
                if (System.IO.File.Exists(bgPath))
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(bgPath);
                    Texture2D tempTex = new Texture2D(2, 2);
                    ImageConversion.LoadImage(tempTex, fileData);
                    customMenuBg = new Texture2D(tempTex.width, tempTex.height, TextureFormat.RGBA32, false);
                    customMenuBg.hideFlags = HideFlags.HideAndDontSave;
                    Color[] pix = tempTex.GetPixels();
                    UnityEngine.Object.Destroy(tempTex);
                    int w = customMenuBg.width, h = customMenuBg.height;
                    float targetRadius = 12f, rx = targetRadius * (w / windowRect.width), ry = targetRadius * (h / windowRect.height);
                    for (int y = 0; y < h; y++)
                        for (int x = 0; x < w; x++)
                        {
                            float dx = 0f, dy = 0f;
                            if (x < rx) dx = rx - x;
                            else if (x > w - rx) dx = x - (w - rx);
                            if (y < ry) dy = ry - y;
                            else if (y > h - ry) dy = y - (h - ry);
                            if (dx > 0 && dy > 0)
                            {
                                float nx = dx / rx, ny = dy / ry;
                                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                                if (dist > 1f) { Color c = pix[y * w + x]; c.a = 0f; pix[y * w + x] = c; }
                                else
                                {
                                    float alphaMult = Mathf.Clamp01((1f - dist) * Mathf.Max(rx, ry));
                                    Color c = pix[y * w + x]; c.a *= alphaMult; pix[y * w + x] = c;
                                }
                            }
                        }
                    customMenuBg.SetPixels(pix); customMenuBg.Apply();
                }
                else enableBackground = false;
            }
            catch { enableBackground = false; }
        }

        public static string ApplyMenuShimmer(string text)
        {
            string result = "";
            Color baseColor = currentAccentColor, glowColor = Color.white;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ') { result += " "; continue; }
                float wave = Mathf.Sin(Time.unscaledTime * 6f - (i * 0.4f)) * 0.5f + 0.5f;
                Color c = Color.Lerp(baseColor, glowColor, wave);
                result += $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text[i]}</color>";
            }
            return result;
        }

        private bool DrawToggle(bool value, string text, int width = 0)
        {
            GUILayout.BeginHorizontal(GUILayout.MinWidth(width > 0 ? width : 200), GUILayout.Height(20));

            bool clickedBox = GUILayout.Button("", value ? toggleOnStyle : toggleOffStyle, GUILayout.Width(30), GUILayout.Height(16));

            GUILayout.Space(6);

            GUIStyle toggleTextStyle = new GUIStyle(toggleLabelStyle)
            {
                clipping = TextClipping.Overflow,
                wordWrap = false,
                richText = true,
                stretchWidth = false,
                alignment = TextAnchor.MiddleLeft
            };

            GUIContent toggleContent = new GUIContent(text);
            float toggleTextWidth = Mathf.Ceil(toggleTextStyle.CalcSize(toggleContent).x) + 8f;
            Rect textRect = GUILayoutUtility.GetRect(toggleTextWidth, 18f, GUILayout.Width(toggleTextWidth), GUILayout.Height(18f));
            GUI.Label(textRect, toggleContent, toggleTextStyle);

            bool clickedText = Event.current.type == EventType.MouseDown && textRect.Contains(Event.current.mousePosition);
            if (clickedText) Event.current.Use();

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            if (clickedBox || clickedText) settingsDirty = true;
            return (clickedBox || clickedText) ? !value : value;
        }

        private bool DrawBindableButton(string label, string bindKey, float width)
        {
            bool clicked = false;
            GUILayout.BeginVertical(GUILayout.Width(width));
            if (GUILayout.Button(label, btnStyle, GUILayout.Height(25), GUILayout.Width(width))) clicked = true;
            string bindTxt = bindingAction == bindKey ? "Press Key..." : (keyBinds.ContainsKey(bindKey) ? $"[{keyBinds[bindKey]}]" : "[Bind Key]");
            GUIStyle bindStyle = new GUIStyle(btnStyle) { fontSize = 10, normal = { textColor = new Color(0.6f, 0.6f, 0.6f) } };
            if (bindingAction == bindKey) bindStyle.normal.textColor = GetMenuAccentColor();
            if (GUILayout.Button(bindTxt, bindStyle, GUILayout.Height(15), GUILayout.Width(width))) bindingAction = bindKey;
            GUILayout.EndVertical();
            return clicked;
        }

        private bool DrawHostToggle(bool value, string text, float totalWidth = 250f)
        {
            GUILayout.BeginHorizontal(GUILayout.MinWidth(totalWidth), GUILayout.Height(20));
            bool clickedBox = GUILayout.Button("", value ? toggleOnStyle : toggleOffStyle, GUILayout.Width(30), GUILayout.Height(16));
            GUILayout.Space(6);

            GUIStyle hostToggleTextStyle = new GUIStyle(toggleLabelStyle)
            {
                clipping = TextClipping.Overflow,
                wordWrap = false,
                richText = true,
                stretchWidth = false,
                alignment = TextAnchor.MiddleLeft
            };

            GUIContent hostToggleContent = new GUIContent(text);
            float hostToggleTextWidth = Mathf.Ceil(hostToggleTextStyle.CalcSize(hostToggleContent).x) + 8f;
            Rect textRect = GUILayoutUtility.GetRect(hostToggleTextWidth, 16f, GUILayout.Width(hostToggleTextWidth), GUILayout.Height(16f));
            GUI.Label(textRect, hostToggleContent, hostToggleTextStyle);

            bool clickedText = Event.current.type == EventType.MouseDown && textRect.Contains(Event.current.mousePosition);
            if (clickedText) Event.current.Use();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (clickedBox || clickedText) settingsDirty = true;
            return (clickedBox || clickedText) ? !value : value;
        }
        private void DrawBindsTab()
        {
            GUILayout.BeginVertical(menuCardStyle);
            try
            {
                DrawMenuSectionHeader("CUSTOM KEYBINDS");
                GUILayout.Label(L("Menu toggle is configurable. Right Shift stays disabled.", "Кнопку меню можно менять. Right Shift выключен."), menuDescStyle);
                GUILayout.Space(10);

                DrawKeybindRow("Menu Toggle:", ref menuToggleKey, ref isWaitingForBind);
                DrawKeybindRow("Magnet Cursor:", ref bindMagnetCursor, ref isWaitBindMagnetCursor);
                DrawKeybindRow("Mass Morph:", ref bindMassMorph, ref isWaitBindMassMorph);
                DrawKeybindRow("Spawn Lobby:", ref bindSpawnLobby, ref isWaitBindSpawnLobby);
                DrawKeybindRow("Despawn Lobby:", ref bindDespawnLobby, ref isWaitBindDespawnLobby);
                DrawKeybindRow("Close Meeting:", ref bindCloseMeeting, ref isWaitBindCloseMeeting);
                DrawKeybindRow("Insta Start:", ref bindInstaStart, ref isWaitBindInstaStart);
                DrawKeybindRow("End: Crewmate Win:", ref bindEndCrew, ref isWaitBindEndCrew);
                DrawKeybindRow("End: Impostor Win:", ref bindEndImp, ref isWaitBindEndImp);
                DrawKeybindRow("End: Imp Disconnect:", ref bindEndImpDC, ref isWaitBindEndImpDC);
                DrawKeybindRow("End: H&S Disconnect:", ref bindEndHnsDC, ref isWaitBindEndHnsDC);
                DrawKeybindRow("Toggle Tracers:", ref bindToggleTracers, ref isWaitBindToggleTracers);
                DrawKeybindRow("Toggle NoClip:", ref bindToggleNoClip, ref isWaitBindToggleNoClip);
                DrawKeybindRow("Toggle Freecam:", ref bindToggleFreecam, ref isWaitBindToggleFreecam);
                DrawKeybindRow("Toggle Camera Zoom:", ref bindToggleCameraZoom, ref isWaitBindToggleCameraZoom);
                DrawKeybindRow("Toggle Player Info:", ref bindTogglePlayerInfo, ref isWaitBindTogglePlayerInfo);
                DrawKeybindRow("Toggle See Roles:", ref bindToggleSeeRoles, ref isWaitBindToggleSeeRoles);
                DrawKeybindRow("Toggle See Ghosts:", ref bindToggleSeeGhosts, ref isWaitBindToggleSeeGhosts);
                DrawKeybindRow("Toggle Full Bright:", ref bindToggleFullBright, ref isWaitBindToggleFullBright);
                DrawKeybindRow("Kill All:", ref bindKillAll, ref isWaitBindKillAll);
                DrawKeybindRow("Call Meeting:", ref bindCallMeeting, ref isWaitBindCallMeeting);
                DrawKeybindRow("Kick All:", ref bindKickAll, ref isWaitBindKickAll);
                DrawKeybindRow("Fix Sabotages:", ref bindFixSabotages, ref isWaitBindFixSabotages);
                DrawKeybindRow("All -> Ghost:", ref bindSetAllGhost, ref isWaitBindSetAllGhost);
                DrawKeybindRow("All -> Ghost Imp:", ref bindSetAllGhostImp, ref isWaitBindSetAllGhostImp);
            }
            finally { GUILayout.EndVertical(); }
        }

        private void DrawKeybindRow(string label, ref KeyCode currentKey, ref bool isWaiting)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUIStyle alignedLabel = new GUIStyle(toggleLabelStyle) { alignment = TextAnchor.MiddleLeft, margin = CreateRectOffset(0, 0, 4, 0) };
            GUILayout.Label(label, alignedLabel, GUILayout.Width(220), GUILayout.Height(25));

            string bindText = isWaiting ? "Press any key..." : (currentKey == KeyCode.None ? "NONE" : currentKey.ToString());
            if (GUILayout.Button(bindText, isWaiting ? activeTabStyle : btnStyle, GUILayout.Width(120), GUILayout.Height(25)))
            {
                ResetAllBindWaits();
                isWaiting = true;
            }

            if (GUILayout.Button("Clear", btnStyle, GUILayout.Width(50), GUILayout.Height(25)))
            {
                currentKey = KeyCode.None;
                isWaiting = false;
                SaveConfig();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
        }
        public static bool AnimShieldsEnabled = false;
        public static bool AnimAsteroidsEnabled = false;
        public static bool AnimCamsInUseEnabled = false;
        public static bool IsScanning = false;
        private void ResetAllBindWaits()
        {
            isWaitingForBind = false;
            isWaitBindMassMorph = false;
            isWaitBindSpawnLobby = false;
            isWaitBindDespawnLobby = false;
            isWaitBindCloseMeeting = false;
            isWaitBindInstaStart = false;
            isWaitBindEndCrew = false;
            isWaitBindEndImp = false;
            isWaitBindEndImpDC = false;
            isWaitBindEndHnsDC = false;
            isWaitBindMagnetCursor = false;
            isWaitBindToggleTracers = false;
            isWaitBindToggleNoClip = false;
            isWaitBindToggleFreecam = false;
            isWaitBindToggleCameraZoom = false;
            isWaitBindKillAll = false;
            isWaitBindCallMeeting = false;
            isWaitBindTogglePlayerInfo = false;
            isWaitBindToggleSeeRoles = false;
            isWaitBindToggleSeeGhosts = false;
            isWaitBindToggleFullBright = false;
            isWaitBindKickAll = false;
            isWaitBindFixSabotages = false;
            isWaitBindSetAllGhost = false;
            isWaitBindSetAllGhostImp = false;
        }

        private void DrawGeneralTab()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("GENERAL", "ГЛАВНОЕ"));
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            for (int i = 0; i < generalSubTabs.Length; i++)
            {
                if (GUILayout.Button(generalSubTabs[i], currentGeneralSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(22)))
                {
                    currentGeneralSubTab = i;
                    scrollPosition = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            if (currentGeneralSubTab == 0) DrawGeneralInfoTab();
            else if (currentGeneralSubTab == 1) DrawBindsTab();
        }

        private bool DrawColoredActionButton(string text, Color color, float width, float height = 24f)
        {
            GUIStyle style = new GUIStyle(btnStyle);
            Color themedColor = RgbMenuTextActive() ? GetMenuAccentColor() : (whiteMenuTheme ? GetThemeAccentColor(color) : color);
            Color hoverColor = whiteMenuTheme
                ? Color.Lerp(themedColor, Color.black, 0.18f)
                : Color.Lerp(themedColor, Color.white, 0.22f);

            style.normal.textColor = themedColor;
            style.hover.textColor = hoverColor;
            style.focused.textColor = themedColor;
            style.active.textColor = whiteMenuTheme ? Color.white : Color.black;
            style.clipping = TextClipping.Overflow;
            style.wordWrap = false;

            float minContentWidth = Mathf.Ceil(style.CalcSize(new GUIContent(text)).x) + 32f;
            float finalButtonWidth = Mathf.Max(width, minContentWidth);
            return GUILayout.Button(text, style, GUILayout.Width(finalButtonWidth), GUILayout.Height(height));
        }

        private bool DrawPseudoInputButton(string value, bool editing, float height = 28f, int maxChars = 52)
        {
            GUIStyle style = new GUIStyle(editing ? activeTabStyle : inputBlockStyle);
            style.alignment = TextAnchor.MiddleLeft;
            style.clipping = TextClipping.Clip;
            style.wordWrap = false;
            style.padding = CreateRectOffset(10, 10, 0, 0);

            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, style, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            return GUI.Button(rect, FormatInputPreview(value, editing, maxChars), style);
        }

        private void DrawClippedHint(string text, float height = 13f)
        {
            GUIStyle style = new GUIStyle(toggleLabelStyle)
            {
                fontSize = 10,
                clipping = TextClipping.Clip,
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft
            };

            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, style, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            GUI.Label(rect, text, style);
        }

        private void OpenExternalLink(string url, string label)
        {
            try
            {
                Application.OpenURL(url);
                ShowNotification($"<color=#00FFAA>[LINK]</color> {L("Opening", "Открываю")} <b>{label}</b>");
            }
            catch
            {
                ShowNotification($"<color=#FF4444>[LINK]</color> {L("Failed to open link.", "Не удалось открыть ссылку.")}");
            }
        }

        private void DrawGeneralInfoTab()
        {
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("ELYSIUM OVERVIEW", headerStyle);
            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            for (int i = 0; i < generalInfoSubTabs.Length; i++)
            {
                GUIStyle tabStyle = currentGeneralInfoSubTab == i ? activeSubTabStyle : subTabStyle;
                float tabWidth = Mathf.Max(116f, Mathf.Ceil(tabStyle.CalcSize(new GUIContent(generalInfoSubTabs[i])).x) + 28f);
                if (GUILayout.Button(generalInfoSubTabs[i], tabStyle, GUILayout.Width(tabWidth), GUILayout.Height(24)))
                {
                    currentGeneralInfoSubTab = i;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Menu language:", "Язык меню:"), toggleLabelStyle, GUILayout.MinWidth(128), GUILayout.ExpandWidth(false));
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(26), GUILayout.Height(24)))
            {
                currentMenuLanguageIndex--;
                if (currentMenuLanguageIndex < 0) currentMenuLanguageIndex = menuLanguageNames.Length - 1;
                SaveConfig();
            }
            GUIStyle languageValueStyle = new GUIStyle(btnStyle) { normal = { background = null, textColor = GetMenuAccentColor() }, fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow, wordWrap = false };
            string languageValue = menuLanguageNames[Mathf.Clamp(currentMenuLanguageIndex, 0, menuLanguageNames.Length - 1)];
            float languageValueWidth = Mathf.Max(132f, Mathf.Ceil(languageValueStyle.CalcSize(new GUIContent(languageValue)).x) + 24f);
            GUILayout.Label(languageValue, languageValueStyle, GUILayout.Width(languageValueWidth), GUILayout.Height(24));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(26), GUILayout.Height(24)))
            {
                currentMenuLanguageIndex++;
                if (currentMenuLanguageIndex >= menuLanguageNames.Length) currentMenuLanguageIndex = 0;
                SaveConfig();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            string accentHex = GetMenuAccentHex();
            string githubHex = ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(26, 188, 156, 255)) : new Color32(26, 188, 156, 255));
            string goldHex = ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(255, 187, 54, 255)) : new Color32(255, 187, 54, 255));
            string leadHex = ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(255, 92, 122, 255)) : new Color32(255, 92, 122, 255));
            string devHex = ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(38, 194, 129, 255)) : new Color32(38, 194, 129, 255));
            string contributorHex = ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(109, 138, 255, 255)) : new Color32(109, 138, 255, 255));
            string dangerHex = ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(231, 76, 60, 255)) : new Color32(231, 76, 60, 255));
            string safeHex = ColorUtility.ToHtmlStringRGB(whiteMenuTheme ? GetThemeAccentColor(new Color32(57, 255, 20, 255)) : new Color32(57, 255, 20, 255));
            string versionText = "1.3.5.1";

            GUIStyle textStyle = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 12 };
            textStyle.normal.textColor = whiteMenuTheme ? new Color(0.16f, 0.16f, 0.16f, 1f) : new Color(0.85f, 0.85f, 0.85f, 1f);

            if (currentGeneralInfoSubTab == 0)
            {
                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label(
                    $"{L("Welcome to", "Добро пожаловать в")} <b><color=#{accentHex}>ElysiumModMenu</color></b> " +
                    $"<b><color=#{goldHex}>v{versionText}</color></b> {L("by", "от")} <b><color=#{leadHex}>meowchelo</color></b>!",
                    textStyle);
                GUILayout.Space(4);
                GUILayout.Label(L(
                    "ElysiumModMenu is a lightweight BepInEx IL2CPP utility for Among Us with lobby tools, visuals, spoofing and host-side controls.",
                    "ElysiumModMenu это легкий BepInEx IL2CPP мод для Among Us с инструментами для лобби, визуалом, спуфингом и хост-функциями."), textStyle);
                GUILayout.Label(L(
                    "Use the buttons below to open the GitHub repository or jump straight to the latest public release.",
                    "Кнопки ниже открывают GitHub репозиторий и страницу с последним публичным релизом."), textStyle);
                GUILayout.Space(6);

                GUILayout.BeginHorizontal();
                if (DrawColoredActionButton("GitHub", new Color32(26, 188, 156, 255), 110f))
                    OpenExternalLink("https://github.com/meowchelo/ElysiumModMenu", "GitHub");
                GUILayout.Space(6);
                if (DrawColoredActionButton("Check for Updates", new Color32(255, 187, 54, 255), 165f))
                    OpenExternalLink("https://github.com/meowchelo/ElysiumModMenu/releases/latest", "Latest Release");
                GUILayout.EndHorizontal();

                GUILayout.Space(8);
                GUILayout.Label($"{L("Project", "Проект")}: <b><color=#{githubHex}>meowchelo/ElysiumModMenu</color></b>", textStyle);
                GUILayout.Label($"{L("Main page", "Главная ссылка")}: <color=#{githubHex}>https://github.com/meowchelo/ElysiumModMenu</color>", textStyle);
                GUILayout.Space(8);
                GUILayout.Label($"{L("ElysiumModMenu is free and open-source software.", "ElysiumModMenu это бесплатный open-source проект.")}", textStyle);
                GUILayout.Label($"<b><color=#{dangerHex}>{L("If you paid for this menu, demand a refund immediately.", "Если вы заплатили за это меню, требуйте возврат денег сразу.")}</color></b>", textStyle);
                GUILayout.Label($"<b><color=#{safeHex}>{L("Make sure you are using the latest version from GitHub releases.", "Убедитесь, что используете последнюю версию из GitHub releases.")}</color></b>", textStyle);
                GUILayout.Space(8);
                GUILayout.Label($"<b><color=#{accentHex}>{L("Quick Hotkeys", "Быстрые клавиши")}</color></b>", textStyle);
                string menuKeyText = (menuToggleKey == KeyCode.None ? KeyCode.Insert : menuToggleKey).ToString();
                GUILayout.Label($"{L("Menu key", "Кнопка меню")}: <b>{menuKeyText}</b>", textStyle);
                GUILayout.Label(L("Right Click: teleport to cursor", "ПКМ: телепорт к курсору"), textStyle);
                GUILayout.Label(L("F9: magnet cursor", "F9: магнит курсора"), textStyle);
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label(L(
                    "ElysiumModMenu is an open-source project. Meet the people behind this build:",
                    "ElysiumModMenu это open-source проект. Ниже люди, которые стоят за этой сборкой:"), textStyle);
                GUILayout.Space(8);

                GUILayout.Label($"<b><color=#{goldHex}>LEAD DEVELOPER</color></b>", textStyle);
                GUILayout.Space(4);
                if (DrawColoredActionButton("meowchelo", new Color32(255, 92, 122, 255), 150f))
                    OpenExternalLink("https://github.com/meowchelo", "meowchelo");

                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{devHex}>DEVELOPERS</color></b>", textStyle);
                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (DrawColoredActionButton("Carrot", new Color32(38, 194, 129, 255), 150f))
                    OpenExternalLink("https://github.com/abobanamne", "Carrot");
                GUILayout.Space(6);
                if (DrawColoredActionButton("wextikit", new Color32(109, 138, 255, 255), 150f))
                    OpenExternalLink("https://github.com/wextikit", "wextikit");
                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{contributorHex}>TESTERS</color></b>", textStyle);
                GUILayout.Space(4);
                DrawColoredActionButton("Жена", new Color32(109, 138, 255, 255), 150f);

                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{accentHex}>{L("Repository", "Репозиторий")}</color></b>", textStyle);
                GUILayout.Label(L(
                    "The public source, releases and project updates are published on GitHub.",
                    "Публичный исходный код, релизы и обновления проекта публикуются на GitHub."), textStyle);
                GUILayout.Space(4);
                if (DrawColoredActionButton("Open ElysiumModMenu Repository", new Color32(26, 188, 156, 255), 220f))
                    OpenExternalLink("https://github.com/meowchelo/ElysiumModMenu", "ElysiumModMenu Repository");

                GUILayout.Space(10);
                GUILayout.Label($"<b><color=#{contributorHex}>{L("Notes", "Примечание")}</color></b>", textStyle);
                GUILayout.Label(L(
                    "Thank you to everyone helping with ideas, testing and polishing the menu.",
                    "Спасибо всем, кто помогает идеями, тестами и полировкой меню."), textStyle);
                GUILayout.EndVertical();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
        [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
        public static class ChatLogger_Patch
        {
            public static void Prefix(PlayerControl sourcePlayer, ref string chatText)
            {
                if (!ElysiumModMenuGUI.enableChatLog || string.IsNullOrWhiteSpace(chatText)) return;

                try
                {
                    string time = System.DateTime.Now.ToString("HH:mm:ss");

                    string name = "System/Unknown";
                    string levelStr = "?";
                    string fc = "Hidden";
                    string puid = "Unknown";
                    string platformStr = "Unknown";

                    if (sourcePlayer != null && sourcePlayer.Data != null)
                    {
                        name = sourcePlayer.Data.PlayerName;

                        uint rawLevel = sourcePlayer.Data.PlayerLevel;
                        if (rawLevel != uint.MaxValue && rawLevel < 10000) levelStr = (rawLevel + 1).ToString();

                        fc = GetDisplayedFriendCode(sourcePlayer.Data, "Hidden");

                        var client = AmongUsClient.Instance?.GetClientFromCharacter(sourcePlayer);
                        if (client != null)
                        {
                            puid = GetClientPuid(client);
                            platformStr = ElysiumModMenuGUI.GetPlatform(client);
                        }
                    }

                    string cleanText = System.Text.RegularExpressions.Regex.Replace(chatText, "<.*?>", string.Empty);

                    string logLine = $"[{time}] [{name}] [Lv:{levelStr}] [FC:{fc}] [ID:{puid}] [{platformStr}] : {cleanText}\n";

                    string chatLogPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "ChatLog.txt");
                    System.IO.File.AppendAllText(chatLogPath, logLine);
                }
                catch { }
            }
        }


        private void DrawSelfTab()
        {
            if (currentSelfSubTab == 0) currentSelfSubTab = 1;

            float selfColumnWidth = Mathf.Max(270f, (windowRect.width - 186f) * 0.5f);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(selfColumnWidth));
            DrawSelfSpoof();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(selfColumnWidth), GUILayout.ExpandHeight(false));
            DrawMenuSectionHeader("SELF TOOLS");
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            for (int i = 0; i < selfOtherTabs.Length; i++)
            {
                int tabIndex = i + 1;
                if (GUILayout.Button(selfOtherTabs[i], currentSelfSubTab == tabIndex ? activeSubTabStyle : subTabStyle, GUILayout.Height(22)))
                {
                    currentSelfSubTab = tabIndex;
                    scrollPosition = Vector2.zero;
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (currentSelfSubTab == 1) DrawPlayerMovementCompact();
            else if (currentSelfSubTab == 2) DrawRolesCompact();
            else if (currentSelfSubTab == 3) DrawChatSettingsCompact();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawPlayerMovementCompact()
        {
            GUILayout.BeginVertical();
            DrawMenuSectionHeader("MOVEMENT & TELEPORT");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Engine: {Mathf.Round(engineSpeed)}x", toggleLabelStyle, GUILayout.Width(86));
            engineSpeed = GUILayout.HorizontalSlider(engineSpeed, 1f, 555f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("R", btnStyle, GUILayout.Width(28), GUILayout.Height(22))) engineSpeed = 1f;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Walk: {Mathf.Round(walkSpeed)}x", toggleLabelStyle, GUILayout.Width(86));
            walkSpeed = GUILayout.HorizontalSlider(walkSpeed, 1f, 30f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("R", btnStyle, GUILayout.Width(28), GUILayout.Height(22))) walkSpeed = 1f;
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            tpToCursor = DrawToggle(tpToCursor, "TP To Cursor", 230);
            GUILayout.Space(3);
            dragToCursor = DrawToggle(dragToCursor, "Drag To Cursor", 230);
            GUILayout.Space(3);
            autoFollowCursor = DrawToggle(autoFollowCursor, $"Magnet Cursor ({bindMagnetCursor})", 230);
            GUILayout.Space(3);
            noClip = DrawToggle(noClip, "True NoClip", 230);

            GUILayout.EndVertical();
        }

        private void DrawRolesCompact()
        {
            GUILayout.BeginVertical();
            DrawMenuSectionHeader("ROLE TOOLS");

            GUIStyle roleMidStyle = new GUIStyle(btnStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { background = null, textColor = GetMenuAccentColor() },
                alignment = TextAnchor.MiddleCenter
            };

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
            {
                fakeRoleIdx--;
                if (fakeRoleIdx < 0) fakeRoleIdx = forceRoleOptions.Length - 1;
            }
            GUILayout.Label(forceRoleOptions[fakeRoleIdx].ToString(), roleMidStyle, GUILayout.ExpandWidth(true), GUILayout.Height(24));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
            {
                fakeRoleIdx++;
                if (fakeRoleIdx >= forceRoleOptions.Length) fakeRoleIdx = 0;
            }
            if (GUILayout.Button("Set", activeTabStyle, GUILayout.Width(42), GUILayout.Height(24)))
                RoleManager.Instance?.SetRole(PlayerControl.LocalPlayer, forceRoleOptions[fakeRoleIdx]);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            DrawMenuSectionHeader("IMPOSTOR");
            killReach = DrawToggle(killReach, "Kill Reach", 230);
            GUILayout.Space(3);
            killAnyone = DrawToggle(killAnyone, "Kill Anyone", 230);
            GUILayout.Space(3);
            killAuraHostOnly = DrawToggle(killAuraHostOnly, "Kill Aura", 230);
            GUILayout.Space(3);
            noKillCooldownHostOnly = DrawToggle(noKillCooldownHostOnly, "Kill Cooldown 0", 230);
            GUILayout.Space(3);
            spamReportBodies = DrawToggle(spamReportBodies, "Spam Report Bodies", 230);

            GUILayout.Space(8);
            DrawMenuSectionHeader("SPECIAL ROLES");
            NoShapeshiftAnim = DrawToggle(NoShapeshiftAnim, "No Ss Animation", 230);
            GUILayout.Space(3);
            endlessSsDuration = DrawToggle(endlessSsDuration, "Endless Ss Duration", 230);
            GUILayout.Space(3);
            EndlessTracking = DrawToggle(EndlessTracking, "Endless Tracking", 230);
            GUILayout.Space(3);
            NoTrackingCooldown = DrawToggle(NoTrackingCooldown, "No Track Cooldown", 230);
            GUILayout.Space(3);
            endlessVentTime = DrawToggle(endlessVentTime, "Endless Vent Time", 230);
            GUILayout.Space(3);
            noVentCooldown = DrawToggle(noVentCooldown, "No Vent Cooldown", 230);
            GUILayout.Space(3);
            noMapCooldowns = DrawToggle(noMapCooldowns, "No Map Cooldowns", 230);
            GUILayout.Space(3);
            endlessBattery = DrawToggle(endlessBattery, "Endless Battery", 230);
            GUILayout.Space(3);
            noVitalsCooldown = DrawToggle(noVitalsCooldown, "No Vitals Cooldown", 230);
            GUILayout.Space(3);
            UnlimitedInterrogateRange = DrawToggle(UnlimitedInterrogateRange, "Interrogate Reach", 230);

            GUILayout.EndVertical();
        }

        private void DrawChatSettingsCompact()
        {
            GUILayout.BeginVertical();
            DrawMenuSectionHeader(L("CHAT SETTINGS", "НАСТРОЙКИ ЧАТА"));

            alwaysChat = DrawToggle(alwaysChat, L("Always Show Chat", "Всегда показывать чат"), 230);
            GUILayout.Space(3);
            readGhostChat = DrawToggle(readGhostChat, L("Read Ghost Chat", "Читать чат призраков"), 230);
            GUILayout.Space(4);
            DrawGhostChatColorControl(230f);
            GUILayout.Space(3);
            enableExtendedChat = DrawToggle(enableExtendedChat, L("Extended Chat", "Длинный чат"), 230);
            GUILayout.Space(3);
            enableFastChat = DrawToggle(enableFastChat, L("Fast Chat", "Быстрый чат"), 230);
            GUILayout.Space(3);
            allowLinksAndSymbols = DrawToggle(allowLinksAndSymbols, L("Unlock Extra Characters", "Все символы"), 230);
            GUILayout.Space(3);
            enableSpellCheck = DrawToggle(enableSpellCheck, L("Spell Check", "Проверка орфографии"), 230);

            GUILayout.Space(8);
            DrawMenuSectionHeader(L("CHAT UTILITY", "УТИЛИТЫ ЧАТА"));
            enableChatHistory = DrawToggle(enableChatHistory, L("Chat History", "История чата"), 230);
            GUILayout.Space(3);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{L("History:", "История:")} {chatHistoryLimit}", toggleLabelStyle, GUILayout.MinWidth(106), GUILayout.ExpandWidth(false));
            chatHistoryLimit = Mathf.Clamp((int)GUILayout.HorizontalSlider(chatHistoryLimit, 5f, 80f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true)), 5, 80);
            TrimChatHistoryToLimit();
            GUILayout.EndHorizontal();
            GUILayout.Space(3);
            enableClipboard = DrawToggle(enableClipboard, L("Clipboard", "Буфер обмена"), 230);
            GUILayout.Space(3);
            enableChatLog = DrawToggle(enableChatLog, L("Save Chat Log", "Сохранять лог чата"), 230);
            GUILayout.Space(3);
            enableChatDarkMode = DrawToggle(enableChatDarkMode, L("Dark Chat Theme", "Темная тема чата"), 230);
            GUILayout.Space(3);
            if (enableChatDarkMode && GUILayout.Button(L("Turn Off Dark Chat", "Выключить темный чат"), btnStyle, GUILayout.Height(24)))
            {
                enableChatDarkMode = false;
                SaveConfig();
            }
            GUILayout.Space(3);
            enableColorCommand = DrawToggle(enableColorCommand, L("Enable /color", "Разрешить /color"), 230);
            GUILayout.Space(3);
            blockFortegreenChat = DrawToggle(blockFortegreenChat, L("Block Fortegreen", "Блок Fortegreen"), 230);
            GUILayout.Space(3);
            blockRainbowChat = DrawToggle(blockRainbowChat, L("Block Rainbow", "Блок Rainbow"), 230);

            GUILayout.Space(8);
            DrawMenuSectionHeader(L("CHAT SENDER", "ОТПРАВКА ЧАТА"));
            GUILayout.Space(4);

            GUIStyle fieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
            fieldStyle.normal.textColor = whiteMenuTheme ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);

            Rect chatInputRect = GUILayoutUtility.GetRect(10f, 32f, GUILayout.ExpandWidth(true), GUILayout.Height(32));
            GUI.Box(chatInputRect, string.Empty, fieldStyle);

            string drawText = string.IsNullOrEmpty(customChatMessage)
                ? L("Type a message...", "Введите сообщение...")
                : customChatMessage;
            if (customChatInputFocused && (Time.unscaledTime % 1f) < 0.5f) drawText += "|";

            GUIStyle chatInputTextStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                richText = false,
                fontSize = 12
            };
            chatInputTextStyle.normal.textColor = whiteMenuTheme ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.9f, 0.9f, 0.9f, 1f);
            GUI.Label(new Rect(chatInputRect.x + 9f, chatInputRect.y + 3f, chatInputRect.width - 18f, chatInputRect.height - 6f), drawText, chatInputTextStyle);

            Event e = Event.current;
            if (e != null)
            {
                if (e.type == EventType.MouseDown)
                {
                    customChatInputFocused = chatInputRect.Contains(e.mousePosition);
                    if (customChatInputFocused) e.Use();
                }
                else if (customChatInputFocused && e.type == EventType.KeyDown)
                {
                    if (HandleClipboardShortcut(e, ref customChatMessage, 120))
                    {
                    }
                    else if (e.keyCode == KeyCode.Backspace)
                    {
                        if (!string.IsNullOrEmpty(customChatMessage))
                            customChatMessage = customChatMessage.Substring(0, customChatMessage.Length - 1);
                        e.Use();
                    }
                    else if (e.keyCode == KeyCode.Escape)
                    {
                        customChatInputFocused = false;
                        e.Use();
                    }
                    else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                    {
                        TrySendCustomChatMessage(customChatMessage);
                        e.Use();
                    }
                    else if (!char.IsControl(e.character))
                    {
                        if (customChatMessage == null) customChatMessage = string.Empty;
                        if (customChatMessage.Length < 120) customChatMessage += e.character;
                        e.Use();
                    }
                }
            }

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(L("Send", "Отправить"), btnStyle, GUILayout.Height(28)))
                TrySendCustomChatMessage(customChatMessage);
            GUILayout.Space(6);
            string spamBtnText = customChatSpamEnabled ? L("Spam ON", "Спам ВКЛ") : L("Spam OFF", "Спам ВЫКЛ");
            if (GUILayout.Button(spamBtnText, customChatSpamEnabled ? activeTabStyle : btnStyle, GUILayout.Height(28)))
                customChatSpamEnabled = !customChatSpamEnabled;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{L("Delay:", "Задержка:")} {Mathf.Round(customChatSpamDelay * 10f) / 10f}s", toggleLabelStyle, GUILayout.Width(92));
            customChatSpamDelay = GUILayout.HorizontalSlider(customChatSpamDelay, 0.5f, 10f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawGhostChatColorControl(float width)
        {
            GUILayout.BeginHorizontal(GUILayout.Width(width));
            GUILayout.Label(L("Ghost Chat:", "Ghost Chat:"), new GUIStyle(toggleLabelStyle) { fontSize = 11 }, GUILayout.Width(74));
            if (DrawPseudoInputButton(ghostChatColorHex, isEditingGhostChatColor, 24f, 16))
            {
                isEditingGhostChatColor = !isEditingGhostChatColor;
                if (isEditingGhostChatColor)
                {
                    ghostChatColorHex = FilterHexInput(ghostChatColorHex, 7);
                }
                isEditingName = false;
                isEditingLevel = false;
                isEditingFriendCode = false;
                isEditingLocalFriendCode = false;
                isEditingBan = false;
                ResetAllBindWaits();
            }
            if (GUILayout.Button(L("Apply", "OK"), btnStyle, GUILayout.Width(48), GUILayout.Height(24)))
            {
                isEditingGhostChatColor = false;
                ghostChatColorHex = SanitizeHexColor(ghostChatColorHex, "#D7B8FF");
                SaveConfig();
            }
            GUILayout.EndHorizontal();

            string previewHex = GetGhostChatColorHex();
            GUILayout.Label($"<color={previewHex}>{L("Preview ghost chat color", "Пример цвета чата призраков")}</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11, wordWrap = false, clipping = TextClipping.Clip }, GUILayout.Width(width), GUILayout.Height(16f));
        }

        private void DrawPlayerMovement()
        {
            GUILayout.BeginVertical(boxStyle);
            try
            {
                GUILayout.Label("MOVEMENT & TELEPORT", headerStyle);

                GUILayout.BeginHorizontal();
                try
                {
                    GUILayout.Label($"Engine Speed: {Mathf.Round(engineSpeed)}x", GUILayout.Width(130));
                    engineSpeed = GUILayout.HorizontalSlider(engineSpeed, 1f, 555f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
                    GUILayout.Space(10);
                    if (GUILayout.Button("Reset", btnStyle, GUILayout.Width(50), GUILayout.Height(20))) engineSpeed = 1f;
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                try
                {
                    GUILayout.Label($"Walk Speed: {Mathf.Round(walkSpeed)}x", GUILayout.Width(130));
                    walkSpeed = GUILayout.HorizontalSlider(walkSpeed, 1f, 30f, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
                    GUILayout.Space(10);
                    if (GUILayout.Button("Reset", btnStyle, GUILayout.Width(50), GUILayout.Height(20))) walkSpeed = 1f;
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                try
                {
                    tpToCursor = DrawToggle(tpToCursor, "TP To Cursor", 160);
                    dragToCursor = DrawToggle(dragToCursor, "Drag To Cursor", 160);
                    GUILayout.FlexibleSpace();
                }
                finally { GUILayout.EndHorizontal(); }

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                try
                {
                    autoFollowCursor = DrawToggle(autoFollowCursor, $"Magnet Cursor ({bindMagnetCursor})", 160);
                    noClip = DrawToggle(noClip, "True NoClip", 160);
                    GUILayout.FlexibleSpace();
                }
                finally { GUILayout.EndHorizontal(); }
            }
            finally { GUILayout.EndVertical(); }
        }
        private void SmartEndGame(string outcome)
        {
            if (GameManager.Instance == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;

            bool isHns = GameManager.Instance.IsHideAndSeek();
            int reasonCode = 0;

            switch (outcome)
            {
                case "CrewWin": reasonCode = isHns ? 7 : 0; break;
                case "ImpWin": reasonCode = isHns ? 8 : 3; break;
                case "ImpDisconnect":
                case "HnsImpDisconnect": reasonCode = 5; break;
            }

            bool tempBlock = neverEndGame;
            neverEndGame = false;
            GameManager.Instance.RpcEndGame((GameOverReason)reasonCode, false);
            neverEndGame = tempBlock;
        }

        private static string SanitizeSpoofFriendCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            string clean = "";
            foreach (char c in input.ToLowerInvariant())
            {
                if (char.IsWhiteSpace(c)) break;
                if (char.IsLetterOrDigit(c)) clean += c;
                if (clean.Length >= 10) break;
            }
            return clean;
        }

        private static string SanitizeHexColor(string input, string fallback)
        {
            string value = (input ?? string.Empty).Trim();
            if (value.StartsWith("#")) value = value.Substring(1);

            string clean = "";
            foreach (char c in value)
            {
                if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
                {
                    clean += char.ToUpperInvariant(c);
                    if (clean.Length >= 6) break;
                }
            }

            return clean.Length == 6 ? "#" + clean : fallback;
        }

        private static string FilterHexInput(string input, int maxChars)
        {
            string value = (input ?? string.Empty).Trim();
            string clean = "";
            bool hasHash = false;

            foreach (char c in value)
            {
                if (c == '#' && clean.Length == 0 && !hasHash)
                {
                    hasHash = true;
                    clean = "#";
                    continue;
                }

                if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
                {
                    if (clean.Length == 0) clean = "#";
                    clean += char.ToUpperInvariant(c);
                    if (clean.Length >= maxChars) break;
                }
            }

            return clean.Length == 0 ? "#" : clean;
        }

        public static string GetGhostChatColorHex()
        {
            if (isEditingGhostChatColor)
            {
                return SanitizeHexColor(ghostChatColorHex, "#D7B8FF");
            }

            ghostChatColorHex = SanitizeHexColor(ghostChatColorHex, "#D7B8FF");
            return ghostChatColorHex;
        }

        private static string BuildLocalNameRenderText(string input)
        {
            string value = (input ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            string trimmed = value.TrimStart();
            if (trimmed.StartsWith("shimmer:", StringComparison.OrdinalIgnoreCase))
                return ApplyMenuShimmer(trimmed.Substring("shimmer:".Length).TrimStart());

            Match hexPrefix = Regex.Match(trimmed, @"^#([0-9A-Fa-f]{6})(.*)$");
            if (hexPrefix.Success)
            {
                string payload = hexPrefix.Groups[2].Value.TrimStart(' ', ':', '|', '-', '>');
                if (!string.IsNullOrEmpty(payload))
                    return $"<color=#{hexPrefix.Groups[1].Value}>{payload}</color>";
            }

            return value;
        }

        private static string GetDisplayedFriendCode(NetworkedPlayerInfo data, string emptyValue = "Hidden")
        {
            if (data == null) return emptyValue;

            string value = data.FriendCode;
            if (enableLocalFriendCodeSpoof &&
                PlayerControl.LocalPlayer != null &&
                data.PlayerId == PlayerControl.LocalPlayer.PlayerId &&
                !string.IsNullOrEmpty(localFriendCodeInput))
            {
                value = localFriendCodeInput;
            }

            return string.IsNullOrEmpty(value) ? emptyValue : value;
        }

        public static bool PrepareLocalFriendCodeForSerialize(NetworkedPlayerInfo data, out string restoreValue)
        {
            restoreValue = null;
            try
            {
                if (!enableLocalFriendCodeSpoof || enableFriendCodeSpoof) return false;
                if (data == null || PlayerControl.LocalPlayer == null || data.PlayerId != PlayerControl.LocalPlayer.PlayerId) return false;

                restoreValue = data.FriendCode;
                TrySetStringMember(data, "FriendCode", originalLocalFriendCode ?? string.Empty);
                return true;
            }
            catch
            {
                restoreValue = null;
                return false;
            }
        }

        public static void RestoreLocalFriendCodeAfterSerialize(NetworkedPlayerInfo data, string restoreValue)
        {
            try
            {
                if (data == null || restoreValue == null) return;
                TrySetStringMember(data, "FriendCode", restoreValue);
            }
            catch { }
        }

        private static string FormatInputPreview(string value, bool editing, int maxChars = 52)
        {
            string preview = value ?? string.Empty;
            if (preview.Length > maxChars)
                preview = "..." + preview.Substring(preview.Length - (maxChars - 3));

            if (editing) preview += "_";
            return string.IsNullOrEmpty(preview) ? " " : preview;
        }

        private static bool HandleClipboardShortcut(Event e, ref string target, int maxLength = -1)
        {
            if (e == null || e.type != EventType.KeyDown) return false;

            bool ctrlOrCmd = e.control || e.command;
            bool pasteAlt = e.shift && e.keyCode == KeyCode.Insert;
            if (!ctrlOrCmd && !pasteAlt) return false;

            target ??= string.Empty;

            if (ctrlOrCmd && e.keyCode == KeyCode.C)
            {
                GUIUtility.systemCopyBuffer = target;
                e.Use();
                return true;
            }

            if (ctrlOrCmd && e.keyCode == KeyCode.X)
            {
                GUIUtility.systemCopyBuffer = target;
                target = string.Empty;
                e.Use();
                return true;
            }

            if ((ctrlOrCmd && e.keyCode == KeyCode.V) || pasteAlt)
            {
                string paste = (GUIUtility.systemCopyBuffer ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
                if (paste.Length > 0)
                {
                    target += paste;
                    if (maxLength >= 0 && target.Length > maxLength)
                        target = target.Substring(0, maxLength);
                }
                e.Use();
                return true;
            }

            return false;
        }

        private static bool IsBrokenFriendCode(string friendCode)
        {
            if (string.IsNullOrWhiteSpace(friendCode)) return true;
            if (friendCode.Contains(" ")) return true;
            if (friendCode.Contains("<") || friendCode.Contains(">")) return true;
            if (!friendCode.Contains("#")) return true;

            string[] parts = friendCode.Split('#');
            if (parts.Length != 2) return true;
            if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1])) return true;
            if (parts[0].Length < 3 || parts[0].Length > 16) return true;
            if (parts[1].Length < 3 || parts[1].Length > 8) return true;
            if (!parts[0].All(char.IsLetterOrDigit)) return true;
            if (!parts[1].All(char.IsDigit)) return true;

            return false;
        }

        private void TryAutoBanBrokenFriendCodeTick()
        {
            try
            {
                if (!autoBanBrokenFriendCode)
                {
                    brokenFcScanTimer = 0f;
                    brokenFcPunishedOwners.Clear();
                    return;
                }

                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls == null)
                {
                    brokenFcScanTimer = 0f;
                    return;
                }

                if (PlayerControl.AllPlayerControls.Count <= 1)
                    brokenFcPunishedOwners.Clear();

                brokenFcScanTimer += Time.deltaTime;
                if (brokenFcScanTimer < 0.8f) return;
                brokenFcScanTimer = 0f;

                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == PlayerControl.LocalPlayer || pc.Data == null || pc.Data.Disconnected) continue;

                    string fc = pc.Data.FriendCode ?? "";
                    if (!IsBrokenFriendCode(fc)) continue;

                    int owner = (int)pc.OwnerId;
                    if (brokenFcPunishedOwners.Contains(owner)) continue;
                    brokenFcPunishedOwners.Add(owner);

                    string name = string.IsNullOrWhiteSpace(pc.Data.PlayerName) ? "Unknown" : pc.Data.PlayerName;
                    string puid = "Unknown";
                    try
                    {
                        var client = AmongUsClient.Instance.GetClientFromCharacter(pc);
                        if (client != null) puid = GetClientPuid(client);
                    }
                    catch { }

                    AddToBanList(string.IsNullOrWhiteSpace(fc) ? "Unknown" : fc, puid, name, "Broken FriendCode");
                    AmongUsClient.Instance.KickPlayer(owner, true);
                    ShowNotification($"<color=#FF4444>[ANTICHEAT]</color> {name} banned: broken FC");
                }
            }
            catch { }
        }

        private void TryAutoKickLowLevelTick()
        {
            try
            {
                if (!autoKickLowLevelEnabled)
                {
                    lowLevelKickScanTimer = 0f;
                    lowLevelKickPunishedOwners.Clear();
                    return;
                }

                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls == null)
                {
                    lowLevelKickScanTimer = 0f;
                    return;
                }

                if (PlayerControl.AllPlayerControls.Count <= 1)
                    lowLevelKickPunishedOwners.Clear();

                lowLevelKickScanTimer += Time.deltaTime;
                if (lowLevelKickScanTimer < 0.8f) return;
                lowLevelKickScanTimer = 0f;

                int minLevel = Mathf.Clamp(autoKickMinLevel, 1, 300);

                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == PlayerControl.LocalPlayer || pc.Data == null || pc.Data.Disconnected) continue;

                    int level = 1;
                    try
                    {
                        uint rawLevel = pc.Data.PlayerLevel;
                        if (rawLevel != uint.MaxValue && rawLevel < 10000) level = (int)rawLevel + 1;
                    }
                    catch { }

                    if (level >= minLevel) continue;

                    int owner = (int)pc.OwnerId;
                    if (lowLevelKickPunishedOwners.Contains(owner)) continue;
                    lowLevelKickPunishedOwners.Add(owner);

                    string name = string.IsNullOrWhiteSpace(pc.Data.PlayerName) ? "Unknown" : pc.Data.PlayerName;
                    AmongUsClient.Instance.KickPlayer(owner, false);
                    ShowNotification($"<color=#FF4444>[LEVEL KICK]</color> {name} kicked: level {level} < {minLevel}");
                }
            }
            catch { }
        }

        private static void TryAutoGhostAfterStartTick()
        {
            try
            {
                bool gameStarted = AmongUsClient.Instance != null && AmongUsClient.Instance.IsGameStarted;
                if (!gameStarted)
                {
                    wasGameStartedForAutoGhost = false;
                    autoGhostAppliedThisGame = false;
                    return;
                }

                if (!wasGameStartedForAutoGhost)
                {
                    wasGameStartedForAutoGhost = true;
                    autoGhostAppliedThisGame = false;
                }

                if (!autoGhostAfterStart || autoGhostAppliedThisGame || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
                    return;

                if (PlayerControl.LocalPlayer.Data.IsDead)
                {
                    autoGhostAppliedThisGame = true;
                    return;
                }

                MakePlayerGhost(PlayerControl.LocalPlayer, false, false);
                autoGhostAppliedThisGame = true;
                ShowNotification($"<color=#AA88FF>[AUTO HOST]</color> {L("Auto ghost applied.", "Авто-призрак применен.")}");
            }
            catch { }
        }

        private static void EnsurePlatformBanListLoaded()
        {
            try
            {
                if (string.IsNullOrEmpty(platformBanListPath))
                    platformBanListPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "ElysiumPlatformBanList.txt");

                if (!System.IO.File.Exists(platformBanListPath))
                    System.IO.File.WriteAllText(platformBanListPath, "# One custom platform token per line. Matching PlatformName values are host-banned when enabled.\n# Example: github\n");

                if (Time.unscaledTime < platformBanListNextLoadAt) return;
                platformBanListNextLoadAt = Time.unscaledTime + 3f;

                customPlatformBanTokens.Clear();
                foreach (string rawLine in System.IO.File.ReadAllLines(platformBanListPath))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    customPlatformBanTokens.Add(line);
                }
            }
            catch { }
        }

        private static bool IsCustomPlatformName(ClientData client, out string platformName)
        {
            platformName = "";
            try
            {
                if (client == null || client.PlatformData == null) return false;
                platformName = client.PlatformData.PlatformName ?? "";
                if (string.IsNullOrWhiteSpace(platformName)) return false;

                string enumName = client.PlatformData.Platform.ToString();
                if (platformName.Equals("TESTNAME", StringComparison.OrdinalIgnoreCase)) return false;
                return !platformName.Equals(enumName, StringComparison.OrdinalIgnoreCase) &&
                       !platformName.Equals(GetPlatform(client), StringComparison.OrdinalIgnoreCase);
            }
            catch { }

            return false;
        }

        private static bool IsInvalidPlatformData(ClientData client, out string reason)
        {
            reason = "";
            try
            {
                if (client == null || client.PlatformData == null) return false;

                var platform = client.PlatformData;
                string pName = platform.PlatformName ?? "";
                ulong xuid = platform.XboxPlatformId;
                ulong psid = platform.PsnPlatformId;
                bool isValid = true;

                switch (platform.Platform)
                {
                    case Platforms.StandaloneEpicPC:
                    case Platforms.StandaloneSteamPC:
                    case Platforms.StandaloneMac:
                    case Platforms.StandaloneItch:
                    case Platforms.IPhone:
                    case Platforms.Android:
                        isValid = (pName == "TESTNAME" && xuid == 0 && psid == 0);
                        break;
                    case Platforms.StandaloneWin10:
                        isValid = (pName == "TESTNAME" && xuid != 0 && psid == 0);
                        break;
                    case Platforms.Xbox:
                        isValid = (pName != "TESTNAME" && pName.Length >= 3 && xuid != 0 && psid == 0);
                        break;
                    case Platforms.Playstation:
                        isValid = (pName != "TESTNAME" && xuid == 0 && psid != 0);
                        break;
                    case Platforms.Switch:
                        isValid = (pName != "TESTNAME" && xuid == 0 && psid == 0);
                        break;
                }

                if (!isValid)
                {
                    reason = $"Platform Spoof detected ({platform.Platform})";
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static bool MatchesPlatformBanTxt(ClientData client, out string platformName, out string matchedToken)
        {
            platformName = "";
            matchedToken = "";
            EnsurePlatformBanListLoaded();

            if (!IsCustomPlatformName(client, out platformName) || customPlatformBanTokens.Count == 0)
                return false;

            foreach (string token in customPlatformBanTokens)
            {
                if (platformName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matchedToken = token;
                    return true;
                }
            }

            return false;
        }

        private static void HostBanForPlatform(PlayerControl player, string reason)
        {
            try
            {
                if (player == null || player == PlayerControl.LocalPlayer || player.Data == null ||
                    AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                    return;

                int owner = (int)player.OwnerId;
                if (platformSpoofPunishedOwners.Contains(owner)) return;
                platformSpoofPunishedOwners.Add(owner);

                string name = string.IsNullOrWhiteSpace(player.Data.PlayerName) ? "Unknown" : player.Data.PlayerName;
                string fc = string.IsNullOrWhiteSpace(player.Data.FriendCode) ? "Unknown" : player.Data.FriendCode;
                string puid = "Unknown";
                try
                {
                    var client = AmongUsClient.Instance.GetClientFromCharacter(player);
                    if (client != null) puid = GetClientPuid(client);
                }
                catch { }

                AddToBanList(fc, puid, name, reason);
                AmongUsClient.Instance.KickPlayer(owner, true);
                ShowNotification($"<color=#FF4444>[PLATFORM BAN]</color> <b>{name}</b>: {reason}");
            }
            catch { }
        }

        private static void TryAutoBanCustomPlatformsTick()
        {
            try
            {
                if ((!autoBanPlatformSpoof && !banCustomPlatformsFromTxt) ||
                    AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.AllPlayerControls == null)
                {
                    platformBanScanTimer = 0f;
                    return;
                }

                platformBanScanTimer += Time.deltaTime;
                if (platformBanScanTimer < 1f) return;
                platformBanScanTimer = 0f;

                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc == PlayerControl.LocalPlayer || pc.Data == null || pc.Data.Disconnected) continue;

                    ClientData client = null;
                    try { client = AmongUsClient.Instance.GetClientFromCharacter(pc); } catch { }
                    if (client == null) continue;

                    if (banCustomPlatformsFromTxt && MatchesPlatformBanTxt(client, out string platformName, out string token))
                    {
                        HostBanForPlatform(pc, $"Custom platform TXT match '{token}' ({platformName})");
                        continue;
                    }

                    if (autoBanPlatformSpoof && IsInvalidPlatformData(client, out string reason))
                        HostBanForPlatform(pc, reason);
                }
            }
            catch { }
        }

        private void DrawSelfSpoof()
        {
            GUILayout.BeginVertical();
            GUIStyle greenHeader = new GUIStyle(headerStyle);
            greenHeader.normal.textColor = GetMenuAccentColor();
            GUILayout.Label("ACCOUNT SPOOFER", greenHeader);

            GUILayout.Space(4);
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("LEVEL SPOOF");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Fake Level", btnStyle, GUILayout.Width(86), GUILayout.Height(28));
            if (DrawPseudoInputButton(spoofLevelString, isEditingLevel, 28f, 32))
            {
                isEditingLevel = !isEditingLevel;
                isEditingName = false;
                isEditingFriendCode = false;
                isEditingLocalFriendCode = false;
                isEditingGhostChatColor = false;
            }
            if (GUILayout.Button("Apply", btnStyle, GUILayout.Width(56), GUILayout.Height(28)))
            {
                isEditingLevel = false;
                if (uint.TryParse(spoofLevelString, out uint parsedLvl))
                {
                    try { AmongUs.Data.DataManager.Player.stats.level = parsedLvl > 0 ? parsedLvl - 1 : 0; AmongUs.Data.DataManager.Player.Save(); }
                    catch { try { AmongUs.Data.DataManager.Player.Stats.Level = parsedLvl > 0 ? parsedLvl - 1 : 0; AmongUs.Data.DataManager.Player.Save(); } catch { } }
                }
                SaveConfig();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(6);

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("LOCAL NAME SPOOF");
            bool newLocalNameToggle = DrawToggle(enableLocalNameSpoof, "Keep Local Nick", 180);
            if (newLocalNameToggle != enableLocalNameSpoof)
            {
                enableLocalNameSpoof = newLocalNameToggle;
                if (enableLocalNameSpoof) ApplyLocalNameSelf(customNameInput, false);
                else RestoreLocalNameSelf();
                SaveConfig();
            }
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Nick", btnStyle, GUILayout.Width(58), GUILayout.Height(28));
            if (DrawPseudoInputButton(customNameInput, isEditingName, 28f, 54))
            {
                isEditingName = !isEditingName;
                isEditingLevel = false;
                isEditingFriendCode = false;
                isEditingLocalFriendCode = false;
                isEditingGhostChatColor = false;
            }
            if (GUILayout.Button("Apply", btnStyle, GUILayout.Width(56), GUILayout.Height(28)))
            {
                isEditingName = false;
                ApplyLocalNameSelf(customNameInput, true);
                SaveConfig();
            }
            GUILayout.EndHorizontal();
            DrawClippedHint("Local only: no RPC broadcast. Supports shimmer:Text, #68B6E7Text and raw rich text.");
            GUILayout.EndVertical();

            GUILayout.Space(6);

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("LOCAL FAKE FRIEND CODE");
            bool newLocalFcToggle = DrawToggle(enableLocalFriendCodeSpoof, "Keep Fake FC Local", 180);
            if (newLocalFcToggle != enableLocalFriendCodeSpoof)
            {
                enableLocalFriendCodeSpoof = newLocalFcToggle;
                if (enableLocalFriendCodeSpoof) ApplyLocalFriendCodeSelf(localFriendCodeInput, false);
                else RestoreLocalFriendCodeSelf();
                SaveConfig();
            }
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Fake FC", btnStyle, GUILayout.Width(58), GUILayout.Height(28));
            if (DrawPseudoInputButton(localFriendCodeInput, isEditingLocalFriendCode, 28f, 54))
            {
                isEditingLocalFriendCode = !isEditingLocalFriendCode;
                isEditingName = false;
                isEditingLevel = false;
                isEditingFriendCode = false;
                isEditingGhostChatColor = false;
            }
            if (GUILayout.Button("Apply", btnStyle, GUILayout.Width(56), GUILayout.Height(28)))
            {
                isEditingLocalFriendCode = false;
                ApplyLocalFriendCodeSelf(localFriendCodeInput, true);
                SaveConfig();
            }
            GUILayout.EndHorizontal();
            DrawClippedHint("Local only: any text, any symbols. Used in this client UI only.");
            GUILayout.EndVertical();

            GUILayout.Space(6);

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("FRIEND CODE SPOOF");
            enableFriendCodeSpoof = DrawToggle(enableFriendCodeSpoof, "Enable FC Spoof", 180);
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            if (DrawPseudoInputButton(spoofFriendCodeInput, isEditingFriendCode, 28f, 54))
            {
                isEditingFriendCode = !isEditingFriendCode;
                isEditingName = false;
                isEditingLevel = false;
                isEditingLocalFriendCode = false;
                isEditingGhostChatColor = false;
            }
            if (GUILayout.Button("Apply", btnStyle, GUILayout.Width(56), GUILayout.Height(28)))
            {
                isEditingFriendCode = false;
                spoofFriendCodeInput = SanitizeSpoofFriendCode(spoofFriendCodeInput);
                SaveConfig();
            }
            GUILayout.EndHorizontal();
            DrawClippedHint("Guest-style code: <=10, [a-z0-9], no spaces");
            GUILayout.EndVertical();

            GUILayout.Space(6);

            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("PLATFORM SPOOF");
            if (GUILayout.Button("Spoof Platform", enablePlatformSpoof ? activeTabStyle : btnStyle, GUILayout.Height(26)))
            {
                enablePlatformSpoof = !enablePlatformSpoof;
                SaveConfig();
            }
            GUILayout.Space(2);
            string hexColor = GetMenuAccentHex();
            GUILayout.Label($"Platform: <color=#{hexColor}>{platformNames[currentPlatformIndex]}</color>", new GUIStyle(toggleLabelStyle) { fontSize = 12, richText = true }, GUILayout.Height(23));
            int newPlatIdx = (int)GUILayout.HorizontalSlider(currentPlatformIndex, 0, platformNames.Length - 1, sliderStyle, sliderThumbStyle, GUILayout.ExpandWidth(true));
            if (newPlatIdx != currentPlatformIndex)
            {
                currentPlatformIndex = newPlatIdx;
                SaveConfig();
            }
            GUILayout.EndVertical();

            GUILayout.Space(8);
            DrawMenuSectionHeader("TASKS");
            if (GUILayout.Button("Complete My Tasks", btnStyle, GUILayout.Height(30)))
            {
                if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.myTasks != null)
                    foreach (var task in PlayerControl.LocalPlayer.myTasks)
                        if (task != null && !task.IsComplete) PlayerControl.LocalPlayer.RpcCompleteTask((uint)task.Id);
            }
            GUILayout.EndVertical();
        }



        private void DrawVisualsTab()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < visualsSubTabs.Length; i++)
                if (GUILayout.Button(visualsSubTabs[i], currentVisualsSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                { currentVisualsSubTab = i; scrollPosition = Vector2.zero; }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
            if (currentVisualsSubTab == 0) DrawVisualsInGame();
            else if (currentVisualsSubTab == 1) DrawOutfitsTab();
        }



        [HarmonyPatch(typeof(PlayerBanData), nameof(PlayerBanData.BanPoints), MethodType.Setter)]
        public static class RemoveDisconnectPenalty_Patch
        {
            public static bool Prefix(PlayerBanData __instance, ref float value)
            {
                if (!ElysiumModMenuGUI.removePenalty) return true;
                if (AmongUsClient.Instance == null || AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
                    return true;

                value = 0f;
                return false;
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
        public static class ShowLobbyTimer_Patch
        {
            public static void Postfix(GameStartManager __instance)
            {
                if (!ElysiumModMenuGUI.alwaysShowLobbyTimer) return;

                if (__instance == null || GameData.Instance == null || AmongUsClient.Instance == null) return;
                if (AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame || !AmongUsClient.Instance.AmHost) return;

                if (HudManager.Instance != null)
                {
                    HudManager.Instance.ShowLobbyTimer(600);
                }
            }
        }
        public static bool IsCursorOverMenu()
        {
            try
            {
                if (!showMenu || !hardMenu) return false;
                Vector2 guiPos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                return windowRect.Contains(guiPos);
            }
            catch { return false; }
        }

        [HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.ReceiveClickDown))]
        public static class HardMenu_BlockClickDown_Patch
        {
            public static bool Prefix() { return !ElysiumModMenuGUI.IsCursorOverMenu(); }
        }

        [HarmonyPatch(typeof(PassiveButton), nameof(PassiveButton.ReceiveClickUp))]
        public static class HardMenu_BlockClickUp_Patch
        {
            public static bool Prefix() { return !ElysiumModMenuGUI.IsCursorOverMenu(); }
        }

        private void DrawPlayersTab()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < playersSubTabs.Length; i++)
                if (GUILayout.Button(playersSubTabs[i], currentPlayersSubTab == i ? activeSubTabStyle : subTabStyle, GUILayout.Height(18)))
                { currentPlayersSubTab = i; scrollPosition = Vector2.zero; }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            if (currentPlayersSubTab == 1)
            {
                DrawPlayersHistoryTab();
                return;
            }

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(200));
            playerListScrollPos = GUILayout.BeginScrollView(playerListScrollPos);
            if (lockedPlayersList.Count > 0)
            {
                foreach (var pc in lockedPlayersList)
                {
                    if (pc == null || pc.Data == null || pc.PlayerId >= 100) continue;
                    string pName = pc.Data.PlayerName ?? "Unknown";

                    if (forcedPreGameRoles.ContainsKey(pc.PlayerId)) pName += " [*]";
                    else if (forcedImpostors.Contains(pc.PlayerId)) pName += " [Imp]";

                    bool isSelected = selectedAntiCheatPlayerId == pc.PlayerId;

                    GUI.contentColor = Color.white;
                    try { GUI.contentColor = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId]; } catch { }

                    if (GUILayout.Button(pName, isSelected ? activeTabStyle : btnStyle, GUILayout.Height(30)))
                    {
                        selectedAntiCheatPlayerId = pc.PlayerId;
                    }
                    GUI.contentColor = Color.white;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(8); GUILayout.BeginVertical(menuCardStyle, GUILayout.ExpandWidth(true));
            playerActionScrollPos = GUILayout.BeginScrollView(playerActionScrollPos);

            PlayerControl target = lockedPlayersList.FirstOrDefault(p => p.PlayerId == selectedAntiCheatPlayerId);

            if (target != null && target.Data != null)
            {
                GUILayout.Label($"<color=#aaaaaa>Selected:</color> {target.Data.PlayerName}", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                if (GUILayout.Button("KILL", btnStyle, GUILayout.Height(25)))
                {
                    Vector3 op = PlayerControl.LocalPlayer.transform.position;
                    PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(target.transform.position);
                    PlayerControl.LocalPlayer.CmdCheckMurder(target);
                    PlayerControl.LocalPlayer.RpcMurderPlayer(target, true);
                    PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(op);
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("TP TO", activeTabStyle, GUILayout.Height(25)))
                {
                    teleportToPlayer(target);
                    ShowNotification($"<color=#00FF00>[TELEPORT]</color> Телепортирован к <b>{target.Data.PlayerName}</b>!");
                }

                GUI.backgroundColor = new Color(1f, 0.5f, 0f, 1f);
                if (GUILayout.Button("Force Eject", btnStyle, GUILayout.Height(25))) ForceGlobalEject(target);
                GUI.backgroundColor = Color.white;

                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Force Meeting", btnStyle, GUILayout.Height(25))) ForceMeetingAsPlayer(target);

                bool hr = rainbowPlayers.Contains(target.PlayerId);
                if (GUILayout.Button(hr ? "RGB: ON" : "RGB: OFF", hr ? activeTabStyle : btnStyle, GUILayout.Height(25)))
                {
                    if (!hr) rainbowPlayers.Add(target.PlayerId);
                    else rainbowPlayers.Remove(target.PlayerId);
                }

                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Report Body", btnStyle, GUILayout.Height(25)))
                    AttemptReportBody(target);

                if (GUILayout.Button("Flood Tasks", btnStyle, GUILayout.Height(25)))
                    FloodPlayerWithTasks(target);

                if (GUILayout.Button("Clear Tasks", btnStyle, GUILayout.Height(25)))
                    ClearPlayerTasks(target);

                GUILayout.EndHorizontal();

                GUILayout.Space(10);
                DrawMenuSectionHeader("TARGET ROLE CONTROL");

                GUILayout.BeginHorizontal();
                GUIStyle roleMidStyle = new GUIStyle(btnStyle) { fontStyle = FontStyle.Bold, normal = { background = null, textColor = GetMenuAccentColor() }, alignment = TextAnchor.MiddleCenter };
                if (GUILayout.Button("<", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
                {
                    targetRoleAssignIdx--;
                    if (targetRoleAssignIdx < 0) targetRoleAssignIdx = roleAssignOptions.Length - 1;
                }
                GUILayout.Label(roleAssignNames[targetRoleAssignIdx], roleMidStyle, GUILayout.Height(24), GUILayout.ExpandWidth(true));
                if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(24)))
                {
                    targetRoleAssignIdx++;
                    if (targetRoleAssignIdx >= roleAssignOptions.Length) targetRoleAssignIdx = 0;
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("TARGET -> ROLE", btnStyle, GUILayout.Height(26)))
                {
                    if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                    {
                        ShowNotification("<color=#FF0000>[ОШИБКА]</color> Требуются права хоста!");
                    }
                    else
                    {
                        if (IsGhostRoleSelection(targetRoleAssignIdx))
                        {
                            MakePlayerGhost(target);
                        }
                        else if (IsGhostImpostorRoleSelection(targetRoleAssignIdx))
                        {
                            MakePlayerGhost(target, true);
                        }
                        else
                        {
                            SetPlayerRole(target, roleAssignOptions[targetRoleAssignIdx]);
                            ShowNotification($"<color=#00FF00>[ROLE]</color> {target.Data.PlayerName} -> {roleAssignNames[targetRoleAssignIdx]}");
                        }
                    }
                }
                if (GUILayout.Button("TARGET -> GHOST", btnStyle, GUILayout.Height(26)))
                {
                    MakePlayerGhost(target);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("REVIVE TARGET", activeTabStyle, GUILayout.Height(26)))
                {
                    RevivePlayer(target);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(15);
                GUILayout.Label("<color=#aaaaaa>Morph Target:</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });
                GUILayout.BeginHorizontal();

                int mIdx = lockedPlayersList.FindIndex(p => p.PlayerId == selectedMorphTargetId);

                GUI.backgroundColor = currentAccentColor;
                if (GUILayout.Button("<", btnStyle, GUILayout.Width(25), GUILayout.Height(25)))
                {
                    if (lockedPlayersList.Count > 0) { mIdx--; if (mIdx < 0) mIdx = lockedPlayersList.Count - 1; selectedMorphTargetId = lockedPlayersList[mIdx].PlayerId; }
                }
                GUI.backgroundColor = Color.white;

                string morphName = "Target";
                if (mIdx >= 0 && mIdx < lockedPlayersList.Count) morphName = lockedPlayersList[mIdx].Data.PlayerName;
                if (morphName.Length > 10) morphName = morphName.Substring(0, 10) + "..";

                GUIStyle morphLabelStyle = new GUIStyle(btnStyle);
                morphLabelStyle.normal.background = null;
                morphLabelStyle.hover.background = null;
                morphLabelStyle.normal.textColor = GetMenuAccentColor();
                morphLabelStyle.fontStyle = FontStyle.Bold;
                morphLabelStyle.alignment = TextAnchor.MiddleCenter;

                GUILayout.Label(morphName, morphLabelStyle, GUILayout.Height(25), GUILayout.ExpandWidth(true));

                GUI.backgroundColor = currentAccentColor;
                if (GUILayout.Button(">", btnStyle, GUILayout.Width(25), GUILayout.Height(25)))
                {
                    if (lockedPlayersList.Count > 0) { mIdx++; if (mIdx >= lockedPlayersList.Count) mIdx = 0; selectedMorphTargetId = lockedPlayersList[mIdx].PlayerId; }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                GUI.backgroundColor = currentAccentColor;
                if (GUILayout.Button("MORPH TARGET", btnStyle, GUILayout.Width(160), GUILayout.Height(25)))
                {
                    var morphTarget = lockedPlayersList.FirstOrDefault(p => p.PlayerId == selectedMorphTargetId) ?? target;
                    this.StartCoroutine(AttemptShapeshiftFrame(target, morphTarget).WrapToIl2Cpp());
                }
                GUI.backgroundColor = Color.white;

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(15);
                DrawMenuSectionHeader("SET PLAYER COLOR");
                GUILayout.BeginVertical();

                GUIStyle roundedColorBtnStyle = new GUIStyle();
                roundedColorBtnStyle.normal.background = texColorBtn;
                roundedColorBtnStyle.margin = CreateRectOffset(2, 2, 2, 2);

                int colorsPerRow = 7;
                for (int i = 0; i < Palette.PlayerColors.Length; i++)
                {
                    if (i % colorsPerRow == 0) GUILayout.BeginHorizontal();

                    GUI.color = Palette.PlayerColors[i];

                    if (GUILayout.Button("", roundedColorBtnStyle, GUILayout.Width(32), GUILayout.Height(30)))
                        target.RpcSetColor((byte)i);

                    if (i % colorsPerRow == colorsPerRow - 1 || i == Palette.PlayerColors.Length - 1)
                        GUILayout.EndHorizontal();
                }
                GUI.color = Color.white;
                GUILayout.EndVertical();

                GUILayout.Space(15);
                DrawMenuSectionHeader("PRE-GAME ROLE (HOST)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Impostor", btnStyle, GUILayout.Height(25))) { forcedPreGameRoles.Remove(target.PlayerId); forcedImpostors.Add(target.PlayerId); enablePreGameRoleForce = true; }
                if (GUILayout.Button("Crewmate", btnStyle, GUILayout.Height(25))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Crewmate; enablePreGameRoleForce = true; }
                if (GUILayout.Button("Shapeshifter", btnStyle, GUILayout.Height(25))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Shapeshifter; enablePreGameRoleForce = true; }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                if (GUILayout.Button("REMOVE FORCED ROLE", activeTabStyle, GUILayout.Height(25))) { forcedPreGameRoles.Remove(target.PlayerId); forcedImpostors.Remove(target.PlayerId); }
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#777777>Select a player...</color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
                GUILayout.FlexibleSpace();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawPlayersHistoryTab()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("PLAYER HISTORY");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Entries: {playerHistoryEntries.Count}", new GUIStyle(toggleLabelStyle) { fontSize = 11, clipping = TextClipping.Overflow, wordWrap = false }, GUILayout.MinWidth(128), GUILayout.ExpandWidth(false));
            GUILayout.Label("File: ElysiumPlayerHistory.txt", new GUIStyle(toggleLabelStyle) { fontSize = 11, clipping = TextClipping.Overflow, wordWrap = false }, GUILayout.MinWidth(220), GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear History", btnStyle, GUILayout.Width(136), GUILayout.Height(24)))
            {
                playerHistoryEntries.Clear();
                playerHistoryKeysById.Clear();
                WritePlayerHistoryFile();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            playersHistoryScroll = GUILayout.BeginScrollView(playersHistoryScroll);
            if (playerHistoryEntries.Count == 0)
            {
                GUILayout.Label("<color=#777777>История пока пустая.</color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
            }
            else
            {
                foreach (var e in playerHistoryEntries.OrderByDescending(x => x.LastSeenUtc))
                {
                    GUILayout.BeginVertical();
                    string status = e.IsOnline ? "<color=#55FF77>ONLINE</color>" : "<color=#aaaaaa>LEFT</color>";
                    GUILayout.Label($"{e.Name}  {status}", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 });
                    GUILayout.Label($"Lv: {e.Level} | FC: {e.FriendCode} | PUID: {e.Puid}", new GUIStyle(GUI.skin.label) { fontSize = 11 });
                    GUILayout.Label($"Joined: {e.FirstSeenUtc:HH:mm:ss} | Left: {(e.LeftUtc.HasValue ? e.LeftUtc.Value.ToString("HH:mm:ss") : "online")}", new GUIStyle(GUI.skin.label) { fontSize = 11 });
                    GUILayout.Label($"Platform: {FormatPlatformHistory(e)}", new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true });
                    GUILayout.Label($"RPC: {FormatRpcHistory(e)}", new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true });
                    GUILayout.EndVertical();
                    GUILayout.Space(2);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        private void ForceGlobalEject(PlayerControl target)
        {
            if (target == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[ERROR]</color> Нужны права Хоста!");
                return;
            }

            try
            {
                target.Data.IsDead = false;

                if (MeetingHud.Instance == null)
                {
                    MeetingHud.Instance = UnityEngine.Object.Instantiate<MeetingHud>(DestroyableSingleton<HudManager>.Instance.MeetingPrefab);
                    AmongUsClient.Instance.Spawn(MeetingHud.Instance.Cast<InnerNetObject>(), -2, SpawnFlags.None);
                }

                var emptyStates = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<MeetingHud.VoterState>(0);

                MeetingHud.Instance.RpcVotingComplete(emptyStates, target.Data, false);

                MeetingHud.Instance.RpcClose();

                ShowNotification($"<color=#00FF00>[EJECT]</color> Изгоняем <b>{target.Data.PlayerName}</b>...");
            }
            catch (Exception)
            {
            }
        }

        private static bool IsDeadBodyForPlayerPresent(byte playerId)
        {
            try
            {
                var allBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                foreach (var mb in allBehaviours)
                {
                    if (mb == null || mb.gameObject == null) continue;
                    Type t = mb.GetType();
                    if (t == null || t.Name != "DeadBody") continue;

                    byte parentId = byte.MaxValue;
                    var parentProp = t.GetProperty("ParentId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (parentProp != null)
                    {
                        object val = parentProp.GetValue(mb, null);
                        if (val is byte b) parentId = b;
                        else if (val is int i) parentId = (byte)i;
                    }
                    else
                    {
                        var parentField = t.GetField("ParentId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (parentField != null)
                        {
                            object val = parentField.GetValue(mb);
                            if (val is byte b) parentId = b;
                            else if (val is int i) parentId = (byte)i;
                        }
                    }

                    if (parentId == playerId) return true;
                }
            }
            catch { }

            return false;
        }

        private static void AttemptReportBody(PlayerControl target)
        {
            if (target == null || target.Data == null || PlayerControl.LocalPlayer == null) return;

            try
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    PlayerControl.LocalPlayer.CmdReportDeadBody(target.Data);
                    ShowNotification($"<color=#00FF00>[REPORT]</color> Репорт {target.Data.PlayerName}");
                    return;
                }

                if (LobbyBehaviour.Instance != null)
                {
                    ShowNotification("<color=#FF0000>[REPORT]</color> Игра должна начаться.");
                    return;
                }

                if (!target.Data.IsDead)
                {
                    ShowNotification("<color=#FF0000>[REPORT]</color> Можно репортить только мертвых игроков.");
                    return;
                }

                if (!IsDeadBodyForPlayerPresent(target.PlayerId))
                {
                    ShowNotification("<color=#FF0000>[REPORT]</color> Труп не найден или уже исчез.");
                    return;
                }

                PlayerControl.LocalPlayer.CmdReportDeadBody(target.Data);
                ShowNotification($"<color=#00FF00>[REPORT]</color> Репорт {target.Data.PlayerName}");
            }
            catch (Exception)
            {
            }
        }

        private static void FloodPlayerWithTasks(PlayerControl target)
        {
            if (target == null || target.Data == null)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Цель не найдена.");
                return;
            }

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Нужны права хоста.");
                return;
            }

            try
            {
                byte[] taskIds = new byte[255];
                for (byte i = 0; i < 255; i++) taskIds[i] = i;
                target.Data.RpcSetTasks(taskIds);
                ShowNotification($"<color=#00FF00>[TASKS]</color> {target.Data.PlayerName} получил flood tasks.");
            }
            catch (Exception)
            {
            }
        }

        private static void ClearPlayerTasks(PlayerControl target)
        {
            if (target == null || target.Data == null)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Цель не найдена.");
                return;
            }

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[TASKS]</color> Нужны права хоста.");
                return;
            }

            try
            {
                target.Data.RpcSetTasks(Array.Empty<byte>());
                ShowNotification($"<color=#00FF00>[TASKS]</color> Задачи {target.Data.PlayerName} очищены.");
            }
            catch (Exception)
            {
            }
        }

        private static string GetRoleDisplayName(RoleTypes role)
        {
            for (int i = 0; i < roleAssignOptions.Length; i++)
                if (roleAssignOptions[i].Equals(role))
                    return roleAssignNames[i];
            return role.ToString();
        }

        private static bool IsGhostRoleSelection(int roleIndex)
        {
            return roleIndex >= 0 &&
                   roleIndex < roleAssignNames.Length &&
                   string.Equals(roleAssignNames[roleIndex], "Ghost", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsGhostImpostorRoleSelection(int roleIndex)
        {
            return roleIndex >= 0 &&
                   roleIndex < roleAssignNames.Length &&
                   string.Equals(roleAssignNames[roleIndex], "Ghost Imp", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsImpostorTeamRole(RoleTypes role)
        {
            int roleId = (int)role;
            return role == RoleTypes.Impostor || role == RoleTypes.Shapeshifter || roleId == 9 || roleId == 18;
        }

        public static void RevivePlayer(PlayerControl target)
        {
            if (target == null || target.Data == null)
            {
                ShowNotification("<color=#FF0000>[ОШИБКА]</color> Цель не найдена!");
                return;
            }
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[ОШИБКА]</color> Требуются права хоста!");
                return;
            }
            if (!target.Data.IsDead)
            {
                ShowNotification($"{target.Data.PlayerName} уже жив!");
                return;
            }

            try
            {
                target.Data.IsDead = false;

                if (target.Collider != null) target.Collider.enabled = true;

                if (target.MyPhysics != null)
                    target.MyPhysics.gameObject.layer = LayerMask.NameToLayer("Players");

                try
                {
                    var allBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                    foreach (var mb in allBehaviours)
                    {
                        if (mb == null || mb.gameObject == null) continue;
                        Type t = mb.GetType();
                        if (t == null || t.Name != "DeadBody") continue;

                        byte parentId = byte.MaxValue;

                        var parentProp = t.GetProperty("ParentId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (parentProp != null)
                        {
                            object val = parentProp.GetValue(mb, null);
                            if (val is byte b) parentId = b;
                            else if (val is int i) parentId = (byte)i;
                        }
                        else
                        {
                            var parentField = t.GetField("ParentId", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (parentField != null)
                            {
                                object val = parentField.GetValue(mb);
                                if (val is byte b) parentId = b;
                                else if (val is int i) parentId = (byte)i;
                            }
                        }

                        if (parentId == target.PlayerId)
                            mb.gameObject.SetActive(false);
                    }
                }
                catch { }

                bool wasImpTeam = false;
                try
                {
                    if (target.Data.Role != null)
                    {
                        int roleId = (int)target.Data.Role.Role;
                        wasImpTeam = roleId == 1 || roleId == 5 || roleId == 7 || roleId == 9 || roleId == 18;
                    }
                    else
                    {
                        var rt = target.Data.RoleType;
                        wasImpTeam = rt == RoleTypes.Impostor || rt == RoleTypes.Shapeshifter || (int)rt == 9 || (int)rt == 18;
                    }
                }
                catch { }

                target.RpcSetRole(wasImpTeam ? RoleTypes.Impostor : RoleTypes.Crewmate, true);

                var netObj = GameData.Instance?.GetComponent<InnerNetObject>();
                if (netObj != null) netObj.SetDirtyBit(uint.MaxValue);

                ShowNotification($"<color=#00FF00>[ВОСКРЕШЕНИЕ]</color> {target.Data.PlayerName} воскрешён!");
            }
            catch (Exception)
            {
                ShowNotification("<color=#FF0000>Ошибка воскрешения!</color>");
            }
        }

        public static void MakePlayerGhost(PlayerControl target, bool impostorGhost = false, bool notify = true)
        {
            if (target == null || target.Data == null)
            {
                if (notify) ShowNotification("<color=#FF0000>[ОШИБКА]</color> Цель не найдена!");
                return;
            }
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                if (notify) ShowNotification("<color=#FF0000>[ОШИБКА]</color> Требуются права хоста!");
                return;
            }
            if (target.Data.IsDead)
            {
                if (!TrySetGhostRole(target, impostorGhost, out _))
                    SetPlayerRole(target, impostorGhost ? RoleTypes.Impostor : (IsImpostorTeamRole(target.Data.RoleType) ? RoleTypes.Impostor : RoleTypes.Crewmate));
                if (notify) ShowNotification($"{target.Data.PlayerName} уже призрак!");
                return;
            }

            try
            {
                string methodUsed;
                if (!TrySetGhostRole(target, impostorGhost, out methodUsed))
                {
                    RoleTypes fallbackRole = impostorGhost ? RoleTypes.Impostor : (IsImpostorTeamRole(target.Data.RoleType) ? RoleTypes.Impostor : RoleTypes.Crewmate);
                    SetPlayerRole(target, fallbackRole);
                    TryActivateGhostState(target, out methodUsed);
                }

                var netObj = GameData.Instance?.GetComponent<InnerNetObject>();
                if (netObj != null) netObj.SetDirtyBit(uint.MaxValue);

                if (notify) ShowNotification($"<color=#00FF00>[GHOST]</color> {target.Data.PlayerName} стал призраком ({methodUsed})!");
            }
            catch (Exception)
            {
                if (notify) ShowNotification("<color=#FF0000>Ошибка перевода в призрака!</color>");
            }
        }

        private static bool TrySetGhostRole(PlayerControl target, bool impostorGhost, out string methodUsed)
        {
            methodUsed = string.Empty;
            if (target == null || target.Data == null) return false;

            string[] roleNames = impostorGhost
                ? new[] { "ImpostorGhost", "GhostImpostor", "ImpGhost", "Ghost" }
                : new[] { "CrewmateGhost", "GhostCrewmate", "CrewGhost", "Ghost" };

            foreach (string roleName in roleNames)
            {
                if (!Enum.TryParse(roleName, true, out RoleTypes ghostRole)) continue;

                try { target.RpcSetRole(ghostRole, true); } catch { }
                try { RoleManager.Instance?.SetRole(target, ghostRole); } catch { }

                methodUsed = $"SetRole:{roleName}";
                return true;
            }

            return false;
        }

        private static bool TryActivateGhostState(PlayerControl target, out string methodUsed)
        {
            methodUsed = string.Empty;
            if (target == null) return false;
            if (target.Data != null && target.Data.IsDead)
            {
                methodUsed = "already_dead";
                return true;
            }

            if (TryDie(target, DeathReason.Exile, true) ||
                TryDie(target, DeathReason.Exile, false) ||
                TryDie(target, DeathReason.Kill, true) ||
                TryDie(target, DeathReason.Kill, false))
            {
                methodUsed = "Die";
                return true;
            }

            if (TryInvokeNoArg(target, "Exiled") ||
                TryInvokeNoArg(target, "RpcExiled") ||
                TryInvokeNoArg(target, "RpcExiledV2") ||
                TryInvokeNoArg(target, "SetDead"))
            {
                methodUsed = "Exiled/SetDead";
                return true;
            }

            if (TrySetDeadFlag(target))
            {
                methodUsed = "Data.IsDead";
                return true;
            }

            methodUsed = "fallback";
            return false;
        }

        private static bool TryDie(PlayerControl target, DeathReason reason, bool allowAnimation)
        {
            try { target.Die(reason, allowAnimation); }
            catch { }
            return target != null && target.Data != null && target.Data.IsDead;
        }

        private static bool TryInvokeNoArg(object target, string methodName)
        {
            if (target == null || string.IsNullOrWhiteSpace(methodName)) return false;
            try
            {
                for (Type type = target.GetType(); type != null; type = type.BaseType)
                {
                    MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    if (method == null) continue;
                    method.Invoke(target, null);
                    return target is PlayerControl player && player.Data != null && player.Data.IsDead;
                }
            }
            catch { }
            return false;
        }

        private static bool TrySetDeadFlag(PlayerControl target)
        {
            if (target == null || target.Data == null) return false;
            try
            {
                target.Data.IsDead = true;
                if (target.Collider != null) target.Collider.enabled = false;
                if (target.MyPhysics != null) target.MyPhysics.gameObject.layer = LayerMask.NameToLayer("Ghost");
            }
            catch { }
            return target.Data.IsDead;
        }

        public static void SetAllPlayersGhost()
        {
            SetAllPlayersGhost(false);
        }

        public static void SetAllPlayersGhost(bool impostorGhost)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[ОШИБКА]</color> Требуются права хоста!");
                return;
            }
            if (PlayerControl.AllPlayerControls == null) return;

            int count = 0;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                {
                    MakePlayerGhost(pc, impostorGhost, false);
                    count++;
                }
            }

            ShowNotification($"<color=#00FF00>[GHOST]</color> {count} игрок(а/ов) стали {(impostorGhost ? "ghost impostor" : "призраками")}!");
        }

        public static void SetAllPlayersRole(RoleTypes role)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            {
                ShowNotification("<color=#FF0000>[ОШИБКА]</color> Требуются права хоста!");
                return;
            }
            if (PlayerControl.AllPlayerControls == null) return;

            int count = 0;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                {
                    pc.RpcSetRole(role, true);
                    count++;
                }
            }

            ShowNotification($"<color=#00FF00>[РОЛИ]</color> {count} игрок(а/ов) получили роль {GetRoleDisplayName(role)}!");
        }

        public static void SetPlayerRole(PlayerControl target, RoleTypes newRole)
        {
            if (target == null || target.Data == null) return;
            target.RpcSetRole(newRole, true);
        }

        private void DrawRolesTab()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(280));

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Roles", headerStyle);
            GUILayout.BeginHorizontal();
            GUIStyle middleLabelStyle = new GUIStyle(btnStyle) { fontStyle = FontStyle.Bold, normal = { background = null, textColor = GetMenuAccentColor() } };
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(25), GUILayout.Height(22))) { fakeRoleIdx--; if (fakeRoleIdx < 0) fakeRoleIdx = forceRoleOptions.Length - 1; }
            GUILayout.Label(forceRoleOptions[fakeRoleIdx].ToString(), middleLabelStyle, GUILayout.Width(100), GUILayout.Height(22));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(25), GUILayout.Height(22))) { fakeRoleIdx++; if (fakeRoleIdx >= forceRoleOptions.Length) fakeRoleIdx = 0; }
            GUILayout.Space(15);
            if (GUILayout.Button("Set", activeTabStyle, GUILayout.Width(45), GUILayout.Height(22))) RoleManager.Instance?.SetRole(PlayerControl.LocalPlayer, forceRoleOptions[fakeRoleIdx]);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Impostor", headerStyle);
            killReach = DrawToggle(killReach, "Kill Reach", 160);
            GUILayout.Space(5);
            killAnyone = DrawToggle(killAnyone, "Kill Anyone", 160);
            GUILayout.Space(5);
            killAuraHostOnly = DrawToggle(killAuraHostOnly, "Kill Aura", 160);
            GUILayout.Space(5);
            noKillCooldownHostOnly = DrawToggle(noKillCooldownHostOnly, "Kill Cooldown 0 (Host)", 160);
            GUILayout.Space(5);
            spamReportBodies = DrawToggle(spamReportBodies, "Spam Report Bodies", 160);
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Shapeshifter", headerStyle);
            NoShapeshiftAnim = DrawToggle(NoShapeshiftAnim, "No Ss Animation", 160);
            GUILayout.Space(5);
            endlessSsDuration = DrawToggle(endlessSsDuration, "Endless Ss Duration", 160);
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Tracker", headerStyle);
            EndlessTracking = DrawToggle(EndlessTracking, "Endless Tracking", 160);
            GUILayout.Space(5);
            NoTrackingCooldown = DrawToggle(NoTrackingCooldown, "No Track Cooldown", 160);
            GUILayout.EndVertical();

            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(GUILayout.Width(280));

            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Engineer", headerStyle);
            endlessVentTime = DrawToggle(endlessVentTime, "Endless Vent Time", 160);
            GUILayout.Space(5);
            noVentCooldown = DrawToggle(noVentCooldown, "No Vent Cooldown", 160);
            GUILayout.Space(5);
            noMapCooldowns = DrawToggle(noMapCooldowns, "No Map Cooldowns", 160);
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Scientist", headerStyle);
            endlessBattery = DrawToggle(endlessBattery, "Endless Battery", 160);
            GUILayout.Space(5);
            noVitalsCooldown = DrawToggle(noVitalsCooldown, "No Vitals Cooldown", 160);
            GUILayout.EndVertical();

            GUILayout.Space(5);
            GUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Detective", headerStyle);
            UnlimitedInterrogateRange = DrawToggle(UnlimitedInterrogateRange, "Interrogate Reach", 160);
            GUILayout.EndVertical();

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private Vector2 doorsScrollPos = Vector2.zero;

        private void DrawSabotagesTab()
        {
            GUIStyle miniLabelStyle = new GUIStyle(toggleLabelStyle) { fontSize = 11, richText = true, wordWrap = true };
            miniLabelStyle.normal.textColor = whiteMenuTheme ? new Color(0.25f, 0.25f, 0.25f, 1f) : new Color(0.72f, 0.72f, 0.72f, 1f);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(276), GUILayout.ExpandHeight(false));
            DrawMenuSectionHeader("CRITICAL SABOTAGES");
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            if (DrawColoredActionButton("FIX ALL", new Color32(83, 231, 139, 255), 116f, 32f)) FixAllSabotages();
            GUILayout.Space(6);
            if (DrawColoredActionButton("TRIGGER ALL", new Color32(255, 74, 74, 255), 116f, 32f)) TriggerAllSabotages();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            if (GUILayout.Button("CALL MEETING", btnStyle, GUILayout.Height(30))) callMeetingPublic();

            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            DrawSabotageButton("Reactor", ref reactorSab, ToggleReactor, new Color32(255, 84, 84, 255));
            GUILayout.Space(6);
            DrawSabotageButton("Oxygen", ref oxygenSab, ToggleO2, new Color32(255, 132, 54, 255));
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            DrawSabotageButton("Comms", ref commsSab, ToggleComms, new Color32(66, 205, 128, 255));
            GUILayout.Space(6);
            DrawSabotageButton("Lights", ref elecSab, ToggleLights, new Color32(255, 218, 77, 255));
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            if (GUILayout.Button("MUSHROOM MIXUP", btnStyle, GUILayout.Height(28))) SabotageMushroom();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.ExpandWidth(true));
            DrawMenuSectionHeader("DOOR LOCKDOWN");
            GUILayout.Space(4);
            GUILayout.Label("<b>Global controls</b>", miniLabelStyle);

            GUILayout.BeginHorizontal();
            if (DrawColoredActionButton("CLOSE", new Color32(255, 106, 66, 255), 82f, 30f)) SabotageDoors();
            GUILayout.Space(6);
            if (DrawColoredActionButton("LOCK", new Color32(255, 184, 64, 255), 82f, 30f)) LockAllDoors();
            GUILayout.Space(6);
            if (DrawColoredActionButton("OPEN", new Color32(89, 219, 146, 255), 82f, 30f)) OpenAllDoors();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("<b>Target doors</b>", miniLabelStyle);

            if (ShipStatus.Instance != null && ShipStatus.Instance.AllDoors != null)
            {
                var rooms = ShipStatus.Instance.AllDoors
                    .Where(d => d != null)
                    .Select(d => d.Room)
                    .Distinct()
                    .OrderBy(r => r.ToString())
                    .ToList();

                doorsScrollPos = GUILayout.BeginScrollView(doorsScrollPos, false, true, GUILayout.Height(214));
                foreach (var room in rooms)
                {
                    DrawDoorTargetRow(room);
                    GUILayout.Space(3);
                }
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#777777>Вы не в игре или на карте нет дверей.</color>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true });
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawSabotageButton(string label, ref bool state, Action<bool> toggleAction, Color accent)
        {
            GUIStyle style = state ? activeTabStyle : btnStyle;
            Color oldBackground = GUI.backgroundColor;
            GUI.backgroundColor = state ? accent : Color.white;

            if (GUILayout.Button(state ? label + "  ON" : label, style, GUILayout.Height(30)))
            {
                state = !state;
                toggleAction(state);
            }

            GUI.backgroundColor = oldBackground;
        }

        private void DrawDoorTargetRow(SystemTypes room)
        {
            GUILayout.BeginHorizontal(boxStyle);
            GUILayout.Label($"<b>{room}</b>", toggleLabelStyle, GUILayout.Width(96));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close", btnStyle, GUILayout.Width(52), GUILayout.Height(24))) CloseDoorsOfType(room);
            GUILayout.Space(4);
            if (GUILayout.Button("Lock", activeSubTabStyle, GUILayout.Width(52), GUILayout.Height(24))) LockDoorsOfType(room);
            GUILayout.Space(4);
            if (GUILayout.Button("Open", btnStyle, GUILayout.Width(52), GUILayout.Height(24))) OpenDoorsOfType(room);

            GUILayout.EndHorizontal();
        }
        private void callMeetingPublic()
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.AllPlayerControls == null) return;
            try
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc != null && pc.Data != null && pc.Data.IsDead && !pc.Data.Disconnected)
                    {
                        PlayerControl.LocalPlayer.CmdReportDeadBody(pc.Data);
                        ShowNotification($"<color=#00FF00>[MEETING]</color> Найден и зарепорчен труп: <b>{pc.Data.PlayerName}</b>!");
                        return;
                    }
                }

                PlayerControl.LocalPlayer.CmdReportDeadBody(null);
                ShowNotification("<color=#00FF00>[MEETING]</color> Легально нажата кнопка собрания!");
            }
            catch { }
        }
        private void TriggerAllSabotages()
        {
            if (ShipStatus.Instance == null) return;
            try
            {
                reactorSab = true;
                oxygenSab = true;
                commsSab = true;
                elecSab = true;

                ToggleReactor(true);
                ToggleO2(true);
                ToggleComms(true);
                ToggleLights(true);

                ShowNotification("<color=#FF0000>[SABOTAGE]</color> Все системы саботированы!");
            }
            catch { }
        }
        private void FixAllSabotages()
        {
            if (ShipStatus.Instance == null) return;
            try
            {
                reactorSab = false;
                oxygenSab = false;
                commsSab = false;
                elecSab = false;

                ToggleReactor(false);
                ToggleO2(false);
                ToggleComms(false);
                ToggleLights(false);

                if (ShipStatus.Instance.AllDoors != null)
                {
                    foreach (var door in ShipStatus.Instance.AllDoors)
                    {
                        if (door != null)
                        {
                            door.SetDoorway(true);
                            try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(door.Id | 64)); } catch { }
                        }
                    }
                }
                try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.MushroomMixupSabotage, 0); } catch { }
                ShowNotification("<color=#00FF00>[SABOTAGE]</color> Все саботажи и двери починены!");
            }
            catch { }
        }

        private void SabotageDoors()
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;
            try
            {
                var rooms = new System.Collections.Generic.HashSet<SystemTypes>();
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door != null)
                    {
                        rooms.Add(door.Room);
                        try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)door.Id); } catch { }
                    }
                }
                foreach (var room in rooms)
                {
                    try { ShipStatus.Instance.RpcCloseDoorsOfType(room); } catch { }
                }
                ShowNotification("<color=#FF0000>[DOORS]</color> Сигнал на закрытие отправлен!");
            }
            catch { }
        }


        private void CloseDoorsOfType(SystemTypes room)
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;
            try
            {
                ShipStatus.Instance.RpcCloseDoorsOfType(room);
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door != null && door.Room == room)
                        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)door.Id);
                }
                ShowNotification($"<color=#FF6A42>[DOORS]</color> {room}: close sent");
            }
            catch { }
        }

        private void LockDoorsOfType(SystemTypes room)
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;
            try
            {
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door != null && door.Room == room)
                    {
                        door.SetDoorway(false);
                        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)door.Id);
                    }
                }
                ShipStatus.Instance.RpcCloseDoorsOfType(room);
                ShowNotification($"<color=#FFB840>[DOORS]</color> {room}: locked");
            }
            catch { }
        }

        private void OpenDoorsOfType(SystemTypes room)
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;
            try
            {
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door != null && door.Room == room)
                    {
                        door.SetDoorway(true);
                        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(door.Id | 64));
                    }
                }
                ShowNotification($"<color=#59DB92>[DOORS]</color> {room}: opened");
            }
            catch { }
        }

        private void LockAllDoors()
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;
            try
            {
                var rooms = new System.Collections.Generic.HashSet<SystemTypes>();
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door != null)
                    {
                        door.SetDoorway(false);
                        rooms.Add(door.Room);
                        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)door.Id);
                    }
                }
                foreach (var room in rooms)
                    ShipStatus.Instance.RpcCloseDoorsOfType(room);

                ShowNotification("<color=#FFB840>[DOORS]</color> Все двери залочены!");
            }
            catch { }
        }
        private void OpenAllDoors()
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllDoors == null) return;
            try
            {
                foreach (var door in ShipStatus.Instance.AllDoors)
                {
                    if (door != null)
                    {
                        door.SetDoorway(true);
                        try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(door.Id | 64)); } catch { }
                    }
                }
                ShowNotification("<color=#00FF00>[DOORS]</color> Все двери открыты!");
            }
            catch { }
        }

        private void ToggleReactor(bool state) { if (ShipStatus.Instance == null) return; byte flag = (byte)(state ? 128 : 16); try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Reactor, flag); ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Laboratory, flag); if (state) ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, (byte)128); else { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, (byte)16); ShipStatus.Instance.RpcUpdateSystem(SystemTypes.HeliSabotage, (byte)17); } } catch { } }
        private void ToggleO2(bool state) { if (ShipStatus.Instance == null) return; try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.LifeSupp, (byte)(state ? 128 : 16)); } catch { } }
        private void ToggleComms(bool state) { if (ShipStatus.Instance == null) return; try { if (state) ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, (byte)128); else { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, (byte)16); ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Comms, (byte)17); } } catch { } }
        private void ToggleLights(bool state)
        {
            if (ShipStatus.Instance == null) return;
            try
            {
                if (state)
                {
                    byte b = 4;
                    for (int i = 0; i < 5; i++) if (UnityEngine.Random.value > 0.5f) b |= (byte)(1 << i);
                    ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, (byte)(b | 128));
                }
                else
                {
                    var sys = ShipStatus.Instance.Systems[SystemTypes.Electrical].Cast<SwitchSystem>();
                    if (sys != null)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            bool expected = (sys.ExpectedSwitches & (1 << i)) != 0;
                            bool actual = (sys.ActualSwitches & (1 << i)) != 0;
                            if (expected != actual) ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Electrical, (byte)i);
                        }
                    }
                }
            }
            catch { }
        }
        private void SabotageMushroom() { if (ShipStatus.Instance == null) return; try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.MushroomMixupSabotage, (byte)1); } catch { } }

        private void DrawPlayersRoles()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("PRE-GAME ROLE MANAGER");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(enablePreGameRoleForce ? "Role Forcing: ON" : "Role Forcing: OFF", enablePreGameRoleForce ? activeTabStyle : btnStyle, GUILayout.Height(25))) enablePreGameRoleForce = !enablePreGameRoleForce;
            if (GUILayout.Button("Random 2 Imps", btnStyle, GUILayout.Width(110), GUILayout.Height(25)))
            {
                forcedPreGameRoles.Clear(); forcedImpostors.Clear();
                var activePlayers = PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && !p.Data.Disconnected).ToList();
                if (activePlayers.Count >= 2)
                {
                    for (int i = activePlayers.Count - 1; i > 0; i--) { int swapIndex = UnityEngine.Random.Range(0, i + 1); var temp = activePlayers[i]; activePlayers[i] = activePlayers[swapIndex]; activePlayers[swapIndex] = temp; }
                    forcedImpostors.Add(activePlayers[0].PlayerId); forcedImpostors.Add(activePlayers[1].PlayerId);
                    enablePreGameRoleForce = true;
                }
            }
            if (GUILayout.Button("Clear All Roles", btnStyle, GUILayout.Width(110), GUILayout.Height(25))) { forcedPreGameRoles.Clear(); forcedImpostors.Clear(); }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader("LIVE ROLE DISTRIBUTOR (HOST)");
            GUILayout.BeginHorizontal();

            GUIStyle allRoleMidStyle = new GUIStyle(btnStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { background = null, textColor = GetMenuAccentColor() },
                alignment = TextAnchor.MiddleCenter
            };

            if (GUILayout.Button("<", btnStyle, GUILayout.Width(28), GUILayout.Height(25)))
            {
                allPlayersRoleAssignIdx--;
                if (allPlayersRoleAssignIdx < 0) allPlayersRoleAssignIdx = roleAssignOptions.Length - 1;
            }

            GUILayout.Label(roleAssignNames[allPlayersRoleAssignIdx], allRoleMidStyle, GUILayout.Height(25), GUILayout.ExpandWidth(true));

            if (GUILayout.Button(">", btnStyle, GUILayout.Width(28), GUILayout.Height(25)))
            {
                allPlayersRoleAssignIdx++;
                if (allPlayersRoleAssignIdx >= roleAssignOptions.Length) allPlayersRoleAssignIdx = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            if (GUILayout.Button("SET ALL PLAYERS ROLE", activeTabStyle, GUILayout.Height(28)))
            {
                if (IsGhostRoleSelection(allPlayersRoleAssignIdx))
                    SetAllPlayersGhost();
                else if (IsGhostImpostorRoleSelection(allPlayersRoleAssignIdx))
                    SetAllPlayersGhost(true);
                else
                    SetAllPlayersRole(roleAssignOptions[allPlayersRoleAssignIdx]);
            }
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("ALL -> GHOST", btnStyle, GUILayout.Height(26)))
                SetAllPlayersGhost();
            if (GUILayout.Button("ALL -> GHOST IMP", btnStyle, GUILayout.Height(26)))
                SetAllPlayersGhost(true);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(170));
            preRolesListScrollPos = GUILayout.BeginScrollView(preRolesListScrollPos);
            foreach (var pc in lockedPlayersList)
            {
                if (pc == null || pc.Data == null || pc.PlayerId >= 100) continue;
                string pName = pc.Data.PlayerName ?? "Unknown";
                if (forcedPreGameRoles.ContainsKey(pc.PlayerId)) { string rShort = forcedPreGameRoles[pc.PlayerId].ToString().Replace("9", "Pha").Replace("10", "Tra").Replace("8", "Noi").Replace("12", "Det").Replace("18", "Vip"); if (rShort.Length > 3) rShort = rShort.Substring(0, 3); pName += $" [{rShort}]"; }
                else if (forcedImpostors.Contains(pc.PlayerId)) pName += " [Imp]";
                bool isSelected = selectedPreRoleId == pc.PlayerId;
                try { GUI.contentColor = Palette.PlayerColors[pc.Data.DefaultOutfit.ColorId]; } catch { }
                if (GUILayout.Button(pName, isSelected ? activeTabStyle : btnStyle, GUILayout.Height(30))) selectedPreRoleId = pc.PlayerId;
                GUI.contentColor = Color.white;
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.Space(8);
            GUILayout.BeginVertical(menuCardStyle, GUILayout.ExpandWidth(true));
            preRolesActionScrollPos = GUILayout.BeginScrollView(preRolesActionScrollPos);
            PlayerControl target = lockedPlayersList.FirstOrDefault(p => p.PlayerId == selectedPreRoleId);
            if (target != null && target.Data != null)
            {
                GUIStyle infoStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 };
                GUILayout.Label($"<color=#aaaaaa>Selecting role for:</color> {target.Data.PlayerName}", infoStyle);
                RoleTypes currentForced = forcedPreGameRoles.ContainsKey(target.PlayerId) ? forcedPreGameRoles[target.PlayerId] : RoleTypes.Crewmate;
                bool isForced = forcedPreGameRoles.ContainsKey(target.PlayerId) || forcedImpostors.Contains(target.PlayerId);
                string roleNameStr = currentForced.ToString().Replace("9", "Phantom").Replace("10", "Tracker").Replace("8", "Noisemaker").Replace("12", "Detective").Replace("18", "Viper");
                if (forcedImpostors.Contains(target.PlayerId)) roleNameStr = "Impostor";
                GUILayout.Label($"<color=#aaaaaa>Status:</color> {(isForced ? $"<color=#00FF00>Forced ({roleNameStr})</color>" : "<color=#FF0000>Not Forced (Random)</color>")}", infoStyle);
                GUILayout.Space(15);
                DrawMenuSectionHeader("IMPOSTOR ROLES (Red Team)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Impostor", btnStyle, GUILayout.Height(30))) { forcedPreGameRoles.Remove(target.PlayerId); forcedImpostors.Add(target.PlayerId); }
                if (GUILayout.Button("Shapeshifter", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Shapeshifter; }
                if (GUILayout.Button("Phantom", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)9; }
                if (GUILayout.Button("Viper", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)18; }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
                DrawMenuSectionHeader("CREWMATE ROLES (Blue Team)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Crewmate", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Crewmate; }
                if (GUILayout.Button("Engineer", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Engineer; }
                if (GUILayout.Button("Scientist", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.Scientist; }
                if (GUILayout.Button("Tracker", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)10; }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Noisemaker", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)8; }
                if (GUILayout.Button("Guardian Angel", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = RoleTypes.GuardianAngel; }
                if (GUILayout.Button("Detective", btnStyle, GUILayout.Height(30))) { forcedImpostors.Remove(target.PlayerId); forcedPreGameRoles[target.PlayerId] = (RoleTypes)12; }
                GUILayout.EndHorizontal();
                GUILayout.Space(15);
                if (GUILayout.Button("REMOVE FORCED ROLE", activeTabStyle, GUILayout.Height(35))) { forcedPreGameRoles.Remove(target.PlayerId); forcedImpostors.Remove(target.PlayerId); }
                GUILayout.Space(20);
                GUILayout.Label("<color=#777777><b>Hide & Seek Notice:</b>\nВыбор Impostor/Shapeshifter/Phantom/Viper расширит лимит маньяков (Seekers) в Прятках!</color>", new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true });
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("<color=#777777>Select a player to set their role</color>", new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter });
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawMenuSectionHeader(string title)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(GUIContent.none, menuAccentBarStyle, GUILayout.Width(3), GUILayout.Height(16));
            GUILayout.Space(8);
            GUILayout.Label(title, menuSectionTitleStyle, GUILayout.Height(16));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
        }

        private void DrawMenuTab()
        {
            bool menuPrefsChanged = false;

            // ----- APPEARANCE -----
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("MENU CUSTOMIZATION", "ОФОРМЛЕНИЕ МЕНЮ"));

            bool prevRgb = rgbMenuMode;
            rgbMenuMode = DrawToggle(rgbMenuMode, "RGB Menu Mode", 260);
            if (prevRgb && !rgbMenuMode) UpdateAccentColor(menuColors[currentMenuColorIndex]);
            if (prevRgb != rgbMenuMode) menuPrefsChanged = true;
            GUILayout.Label(L("Smoothly cycles the accent through the rainbow.", "Плавно переливает акцент по радуге."), menuDescStyle);
            GUILayout.Space(8);

            bool prevRgbText = rgbMenuText;
            rgbMenuText = DrawToggle(rgbMenuText, "RGB Text", 260);
            if (prevRgbText != rgbMenuText)
            {
                InitStyles();
                UpdateAccentColor(currentAccentColor);
                menuPrefsChanged = true;
            }
            GUILayout.Label("When off, RGB Menu Mode does not recolor menu text.", menuDescStyle);
            GUILayout.Space(8);

            bool prevWhiteTheme = whiteMenuTheme;
            whiteMenuTheme = DrawToggle(whiteMenuTheme, "White Theme", 260);
            if (prevWhiteTheme != whiteMenuTheme)
            {
                InitStyles();
                UpdateAccentColor(currentAccentColor);
                menuPrefsChanged = true;
            }
            GUILayout.Label(L("Switches between the dark and light interface.", "Переключает тёмный и светлый интерфейс."), menuDescStyle);
            GUILayout.Space(8);

            bool prevBg = enableBackground;
            enableBackground = DrawToggle(enableBackground, "Enable Image Background", 260);
            if (enableBackground && !prevBg) LoadBackgroundImage();
            if (prevBg != enableBackground) menuPrefsChanged = true;
            GUILayout.Label(L("Put 'MenuBG.png' or .jpg in BepInEx/config to add a background image.", "Положите 'MenuBG.png' или .jpg в BepInEx/config для фона."), menuDescStyle);
            GUILayout.Space(8);

            bool prevHardMenu = hardMenu;
            hardMenu = DrawToggle(hardMenu, L("Solid Menu (block game clicks)", "Твердое меню (блок кликов по игре)"), 260);
            if (prevHardMenu != hardMenu) menuPrefsChanged = true;
            GUILayout.Label(L("When on, clicks over the menu stay in the menu so you can't misclick the game behind it.", "Когда включено, клики по меню остаются в меню — вы не промахнётесь по игре за ним."), menuDescStyle);
            GUILayout.EndVertical();

            // ----- ACCENT & PERFORMANCE -----
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("ACCENT & PERFORMANCE", "АКЦЕНТ И ПРОИЗВОДИТЕЛЬНОСТЬ"));

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("FPS Limit", "Лимит FPS"), new GUIStyle(toggleLabelStyle), GUILayout.Height(25), GUILayout.Width(110));
            int newFpsLimit = Mathf.Clamp((int)GUILayout.HorizontalSlider(fpsLimit, 60f, 240f, sliderStyle, sliderThumbStyle, GUILayout.Width(180)), 60, 240);
            GUILayout.Space(10);
            GUILayout.Label(fpsLimit.ToString(), menuBadgeStyle, GUILayout.Width(52), GUILayout.Height(22));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            if (newFpsLimit != fpsLimit)
            {
                fpsLimit = newFpsLimit;
                ApplyFpsLimit();
                menuPrefsChanged = true;
            }

            GUILayout.Space(12);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Accent Color", "Цвет акцента"), new GUIStyle(toggleLabelStyle), GUILayout.Height(25), GUILayout.Width(110));
            Color prevGuiColor = GUI.color;
            GUI.color = GetMenuControlAccentColor();
            GUILayout.Label(GUIContent.none, menuSwatchStyle, GUILayout.Width(22), GUILayout.Height(22));
            GUI.color = prevGuiColor;
            GUILayout.Space(8);
            GUI.enabled = !rgbMenuMode;
            GUIStyle middleColorStyle = new GUIStyle(btnStyle) { normal = { background = null, textColor = GetMenuControlAccentColor() }, fontStyle = FontStyle.Bold };
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) { currentMenuColorIndex--; if (currentMenuColorIndex < 0) currentMenuColorIndex = menuColors.Length - 1; if (!rgbMenuMode) UpdateAccentColor(menuColors[currentMenuColorIndex]); menuPrefsChanged = true; }
            GUILayout.Label(rgbMenuMode ? "RGB" : menuColorNames[currentMenuColorIndex], middleColorStyle, GUILayout.Width(120), GUILayout.Height(25));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) { currentMenuColorIndex++; if (currentMenuColorIndex >= menuColors.Length) currentMenuColorIndex = 0; if (!rgbMenuMode) UpdateAccentColor(menuColors[currentMenuColorIndex]); menuPrefsChanged = true; }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            // ----- SPOOF IDENTITY -----
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("SPOOF MENU IDENTITY", "ПОДМЕНА МЕНЮ"));
            bool prevSpoofMenuEnabled = SpoofMenuEnabled;
            SpoofMenuEnabled = DrawToggle(SpoofMenuEnabled, "Enable Fake RPC", 260);
            if (prevSpoofMenuEnabled != SpoofMenuEnabled) menuPrefsChanged = true;
            GUILayout.Label(L("Reports a fake mod menu name to other players.", "Показывает игрокам поддельное имя меню."), menuDescStyle);
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Fake Name", "Поддельное имя"), new GUIStyle(toggleLabelStyle), GUILayout.Height(25), GUILayout.Width(110));
            GUI.enabled = SpoofMenuEnabled;
            GUIStyle middleLabelStyle = new GUIStyle(btnStyle) { fontStyle = FontStyle.Bold, normal = { background = null, textColor = GetMenuAccentColor() } };
            if (GUILayout.Button("<", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) { selectedSpoofMenuIndex--; if (selectedSpoofMenuIndex < 0) selectedSpoofMenuIndex = spoofMenuNames.Length - 1; menuPrefsChanged = true; }
            GUILayout.Label(spoofMenuNames[selectedSpoofMenuIndex], middleLabelStyle, GUILayout.Width(150), GUILayout.Height(25));
            if (GUILayout.Button(">", btnStyle, GUILayout.Width(30), GUILayout.Height(25))) { selectedSpoofMenuIndex++; if (selectedSpoofMenuIndex >= spoofMenuNames.Length) selectedSpoofMenuIndex = 0; menuPrefsChanged = true; }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            // ----- NOTIFICATIONS & LOGGING -----
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("NOTIFICATIONS & LOGGING", "УВЕДОМЛЕНИЯ И ЛОГИ"));
            bool prevCustomNotifs = EnableCustomNotifs;
            EnableCustomNotifs = DrawToggle(EnableCustomNotifs, "Enable Custom UI Notifications", 280);
            if (prevCustomNotifs != EnableCustomNotifs) menuPrefsChanged = true;
            GUILayout.Space(6);
            bool prevLogAllRpcs = LogAllRPCs;
            LogAllRPCs = DrawToggle(LogAllRPCs, "Sniff All RPCs (On-Screen)", 280);
            if (prevLogAllRpcs != LogAllRPCs) menuPrefsChanged = true;
            GUILayout.EndVertical();

            if (menuPrefsChanged) SaveConfig();
        }

        private Vector2 outfitsScrollPos = Vector2.zero;
        public static bool AutoHostEnabled = false;
        public static bool AutoReturnLobbyAfterMatch = true;
        public static bool AutoHostNotifications = true;
        public static bool AutoHostForceLastMinute = true;
        public static bool AutoHostWaitLoadedPlayers = true;
        public static bool AutoHostCancelBelowMin = true;
        public static bool AutoHostInstantStart = false;

        public static int AutoHostMinPlayers = 4;
        public static int AutoHostForceMinPlayers = 2;
        public static float AutoHostStartDelaySeconds = 15f;
        public static float AutoHostBackoffSeconds = 8f;
        public static float AutoHostWarmupSeconds = 5f;
        public static float AutoHostLoadGraceSeconds = 20f;

        public static int AutoHostForceAfterMinutes = 0;
        public static int AutoHostFastStartPlayers = 13;
        public static float AutoHostFastStartDelaySeconds = 5f;

        private int currentAutoHostSubTab = 0;
        private string[] autoHostSubTabs = { "LOBBY CONTROLS", "ROLE MANAGER", "ANTI CHEAT", "AUTO HOST" };

        private struct FavoriteOutfitSnapshot
        {
            public int ColorId;
            public string HatId;
            public string SkinId;
            public string VisorId;
            public string NamePlateId;
            public string PetId;

            public FavoriteOutfitSnapshot(int colorId, string hatId, string skinId, string visorId, string namePlateId, string petId)
            {
                ColorId = colorId;
                HatId = hatId ?? string.Empty;
                SkinId = skinId ?? string.Empty;
                VisorId = visorId ?? string.Empty;
                NamePlateId = namePlateId ?? string.Empty;
                PetId = petId ?? string.Empty;
            }
        }

        private void DrawOutfitsTab()
        {
            GUILayout.BeginVertical(menuCardStyle);
            DrawMenuSectionHeader(L("FAVORITE OUTFITS", "ИЗБРАННЫЕ ОБРАЗЫ"));

            PlayerControl selected = SelectedOutfitSourcePlayer();
            for (int i = 0; i < FavoriteOutfitSlotCount; i++)
            {
                bool hasOutfit = TryDeserializeFavoriteOutfit(favoriteOutfitSlots[i], out FavoriteOutfitSnapshot outfit);
                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{L("Slot", "Слот")} {i + 1}", toggleLabelStyle, GUILayout.Width(52), GUILayout.Height(22));
                GUILayout.Label(hasOutfit ? FavoriteOutfitSummary(outfit) : L("Empty", "Пусто"), new GUIStyle(GUI.skin.label) { fontSize = 11, clipping = TextClipping.Clip, alignment = TextAnchor.MiddleLeft }, GUILayout.ExpandWidth(true), GUILayout.Height(22));
                GUI.enabled = hasOutfit;
                if (GUILayout.Button(L("Apply", "Надеть"), btnStyle, GUILayout.Width(58), GUILayout.Height(22)))
                    ApplyFavoriteOutfitSlot(i, outfit, hasOutfit);
                GUI.enabled = true;
                if (GUILayout.Button("X", btnStyle, GUILayout.Width(28), GUILayout.Height(22)))
                    ClearFavoriteOutfitSlot(i);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(52);
                if (GUILayout.Button(L("Save Mine", "Сохр. мой"), btnStyle, GUILayout.Width(100), GUILayout.Height(22)))
                    SaveFavoriteOutfitSlot(i, PlayerControl.LocalPlayer);
                if (GUILayout.Button(L("Save Selected", "Сохр. выбран"), btnStyle, GUILayout.Width(120), GUILayout.Height(22)))
                    SaveFavoriteOutfitSlot(i, selected);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.Space(4);
            }

            GUILayout.Space(12);
            DrawMenuSectionHeader("COPY SPECIFIC PLAYER");

            outfitsScrollPos = GUILayout.BeginScrollView(outfitsScrollPos);
            if (lockedPlayersList.Count > 0)
            {
                foreach (var pc in lockedPlayersList)
                {
                    if (pc == null || pc == PlayerControl.LocalPlayer || pc.Data == null) continue;

                    GUILayout.BeginHorizontal(boxStyle);
                    try
                    {
                        string pName = pc.Data.PlayerName ?? "Unknown";
                        GUILayout.Label(pName, GUILayout.Width(150));

                        if (GUILayout.Button("Copy Outfit", btnStyle, GUILayout.Height(25)))
                        {
                            try
                            {
                                PlayerControl.LocalPlayer.RpcSetSkin(pc.Data.DefaultOutfit.SkinId);
                                PlayerControl.LocalPlayer.RpcSetHat(pc.Data.DefaultOutfit.HatId);
                                PlayerControl.LocalPlayer.RpcSetVisor(pc.Data.DefaultOutfit.VisorId);
                                PlayerControl.LocalPlayer.RpcSetNamePlate(pc.Data.DefaultOutfit.NamePlateId);
                                PlayerControl.LocalPlayer.RpcSetPet(pc.Data.DefaultOutfit.PetId);
                            }
                            catch { }
                        }
                    }
                    finally { GUILayout.EndHorizontal(); }
                    GUILayout.Space(2);
                }
            }
            else
            {
                GUILayout.Label("<color=#777777>Нет игроков для копирования.</color>");
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private static PlayerControl SelectedOutfitSourcePlayer()
        {
            try
            {
                if (lockedPlayersList != null)
                {
                    foreach (PlayerControl pc in lockedPlayersList)
                    {
                        if (pc != null && pc != PlayerControl.LocalPlayer && pc.Data != null && !pc.Data.Disconnected)
                            return pc;
                    }
                }
            }
            catch { }

            return PlayerControl.LocalPlayer;
        }

        private static int MaxOutfitColorId()
        {
            try { return Palette.PlayerColors != null ? Mathf.Max(0, Palette.PlayerColors.Length - 1) : 18; }
            catch { return 18; }
        }

        private static bool TryCaptureFavoriteOutfit(PlayerControl source, out FavoriteOutfitSnapshot outfit)
        {
            outfit = default;
            try
            {
                if (source == null || source.Data == null || source.Data.DefaultOutfit == null) return false;
                var sourceOutfit = source.Data.DefaultOutfit;
                outfit = new FavoriteOutfitSnapshot(
                    Mathf.Clamp(sourceOutfit.ColorId, 0, MaxOutfitColorId()),
                    sourceOutfit.HatId,
                    sourceOutfit.SkinId,
                    sourceOutfit.VisorId,
                    sourceOutfit.NamePlateId,
                    sourceOutfit.PetId);
                return true;
            }
            catch { }

            return false;
        }

        private static void ApplyFavoriteOutfit(PlayerControl target, FavoriteOutfitSnapshot outfit)
        {
            if (target == null) return;
            target.RpcSetColor((byte)Mathf.Clamp(outfit.ColorId, 0, MaxOutfitColorId()));
            target.RpcSetSkin(outfit.SkinId ?? string.Empty);
            target.RpcSetHat(outfit.HatId ?? string.Empty);
            target.RpcSetVisor(outfit.VisorId ?? string.Empty);
            target.RpcSetNamePlate(outfit.NamePlateId ?? string.Empty);
            target.RpcSetPet(outfit.PetId ?? string.Empty);
        }

        private static string SerializeFavoriteOutfit(FavoriteOutfitSnapshot outfit)
        {
            return string.Join("\t", new[]
            {
                Mathf.Clamp(outfit.ColorId, 0, MaxOutfitColorId()).ToString(),
                CleanFavoriteOutfitPart(outfit.HatId),
                CleanFavoriteOutfitPart(outfit.SkinId),
                CleanFavoriteOutfitPart(outfit.VisorId),
                CleanFavoriteOutfitPart(outfit.NamePlateId),
                CleanFavoriteOutfitPart(outfit.PetId)
            });
        }

        private static bool TryDeserializeFavoriteOutfit(string value, out FavoriteOutfitSnapshot outfit)
        {
            outfit = default;
            if (string.IsNullOrWhiteSpace(value)) return false;

            string[] parts = value.Split('\t');
            if (parts.Length < 6 || !int.TryParse(parts[0], out int colorId)) return false;

            outfit = new FavoriteOutfitSnapshot(Mathf.Clamp(colorId, 0, MaxOutfitColorId()), parts[1], parts[2], parts[3], parts[4], parts[5]);
            return true;
        }

        private static string CleanFavoriteOutfitPart(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Replace("\t", " ").Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static string FavoriteOutfitSummary(FavoriteOutfitSnapshot outfit)
        {
            string color = "Color " + outfit.ColorId;
            try { color = Palette.GetColorName(outfit.ColorId); } catch { }
            return $"{color} | {ShortOutfitId(outfit.HatId)}";
        }

        private static string ShortOutfitId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "-";
            string cleaned = value.Trim();
            return cleaned.Length <= 10 ? cleaned : cleaned.Substring(0, 10);
        }

        private void SaveFavoriteOutfitSlot(int index, PlayerControl source)
        {
            if (index < 0 || index >= favoriteOutfitSlots.Length) return;
            if (!TryCaptureFavoriteOutfit(source, out FavoriteOutfitSnapshot outfit))
            {
                ShowNotification($"<color=#FF4444>[OUTFIT]</color> {L("Player outfit is not ready.", "Образ игрока еще не готов.")}");
                return;
            }

            favoriteOutfitSlots[index] = SerializeFavoriteOutfit(outfit);
            SaveConfig();
            ShowNotification($"<color=#00FFAA>[OUTFIT]</color> {L("Saved slot", "Сохранен слот")} {index + 1}");
        }

        private void ApplyFavoriteOutfitSlot(int index, FavoriteOutfitSnapshot outfit, bool hasOutfit)
        {
            if (!hasOutfit)
            {
                ShowNotification($"<color=#FFAA00>[OUTFIT]</color> {L("Slot is empty.", "Слот пуст.")}");
                return;
            }

            try
            {
                ApplyFavoriteOutfit(PlayerControl.LocalPlayer, outfit);
                ShowNotification($"<color=#00FFAA>[OUTFIT]</color> {L("Applied slot", "Надет слот")} {index + 1}");
            }
            catch { }
        }

        private void ClearFavoriteOutfitSlot(int index)
        {
            if (index < 0 || index >= favoriteOutfitSlots.Length) return;
            favoriteOutfitSlots[index] = string.Empty;
            SaveConfig();
            ShowNotification($"<color=#AAAAAA>[OUTFIT]</color> {L("Cleared slot", "Очищен слот")} {index + 1}");
        }
        public static bool removePenalty = true;
        public static bool alwaysShowLobbyTimer = false;
        public static bool enableChatLog = true;
        public static bool enableFastChat = true;
        public static bool allowLinksAndSymbols = false;

        private static readonly System.Collections.Generic.Dictionary<string, Sprite> CachedSprites = new();

        public static Sprite LoadEmbeddedSprite(string fileName, float pixelsPerUnit = 1f)
        {
            string path = $"ElysiumModMenu.{fileName}";

            try
            {
                if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var cachedSprite))
                    return cachedSprite;

                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(path);

                if (stream == null)
                {
                    return null;
                }

                var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                using System.IO.MemoryStream ms = new System.IO.MemoryStream();
                stream.CopyTo(ms);

                ImageConversion.LoadImage(texture, ms.ToArray(), false);

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);

                sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;

                return CachedSprites[path + pixelsPerUnit] = sprite;
            }
            catch (System.Exception)
            {
                return null;
            }
        }
        public void Start()
        {
            activeGui = this;
            if (enableBackground) LoadBackgroundImage();
            UnlockCosmetics();
            LoadConfig();
            LoadBanList();
            LoadBotBanList();
            ClearSpamErrorLogOnStartup();
            StartBackgroundAnomalyLogMonitor();


            try
            {
                int starts = UnityEngine.PlayerPrefs.GetInt("Elysium_GameStarts", 0);
                starts++;

                string chatLogPath = System.IO.Path.Combine(Plugin.ElysiumFolder, "ChatLog.txt");

                if (starts >= 3)
                {
                    if (System.IO.File.Exists(chatLogPath))
                    {
                        System.IO.File.WriteAllText(chatLogPath, string.Empty);
                    }
                    starts = 0;
                }

                UnityEngine.PlayerPrefs.SetInt("Elysium_GameStarts", starts);
                UnityEngine.PlayerPrefs.Save();
            }
            catch { }
        }

        public void OnApplicationQuit()
        {
            StopBackgroundAnomalyLogMonitor();
            SaveConfig();
        }

        public void OnDisable()
        {
            SaveConfig();
        }

        private static void ClearSpamErrorLogOnStartup()
        {
            try
            {
                watchedLogLineCounts.Clear();
                logBurstWindowStartedAt = -1f;
                logBurstCooldownUntil = 0f;
                logBurstLineCount = 0;
                anomalyLogWatchNotified = false;
                logMonitorNextScanAt = 0f;

                string root = string.IsNullOrWhiteSpace(Plugin.ElysiumFolder)
                    ? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "ElysiumModMenu")
                    : Plugin.ElysiumFolder;

                if (!System.IO.Directory.Exists(root)) return;

                foreach (string file in System.IO.Directory.GetFiles(root, "SpamErrorLog*.txt", System.IO.SearchOption.AllDirectories))
                {
                    try { System.IO.File.Delete(file); }
                    catch { }
                }

                System.Console.WriteLine("[ElysiumModMenu] Cleared previous SpamErrorLog files and reset log monitor state.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ElysiumModMenu] Failed to clear SpamErrorLog files: {ex.GetType().Name}: {ex.Message}");
            }
        }



        private static void StartBackgroundAnomalyLogMonitor()
        {
            try
            {
                if (anomalyLogMonitorTimer != null) return;
                anomalyLogMonitorTimer = new System.Threading.Timer(_ => TryDetectLogBurstTick(false), null, 1000, 1000);
                System.Console.WriteLine("[ElysiumModMenu] Background freeze/overload log monitor started.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ElysiumModMenu] Failed to start background log monitor: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static void StopBackgroundAnomalyLogMonitor()
        {
            try
            {
                anomalyLogMonitorTimer?.Dispose();
                anomalyLogMonitorTimer = null;
            }
            catch { }
        }

        private static float GetLogMonitorSeconds()
        {
            try { return (float)(DateTime.UtcNow - logMonitorStartedUtc).TotalSeconds; }
            catch { return 0f; }
        }

        private static string BuildAnomalyReportDetails(bool allowUnityAccess = true)
        {
            if (!allowUnityAccess)
                return anomalyReportDetailsCache + "\nmonitor=background";

            string clientId = "Unknown";
            string networkMode = "Unknown";
            string inGame = "no";
            string host = "no";
            string platform = "Unknown";

            try
            {
                if (AmongUsClient.Instance != null)
                {
                    clientId = AmongUsClient.Instance.ClientId.ToString();
                    networkMode = AmongUsClient.Instance.NetworkMode.ToString();
                    host = AmongUsClient.Instance.AmHost ? "yes" : "no";

                    ClientData client = AmongUsClient.Instance.GetClientFromCharacter(PlayerControl.LocalPlayer);
                    if (client != null)
                    {
                        platform = GetPlatform(client);
                    }
                }
            }
            catch { }

            try { inGame = ShipStatus.Instance != null && LobbyBehaviour.Instance == null ? "yes" : "no"; } catch { }

            string details = $"sessionId={relaySessionId}\nclientId={clientId}\nnetworkMode={networkMode}\nhost={host}\nplatform={platform}\ninGame={inGame}\nmonitor=unity";
            anomalyReportDetailsCache = details;
            return details;
        }

        private static void TryDetectLogBurstTick(bool allowUnityAccess = true)
        {
            if (!enableAnomalyLogReports) return;
            lock (anomalyLogMonitorLock)
            {
                try
                {
                    float now = GetLogMonitorSeconds();
                    if (now < logMonitorNextScanAt) return;
                    logMonitorNextScanAt = now + LogBurstScanIntervalSeconds;

                    List<string> watchedFiles = GetWatchedLogFiles().ToList();
                    if (!anomalyLogWatchNotified)
                    {
                        anomalyLogWatchNotified = true;
                        System.Console.WriteLine($"[ElysiumModMenu] Freeze/overload log reporting is enabled. Watching: {string.Join(", ", watchedFiles.Select(System.IO.Path.GetFileName).ToArray())}. Sends only summary counters when error/red logs appear or {LogBurstLineThreshold}+ new lines arrive within {LogBurstWindowSeconds:0}s.");
                    }

                    int newLines = 0;
                    int errorLines = 0;
                    int storedMsgLines = 0;
                    List<string> touchedLogs = new List<string>();
                    List<string> touchedLogFiles = new List<string>();
                    foreach (string file in watchedFiles)
                    {
                        List<string> addedLines = ReadNewLogLines(file, out int currentLines);
                        if (!watchedLogLineCounts.TryGetValue(file, out int previousLines))
                        {
                            watchedLogLineCounts[file] = currentLines;
                            addedLines = ReadInitialRecentLogTail(file);
                            if (addedLines.Count <= 0) continue;
                        }
                        else
                        {
                            watchedLogLineCounts[file] = currentLines;
                        }

                        if (addedLines.Count <= 0) continue;

                        newLines += addedLines.Count;
                        touchedLogs.Add(System.IO.Path.GetFileName(file));
                        touchedLogFiles.Add(file);
                        errorLines += addedLines.Count(IsErrorLogLine);
                        storedMsgLines += addedLines.Count(IsStoredMessageOverloadLine);
                    }

                    if (newLines <= 0) return;

                    if (logBurstWindowStartedAt < 0f || now - logBurstWindowStartedAt > LogBurstWindowSeconds)
                    {
                        logBurstWindowStartedAt = now;
                        logBurstLineCount = 0;
                    }

                    logBurstLineCount += newLines;
                    bool isStoredRpcBurst = storedMsgLines > 0;
                    bool isErrorBurst = errorLines > 0;
                    bool isLineBurst = logBurstLineCount >= LogBurstLineThreshold;
                    if ((!isErrorBurst && !isLineBurst) || (!isStoredRpcBurst && now < logBurstCooldownUntil)) return;

                    logBurstCooldownUntil = now + (isStoredRpcBurst ? 5f : LogBurstAlertCooldownSeconds);
                    string reason = isStoredRpcBurst ? "stored rpc overload detected" : (isErrorBurst ? "error/red log detected" : "log spam detected");
                    string message = $"{BuildAnomalyReportDetails(allowUnityAccess)}\nnewLogLines={logBurstLineCount}\nerrorLines={errorLines}\nstoredMsgLines={storedMsgLines}\nwindowSeconds={LogBurstWindowSeconds}\nthreshold={LogBurstLineThreshold}\nreason={reason}, needs fix\nwatchedLogs={string.Join(", ", touchedLogs.Distinct().ToArray())}";
                    logBurstWindowStartedAt = -1f;
                    logBurstLineCount = 0;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[ElysiumModMenu] Log monitor failed: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        private static IEnumerable<string> GetWatchedLogFiles()
        {
            string root = GetAmongUsRoot();
            List<string> files = new List<string>();

            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string unityLogRoot = System.IO.Path.Combine(userProfile, "AppData", "LocalLow", "Innersloth", "Among Us");
                AddLogPath(files, System.IO.Path.Combine(unityLogRoot, "Player.log"));
                AddLogPath(files, System.IO.Path.Combine(unityLogRoot, "Player-prev.log"));
            }
            catch { }

            AddLogPath(files, System.IO.Path.Combine(root, "BepInEx", "ErrorLog.log"));
            AddLogPath(files, System.IO.Path.Combine(root, "ErrorLog.log"));
            AddLogPath(files, System.IO.Path.Combine(root, "BepInEx", "LogOutput.log"));
            AddLogPath(files, System.IO.Path.Combine(root, "LogOutput.log"));

            try
            {
                string[] banLogDirs =
                {
                    System.IO.Path.Combine(root, "BepInEx", "BAN_DATA", "LOG"),
                    System.IO.Path.Combine(root, "BAN_DATA", "LOG")
                };

                foreach (string banLogDir in banLogDirs)
                {
                    if (!System.IO.Directory.Exists(banLogDir)) continue;
                    foreach (string file in System.IO.Directory.GetFiles(banLogDir))
                        AddLogPath(files, file);
                }
            }
            catch { }

            try
            {
                string playerLogRoot = System.IO.Path.Combine(root, "ElysiumModMenu", "PlayerLogs");
                if (System.IO.Directory.Exists(playerLogRoot))
                {
                    foreach (string dir in System.IO.Directory.GetDirectories(playerLogRoot))
                    {
                        AddLogPath(files, System.IO.Path.Combine(dir, "LogOutput.txt"));
                        AddLogPath(files, System.IO.Path.Combine(dir, "LogOutput.log"));
                    }
                }
            }
            catch { }

            return files;
        }

        private static void AddLogPath(List<string> files, string file)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(file) && System.IO.File.Exists(file) && !files.Contains(file))
                    files.Add(file);
            }
            catch { }
        }

        private static List<string> ReadNewLogLines(string file, out int currentLines)
        {
            currentLines = 0;
            List<string> lines = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(file) || !System.IO.File.Exists(file)) return lines;

                watchedLogLineCounts.TryGetValue(file, out int previousLines);
                using (System.IO.FileStream stream = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete))
                using (System.IO.StreamReader reader = new System.IO.StreamReader(stream, Encoding.UTF8, true))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        currentLines++;
                        if (currentLines > previousLines)
                            lines.Add(line);
                    }
                }
            }
            catch
            {
            }

            return lines;
        }

        private static List<string> ReadInitialRecentLogTail(string file)
        {
            List<string> result = new List<string>();
            try
            {
                if (string.IsNullOrWhiteSpace(file) || !System.IO.File.Exists(file)) return result;

                Queue<string> errorTail = new Queue<string>();
                using (System.IO.FileStream stream = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete))
                using (System.IO.StreamReader reader = new System.IO.StreamReader(stream, Encoding.UTF8, true))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!IsErrorLogLine(line)) continue;
                        errorTail.Enqueue(line);
                        while (errorTail.Count > InitialLogTailLineLimit)
                            errorTail.Dequeue();
                    }
                }

                result.AddRange(errorTail);
            }
            catch { }

            return result;
        }

        private static bool IsErrorLogLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            string lower = line.ToLowerInvariant();
            if (lower.Contains("[elysiummodmenu]")) return false;
            if (lower.Contains("method ") && lower.Contains(" has unsupported ") && lower.Contains("elysiummodmenu")) return false;
            if (lower.Contains("registered mono type") && lower.Contains("elysiummodmenu")) return false;
            return lower.Contains("[error") ||
                   lower.Contains("[fatal") ||
                   lower.Contains("exception") ||
                   lower.Contains(" stack trace") ||
                   lower.Contains("traceback") ||
                   lower.Contains("stored data") ||
                   lower.Contains("storeddata") ||
                   IsStoredMessageOverloadLine(lower) ||
                   IsKnownSpamWarningLine(lower) ||
                   lower.Contains("overload") ||
                   lower.Contains("freeze") ||
                   lower.Contains("color=red") ||
                   lower.Contains("#ff0000");
        }

        public static bool IsRelevantAnomalyLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            string lower = line.ToLowerInvariant();
            if (lower.Contains("[elysiummodmenu]")) return false;
            if (lower.Contains("bepinex") && lower.Contains("chainloader")) return false;
            if (lower.Contains("registered mono type") && lower.Contains("elysiummodmenu")) return false;
            if (lower.Contains("method ") && lower.Contains(" has unsupported ") && lower.Contains("elysiummodmenu")) return false;
            return IsStoredMessageOverloadLine(lower) ||
                   IsKnownSpamWarningLine(lower) ||
                   lower.Contains("[error") ||
                   lower.Contains("[fatal") ||
                   lower.Contains("nullreferenceexception") ||
                   lower.Contains("invaliddataexception") ||
                   lower.Contains("exception:") ||
                   lower.Contains(" stack trace") ||
                   lower.Contains("traceback") ||
                   lower.Contains("stored data") ||
                   lower.Contains("storeddata");
        }

        private static bool IsKnownSpamWarningLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            string lower = line.ToLowerInvariant();
            return lower.Contains("sendmode set to everything") ||
                   lower.Contains("likely should be reliable") ||
                   lower.Contains("stored msg");
        }

        private static bool IsStoredMessageOverloadLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return false;
            string lower = line.ToLowerInvariant();
            return lower.Contains("stored msg") && lower.Contains(" rpc ");
        }

        private static string GetAmongUsRoot()
        {
            try { return System.IO.Directory.GetCurrentDirectory(); }
            catch { return string.Empty; }
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            StringBuilder builder = new StringBuilder(value.Length + 16);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < 32) builder.Append("\\u").Append(((int)c).ToString("x4"));
                        else builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

      

        public static KeyCode bindMagnetCursor = KeyCode.F9;
        public static bool isWaitBindMagnetCursor = false;

        private bool CanRunHostBind(string actionName)
        {
            try
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) return true;
            }
            catch { }

            ShowNotification($"<color=#FF0000>[BIND]</color> {actionName}: host only");
            return false;
        }

        public void Update()
        {
            bool isTypingOrBinding = isEditingName || isEditingLevel || isEditingFriendCode || isEditingLocalFriendCode || isEditingGhostChatColor || isEditingBan || customChatInputFocused ||
                                     isWaitingForBind || isWaitBindMassMorph || isWaitBindSpawnLobby ||
                                     isWaitBindDespawnLobby || isWaitBindCloseMeeting || isWaitBindInstaStart ||
                                     isWaitBindEndCrew || isWaitBindEndImp || isWaitBindEndImpDC || isWaitBindEndHnsDC ||
                                     isWaitBindMagnetCursor || isWaitBindToggleTracers || isWaitBindToggleNoClip ||
                                     isWaitBindToggleFreecam || isWaitBindToggleCameraZoom || isWaitBindKillAll ||
                                     isWaitBindCallMeeting || isWaitBindTogglePlayerInfo || isWaitBindToggleSeeRoles ||
                                     isWaitBindToggleSeeGhosts || isWaitBindToggleFullBright || isWaitBindKickAll ||
                                     isWaitBindFixSabotages || isWaitBindSetAllGhost || isWaitBindSetAllGhostImp;

            KeyCode activeMenuKey = menuToggleKey == KeyCode.None ? KeyCode.Insert : menuToggleKey;
            if (!isTypingOrBinding && Input.GetKeyDown(activeMenuKey))
            {
                showMenu = !showMenu;
                if (!showMenu) SaveConfig();
            }

            if (!isTypingOrBinding)
            {
                if (bindMassMorph != KeyCode.None && Input.GetKeyDown(bindMassMorph))
                {
                    if (CanRunHostBind("Mass Morph"))
                        this.StartCoroutine(MassMorphCoroutine().WrapToIl2Cpp());
                }

                if (bindSpawnLobby != KeyCode.None && Input.GetKeyDown(bindSpawnLobby))
                {
                    if (CanRunHostBind("Spawn Lobby")) SpawnLobby();
                }

                if (bindDespawnLobby != KeyCode.None && Input.GetKeyDown(bindDespawnLobby))
                {
                    if (CanRunHostBind("Despawn Lobby")) DespawnLobby();
                }

                if (bindCloseMeeting != KeyCode.None && Input.GetKeyDown(bindCloseMeeting))
                {
                    if (CanRunHostBind("Close Meeting") && MeetingHud.Instance != null)
                        MeetingHud.Instance.RpcClose();
                }

                if (bindInstaStart != KeyCode.None && Input.GetKeyDown(bindInstaStart) && CanRunHostBind("Insta Start") && GameStartManager.Instance != null)
                {
                    GameStartManager.Instance.startState = GameStartManager.StartingStates.Countdown;
                    GameStartManager.Instance.countDownTimer = 0f;
                }
                if (bindMagnetCursor != KeyCode.None && Input.GetKeyDown(bindMagnetCursor))
                {
                    autoFollowCursor = !autoFollowCursor;
                    ShowNotification(autoFollowCursor ?
                        "<color=#00FF00>[MAGNET]</color> Magnet Cursor: ON" :
                        "<color=#FF0000>[MAGNET]</color> Magnet Cursor: OFF");
                }
                if (bindEndCrew != KeyCode.None && Input.GetKeyDown(bindEndCrew) && CanRunHostBind("End: Crewmate Win")) SmartEndGame("CrewWin");
                if (bindEndImp != KeyCode.None && Input.GetKeyDown(bindEndImp) && CanRunHostBind("End: Impostor Win")) SmartEndGame("ImpWin");
                if (bindEndImpDC != KeyCode.None && Input.GetKeyDown(bindEndImpDC) && CanRunHostBind("End: Imp Disconnect")) SmartEndGame("ImpDisconnect");
                if (bindEndHnsDC != KeyCode.None && Input.GetKeyDown(bindEndHnsDC) && CanRunHostBind("End: H&S Disconnect")) SmartEndGame("HnsImpDisconnect");
                if (bindToggleTracers != KeyCode.None && Input.GetKeyDown(bindToggleTracers))
                {
                    showTracers = !showTracers;
                    ShowNotification(showTracers ? "<color=#00FF00>[TRACERS]</color> ON" : "<color=#FF0000>[TRACERS]</color> OFF");
                }
                if (bindToggleNoClip != KeyCode.None && Input.GetKeyDown(bindToggleNoClip))
                {
                    noClip = !noClip;
                    ShowNotification(noClip ? "<color=#00FF00>[NOCLIP]</color> ON" : "<color=#FF0000>[NOCLIP]</color> OFF");
                }
                if (bindToggleFreecam != KeyCode.None && Input.GetKeyDown(bindToggleFreecam))
                {
                    freecam = !freecam;
                    ShowNotification(freecam ? "<color=#00FF00>[FREECAM]</color> ON" : "<color=#FF0000>[FREECAM]</color> OFF");
                }
                if (bindToggleCameraZoom != KeyCode.None && Input.GetKeyDown(bindToggleCameraZoom))
                {
                    cameraZoom = !cameraZoom;
                    ShowNotification(cameraZoom ? "<color=#00FF00>[ZOOM]</color> ON" : "<color=#FF0000>[ZOOM]</color> OFF");
                }
                if (bindTogglePlayerInfo != KeyCode.None && Input.GetKeyDown(bindTogglePlayerInfo))
                {
                    showPlayerInfo = !showPlayerInfo;
                    ShowNotification(showPlayerInfo ? "<color=#00FF00>[PLAYER INFO]</color> ON" : "<color=#FF0000>[PLAYER INFO]</color> OFF");
                }
                if (bindToggleSeeRoles != KeyCode.None && Input.GetKeyDown(bindToggleSeeRoles))
                {
                    seeRoles = !seeRoles;
                    ShowNotification(seeRoles ? "<color=#00FF00>[ROLES]</color> ON" : "<color=#FF0000>[ROLES]</color> OFF");
                }
                if (bindToggleSeeGhosts != KeyCode.None && Input.GetKeyDown(bindToggleSeeGhosts))
                {
                    seeGhosts = !seeGhosts;
                    ShowNotification(seeGhosts ? "<color=#00FF00>[GHOSTS]</color> ON" : "<color=#FF0000>[GHOSTS]</color> OFF");
                }
                if (bindToggleFullBright != KeyCode.None && Input.GetKeyDown(bindToggleFullBright))
                {
                    fullBright = !fullBright;
                    ShowNotification(fullBright ? "<color=#00FF00>[FULL BRIGHT]</color> ON" : "<color=#FF0000>[FULL BRIGHT]</color> OFF");
                }
                if (bindKillAll != KeyCode.None && Input.GetKeyDown(bindKillAll) && CanRunHostBind("Kill All")) KillAll();
                if (bindCallMeeting != KeyCode.None && Input.GetKeyDown(bindCallMeeting) && CanRunHostBind("Call Meeting")) callMeetingPublic();
                if (bindKickAll != KeyCode.None && Input.GetKeyDown(bindKickAll) && CanRunHostBind("Kick All")) KickAll();
                if (bindFixSabotages != KeyCode.None && Input.GetKeyDown(bindFixSabotages) && CanRunHostBind("Fix Sabotages")) FixAllSabotages();
                if (bindSetAllGhost != KeyCode.None && Input.GetKeyDown(bindSetAllGhost) && CanRunHostBind("All -> Ghost")) SetAllPlayersGhost();
                if (bindSetAllGhostImp != KeyCode.None && Input.GetKeyDown(bindSetAllGhostImp) && CanRunHostBind("All -> Ghost Imp")) SetAllPlayersGhost(true);
            }

            ElysiumAutoHostService.Tick();
            ElysiumAutoLobbyReturn.UpdateLogic();
            ApplyFpsLimit();
            TryAutoGhostAfterStartTick();
            TryAutoBanCustomPlatformsTick();
            TryDetectLogBurstTick();
            if (votekickEveryone)
            {
                TickVotekickEveryoneRun();
            }
            if (stylesInited && rgbMenuMode)
            {
                rgbMenuHue += Time.deltaTime * 0.2f;
                if (rgbMenuHue > 1f) rgbMenuHue -= 1f;
                UpdateAccentColor(Color.HSVToRGB(rgbMenuHue, 1f, 1f));
            }

            if (wasShowMenu && !showMenu) SaveConfig();
            wasShowMenu = showMenu;

            if (settingsDirty)
            {
                SaveConfig();
                settingsDirty = false;
            }

            if (PlayerControl.LocalPlayer != null)
            {
                TryHostOnlyKillAuraTick();
                TryAutoBanBrokenFriendCodeTick();
                TryAutoKickLowLevelTick();

                if (enableLocalNameSpoof && !isEditingName)
                {
                    localNameRefreshTimer += Time.deltaTime;
                    if (localNameRefreshTimer >= 0.5f)
                    {
                        localNameRefreshTimer = 0f;
                        ApplyLocalNameSelf(customNameInput, false);
                    }
                }
                else
                {
                    localNameRefreshTimer = 0f;
                }

                if (enableLocalFriendCodeSpoof && !isEditingLocalFriendCode)
                {
                    localFriendCodeRefreshTimer += Time.deltaTime;
                    if (localFriendCodeRefreshTimer >= 0.5f)
                    {
                        localFriendCodeRefreshTimer = 0f;
                        ApplyLocalFriendCodeSelf(localFriendCodeInput, false);
                    }
                }
                else
                {
                    localFriendCodeRefreshTimer = 0f;
                }

                if ((tpToCursor && Input.GetMouseButtonDown(1)) ||
                    (dragToCursor && Input.GetMouseButton(2)) ||
                    autoFollowCursor)
                {
                    if (Camera.main != null)
                    {
                        Vector3 mPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        mPos.z = PlayerControl.LocalPlayer.transform.position.z;
                        PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(mPos);
                    }
                }
                try
                {
                    if (noTaskMode && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                    {
                        if (GameOptionsManager.Instance != null && GameOptionsManager.Instance.CurrentGameOptions != null)
                        {
                            var opts = GameOptionsManager.Instance.CurrentGameOptions;
                            opts.SetInt(Int32OptionNames.NumCommonTasks, 0);
                            opts.SetInt(Int32OptionNames.NumLongTasks, 0);
                            opts.SetInt(Int32OptionNames.NumShortTasks, 0);
                        }
                    }
                }
                catch { }
                if (autoChatEveryone && pendingAutoMeeting && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    try
                    {
                        if (PlayerControl.LocalPlayer != null && ShipStatus.Instance != null && !PlayerControl.LocalPlayer.Data.IsDead)
                        {
                            autoMeetingTimer += Time.deltaTime;

                            if (autoMeetingTimer >= autoChatEveryoneDelay)
                            {
                                if (MeetingHud.Instance == null)
                                {
                                    PlayerControl.LocalPlayer.CmdReportDeadBody(null);
                                }
                                else
                                {
                                    MeetingHud.Instance.RpcClose();
                                    pendingAutoMeeting = false;
                                    autoMeetingTimer = 0f;
                                    ShowNotification("<color=#00FF00>[CHAT EVERYONE]</color> Игроки собраны в кафетерии!");
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (customChatSpamEnabled)
                {
                    customChatSpamTimer += Time.deltaTime;
                    if (customChatSpamTimer >= customChatSpamDelay)
                    {
                        customChatSpamTimer = 0f;
                        TrySendCustomChatMessage(customChatMessage);
                    }
                }
                else
                {
                    customChatSpamTimer = 0f;
                }
                if (autoKickBugs && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && fortegreenTimer.Count > 0)
                {
                    foreach (var kvp in fortegreenTimer.ToList())
                    {
                        if (Time.time >= kvp.Value)
                        {
                            byte pid = kvp.Key;
                            var player = GameData.Instance.GetPlayerById(pid);

                            if (player != null && !player.Disconnected && player.Object != null)
                            {
                                int currentColor = (int)player.DefaultOutfit.ColorId;
                                if (currentColor == 18 || currentColor >= Palette.PlayerColors.Length)
                                {
                                    AmongUsClient.Instance.KickPlayer(player.ClientId, false);
                                    ShowNotification($"<color=#FF0000>[AUTO-KICK]</color> Игрок <b>{player.PlayerName}</b> кикнут (Баг цвета)!");
                                }
                            }
                            fortegreenTimer.Remove(pid);
                        }
                    }
                }
                if (PlayerControl.LocalPlayer != null)
                {
                    try
                    {
                        if (AnimAsteroidsEnabled)
                        {
                            PlayerControl.LocalPlayer.PlayAnimation((byte)TaskTypes.ClearAsteroids);
                            RpcPlayAnimationMessage rpcMessage = new(PlayerControl.LocalPlayer.NetId, (byte)TaskTypes.ClearAsteroids);
                            AmongUsClient.Instance.LateBroadcastUnreliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
                        }

                        if (AnimShieldsEnabled)
                        {
                            PlayerControl.LocalPlayer.PlayAnimation((byte)TaskTypes.PrimeShields);
                            RpcPlayAnimationMessage rpcMessage = new(PlayerControl.LocalPlayer.NetId, (byte)TaskTypes.PrimeShields);
                            AmongUsClient.Instance.LateBroadcastUnreliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
                        }

                        if (IsScanning && !isScannerActiveFlag)
                        {
                            var count = ++PlayerControl.LocalPlayer.scannerCount;
                            PlayerControl.LocalPlayer.SetScanner(true, count);
                            RpcSetScannerMessage rpcMessage = new(PlayerControl.LocalPlayer.NetId, true, count);
                            AmongUsClient.Instance.LateBroadcastReliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
                            isScannerActiveFlag = true;
                        }
                        else if (!IsScanning && isScannerActiveFlag)
                        {
                            var count = ++PlayerControl.LocalPlayer.scannerCount;
                            PlayerControl.LocalPlayer.SetScanner(false, count);
                            RpcSetScannerMessage rpcMessage = new(PlayerControl.LocalPlayer.NetId, false, count);
                            AmongUsClient.Instance.LateBroadcastReliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
                            isScannerActiveFlag = false;
                        }

                        if (ShipStatus.Instance != null)
                        {
                            if (AnimCamsInUseEnabled && !isCamsActiveFlag)
                            {
                                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Security, 1);
                                isCamsActiveFlag = true;
                            }
                            else if (!AnimCamsInUseEnabled && isCamsActiveFlag)
                            {
                                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Security, 0);
                                isCamsActiveFlag = false;
                            }
                        }
                    }
                    catch { }
                }
                try
                {
                    if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.MyPhysics != null && PlayerControl.LocalPlayer.Data != null)
                    {
                        if (PlayerControl.LocalPlayer.Collider != null)
                        {
                            PlayerControl.LocalPlayer.Collider.enabled = !(noClip || PlayerControl.LocalPlayer.onLadder);
                        }

                        float baseSpeed = 3f;
                        float targetSpeed = walkSpeed * baseSpeed;

                        if (PlayerControl.LocalPlayer.Data.IsDead)
                        {
                            PlayerControl.LocalPlayer.MyPhysics.GhostSpeed = targetSpeed;
                        }
                        else
                        {
                            PlayerControl.LocalPlayer.MyPhysics.Speed = targetSpeed;
                        }
                    }
                }
                catch { }

                if (SpoofMenuEnabled && PlayerControl.LocalPlayer != null)
                {
                    uiSpoofTimer += Time.deltaTime;
                    if (uiSpoofTimer >= rpcSpoofDelay)
                    {
                        uiSpoofTimer = 0f;
                        byte rpc = spoofMenuRPCs[selectedSpoofMenuIndex];
                        try
                        {
                            MessageWriter msg = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, rpc, SendOption.None, -1);
                            AmongUsClient.Instance.FinishRpcImmediately(msg);
                        }
                        catch { }
                    }
                }
                try
                {
                    if (autoBanEnabled && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.AllPlayerControls != null)
                    {
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc == null || pc.Data == null || pc.Data.Disconnected || pc == PlayerControl.LocalPlayer) continue;

                            string fc = pc.Data.FriendCode;
                            if (!string.IsNullOrEmpty(fc))
                            {
                                foreach (var entry in bannedEntries)
                                {
                                    string[] parts = entry.Split('|');
                                    if (parts.Length > 0 && parts[0].Trim().ToLower() == fc.Trim().ToLower())
                                    {
                                        AmongUsClient.Instance.KickPlayer(pc.OwnerId, true);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
                try
                {
                    if (banBotsEnabled && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.AllPlayerControls != null)
                    {
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc == null || pc.Data == null || pc.Data.Disconnected || pc == PlayerControl.LocalPlayer) continue;

                            string botName = pc.Data.PlayerName ?? "";
                            string botFc = pc.Data.FriendCode;

                            bool isBot = IsBotName(botName) || (!string.IsNullOrEmpty(botFc) && IsBotBannedFc(botFc));
                            if (!isBot) continue;

                            string banFc = string.IsNullOrEmpty(botFc) ? "Unknown" : botFc;
                            string botPuid = "Unknown";
                            try
                            {
                                var client = AmongUsClient.Instance.GetClientFromCharacter(pc);
                                if (client != null) botPuid = GetClientPuid(client);
                            }
                            catch { }

                            AddToBotBanList(banFc, botPuid, string.IsNullOrEmpty(botName) ? "Unknown" : botName, "Bot nickname");
                            AmongUsClient.Instance.KickPlayer(pc.OwnerId, true);
                            ShowNotification($"<color=#FF0000>[BAN BOTS]</color> {(string.IsNullOrEmpty(botName) ? "Unknown" : botName)} кикнут (бот).");
                        }
                    }
                }
                catch { }
                if (freecam)
                {
                    if (!_freecamActive && Camera.main != null)
                    {
                        var cam = Camera.main.gameObject.GetComponent<FollowerCamera>();
                        if (cam != null) { cam.enabled = false; cam.Target = null; }
                        _freecamActive = true;
                    }
                    if (PlayerControl.LocalPlayer != null) PlayerControl.LocalPlayer.moveable = false;
                    Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0.0f);
                    if (Camera.main != null) Camera.main.transform.position += movement * 15f * Time.deltaTime;
                }
                else if (_freecamActive)
                {
                    if (PlayerControl.LocalPlayer != null) PlayerControl.LocalPlayer.moveable = true;
                    if (Camera.main != null)
                    {
                        var cam = Camera.main.gameObject.GetComponent<FollowerCamera>();
                        if (cam != null && PlayerControl.LocalPlayer != null) { cam.enabled = true; cam.SetTarget(PlayerControl.LocalPlayer); }
                    }
                    _freecamActive = false;
                }

                try
                {
                    if (cameraZoom && Camera.main != null && Input.GetAxis("Mouse ScrollWheel") != 0f)
                    {
                        if (Input.GetAxis("Mouse ScrollWheel") < 0f) Camera.main.orthographicSize += 0.5f;
                        else if (Input.GetAxis("Mouse ScrollWheel") > 0f && Camera.main.orthographicSize > 3f) Camera.main.orthographicSize -= 0.5f;
                    }
                }
                catch { }

                try
                {
                    if (rainbowPlayers.Count > 0 && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.AllPlayerControls != null)
                    {
                        colorTimer += Time.deltaTime;
                        if (colorTimer > 0.15f)
                        {
                            colorTimer = 0f;
                            currentColorId++;
                            if (currentColorId > 17) currentColorId = 0;
                            foreach (var p in PlayerControl.AllPlayerControls)
                                if (p != null && p.Data != null && !p.Data.Disconnected && rainbowPlayers.Contains(p.PlayerId))
                                    p.RpcSetColor(currentColorId);
                        }
                    }
                }
                catch { }
                try
                {
                    if (PlayerControl.AllPlayerControls != null)
                    {
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc != null) HandleTracer(pc, showTracers);
                        }
                    }
                }
                catch { }



                if (!isEditingLevel && uint.TryParse(spoofLevelString, out uint parsedLvl))
                {
                    uint targetLevel = parsedLvl > 0 ? parsedLvl - 1 : 0;
                    try
                    {
                        if (AmongUs.Data.DataManager.Player.stats.level != targetLevel)
                        {
                            AmongUs.Data.DataManager.Player.stats.level = targetLevel;
                        }
                    }
                    catch
                    {
                        try
                        {
                            if (AmongUs.Data.DataManager.Player.Stats.Level != targetLevel)
                            {
                                AmongUs.Data.DataManager.Player.Stats.Level = targetLevel;
                            }
                        }
                        catch { }
                    }
                }
                try
                {
                    if (localRainbow || rainbowPlayers.Count > 0)
                    {
                        colorTimer += Time.deltaTime;
                        if (colorTimer > 0.15f)
                        {
                            colorTimer = 0f;
                            currentColorId++;
                            if (currentColorId > 17) currentColorId = 0;

                            if (localRainbow && PlayerControl.LocalPlayer != null)
                                PlayerControl.LocalPlayer.CmdCheckColor(currentColorId);

                            if (rainbowPlayers.Count > 0 && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost && PlayerControl.AllPlayerControls != null)
                            {
                                foreach (var p in PlayerControl.AllPlayerControls)
                                {
                                    if (p != null && p.Data != null && !p.Data.Disconnected && rainbowPlayers.Contains(p.PlayerId))
                                        p.RpcSetColor(currentColorId);
                                }
                            }
                        }
                    }
                }


                catch { }


            }
        }
        public static void ForceSetScanner(PlayerControl player, bool toggle)
        {
            var count = ++player.scannerCount;
            player.SetScanner(toggle, count);
            RpcSetScannerMessage rpcMessage = new(player.NetId, toggle, count);
            AmongUsClient.Instance.LateBroadcastReliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
        }
        public static void ForcePlayAnimation(byte animationType)
        {
            PlayerControl.LocalPlayer.PlayAnimation(animationType);
            RpcPlayAnimationMessage rpcMessage = new(PlayerControl.LocalPlayer.NetId, animationType);
            AmongUsClient.Instance.LateBroadcastUnreliableMessage(Unsafe.As<IGameDataMessage>(rpcMessage));
        }

        public void OnGUI()
        {
            Event e = Event.current;

            // silly timer....
            HandleMessage.HandleTimer();

            bool isTyping = isEditingName || isEditingLevel || isEditingFriendCode || isEditingLocalFriendCode || isEditingGhostChatColor || isEditingBan;
            bool isBinding = isWaitingForBind || isWaitBindMassMorph || isWaitBindSpawnLobby || isWaitBindDespawnLobby ||
                  isWaitBindCloseMeeting || isWaitBindInstaStart || isWaitBindEndCrew || isWaitBindEndImp ||
                  isWaitBindEndImpDC || isWaitBindEndHnsDC || isWaitBindMagnetCursor || isWaitBindToggleTracers ||
                  isWaitBindToggleNoClip || isWaitBindToggleFreecam || isWaitBindToggleCameraZoom ||
                  isWaitBindKillAll || isWaitBindCallMeeting || isWaitBindTogglePlayerInfo ||
                  isWaitBindToggleSeeRoles || isWaitBindToggleSeeGhosts || isWaitBindToggleFullBright ||
                  isWaitBindKickAll || isWaitBindFixSabotages || isWaitBindSetAllGhost ||
                  isWaitBindSetAllGhostImp;

            if (e != null && e.isKey && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    isEditingName = isEditingLevel = isEditingFriendCode = isEditingLocalFriendCode = isEditingGhostChatColor = isEditingBan = false;
                    ResetAllBindWaits();
                    e.Use();
                }
                else if (isBinding && e.keyCode != KeyCode.None)
                {
                    if (isWaitingForBind) { menuToggleKey = e.keyCode; }
                    else if (isWaitBindMassMorph) { bindMassMorph = e.keyCode; }
                    else if (isWaitBindSpawnLobby) { bindSpawnLobby = e.keyCode; }
                    else if (isWaitBindDespawnLobby) { bindDespawnLobby = e.keyCode; }
                    else if (isWaitBindCloseMeeting) { bindCloseMeeting = e.keyCode; }
                    else if (isWaitBindInstaStart) { bindInstaStart = e.keyCode; }
                    else if (isWaitBindEndCrew) { bindEndCrew = e.keyCode; }
                    else if (isWaitBindEndImp) { bindEndImp = e.keyCode; }
                    else if (isWaitBindEndImpDC) { bindEndImpDC = e.keyCode; }
                    else if (isWaitBindEndHnsDC) { bindEndHnsDC = e.keyCode; }
                    else if (isWaitBindMagnetCursor) { bindMagnetCursor = e.keyCode; }
                    else if (isWaitBindToggleTracers) { bindToggleTracers = e.keyCode; }
                    else if (isWaitBindToggleNoClip) { bindToggleNoClip = e.keyCode; }
                    else if (isWaitBindToggleFreecam) { bindToggleFreecam = e.keyCode; }
                    else if (isWaitBindToggleCameraZoom) { bindToggleCameraZoom = e.keyCode; }
                    else if (isWaitBindKillAll) { bindKillAll = e.keyCode; }
                    else if (isWaitBindCallMeeting) { bindCallMeeting = e.keyCode; }
                    else if (isWaitBindTogglePlayerInfo) { bindTogglePlayerInfo = e.keyCode; }
                    else if (isWaitBindToggleSeeRoles) { bindToggleSeeRoles = e.keyCode; }
                    else if (isWaitBindToggleSeeGhosts) { bindToggleSeeGhosts = e.keyCode; }
                    else if (isWaitBindToggleFullBright) { bindToggleFullBright = e.keyCode; }
                    else if (isWaitBindKickAll) { bindKickAll = e.keyCode; }
                    else if (isWaitBindFixSabotages) { bindFixSabotages = e.keyCode; }
                    else if (isWaitBindSetAllGhost) { bindSetAllGhost = e.keyCode; }
                    else if (isWaitBindSetAllGhostImp) { bindSetAllGhostImp = e.keyCode; }

                    ResetAllBindWaits();
                    SaveConfig();
                    e.Use();
                }
                else if (isTyping)
                {
                    if (isEditingBan && HandleClipboardShortcut(e, ref banInput)) { }
                    else if (isEditingName && HandleClipboardShortcut(e, ref customNameInput)) { }
                    else if (isEditingLevel && HandleClipboardShortcut(e, ref spoofLevelString)) { }
                    else if (isEditingFriendCode && HandleClipboardShortcut(e, ref spoofFriendCodeInput)) { }
                    else if (isEditingLocalFriendCode && HandleClipboardShortcut(e, ref localFriendCodeInput)) { }
                    else if (isEditingGhostChatColor && HandleClipboardShortcut(e, ref ghostChatColorHex, 7)) { ghostChatColorHex = FilterHexInput(ghostChatColorHex, 7); }
                    else if (e.keyCode == KeyCode.Backspace)
                    {
                        if (isEditingBan && banInput.Length > 0) { banInput = banInput.Substring(0, banInput.Length - 1); }
                        if (isEditingName && customNameInput.Length > 0) { customNameInput = customNameInput.Substring(0, customNameInput.Length - 1); }
                        if (isEditingLevel && spoofLevelString.Length > 0) { spoofLevelString = spoofLevelString.Substring(0, spoofLevelString.Length - 1); }
                        if (isEditingFriendCode && spoofFriendCodeInput.Length > 0) { spoofFriendCodeInput = spoofFriendCodeInput.Substring(0, spoofFriendCodeInput.Length - 1); }
                        if (isEditingLocalFriendCode && localFriendCodeInput.Length > 0) { localFriendCodeInput = localFriendCodeInput.Substring(0, localFriendCodeInput.Length - 1); }
                        if (isEditingGhostChatColor && ghostChatColorHex.Length > 0) { ghostChatColorHex = ghostChatColorHex.Substring(0, ghostChatColorHex.Length - 1); }
                        e.Use();
                    }
                    else if (e.character != 0 && e.character != '\n' && e.character != '\r')
                    {
                        if (isEditingBan) { banInput += e.character; }
                        if (isEditingName) { customNameInput += e.character; }
                        if (isEditingLevel) { spoofLevelString += e.character; }
                        if (isEditingFriendCode) { spoofFriendCodeInput += e.character; }
                        if (isEditingLocalFriendCode) { localFriendCodeInput += e.character; }
                        if (isEditingGhostChatColor) { ghostChatColorHex = FilterHexInput((ghostChatColorHex ?? "") + e.character, 7); }
                        e.Use();
                    }
                }
            }

            if (Event.current.type == EventType.Layout)
            {
                lockedPlayersList.Clear();
                if (PlayerControl.AllPlayerControls != null)
                {
                    foreach (var p in PlayerControl.AllPlayerControls)
                    {
                        if (p != null && p.Data != null && !p.Data.Disconnected && p.PlayerId < 100)
                            lockedPlayersList.Add(p);
                    }
                }

                if (!stylesInited) InitStyles();

                if (showMenu)
                {
                    windowRect = GUI.Window(0, windowRect, (Action<int>)DrawElysiumModMenu, "", windowStyle);
                }

                for (int i = screenNotifications.Count - 1; i >= 0; i--)
                {
                    screenNotifications[i].lifetime += Time.deltaTime;
                    if (screenNotifications[i].HasExpired) screenNotifications.RemoveAt(i);
                }
            }

            try
            {
                if (AmongUsClient.Instance != null && (AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined || AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Started))
                {
                    if (PlayerControl.AllPlayerControls != null)
                    {
                        List<byte> currentIds = new List<byte>();
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                            {
                                currentIds.Add(pc.PlayerId);
                                UpsertPlayerHistory(pc);
                            }
                        }

                        foreach (var id in currentIds)
                        {
                            if (!lastPlayerIds.Contains(id) && !pendingJoinTimers.ContainsKey(id))
                            {
                                pendingJoinTimers[id] = 1.5f;
                            }
                        }

                        var keysToProcess = pendingJoinTimers.Keys.ToList();
                        foreach (var id in keysToProcess)
                        {
                            pendingJoinTimers[id] -= Time.deltaTime;
                            if (pendingJoinTimers[id] <= 0f)
                            {
                                pendingJoinTimers.Remove(id);

                                var pc = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p != null && p.PlayerId == id);
                                if (pc != null && pc.Data != null && !pc.Data.Disconnected)
                                {
                                    if (DetailedJoinInfo)
                                    {
                                        int level = 1;
                                        try
                                        {
                                            uint rawLevel = pc.Data.PlayerLevel;
                                            if (rawLevel != uint.MaxValue && rawLevel < 10000) level = (int)rawLevel + 1;
                                        }
                                        catch { }

                                        string platform = GetPlatform(AmongUsClient.Instance.GetClientFromCharacter(pc));
                                        string fc = GetDisplayedFriendCode(pc.Data);

                                        ShowNotification($"<color=#00FF00>[+]</color> {pc.Data.PlayerName} joined\n<color=#aaaaaa>Lvl: {level} | {platform} | FC: {fc}</color>");
                                    }
                                    else
                                    {
                                        ShowNotification($"<color=#00FF00>[+]</color> {pc.Data.PlayerName} присоединился");
                                    }
                                }
                            }
                        }

                        foreach (var id in lastPlayerIds)
                        {
                            if (!currentIds.Contains(id))
                            {
                                pendingJoinTimers.Remove(id);
                                MarkPlayerHistoryLeft(id);
                            }
                        }

                        lastPlayerIds = new List<byte>(currentIds);
                    }
                }
                else
                {
                    foreach (var id in lastPlayerIds)
                        MarkPlayerHistoryLeft(id);
                    lastPlayerIds.Clear();
                    pendingJoinTimers.Clear();
                }
            }
            catch { }
            if (screenNotifications.Count > 0)
            {
                int maxNotifs = 6;
                int startIdx = Mathf.Max(0, screenNotifications.Count - maxNotifs);
                for (int i = startIdx; i < screenNotifications.Count; i++)
                {
                    ElysiumNotification notif = screenNotifications[i];
                    int reverseIndex = screenNotifications.Count - 1 - i;

                    float slideOffset = 0f;
                    float animSpeed = 0.3f;
                    float currentAlpha = 0.95f;

                    if (notif.lifetime < animSpeed)
                    {
                        float t = Mathf.Clamp01(1f - (notif.lifetime / animSpeed));
                        slideOffset = t * t * 300f;
                    }
                    else if (notif.lifetime > notif.ttl - animSpeed)
                    {
                        float t = Mathf.Clamp01((notif.lifetime - (notif.ttl - animSpeed)) / animSpeed);
                        slideOffset = t * t * 300f;
                        currentAlpha = Mathf.Lerp(0.95f, 0f, t);
                    }

                    float xPos = (float)Screen.width - notificationBoxSize.x - 15f + slideOffset;
                    float yPos = Screen.height - 150f - (reverseIndex * (notificationBoxSize.y + 5f));

                    GUI.color = new Color(0.12f, 0.12f, 0.12f, currentAlpha);
                    GUI.Box(new Rect(xPos, yPos, notificationBoxSize.x, notificationBoxSize.y), "", windowStyle);

                    GUI.color = new Color(1f, 1f, 1f, currentAlpha > 0.5f ? 1f : currentAlpha * 2f);
                    string accentHex = GetMenuAccentHex();

                    GUI.Label(new Rect(xPos + 10f, yPos + 5f, notificationBoxSize.x - 20f, 20f), $"<b><color=#{accentHex}>{notif.title}</color></b>");

                    float timeLeft = Mathf.Max(0, notif.ttl - notif.lifetime);
                    GUI.Label(new Rect(xPos + 10f, yPos + 5f, notificationBoxSize.x - 20f, 20f), $"<b><color=#{accentHex}>{timeLeft:F1}s</color></b>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 12, richText = true });
                    GUI.Label(new Rect(xPos + 10f, yPos + 25f, notificationBoxSize.x - 20f, notificationBoxSize.y - 30f), notif.message, new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 12 });

                    float progress = 1f - (notif.lifetime / notif.ttl);
                    GUI.color = new Color(currentAccentColor.r, currentAccentColor.g, currentAccentColor.b, currentAlpha);
                    GUI.Box(new Rect(xPos + 8f, yPos + notificationBoxSize.y - 6f, (notificationBoxSize.x - 16f) * progress, 2f), "", safeLineStyle);
                    GUI.color = Color.white;
                }
            }
        }

        public static bool votekickEveryone = false;
        public static List<int> votekickedPlayerIds = new List<int>();
        private static bool votekickExitQueued = false;
        private static float votekickExitAt = 0f;
        private const float VotekickExitDelay = 0.45f;
        private Vector2 votekickScrollPosition = Vector2.zero;

        private void StartVotekickEveryoneRun()
        {
            votekickEveryone = true;
            votekickedPlayerIds.Clear();
            votekickExitQueued = false;
            votekickExitAt = 0f;
            ShowNotification("<color=#ca08ff>[AUTO-VOTEKICK]</color> <b>Armed.</b> Join a room and votes will be sent automatically.");
        }

        private void StopVotekickEveryoneRun(bool clearVotes = true)
        {
            votekickEveryone = false;
            votekickExitQueued = false;
            votekickExitAt = 0f;
            if (clearVotes) votekickedPlayerIds.Clear();
        }

        private void TickVotekickEveryoneRun()
        {
            if (!votekickEveryone) return;

            if (votekickExitQueued)
            {
                if (Time.unscaledTime >= votekickExitAt)
                    LeaveRoomAfterVotekick();
                return;
            }

            if (VoteBanSystem.Instance == null || PlayerControl.AllPlayerControls == null || AmongUsClient.Instance == null)
                return;

            int sent = ExecuteVotekickEveryone(true);
            if (sent <= 0) return;

            votekickExitQueued = true;
            votekickExitAt = Time.unscaledTime + VotekickExitDelay;
            ShowNotification($"<color=#ca08ff>[AUTO-VOTEKICK]</color> Votes sent: <b>{sent}</b>. <b>Leaving room...</b>");
        }

        private int ExecuteVotekickEveryone(bool rememberTargets)
        {
            if (VoteBanSystem.Instance == null || PlayerControl.AllPlayerControls == null) return 0;

            int votesSent = 0;
            try
            {
                foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.AmOwner || pc.Data == null || pc.Data.Disconnected) continue;

                    int clientId = pc.Data.ClientId;

                    if (!rememberTargets || !votekickedPlayerIds.Contains(clientId))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            VoteBanSystem.Instance.CmdAddVote(clientId);
                            votesSent++;
                        }

                        if (rememberTargets)
                            votekickedPlayerIds.Add(clientId);

                        ShowNotification($"<color=#ca08ff>[VOTEKICK]</color> <b>3 votes</b> sent to <b>{pc.Data.PlayerName}</b>.");
                    }
                }
            }
            catch (Exception)
            {
            }

            return votesSent;
        }

        private void SendVotekickEveryoneStay()
        {
            int sent = ExecuteVotekickEveryone(false);
            if (sent > 0)
                ShowNotification($"<color=#ca08ff>[VOTEKICK]</color> Sent <b>{sent}</b> votes. <b>Staying in room.</b>");
            else
                ShowNotification("<color=#FF4444>[VOTEKICK]</color> No valid targets or VoteBanSystem is not ready.");
        }

        private void LeaveRoomAfterVotekick()
        {
            try
            {
                votekickExitQueued = false;
                votekickExitAt = 0f;
                votekickedPlayerIds.Clear();

                if (AmongUsClient.Instance != null)
                {
                    AmongUsClient.Instance.ExitGame(DisconnectReasons.ExitGame);
                    ShowNotification("<color=#ca08ff>[AUTO-VOTEKICK]</color> <b>Left room.</b> Auto mode is still armed.");
                }
            }
            catch (Exception)
            {
                votekickExitQueued = false;
                votekickExitAt = 0f;
            }
        }

        public static void ExecuteVotekickTarget(PlayerControl target)
        {
            if (target == null || target.Data == null || VoteBanSystem.Instance == null) return;

            try
            {
                int targetClientId = target.Data.ClientId;

                VoteBanSystem.Instance.CmdAddVote(targetClientId);


                if (DestroyableSingleton<HudManager>.Instance != null && DestroyableSingleton<HudManager>.Instance.Notifier != null)
                {
                    DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage("Votekick sent! Leave and rejoin 2 more times.");
                }

                ShowNotification($"<color=#ca08ff>[VOTEKICK]</color> Vote sent to <b>{target.Data.PlayerName}</b>!");
            }
            catch (Exception)
            {
            }
        }

        private void DrawVotekickTab()
        {
            GUILayout.BeginVertical(menuCardStyle);
            try
            {
                GUIStyle voteInfoStyle = new GUIStyle(toggleLabelStyle) { richText = true, wordWrap = true };
                DrawMenuSectionHeader("VOTEKICK MENU");
                GUILayout.Label("<color=#777777><b>Auto mode:</b> sends 3 votes to every valid player, leaves the room, and stays armed until you press it again.</color>", voteInfoStyle);
                GUILayout.Space(5);

                string autoButtonText = votekickEveryone ? "STOP AUTO VOTEKICK + LEAVE" : "AUTO VOTEKICK + LEAVE";
                if (GUILayout.Button(autoButtonText, votekickEveryone ? activeTabStyle : btnStyle, GUILayout.Height(35)))
                {
                    if (votekickEveryone) StopVotekickEveryoneRun();
                    else StartVotekickEveryoneRun();
                }

                GUILayout.Space(5);
                GUILayout.Label("<color=#777777><b>Manual mode:</b> sends 3 votes now and stays in the current room.</color>", voteInfoStyle);
                if (GUILayout.Button("SEND 3 VOTES + STAY", btnStyle, GUILayout.Height(32)))
                {
                    SendVotekickEveryoneStay();
                }
            }
            finally { GUILayout.EndVertical(); }

            GUILayout.Space(10);
            DrawMenuSectionHeader("TARGET VOTE");

            if (PlayerControl.AllPlayerControls != null)
            {
                var safePlayersList = new System.Collections.Generic.List<PlayerControl>();
                foreach (var p in PlayerControl.AllPlayerControls) safePlayersList.Add(p);

                votekickScrollPosition = GUILayout.BeginScrollView(votekickScrollPosition);
                try
                {
                    foreach (var pc in safePlayersList)
                    {
                        if (pc == null || pc.Data == null || pc.PlayerId >= 100 || pc == PlayerControl.LocalPlayer) continue;

                        GUILayout.BeginHorizontal(boxStyle);
                        try
                        {
                            string pName = pc.Data.PlayerName ?? "Unknown";
                            bool isHost = (AmongUsClient.Instance != null && AmongUsClient.Instance.GetHost()?.Character == pc);
                            string displayStr = isHost ? pName + " <color=#FF0000>[H]</color>" : pName;

                            GUILayout.Label(displayStr, GUILayout.Width(110));

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("Vote", btnStyle, GUILayout.Width(60), GUILayout.Height(25)))
                            {
                                ExecuteVotekickTarget(pc);
                            }
                        }
                        finally
                        {
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.Space(2);
                    }
                }
                finally
                {
                    GUILayout.EndScrollView();
                }
            }
        }

        private void DrawElysiumModMenu(int windowID)
        {
            if (Event.current.type == EventType.Repaint && tabTransitionProgress < 1f)
            {
                tabTransitionProgress += Time.unscaledDeltaTime * 8f;
                if (tabTransitionProgress >= 1f) { tabTransitionProgress = 1f; currentTab = targetTabIndex; }
            }

            if (enableBackground && customMenuBg != null)
            {
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
                GUIStyle bgStyle = new GUIStyle() { normal = { background = customMenuBg } };
                GUI.Box(new Rect(0, 0, windowRect.width, windowRect.height), GUIContent.none, bgStyle);
                GUI.color = Color.white;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(ApplyMenuShimmer("ElysiumModMenu Meowchelo & Carrot"), titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("-", new GUIStyle(btnStyle) { fixedWidth = 20, fixedHeight = 18, margin = CreateRectOffset(0, 8, 6, 0) })) showMenu = false;
            GUILayout.EndHorizontal();

            GUI.color = new Color(1f, 1f, 1f, 0.1f);
            GUI.Box(new Rect(0, 30, windowRect.width, 1), "", safeLineStyle);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(0f, 31f, 130f, windowRect.height - 31f));
            GUILayout.BeginVertical(sidebarStyle, GUILayout.ExpandHeight(true));
            GUILayout.Space(5);
            for (int i = 0; i < tabNames.Length; i++)
                if (GUILayout.Button(tabNames[i], i == targetTabIndex ? activeSidebarBtnStyle : sidebarBtnStyle, GUILayout.Height(24)))
                    if (targetTabIndex != i) { targetTabIndex = i; tabTransitionProgress = 0f; scrollPosition = Vector2.zero; }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            GUI.color = new Color(1f, 1f, 1f, 0.1f);
            GUI.Box(new Rect(130, 31, 1, windowRect.height), "", safeLineStyle);
            GUI.color = new Color(1f, 1f, 1f, tabTransitionProgress);

            GUILayout.BeginArea(new Rect(140f, 36f + ((1f - tabTransitionProgress) * 10f), windowRect.width - 150f, windowRect.height - 46f));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            int tabToDraw = (tabTransitionProgress < 1f) ? targetTabIndex : currentTab;

            if (tabToDraw == 0) DrawGeneralTab();
            else if (tabToDraw == 1) DrawSelfTab();
            else if (tabToDraw == 2) DrawVisualsTab();
            else if (tabToDraw == 3) DrawPlayersTab();
            else if (tabToDraw == 4) DrawSabotagesTab();
            else if (tabToDraw == 5) DrawHostOnlyTab();
            else if (tabToDraw == 6) DrawVotekickTab();
            else if (tabToDraw == 7) DrawMenuTab();
            else if (tabToDraw == 8) DrawAnimationsTab();

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUI.color = Color.white;
            GUI.DragWindow(new Rect(0, 0, 10000, 30));
        }
        public static int punishmentMode = 1;
        public static bool settingsDirty = false;
        public static string[] punishmentNames = { "Null", "Warn", "Kick", "Ban" };

        public static bool blockSpoofRPC = true;
        public static bool blockSabotageRPC = true;
        public static bool blockGameRpcInLobby = true;
        public static bool blockChatFloodRpc = true;
        public static bool blockMeetingFloodRpc = true;
        public static bool enablePasosLimit = true;
        public static bool enableLocalPasosBan = true;
        public static bool enableHostPasosBan = true;
        public static bool autoBanBrokenFriendCode = false;
        public static int chatRpcLimit = 1;
        public static float chatRpcWindow = 1f;
        public static int meetingRpcLimit = 2;
        public static float meetingRpcWindow = 9999f;

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleAnimation))]
        public static class PlayerPhysics_HandleAnimation
        {
            public static bool Prefix(PlayerPhysics __instance)
            {
                if (ElysiumModMenuGUI.moonWalk && __instance.AmOwner)
                {
                    __instance.ResetAnimState();
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.UpdateCharCount))]
        public static class FreeChatInputField_UpdateCharCount_Patch
        {
            public static void Postfix(FreeChatInputField __instance)
            {
                if (__instance == null || __instance.textArea == null || __instance.charCountText == null) return;

                __instance.textArea.characterLimit = 120;

                int length = __instance.textArea.text.Length;

                __instance.charCountText.SetText($"{length}/{__instance.textArea.characterLimit}");

                if (length < 90)
                {
                    __instance.charCountText.color = Color.white;
                }
                else if (length < 115)
                {
                    __instance.charCountText.color = Color.yellow;
                }
                else
                {
                    __instance.charCountText.color = Color.red;
                }
            }
        }

        public static class ChatHistory
        {
            public static List<string> sentMessages = new List<string>();
            public static int HistoryIndex = -1;
            public static string DraftBeforeHistory = "";
            public static bool BrowsingHistory = false;

            public static void Remember(string message)
            {
                if (string.IsNullOrWhiteSpace(message)) return;
                bool isNewEntry = sentMessages.Count == 0 || sentMessages[sentMessages.Count - 1] != message;
                if (isNewEntry)
                {
                    sentMessages.Add(message);
                    while (sentMessages.Count > ElysiumModMenuGUI.chatHistoryLimit)
                        sentMessages.RemoveAt(0);
                }
                HistoryIndex = sentMessages.Count;
            }

            public static void HandleNavigation(ChatController chat)
            {
                if (sentMessages.Count == 0 || chat.freeChatField == null || chat.freeChatField.textArea == null || !chat.freeChatField.textArea.hasFocus)
                    return;

                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    if (!BrowsingHistory)
                    {
                        DraftBeforeHistory = chat.freeChatField.textArea.text;
                        BrowsingHistory = true;
                    }
                    if (HistoryIndex <= 0) return;

                    HistoryIndex = Mathf.Clamp(HistoryIndex - 1, 0, sentMessages.Count - 1);
                    chat.freeChatField.textArea.SetText(sentMessages[HistoryIndex], string.Empty);
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    if (!BrowsingHistory) return;

                    HistoryIndex += 1;
                    if (HistoryIndex < sentMessages.Count)
                    {
                        chat.freeChatField.textArea.SetText(sentMessages[HistoryIndex], string.Empty);
                    }
                    else
                    {
                        chat.freeChatField.textArea.SetText(DraftBeforeHistory, string.Empty);
                        BrowsingHistory = false;
                    }
                }
            }
        }

        public static class ClipboardBridge
        {
            private static bool isPastingChatInput = false;
            private static int currentPasteCharPos = 0;
            private static int lastClipboardFrame = -1;

            public static void Run(TextBoxTMP box)
            {
                if (!enableClipboard) return;
                if (box == null || !box.hasFocus) return;

                bool controlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                bool copyPressed = controlHeld && (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Insert));
                bool pastePressed = (controlHeld && Input.GetKeyDown(KeyCode.V)) || (shiftHeld && Input.GetKeyDown(KeyCode.Insert));
                bool cutPressed = controlHeld && Input.GetKeyDown(KeyCode.X);

                if (!copyPressed && !pastePressed && !cutPressed) return;
                if (lastClipboardFrame == Time.frameCount) return;
                lastClipboardFrame = Time.frameCount;

                if (copyPressed)
                {
                    GUIUtility.systemCopyBuffer = box.text ?? string.Empty;
                }
                else if (pastePressed)
                {
                    string paste = GUIUtility.systemCopyBuffer;
                    if (!string.IsNullOrEmpty(paste))
                    {
                        string currentText = box.text ?? string.Empty;
                        int caretPos = Mathf.Clamp(box.caretPos, 0, currentText.Length);
                        string nextText = currentText.Insert(caretPos, paste);

                        isPastingChatInput = true;
                        box.SetText(nextText, string.Empty);
                        isPastingChatInput = false;
                    }
                }
                else if (cutPressed)
                {
                    GUIUtility.systemCopyBuffer = box.text ?? string.Empty;
                    box.SetText(string.Empty, string.Empty);
                }
            }

            public static bool IsCharAllowed(TextBoxTMP box, ref bool result)
            {
                if (box == null) return true;

                string compositionString = Input.compositionString;
                if (!string.IsNullOrEmpty(compositionString))
                {
                    result = true;
                    return false;
                }

                string input = isPastingChatInput ? GUIUtility.systemCopyBuffer : Input.inputString;
                if (string.IsNullOrEmpty(input)) return true;

                string currentText = box.text ?? string.Empty;
                int caretPos = Mathf.Clamp(box.caretPos, 0, currentText.Length);
                string text = currentText.Insert(caretPos, input);

                currentPasteCharPos = Mathf.Clamp(currentPasteCharPos, 0, Mathf.Max(0, text.Length - 1));
                char currentChar = text[currentPasteCharPos];
                currentPasteCharPos = currentPasteCharPos >= text.Length - 1 ? 0 : currentPasteCharPos + 1;

                if (allowLinksAndSymbols)
                {
                    HashSet<char> blockedSymbols = new HashSet<char> { '\b', '\r', '\n', '>', '<', '[' };
                    result = !blockedSymbols.Contains(currentChar);
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.Update))]
        public static class AllowSymbols_TextBoxTMP_Update_Patch
        {
            public static void Postfix(TextBoxTMP __instance)
            {
                if (__instance == null) return;
                __instance.allowAllCharacters = ElysiumModMenuGUI.allowLinksAndSymbols;
                __instance.AllowSymbols = ElysiumModMenuGUI.allowLinksAndSymbols;
                __instance.AllowEmail = ElysiumModMenuGUI.allowLinksAndSymbols;
            }
        }

        [HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.Update))]
        public static class Clipboard_TextBoxTMP_Patch
        {
            public static void Postfix(TextBoxTMP __instance)
            {
                ClipboardBridge.Run(__instance);
            }
        }

        [HarmonyPatch(typeof(TextBoxTMP), nameof(TextBoxTMP.IsCharAllowed))]
        public static class Clipboard_TextBoxTMP_IsCharAllowed_Patch
        {
            public static bool Prefix(TextBoxTMP __instance, ref bool __result)
            {
                return ClipboardBridge.IsCharAllowed(__instance, ref __result);
            }
        }

        [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
        public static class ChatHistory_Update_Patch
        {
            public static void Postfix(ChatController __instance)
            {
                if (__instance != null && __instance.freeChatField != null && __instance.freeChatField.textArea != null)
                {
                    ClipboardBridge.Run(__instance.freeChatField.textArea);
                }
                ChatHistory.HandleNavigation(__instance);
            }
        }
        public static bool enableExtendedChat = true;
        public static bool enableChatHistory = true;
        public static bool enableClipboard = true;
        public static bool AnimEmptyGarbageEnabled = false;
        public static bool skipShhhAnim = false;
        public static bool isManualMapSpawn = false;
        private void DrawAnimationsTab()
        {
            GUILayout.BeginVertical(menuCardStyle);

            DrawMenuSectionHeader(L("LOOPED PLAYER ANIMATIONS", "ЗАЦИКЛЕННЫЕ АНИМАЦИИ ИГРОКА"));

            string animInfo = L("<color=#777777>Animations are looped. They will run as long as the toggle is ON.</color>",
                                "<color=#777777>Анимации зациклены. Будут работать, пока включен тумблер.</color>");
            GUILayout.Label(animInfo, new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11, wordWrap = true });

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            AnimAsteroidsEnabled = DrawToggle(AnimAsteroidsEnabled, L("Weapons (Asteroids)", "Оружие (Астероиды)"), 250);
            IsScanning = DrawToggle(IsScanning, L("Medbay Scan", "Скан в медпункте"), 250);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            AnimShieldsEnabled = DrawToggle(AnimShieldsEnabled, L("Turn On Shields", "Включить щиты"), 250);
            AnimCamsInUseEnabled = DrawToggle(AnimCamsInUseEnabled, L("Use Cameras (Blink Red)", "Камеры (Красный индикатор)"), 250);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            AnimEmptyGarbageEnabled = DrawToggle(AnimEmptyGarbageEnabled, L("Empty Garbage", "Выкинуть мусор"), 250);
            skipShhhAnim = DrawToggle(skipShhhAnim, L("Skip 'Shhh!' Intro", "Пропустить 'Shhh!' интро"), 250);
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        public static string GetPlatform(ClientData client)
        {
            if (client == null || client.PlatformData == null) return "Unknown";

            int platformId = (int)client.PlatformData.Platform;

            switch (platformId)
            {
                case 1: return "Epic";
                case 2: return "Steam";
                case 3: return "Mac";
                case 4: return "Microsoft";
                case 5: return "Itch";
                case 6: return "iOS";
                case 7: return "Android";
                case 8: return "Switch";
                case 9: return "Xbox";
                case 10: return "PlayStation";
                case 112: return "Starlight";
                default: return $"Unknown ({platformId})";
            }
        }

        private static string CompactEspValue(string value, int maxLength = 24)
        {
            value = Regex.Replace(value ?? string.Empty, "<.*?>", string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();

            if (string.IsNullOrEmpty(value)) return "Hidden";
            if (value.Length > maxLength) value = value.Substring(0, maxLength - 3) + "...";
            return value;
        }

        private static string NormalizeEspToken(string value)
        {
            return Regex.Replace(value ?? string.Empty, "<.*?>", string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
        }

        private static string FriendEspIgnoreFilePath()
        {
            string folder = string.IsNullOrWhiteSpace(Plugin.ElysiumFolder)
                ? System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "ElysiumModMenu")
                : Plugin.ElysiumFolder;
            return System.IO.Path.Combine(folder, "ElysiumFriendEspIgnore.txt");
        }

        private static void LoadFriendEspIgnoreTokensIfNeeded()
        {
            try
            {
                if (Time.unscaledTime < friendEspIgnoreNextLoadAt) return;
                friendEspIgnoreNextLoadAt = Time.unscaledTime + 3f;

                friendEspIgnoreTokens.Clear();
                string path = FriendEspIgnoreFilePath();
                if (!System.IO.File.Exists(path))
                {
                    System.IO.File.WriteAllText(path, "# One nickname, Friend Code, or PUID per line. Matching players will not show ESP info.\n");
                    return;
                }

                foreach (string line in System.IO.File.ReadAllLines(path))
                {
                    string token = NormalizeEspToken(line);
                    if (string.IsNullOrWhiteSpace(token) || token.StartsWith("#")) continue;
                    friendEspIgnoreTokens.Add(token);
                }
            }
            catch { }
        }

        private static bool IsEspIgnored(NetworkedPlayerInfo info)
        {
            if (info == null) return false;

            LoadFriendEspIgnoreTokensIfNeeded();
            if (friendEspIgnoreTokens.Count == 0) return false;

            try
            {
                string name = NormalizeEspToken(info.PlayerName);
                if (!string.IsNullOrEmpty(name) && friendEspIgnoreTokens.Contains(name)) return true;

                string displayedFc = NormalizeEspToken(GetDisplayedFriendCode(info, string.Empty));
                if (!string.IsNullOrEmpty(displayedFc) && friendEspIgnoreTokens.Contains(displayedFc)) return true;

                string rawFc = NormalizeEspToken(info.FriendCode);
                if (!string.IsNullOrEmpty(rawFc) && friendEspIgnoreTokens.Contains(rawFc)) return true;

                ClientData client = AmongUsClient.Instance?.GetClientFromPlayerInfo(info);
                string puid = client == null ? string.Empty : NormalizeEspToken(GetClientPuid(client));
                return !string.IsNullOrEmpty(puid) && friendEspIgnoreTokens.Contains(puid);
            }
            catch { return false; }
        }

        public static string BuildESPInfoLine(NetworkedPlayerInfo info)
        {
            if (info == null) return string.Empty;

            int level = 0;
            string platform = "Unknown";
            bool isHost = false;

            try { level = (int)info.PlayerLevel + 1; } catch { }

            try
            {
                var client = AmongUsClient.Instance.GetClientFromPlayerInfo(info);
                if (client != null)
                {
                    platform = GetPlatform(client);
                    isHost = AmongUsClient.Instance.GetHost() == client;
                }
            }
            catch { }

            if (enablePlatformSpoof &&
                PlayerControl.LocalPlayer != null &&
                info.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                platform = $"{platform} spf";
            }

            string fc = CompactEspValue(GetDisplayedFriendCode(info));
            List<string> parts = new List<string>();
            if (isHost) parts.Add("Host");
            parts.Add($"Lv:{level}");
            parts.Add(platform);
            if (showEspFriendCode) parts.Add(fc);
            return string.Join(" - ", parts);
        }

        public static Color GetRoleColor(int roleId, Color fallbackColor)
        {
            switch (roleId)
            {
                case 1: return new Color32(255, 0, 0, 255);
                case 2: return new Color32(0, 0, 128, 255);
                case 3: return new Color32(127, 255, 212, 255);
                case 4: return new Color32(176, 196, 222, 255);
                case 5: return new Color32(255, 140, 0, 255);
                case 8: return new Color32(255, 105, 180, 255);
                case 9: return new Color32(139, 0, 0, 255);
                case 10: return new Color32(106, 90, 205, 255);
                case 12: return new Color32(189, 183, 107, 255);
                case 18: return new Color32(173, 255, 47, 255);
                default: return fallbackColor;
            }
        }

        public static void HandleTracer(PlayerControl target, bool enable)
        {
            try
            {
                if (target == null || target.gameObject == null) return;

                LineRenderer lr = target.GetComponent<LineRenderer>();

                if (!enable || PlayerControl.LocalPlayer == null || target == PlayerControl.LocalPlayer || target.Data == null || target.Data.Disconnected || IsEspIgnored(target.Data))
                {
                    if (lr != null) lr.enabled = false;
                    return;
                }

                if (target.Data.IsDead && !seeGhosts && !PlayerControl.LocalPlayer.Data.IsDead)
                {
                    if (lr != null) lr.enabled = false;
                    return;
                }

                if (lr == null)
                {
                    lr = target.gameObject.AddComponent<LineRenderer>();
                    lr.SetVertexCount(2);
                    lr.SetWidth(0.02f, 0.02f);
                    try { if (HatManager.Instance != null) lr.material = HatManager.Instance.PlayerMaterial; } catch { }
                }

                lr.enabled = true;

                Color tColor = Color.white;
                try
                {
                    if (target.Data.IsDead)
                    {
                        tColor = Color.gray;
                    }
                    else if (target.Data.Role != null)
                    {
                        tColor = GetRoleColor((int)target.Data.Role.Role, target.Data.Role.TeamColor);
                    }
                }
                catch { }

                lr.SetColors(tColor, tColor);

                lr.SetPosition(0, PlayerControl.LocalPlayer.transform.position);
                lr.SetPosition(1, target.transform.position);
            }
            catch { }
        }


        private void DrawLobbyControls()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(282));
            DrawMenuSectionHeader("GAME RULES");
            neverEndGame = DrawToggle(neverEndGame, "Unlimited Game", 250);
            GUILayout.Space(5);
            noSettingLimit = DrawToggle(noSettingLimit, "No Setting Limit", 250);
            GUILayout.Space(5);
            noTaskMode = DrawToggle(noTaskMode, "No Task Mode", 250);
            GUILayout.Space(5);
            allowDuplicateColors = DrawToggle(allowDuplicateColors, L("Allow Duplicate Colors", "Разрешить одинаковые цвета"), 250);
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(282));
            DrawMenuSectionHeader("CHAT MODERATION");
            enableColorCommand = DrawToggle(enableColorCommand, "Enable /c command (Public)", 250);
            GUILayout.Space(5);
            blockFortegreenChat = DrawToggle(blockFortegreenChat, "Block Fortegreen Chat", 250);
            GUILayout.Space(5);
            blockRainbowChat = DrawToggle(blockRainbowChat, "Block Rainbow Chat", 250);
            GUILayout.Space(5);
            autoChatEveryone = DrawToggle(autoChatEveryone, "Chat Everyone (Auto-Meeting)", 250);
            if (autoChatEveryone)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Delay: {autoChatEveryoneDelay:0.0}s", toggleLabelStyle, GUILayout.Width(78));
                autoChatEveryoneDelay = GUILayout.HorizontalSlider(autoChatEveryoneDelay, 0f, 10f, sliderStyle, sliderThumbStyle, GUILayout.Width(170));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(282));
            DrawMenuSectionHeader("LOBBY ACTIONS");
            if (GUILayout.Button("Insta Start", btnStyle, GUILayout.Height(26)))
            { GameStartManager.Instance.startState = GameStartManager.StartingStates.Countdown; GameStartManager.Instance.countDownTimer = 0f; }
            GUILayout.Space(5);
            if (GUILayout.Button("Close Meeting", btnStyle, GUILayout.Height(26))) MeetingHud.Instance.RpcClose();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Spawn Lobby", activeTabStyle, GUILayout.Height(26))) SpawnLobby();
            GUILayout.Space(5);
            if (GUILayout.Button("Despawn", btnStyle, GUILayout.Height(26))) DespawnLobby();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Kill All", btnStyle, GUILayout.Height(26))) KillAll();
            GUILayout.Space(5);
            if (GUILayout.Button("Kick All", btnStyle, GUILayout.Height(26))) KickAll();
            GUILayout.Space(5);
            if (GUILayout.Button("Mass Morph", btnStyle, GUILayout.Height(26))) this.StartCoroutine(MassMorphCoroutine().WrapToIl2Cpp());
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(10);

            GUILayout.BeginVertical(menuCardStyle, GUILayout.Width(282));
            DrawMenuSectionHeader("END GAME");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Crewmate Win", btnStyle, GUILayout.Height(26))) SmartEndGame("CrewWin");
            GUILayout.Space(5);
            if (GUILayout.Button("Impostor Win", btnStyle, GUILayout.Height(26))) SmartEndGame("ImpWin");
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Imp Disconnect", btnStyle, GUILayout.Height(26))) SmartEndGame("ImpDisconnect");
            GUILayout.Space(5);
            if (GUILayout.Button("H&S Disconnect", activeTabStyle, GUILayout.Height(26))) SmartEndGame("HnsImpDisconnect");
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (GUILayout.Button("Force End (Impostor Disconnect)", btnStyle, GUILayout.Height(26)) && GameManager.Instance != null && AmongUsClient.Instance.AmHost)
            { bool tempNeverEnd = neverEndGame; neverEndGame = false; GameManager.Instance.RpcEndGame((GameOverReason)4, false); neverEndGame = tempNeverEnd; }
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        public static string GetESPNameTag(NetworkedPlayerInfo info, string originalName)
        {
            if (info == null) return originalName;
            string newName = originalName;
            if (enableLocalNameSpoof &&
                PlayerControl.LocalPlayer != null &&
                info.PlayerId == PlayerControl.LocalPlayer.PlayerId &&
                !string.IsNullOrWhiteSpace(customNameInput))
            {
                newName = BuildLocalNameRenderText(customNameInput);
            }

            if (seeRoles && info.Role != null)
            {
                string roleName = info.Role.Role.ToString();
                int roleId = (int)info.Role.Role;
                if (roleId == 8) roleName = "Noisemaker";
                else if (roleId == 9) roleName = "Phantom";
                else if (roleId == 10) roleName = "Tracker";
                else if (roleId == 12) roleName = "Detective";
                else if (roleId == 18) roleName = "Viper";
                else if (roleName == "GuardianAngel") roleName = "Guardian Angel";
                Color customColor = GetRoleColor(roleId, info.Role.TeamColor);
                string roleColor = ColorUtility.ToHtmlStringRGB(customColor);
                newName = $"<color=#{roleColor}>{roleName}</color>\n{newName}";
            }
            if (showPlayerInfo)
            {
                string accentHex = GetMenuAccentHex();
                string espLine = BuildESPInfoLine(info);
                if (!string.IsNullOrWhiteSpace(espLine))
                    newName = $"<size=80%><color=#{accentHex}>{espLine}</color></size>\n{newName}";
            }
            if (seeKillCooldown && info.Role != null && info.PlayerId != PlayerControl.LocalPlayer?.PlayerId)
            {
                int roleId = (int)info.Role.Role;
                bool isImpTeam = roleId == 1 || roleId == 5 || roleId == 9 || roleId == 18;
                if (isImpTeam)
                {
                    float rem = GetRemainingKillCooldown(info.PlayerId);
                    string kcdText = rem > 0.01f ? $"KCD: {rem:F1}s" : "KCD: READY";
                    string kcdColor = rem > 0.01f ? "FFAA33" : "55FF77";
                    newName = $"<size=78%><color=#{kcdColor}>{kcdText}</color></size>\n{newName}";
                }
            }
            return newName;
        }

        private static float GetConfiguredKillCooldown()
        {
            try
            {
                object opts = GameOptionsManager.Instance?.CurrentGameOptions;
                if (opts == null) return 25f;
                var m = opts.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(x => x.Name == "GetFloat" && x.GetParameters().Length == 1);
                if (m != null)
                {
                    Type enumType = m.GetParameters()[0].ParameterType;
                    if (enumType.IsEnum)
                    {
                        foreach (var val in Enum.GetValues(enumType))
                        {
                            string n = val.ToString().ToLower();
                            if (n.Contains("kill") && n.Contains("cool"))
                            {
                                object result = m.Invoke(opts, new object[] { val });
                                return Convert.ToSingle(result);
                            }
                        }
                    }
                }
            }
            catch { }
            return 25f;
        }

        private static float GetRemainingKillCooldown(byte playerId)
        {
            if (!lastKillTimestamps.ContainsKey(playerId)) return 0f;
            float elapsed = Time.time - lastKillTimestamps[playerId];
            float rem = GetConfiguredKillCooldown() - elapsed;
            return Mathf.Max(0f, rem);
        }

        private static bool IsImpostorTeamForCooldown(PlayerControl pc)
        {
            try
            {
                if (pc == null || pc.Data == null) return false;
                int roleId = pc.Data.Role != null ? (int)pc.Data.Role.Role : (int)pc.Data.RoleType;
                return roleId == 1 || roleId == 5 || roleId == 9 || roleId == 18;
            }
            catch { return false; }
        }

        public static void InitializeKillCooldownOnRoundStart()
        {
            try
            {
                lastKillTimestamps.Clear();
                if (PlayerControl.AllPlayerControls == null) return;

                float now = Time.time;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc == null || pc.Data == null || pc.Data.Disconnected) continue;
                    if (!IsImpostorTeamForCooldown(pc)) continue;
                    lastKillTimestamps[pc.PlayerId] = now;
                }
            }
            catch { }
        }


        [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
        public static class VersionShower_Start_Patch
        {
            public static void Postfix(VersionShower __instance) { if (__instance != null && __instance.text != null) __instance.text.text = ElysiumModMenuGUI.ApplyMenuShimmer("ElysiumModMenu Meowchelo & Carrot"); }
        }

        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        public static class PingTracker_Watermark_Patch
        {
            private static float _smoothFps = 0f;
            private static int _smoothPing = 0;
            private static float _updateTimer = 0f;
            public static void Postfix(PingTracker __instance)
            {
                try
                {
                    _updateTimer += Time.deltaTime;
                    if (_updateTimer >= 0.5f) { _smoothFps = 1f / Time.deltaTime; if (AmongUsClient.Instance != null) _smoothPing = AmongUsClient.Instance.Ping; _updateTimer = 0f; }
                    int num = Mathf.RoundToInt(_smoothFps);
                    string pingColor = ((_smoothPing < 80) ? "#00FF00" : ((_smoothPing < 400) ? "#FFFF00" : "#FF0000"));

                    string finalString = $"<color=#FFFFFF>PING:</color> <color={pingColor}>{_smoothPing} ms</color> • <color=#FFFFFF>FPS:</color> <color=#FFFFFF>{num}</color>";

                    if (ElysiumModMenuGUI.showWatermark)
                    {
                        string shimmerTitle = ElysiumModMenuGUI.ApplyMenuShimmer("ElysiumModMenu v1.3.5.1");
                        finalString = $"{shimmerTitle} • " + finalString;
                    }

                    if (AmongUsClient.Instance != null)
                    {
                        ClientData host = AmongUsClient.Instance.GetHost();
                        if (host != null && host.Character != null)
                        {
                            string hostName = host.Character.Data.PlayerName ?? "Unknown";
                            string shimmerHostName = ElysiumModMenuGUI.ApplyMenuShimmer(hostName);
                            finalString += $" • <color=#FFFFFF>Host:</color> {shimmerHostName}";
                            if (AmongUsClient.Instance.AmHost) finalString += " <color=#00FF00>(You)</color>";
                        }
                    }
                    __instance.text.text = finalString;
                    __instance.text.alignment = TMPro.TextAlignmentOptions.Center;
                    __instance.aspectPosition.enabled = false;
                    float zPos = MeetingHud.Instance != null && MeetingHud.Instance.gameObject.activeInHierarchy ? -100f : -10f;
                    __instance.transform.localPosition = new Vector3(0f, -2.3f, zPos);
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
        public static class GameStartManager_Update_Patch
        {
            public static void Postfix(GameStartManager __instance)
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.LocalPlayer == null) return;
                if (ElysiumModMenuGUI.fakeStartCounterTroll)
                {
                    try { sbyte[] arr = { -123, -111, -100, -69, -67, -52, -42, 0, 42, 52, 67, 69, 100, 111, 123 }; sbyte b = arr[UnityEngine.Random.Range(0, arr.Length)]; PlayerControl.LocalPlayer.RpcSetStartCounter(b); __instance.SetStartCounter(b); } catch { }
                }
                else if (ElysiumModMenuGUI.fakeStartCounterCustom && int.TryParse(ElysiumModMenuGUI.fakeStartInput, out int custom))
                {
                    try { PlayerControl.LocalPlayer.RpcSetStartCounter(custom); __instance.SetStartCounter((sbyte)Mathf.Clamp(custom, -128, 127)); } catch { }
                }
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.RpcEndGame))]
        public static class InfiniteGamePatch { public static bool Prefix() { try { if (ElysiumModMenuGUI.neverEndGame && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost) return false; } catch { } return true; } }

        [HarmonyPatch(typeof(IntroCutscene), "CoBegin")]
        public static class IntroCutscene_CoBegin_Patch
        {
            public static void Prefix()
            {
                if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
                if (ElysiumModMenuGUI.enablePreGameRoleForce)
                {
                    foreach (var kvp in ElysiumModMenuGUI.forcedPreGameRoles)
                    { var target = GameData.Instance.GetPlayerById(kvp.Key)?.Object; if (target != null && target.Data.RoleType != kvp.Value) target.RpcSetRole(kvp.Value); }
                    foreach (byte impId in ElysiumModMenuGUI.forcedImpostors)
                    { var target = GameData.Instance.GetPlayerById(impId)?.Object; if (target != null && target.Data.Role != null && !target.Data.Role.IsImpostor) target.RpcSetRole(RoleTypes.Impostor); }
                }
            }
        }

        [HarmonyPatch(typeof(LogicRoleSelectionNormal), "AssignRolesForTeam")]
        public static class RoleSelectionNormal_Patch
        {
            public static bool Prefix(Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> players, IGameOptions opts, RoleTeamTypes team, ref int teamMax)
            {
                if (!ElysiumModMenuGUI.enablePreGameRoleForce || !AmongUsClient.Instance.AmHost) return true;
                try
                {
                    if ((int)team == 1)
                    {
                        int numImps = opts.GetInt((Int32OptionNames)1);
                        var impRoleTypes = new HashSet<int> { 1, 5, 9, 18 };
                        List<byte> allForced = new List<byte>(ElysiumModMenuGUI.forcedImpostors);
                        foreach (var kvp in ElysiumModMenuGUI.forcedPreGameRoles) if (impRoleTypes.Contains((int)kvp.Value) && !allForced.Contains(kvp.Key)) allForced.Add(kvp.Key);
                        if (allForced.Count > 0) numImps = allForced.Count;
                        else { if (numImps >= players.Count) numImps = players.Count - 1; if (numImps < 1) numImps = 1; }
                        int assigned = 0;
                        foreach (byte impId in allForced)
                        {
                            if (players.Count == 0 || assigned >= numImps) break;
                            var targetInfo = players.ToArray().FirstOrDefault(p => p.PlayerId == impId);
                            if (targetInfo != null && targetInfo.Object != null)
                            {
                                RoleTypes role = ElysiumModMenuGUI.forcedPreGameRoles.ContainsKey(impId) ? ElysiumModMenuGUI.forcedPreGameRoles[impId] : RoleTypes.Impostor;
                                targetInfo.Object.RpcSetRole(role, false);
                                players.Remove(targetInfo);
                                assigned++;
                            }
                        }
                        while (assigned < numImps && players.Count > 0)
                        {
                            int idx = UnityEngine.Random.Range(0, players.Count);
                            players[idx].Object.RpcSetRole(RoleTypes.Impostor, false);
                            players.RemoveAt(idx);
                            assigned++;
                        }
                        return false;
                    }
                    else if ((int)team == 0)
                    {
                        var crewRoleTypes = new HashSet<int> { 0, 2, 3, 4, 8, 10, 12 };
                        for (int i = players.Count - 1; i >= 0; i--)
                        {
                            var p = players[i];
                            if (p != null && p.Object != null)
                            {
                                RoleTypes role = RoleTypes.Crewmate;
                                if (ElysiumModMenuGUI.forcedPreGameRoles.ContainsKey(p.PlayerId) && crewRoleTypes.Contains((int)ElysiumModMenuGUI.forcedPreGameRoles[p.PlayerId]))
                                    role = ElysiumModMenuGUI.forcedPreGameRoles[p.PlayerId];
                                p.Object.RpcSetRole(role, false);
                                players.RemoveAt(i);
                            }
                        }
                        return false;
                    }
                    return true;
                }
                catch { return true; }
            }
        }

        [HarmonyPatch(typeof(LogicRoleSelectionHnS), "AssignRolesForTeam")]
        public static class RoleSelectionHnS_Patch
        {
            public static bool Prefix(Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> players, IGameOptions opts, RoleTeamTypes team, ref int teamMax)
            {
                if (!ElysiumModMenuGUI.enablePreGameRoleForce || !AmongUsClient.Instance.AmHost) return true;
                if ((int)team != 1) return true;
                try
                {
                    int numImps = opts.GetInt((Int32OptionNames)1);
                    var impRoleTypes = new HashSet<int> { 1, 5, 9, 18 };
                    List<byte> allForced = new List<byte>(ElysiumModMenuGUI.forcedImpostors);
                    foreach (var kvp in ElysiumModMenuGUI.forcedPreGameRoles) if (impRoleTypes.Contains((int)kvp.Value) && !allForced.Contains(kvp.Key)) allForced.Add(kvp.Key);
                    if (allForced.Count > 0) numImps = allForced.Count;
                    else { if (numImps >= players.Count) numImps = players.Count - 1; if (numImps < 1) numImps = 1; }
                    int assigned = 0;
                    foreach (byte impId in allForced)
                    {
                        if (players.Count == 0 || assigned >= numImps) break;
                        var targetInfo = players.ToArray().FirstOrDefault(p => p.PlayerId == impId);
                        if (targetInfo != null) { targetInfo.Object.RpcSetRole((RoleTypes)1, false); players.Remove(targetInfo); assigned++; }
                    }
                    while (assigned < numImps && players.Count > 0)
                    {
                        int idx = UnityEngine.Random.Range(0, players.Count);
                        players[idx].Object.RpcSetRole((RoleTypes)1, false);
                        players.RemoveAt(idx);
                        assigned++;
                    }
                    return false;
                }
                catch { return true; }
            }
        }

        [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
        public static class RoleManager_SelectRoles_Patch
        {
            public static bool Prefix(RoleManager __instance)
            {
                if (!ElysiumModMenuGUI.enablePreGameRoleForce || !AmongUsClient.Instance.AmHost) return true;
                try
                {
                    var allPlayers = PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.Data != null && !p.Data.Disconnected && !p.Data.IsDead).ToList();
                    int numImps = 1;
                    try { numImps = GameOptionsManager.Instance.CurrentGameOptions.GetInt((Int32OptionNames)1); } catch { }
                    var impRoleTypes = new HashSet<int> { 1, 5, 9, 18 };
                    List<PlayerControl> impostors = new List<PlayerControl>();
                    foreach (var p in allPlayers)
                        if (ElysiumModMenuGUI.forcedImpostors.Contains(p.PlayerId) || (ElysiumModMenuGUI.forcedPreGameRoles.ContainsKey(p.PlayerId) && impRoleTypes.Contains((int)ElysiumModMenuGUI.forcedPreGameRoles[p.PlayerId])))
                            impostors.Add(p);
                    if (impostors.Count > 0) numImps = impostors.Count;
                    else { if (numImps >= allPlayers.Count) numImps = allPlayers.Count - 1; if (numImps < 1) numImps = 1; }
                    System.Random rand = new System.Random();
                    while (impostors.Count < numImps && allPlayers.Count > impostors.Count)
                    {
                        var available = allPlayers.Where(p => !impostors.Contains(p)).ToList();
                        impostors.Add(available[rand.Next(available.Count)]);
                    }
                    List<PlayerControl> crewmates = allPlayers.Where(p => !impostors.Contains(p)).ToList();
                    var impData = new Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo>();
                    foreach (var i in impostors) impData.Add(i.Data);
                    var crewData = new Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo>();
                    foreach (var c in crewmates) crewData.Add(c.Data);
                    IGameOptions opts = GameOptionsManager.Instance.CurrentGameOptions;
                    GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(impData, opts, (RoleTeamTypes)1, int.MaxValue, new Il2CppSystem.Nullable<RoleTypes>());
                    GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(crewData, opts, (RoleTeamTypes)0, int.MaxValue, new Il2CppSystem.Nullable<RoleTypes>((RoleTypes)0));
                    foreach (var kvp in ElysiumModMenuGUI.forcedPreGameRoles)
                    {
                        if (kvp.Value != RoleTypes.Crewmate && kvp.Value != RoleTypes.Impostor)
                        {
                            var pc = allPlayers.FirstOrDefault(p => p.PlayerId == kvp.Key);
                            if (pc != null) RoleManager.Instance.SetRole(pc, kvp.Value);
                        }
                    }
                    foreach (var pc in allPlayers) if (pc.Data.Role != null) pc.Data.Role.Initialize(pc);
                    return false;
                }
                catch { return true; }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TurnOnProtection))]
        public static class PlayerControl_TurnOnProtection_Patch
        {
            public static void Prefix(ref bool visible)
            {
                if (ElysiumModMenuGUI.seeGhosts || ElysiumModMenuGUI.seeProtections) visible = true;
            }
        }

        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.LateUpdate))]
        public static class PlayerVisuals_LateUpdate_Patch
        {
            public static void Postfix(PlayerPhysics __instance)
            {
                if (__instance == null || __instance.myPlayer == null || __instance.myPlayer.Data == null) return;
                try
                {
                    if (ElysiumModMenuGUI.seeGhosts && __instance.myPlayer.Data.IsDead && PlayerControl.LocalPlayer != null && !PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        __instance.myPlayer.Visible = true;
                        var rend = __instance.myPlayer.GetComponent<SpriteRenderer>();
                        if (rend != null) { Color c = rend.color; rend.color = new Color(c.r, c.g, c.b, 0.4f); }
                    }
                    var cosmetics = __instance.myPlayer.cosmetics;
                    var outfit = __instance.myPlayer.CurrentOutfit;
                    if (cosmetics != null && cosmetics.nameText != null && outfit != null)
                    {
                        cosmetics.SetName(ElysiumModMenuGUI.GetESPNameTag(__instance.myPlayer.Data, outfit.PlayerName));
                        if (ElysiumModMenuGUI.seeRoles && ElysiumModMenuGUI.showPlayerInfo) cosmetics.nameText.transform.localPosition = new Vector3(0f, 0.186f, 0f);
                        else if (ElysiumModMenuGUI.seeRoles || ElysiumModMenuGUI.showPlayerInfo) cosmetics.nameText.transform.localPosition = new Vector3(0f, 0.093f, 0f);
                        else cosmetics.nameText.transform.localPosition = new Vector3(0f, 0f, 0f);
                    }
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
        public static class ESP_MeetingHud
        {
            public static void Postfix(MeetingHud __instance)
            {
                try
                {
                    if (__instance.playerStates == null) return;
                    foreach (var state in __instance.playerStates)
                    {
                        if (state == null) continue;
                        var data = GameData.Instance.GetPlayerById(state.TargetPlayerId);
                        if (data != null && !data.Disconnected && data.DefaultOutfit != null && state.NameText != null)
                        {
                            string espName = ElysiumModMenuGUI.GetESPNameTag(data, data.DefaultOutfit.PlayerName ?? "???");
                            if (!ElysiumModMenuGUI.seeRoles && ElysiumModMenuGUI.revealMeetingRoles && data.Role != null)
                            {
                                string roleName = data.Role.Role.ToString();
                                int roleId = (int)data.Role.Role;
                                if (roleId == 8) roleName = "Noisemaker";
                                else if (roleId == 9) roleName = "Phantom";
                                else if (roleId == 10) roleName = "Tracker";
                                else if (roleId == 12) roleName = "Detective";
                                else if (roleId == 18) roleName = "Viper";
                                else if (roleName == "GuardianAngel") roleName = "Guardian Angel";
                                Color customColor = ElysiumModMenuGUI.GetRoleColor(roleId, data.Role.TeamColor);
                                string roleColor = ColorUtility.ToHtmlStringRGB(customColor);
                                espName = $"<color=#{roleColor}>{roleName}</color>\n{espName}";
                            }
                            state.NameText.text = espName;
                            bool showingExtra = ElysiumModMenuGUI.seeRoles || ElysiumModMenuGUI.revealMeetingRoles;
                            if (showingExtra && ElysiumModMenuGUI.showPlayerInfo) { state.NameText.transform.localPosition = new Vector3(0.33f, 0.08f, 0f); state.NameText.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f); }
                            else if (showingExtra || ElysiumModMenuGUI.showPlayerInfo) { state.NameText.transform.localPosition = new Vector3(0.3384f, 0.1125f, -0.1f); state.NameText.transform.localScale = new Vector3(0.9f, 1f, 1f); }
                            else { state.NameText.transform.localPosition = new Vector3(0.3384f, 0.0311f, -0.1f); state.NameText.transform.localScale = new Vector3(0.9f, 1f, 1f); }
                        }
                    }
                }
                catch { }
            }
        }
        [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
        public static class ChatBubble_SetName_Patch
        {
            public static void Postfix(ChatBubble __instance)
            {
                if (!ElysiumModMenuGUI.showPlayerInfo || __instance.playerInfo == null) return;
                try
                {
                    string accentHex = ColorUtility.ToHtmlStringRGB(ElysiumModMenuGUI.currentAccentColor);
                    string espLine = ElysiumModMenuGUI.BuildESPInfoLine(__instance.playerInfo);
                    if (string.IsNullOrWhiteSpace(espLine)) return;
                    string extra = $" <color=#{accentHex}><size=80%>{espLine}</size></color>";

                    if (!__instance.NameText.text.Contains("Lv:")) __instance.NameText.text += extra;
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
        public static class KillCooldownTrackerPatch
        {
            public static void Prefix(PlayerControl __instance, PlayerControl target, bool didSucceed)
            {
                try
                {
                    if (!didSucceed || __instance == null || __instance.Data == null) return;
                    ElysiumModMenuGUI.lastKillTimestamps[__instance.PlayerId] = Time.time;
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        public static class FullBright_Patch
        {
            public static void Postfix(HudManager __instance)
            {
                try
                {
                    if (__instance == null || __instance.ShadowQuad == null || __instance.ShadowQuad.gameObject == null) return;
                    __instance.ShadowQuad.gameObject.SetActive(!ElysiumModMenuGUI.fullBright);
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
        public static class HudManager_Update_Patch
        {
            public static void Postfix(HudManager __instance)
            {
                try
                {
                    if (ElysiumModMenuGUI.alwaysChat && __instance.Chat != null)
                        __instance.Chat.gameObject.SetActive(true);
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(PlatformSpecificData), nameof(PlatformSpecificData.Serialize))]
        public static class PlatformSpooferPatch { public static void Prefix(PlatformSpecificData __instance) { try { if (ElysiumModMenuGUI.enablePlatformSpoof && __instance != null) __instance.Platform = ElysiumModMenuGUI.platformValues[ElysiumModMenuGUI.currentPlatformIndex]; } catch { } } }

        [HarmonyPatch(typeof(FullAccount), nameof(FullAccount.CanSetCustomName))]
        public static class FullAccount_CanSetCustomName_Patch { public static void Prefix(ref bool canSetName) { try { if (ElysiumModMenuGUI.unlockFeatures) canSetName = true; } catch { } } }

        [HarmonyPatch(typeof(AccountManager), nameof(AccountManager.CanPlayOnline))]
        public static class AccountManager_CanPlayOnline_Patch { public static void Postfix(ref bool __result) { try { if (ElysiumModMenuGUI.unlockFeatures) __result = true; } catch { } } }

        [HarmonyPatch(typeof(EngineerRole), "FixedUpdate")]
        public static class EngineerCheatsPatch
        {
            public static void Postfix(EngineerRole __instance)
            {
                if (__instance.Player != PlayerControl.LocalPlayer) return;
                if (ElysiumModMenuGUI.endlessVentTime) __instance.inVentTimeRemaining = float.MaxValue;
                if (ElysiumModMenuGUI.noVentCooldown && __instance.cooldownSecondsRemaining > 0f)
                {
                    __instance.cooldownSecondsRemaining = 0f;
                    var btn = DestroyableSingleton<HudManager>.Instance?.AbilityButton;
                    if (btn != null) { btn.ResetCoolDown(); btn.SetCooldownFill(0f); }
                }
            }
        }

        private static bool TrySetCooldownMember(object target, float value)
        {
            if (target == null) return false;

            string[] names = { "CoolDown", "_CoolDown_k__BackingField", "<CoolDown>k__BackingField", "coolDown", "cooldown" };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            try
            {
                Type type = target.GetType();
                foreach (string name in names)
                {
                    PropertyInfo property = type.GetProperty(name, flags);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(target, value, null);
                        return true;
                    }

                    FieldInfo field = type.GetField(name, flags);
                    if (field != null)
                    {
                        field.SetValue(target, value);
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        [HarmonyPatch(typeof(Ladder), "SetDestinationCooldown")]
        public static class Ladder_SetDestinationCooldown_Patch
        {
            public static bool Prefix(Ladder __instance)
            {
                try
                {
                    if (!ElysiumModMenuGUI.noMapCooldowns) return true;
                    TrySetCooldownMember(__instance, 0f);
                    return false;
                }
                catch { return true; }
            }
        }

        [HarmonyPatch(typeof(ZiplineConsole), "Update")]
        public static class ZiplineConsole_Update_Patch
        {
            public static void Postfix(ZiplineConsole __instance)
            {
                try
                {
                    if (!ElysiumModMenuGUI.noMapCooldowns) return;
                    TrySetCooldownMember(__instance, 0f);
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), "MurderPlayer")]
        public static class KillCooldownTrackerPatch2
        {
            public static void Prefix(PlayerControl __instance, PlayerControl target)
            {
                try
                {
                    if (__instance == null || __instance.Data == null) return;
                    ElysiumModMenuGUI.lastKillTimestamps[__instance.PlayerId] = Time.time;

                    if (!ElysiumModMenuGUI.spamReportBodies) return;
                    if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null || PlayerControl.LocalPlayer.Data.IsDead) return;
                    if (target == null || target.Data == null || !target.Data.IsDead) return;

                    PlayerControl.LocalPlayer.CmdReportDeadBody(target.Data);
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetKillTimer))]
        public static class KillAuraNoKillCooldownPatch
        {
            public static void Prefix(PlayerControl __instance, ref float time)
            {
                try
                {
                    if (!ElysiumModMenuGUI.noKillCooldownHostOnly) return;
                    if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
                    if (__instance != PlayerControl.LocalPlayer) return;
                    time = 0f;
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(ScientistRole), "Update")]
        public static class ScientistCheatsPatch
        {
            public static void Postfix(ScientistRole __instance)
            {
                if (__instance.Player != PlayerControl.LocalPlayer) return;
                if (ElysiumModMenuGUI.noVitalsCooldown) __instance.currentCooldown = 0f;
                if (ElysiumModMenuGUI.endlessBattery) __instance.currentCharge = float.MaxValue;
            }
        }

        [HarmonyPatch(typeof(ShapeshifterRole), "FixedUpdate")]
        public static class ShapeshifterDurationPatch
        {
            public static void Postfix(ShapeshifterRole __instance) { if (__instance.Player == PlayerControl.LocalPlayer && ElysiumModMenuGUI.endlessSsDuration) __instance.durationSecondsRemaining = float.MaxValue; }
        }

        [HarmonyPatch(typeof(ImpostorRole), "FindClosestTarget")]
        public static class ImpostorRangePatch
        {
            public static bool Prefix(ImpostorRole __instance, ref PlayerControl __result)
            {
                if (!ElysiumModMenuGUI.killReach) return true;
                try
                {
                    var target = PlayerControl.AllPlayerControls.ToArray()
                        .Where(p => p != null && __instance.IsValidTarget(p.Data) && !p.Data.IsDead && !p.Data.Disconnected)
                        .OrderBy(p => Vector2.Distance(p.transform.position, PlayerControl.LocalPlayer.transform.position))
                        .FirstOrDefault();
                    if (target != null) __result = target;
                    return false;
                }
                catch { return true; }
            }
        }

        [HarmonyPatch(typeof(ImpostorRole), "IsValidTarget")]
        public static class ImpostorKillAnyonePatch
        {
            public static void Postfix(NetworkedPlayerInfo target, ref bool __result) { try { if (ElysiumModMenuGUI.killAnyone && target != null && target.PlayerId != PlayerControl.LocalPlayer.PlayerId && !target.IsDead) __result = true; } catch { } }
        }

        private void teleportToPlayer(PlayerControl t)
        {
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.NetTransform == null || t == null) return;
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(t.transform.position);
        }

        [HarmonyPatch(typeof(DetectiveRole), "FindClosestTarget")]
        public static class DetectiveRangePatch
        {
            public static bool Prefix(DetectiveRole __instance, ref PlayerControl __result)
            {
                if (!ElysiumModMenuGUI.UnlimitedInterrogateRange) return true;
                try
                {
                    var target = PlayerControl.AllPlayerControls.ToArray()
                        .Where(p => p != null && __instance.IsValidTarget(p.Data) && !p.Data.IsDead && !p.Data.Disconnected)
                        .OrderBy(p => Vector2.Distance(p.transform.position, PlayerControl.LocalPlayer.transform.position))
                        .FirstOrDefault();
                    if (target != null) __result = target;
                    return false;
                }
                catch { return true; }
            }
        }

        [HarmonyPatch(typeof(DoorBreakerGame), nameof(DoorBreakerGame.Start))]
        public static class DoorBreakerGame_Start_Patch
        {
            public static bool Prefix(DoorBreakerGame __instance)
            {
                if (!ElysiumModMenuGUI.autoOpenDoors) return true;
                try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(__instance.MyDoor.Id | 64)); } catch { }
                __instance.MyDoor.SetDoorway(true); __instance.Close();
                return false;
            }
        }
        [HarmonyPatch(typeof(DoorCardSwipeGame), nameof(DoorCardSwipeGame.Begin))]
        public static class DoorCardSwipeGame_Begin_Patch
        {
            public static bool Prefix(DoorCardSwipeGame __instance)
            {
                if (!ElysiumModMenuGUI.autoOpenDoors) return true;
                try { ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, (byte)(__instance.MyDoor.Id | 64)); } catch { }
                __instance.MyDoor.SetDoorway(true); __instance.Close();
                return false;
            }
        }
        [HarmonyPatch(typeof(MushroomDoorSabotageMinigame), nameof(MushroomDoorSabotageMinigame.Begin))]
        public static class MushroomDoorSabotageMinigame_Begin_Patch
        {
            public static bool Prefix(MushroomDoorSabotageMinigame __instance) { if (ElysiumModMenuGUI.autoOpenDoors) { __instance.FixDoorAndCloseMinigame(); return false; } return true; }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetTasks))]
        public static class NoTaskMode_Patch { public static bool Prefix(PlayerControl __instance) { if (ElysiumModMenuGUI.noTaskMode) return false; return true; } }
        [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
        public static class ChatController_SendChat_Patch
        {
            public static bool Prefix(ChatController __instance)
            {
                if (__instance.freeChatField == null || __instance.freeChatField.textArea == null) return true;
                string text = __instance.freeChatField.textArea.text;
                if (string.IsNullOrWhiteSpace(text)) return true;

                if (ElysiumModMenuGUI.enableChatHistory)
                {
                    ChatHistory.Remember(text);
                }

                ElysiumModMenuGUI.TrySpellCheckNotify(text);

                string lowerChat = text.ToLower().Trim();

                if (ElysiumModMenuGUI.enableColorCommand)
                {
                    if (lowerChat == "/rainbow" || lowerChat == "!rainbow" || lowerChat == "/lgbt" || lowerChat == "!lgbt")
                    {
                        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                        {
                            if (ElysiumModMenuGUI.rainbowPlayers.Contains(PlayerControl.LocalPlayer.PlayerId))
                            {
                                ElysiumModMenuGUI.rainbowPlayers.Remove(PlayerControl.LocalPlayer.PlayerId);
                                ElysiumModMenuGUI.ShowNotification("<color=#FF00FF>[SERVER]</color> Ваша радуга ВЫКЛ.");
                            }
                            else
                            {
                                ElysiumModMenuGUI.rainbowPlayers.Add(PlayerControl.LocalPlayer.PlayerId);
                                ElysiumModMenuGUI.ShowNotification("<color=#FF00FF>[SERVER]</color> Ваша радуга ВКЛ.");
                            }
                        }
                        else
                        {
                            if (HudManager.Instance?.Chat != null)
                                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Эта команда только для Хоста!");
                        }
                        __instance.freeChatField.textArea.SetText("", "");
                        return false;
                    }

                    if (lowerChat.StartsWith("/color ") || lowerChat.StartsWith("/c ") || lowerChat.StartsWith("/col ") ||
                        lowerChat.StartsWith("!color ") || lowerChat.StartsWith("!c ") || lowerChat.StartsWith("!col "))
                    {
                        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                        {
                            string arg = lowerChat.Substring(lowerChat.IndexOf(' ') + 1).Trim();
                            int colorId = -1;

                            if (int.TryParse(arg, out int parsed)) colorId = parsed;
                            else colorId = ElysiumModMenuGUI.GetColorIdByName(arg);

                            if (colorId >= 0 && colorId <= 18 && PlayerControl.LocalPlayer != null)
                            {
                                PlayerControl.LocalPlayer.RpcSetColor((byte)colorId);
                            }
                            else if (HudManager.Instance?.Chat != null)
                            {
                                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Используйте ID (0-18) или названия (красн, син, зел...)");
                            }
                        }
                        else
                        {
                            if (HudManager.Instance?.Chat != null)
                                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Смена цвета доступна только Хосту!");
                        }
                        __instance.freeChatField.textArea.SetText("", "");
                        return false;
                    }
                }

                if (lowerChat.StartsWith("/w ") || lowerChat.StartsWith("/pm ") ||
                 lowerChat.StartsWith("/msg ") || lowerChat.StartsWith("/am "))
                {
                    string[] parts = text.Split(new char[] { ' ' }, 3);
                    if (parts.Length >= 3)
                    {

                        string targetInput = parts[1].ToLower().Trim();
                        string message = parts[2];
                        PlayerControl target = null;

                        if (byte.TryParse(targetInput, out byte pid))
                        {
                            target = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.PlayerId == pid);
                        }

                        if (target == null && PlayerControl.AllPlayerControls != null)
                        {
                            PlayerControl exactMatch = null;
                            PlayerControl partialMatch = null;

                            foreach (var pc in PlayerControl.AllPlayerControls)
                            {
                                if (pc == null || pc.Data == null || pc.Data.Disconnected || pc == PlayerControl.LocalPlayer) continue;

                                string rawName = Regex.Replace(pc.Data.PlayerName, "<.*?>", string.Empty).ToLower().Trim();
                                int cId = (int)pc.Data.DefaultOutfit.ColorId;
                                int targetColorId = ElysiumModMenuGUI.GetColorIdByName(targetInput);

                                if (rawName == targetInput || (targetColorId != -1 && cId == targetColorId))
                                {
                                    exactMatch = pc;
                                    break;
                                }
                                if (rawName.StartsWith(targetInput))
                                {
                                    if (partialMatch == null) partialMatch = pc;
                                }
                            }
                            target = exactMatch ?? partialMatch;
                        }

                        if (target != null && target != PlayerControl.LocalPlayer)
                        {
                            string safeMessage = Regex.Replace(message, "<.*?>", string.Empty).Replace("<", "").Replace(">", "");
                            string networkMsg = $"шепчет вам:\n{safeMessage}";

                            if (AmongUsClient.Instance != null && PlayerControl.LocalPlayer != null)
                            {
                                MessageWriter msgWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 13, Hazel.SendOption.Reliable, target.OwnerId);
                                msgWriter.Write(networkMsg);
                                AmongUsClient.Instance.FinishRpcImmediately(msgWriter);
                            }

                            string targetClean = Regex.Replace(target.Data.PlayerName, "<.*?>", string.Empty);
                            if (HudManager.Instance?.Chat != null)
                                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"<color=#FFAC1C>Вы шепчете {targetClean}:\n{safeMessage}</color>");
                        }
                        else if (HudManager.Instance?.Chat != null)
                        {
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Игрок не найден! Введите ID, Цвет или Имя.");
                        }
                    }
                    __instance.freeChatField.textArea.SetText("", "");
                    return false;
                }

                return true;
            }
        }

        public static void Postfix(GameStartManager __instance)
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.LocalPlayer == null) return;
            if (ElysiumModMenuGUI.customStartTimer > 0f) return;

            if (ElysiumModMenuGUI.fakeStartCounterTroll)
            {
                try
                {
                    sbyte[] arr = { -123, -100, -69, -42, 0, 42, 69, 100, 123 };
                    sbyte b = arr[UnityEngine.Random.Range(0, arr.Length)];
                    PlayerControl.LocalPlayer.RpcSetStartCounter((int)b);
                    __instance.SetStartCounter(b);
                }
                catch { }
            }
            else if (ElysiumModMenuGUI.fakeStartCounterCustom && int.TryParse(ElysiumModMenuGUI.fakeStartInput, out int custom))
            {
                try
                {
                    PlayerControl.LocalPlayer.RpcSetStartCounter(custom);
                    __instance.SetStartCounter((sbyte)Mathf.Clamp(custom, -128, 127));
                }
                catch { }
            }
        }
    }
}


[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
public static class ChatController_Update_Patch
{
    public static void Postfix(ChatController __instance)
    {
        try
        {
            if (!ElysiumModMenuGUI.enableChatDarkMode) return;

            if (__instance.freeChatField != null && __instance.freeChatField.background != null)
            {
                __instance.freeChatField.background.color = new Color32(40, 40, 40, byte.MaxValue);
                if (__instance.freeChatField.textArea != null && __instance.freeChatField.textArea.outputText != null)
                    __instance.freeChatField.textArea.outputText.color = Color.white;
            }
            if (__instance.quickChatField != null && __instance.quickChatField.background != null)
            {
                __instance.quickChatField.background.color = new Color32(40, 40, 40, byte.MaxValue);
                if (__instance.quickChatField.text != null)
                    __instance.quickChatField.text.color = Color.white;
            }
        }
        catch { }
    }
}

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetText))]
public static class DarkMode_ChatBubblePatch
{
    public static void Postfix(ChatBubble __instance)
    {
        try
        {
            if (!ElysiumModMenuGUI.enableChatDarkMode) return;

            Transform bg = __instance.transform.Find("Background");
            if (bg != null)
            {
                var sr = bg.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color32(35, 35, 35, 255);
            }
            if (__instance.TextArea != null)
                __instance.TextArea.color = Color.white;
        }
        catch { }
    }
}

[HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
public static class GameManager_CheckTaskCompletion_Patch
{
    public static bool Prefix(ref bool __result)
    {
        try
        {
            if (!ElysiumModMenuGUI.neverEndGame) return true;
            __result = false; return false;
        }
        catch { return true; }
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SetVisible))]
public static class ChatController_SetVisible_Patch
{
    public static void Prefix(ref bool visible)
    {
        if (ElysiumModMenuGUI.alwaysChat) visible = true;
    }
}

[HarmonyPatch(typeof(MeetingHud), "Update")]
public static class RevealVotesPatch
{
    internal static List<int> _votedPlayers = new List<int>();
    public static void Prefix(MeetingHud __instance)
    {
        if (!ElysiumModMenuGUI.RevealVotesEnabled) return;
        try
        {
            if ((int)__instance.state >= 4) return;
            foreach (var item in __instance.playerStates)
            {
                if (item == null) continue;
                var playerById = GameData.Instance.GetPlayerById(item.TargetPlayerId);
                if (playerById == null || playerById.Disconnected || item.VotedFor == PlayerVoteArea.HasNotVoted ||
                    item.VotedFor == PlayerVoteArea.MissedVote || item.VotedFor == PlayerVoteArea.DeadVote || _votedPlayers.Contains(item.TargetPlayerId)) continue;
                _votedPlayers.Add(item.TargetPlayerId);
                if (item.VotedFor != PlayerVoteArea.SkippedVote)
                {
                    foreach (var item2 in __instance.playerStates) if (item2.TargetPlayerId == item.VotedFor) { __instance.BloopAVoteIcon(playerById, 0, item2.transform); break; }
                }
                else if (__instance.SkippedVoting != null) __instance.BloopAVoteIcon(playerById, 0, __instance.SkippedVoting.transform);
            }
            foreach (var item3 in __instance.playerStates)
            {
                if (item3 == null) continue;
                var component = item3.transform.GetComponent<VoteSpreader>();
                if (component != null) foreach (var sprite in component.Votes) sprite.gameObject.SetActive(true);
            }
            if (__instance.SkippedVoting != null) __instance.SkippedVoting.SetActive(true);
        }
        catch { }
    }
}
[HarmonyPatch(typeof(MeetingHud), "PopulateResults")]
public static class RevealVotesCleanupPatch
{
    public static void Prefix(MeetingHud __instance)
    {
        if (!ElysiumModMenuGUI.RevealVotesEnabled) return;
        try
        {
            foreach (var item in __instance.playerStates)
            {
                if (item == null) continue;
                var component = item.transform.GetComponent<VoteSpreader>();
                if (component != null && component.Votes.Count != 0)
                {
                    foreach (var sprite in component.Votes) Object.DestroyImmediate(sprite.gameObject);
                    component.Votes.Clear();
                }
            }
            RevealVotesPatch._votedPlayers.Clear();
        }
        catch { }
    }
}

[HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Increase))]
public static class NumberOption_Increase_Patch
{
    public static bool Prefix(NumberOption __instance)
    {
        try
        {
            if (!ElysiumModMenuGUI.noSettingLimit) return true;
            if (GameOptionsManager.Instance.CurrentGameOptions.GameMode != GameModes.HideNSeek &&
                (__instance.Title == StringNames.GameNumImpostors || __instance.Title == StringNames.GamePlayerSpeed))
                return true;
            __instance.Value += __instance.Increment;
            __instance.UpdateValue();
            __instance.OnValueChanged.Invoke(__instance);
            __instance.AdjustButtonsActiveState();
            return false;
        }
        catch { return true; }
    }
}

[HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Decrease))]
public static class NumberOption_Decrease_Patch
{
    public static bool Prefix(NumberOption __instance)
    {
        try
        {
            if (!ElysiumModMenuGUI.noSettingLimit) return true;
            if (GameOptionsManager.Instance.CurrentGameOptions.GameMode != GameModes.HideNSeek &&
                (__instance.Title == StringNames.GameNumImpostors || __instance.Title == StringNames.GamePlayerSpeed))
                return true;
            __instance.Value -= __instance.Increment;
            __instance.UpdateValue();
            __instance.OnValueChanged.Invoke(__instance);
            __instance.AdjustButtonsActiveState();
            return false;
        }
        catch { return true; }
    }
}

[HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Initialize))]
public static class NumberOption_Initialize_Patch
{
    public static void Postfix(NumberOption __instance)
    {
        try
        {
            if (!ElysiumModMenuGUI.noSettingLimit) return;
            if (GameOptionsManager.Instance.CurrentGameOptions.GameMode != GameModes.HideNSeek &&
                (__instance.Title == StringNames.GameNumImpostors || __instance.Title == StringNames.GamePlayerSpeed))
                return;
            __instance.ValidRange = new FloatRange(-999f, 999f);
        }
        catch { }
    }
}

[HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
public static class IGameOptionsExtensions_GetAdjustedNumImpostors_Patch
{
    public static bool Prefix(IGameOptions __instance, ref int __result)
    {
        try
        {
            if (!ElysiumModMenuGUI.noSettingLimit) return true;
            __result = GameOptionsManager.Instance.CurrentGameOptions.NumImpostors;
            return false;
        }
        catch { return true; }
    }
}

[HarmonyPatch(typeof(FindAGameManager), nameof(FindAGameManager.Start))]
public static class ExtendedLobbyListPatch
{
    public static Scroller scroller;

    public static bool Prefix(FindAGameManager __instance)
    {
        if (!ElysiumModMenuGUI.extendedLobby) return true;
        try
        {
            if (__instance.gameContainers == null || __instance.gameContainers.Count == 0) return true;
            if (__instance.gameContainers.Count > 10) return true;

            GameContainer prefab = __instance.gameContainers[0];
            GameObject holder = new GameObject("ExtendedLobbyScroller");
            holder.transform.SetParent(prefab.transform.parent);

            scroller = holder.AddComponent<Scroller>();
            scroller.Inner = holder.transform;
            scroller.MouseMustBeOverToScroll = true;
            scroller.allowY = true;
            scroller.ScrollWheelSpeed = 0.4f;
            scroller.SetYBoundsMin(0f);
            scroller.SetYBoundsMax(4f);

            BoxCollider2D collider = prefab.transform.parent.gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(100f, 100f);
            scroller.ClickMask = collider;

            var list = new System.Collections.Generic.List<GameContainer>();
            foreach (var gc in __instance.gameContainers)
            {
                gc.transform.SetParent(holder.transform);
                gc.transform.localPosition = new Vector3(gc.transform.localPosition.x, gc.transform.localPosition.y, 25f);
                list.Add(gc);
            }

            for (int i = 0; i < 15; i++)
            {
                GameContainer newGc = UnityEngine.Object.Instantiate<GameContainer>(prefab, holder.transform);
                newGc.transform.localPosition = new Vector3(newGc.transform.localPosition.x, newGc.transform.localPosition.y - 0.75f * list.Count, 25f);
                list.Add(newGc);
            }

            __instance.gameContainers = new Il2CppReferenceArray<GameContainer>(list.ToArray());
            return true;
        }
        catch { return true; }
    }
}

[HarmonyPatch(typeof(FindAGameManager), nameof(FindAGameManager.RefreshList))]
public static class ExtendedLobbyRefreshPatch
{
    public static void Postfix()
    {
        try { if (ElysiumModMenuGUI.extendedLobby && ExtendedLobbyListPatch.scroller != null) ExtendedLobbyListPatch.scroller.ScrollRelative(new Vector2(0f, -100f)); } catch { }
    }
}


[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.FixedUpdate))]
public static class InvertControls_Patch
{
    private static void SeePlayerVent(PlayerPhysics player)
    {
#pragma warning disable CS8632
        if (GameManager.Instance.IsHideAndSeek() && player.myPlayer.Data.RoleType == RoleTypes.Impostor || player == null ||
            AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Started)
            return;
        if (!SeePlayersInVent)
        {
            if (player.myPlayer.invisibilityAlpha == 0.3f)
            {
                PhantomRole? role = player.myPlayer.Data.Role as PhantomRole;
                if (role != null)
                {
                    player.myPlayer.SetInvisibility(role.isInvisible);
                    return;
                }
                else
                {
                    player.myPlayer.cosmetics.SetPhantomRoleAlpha(1f);
                    player.myPlayer.invisibilityAlpha = 1;
                    if (player.myPlayer.inVent)
                    {
                        player.myPlayer.Visible = false;
                    }
                }
            }
            return;
        }

        if (player.myPlayer.inVent && player.NetId != PlayerControl.LocalPlayer.MyPhysics.NetId)
        {
            player.myPlayer.Visible = true;
            player.myPlayer.invisibilityAlpha = 0.3f;
            player.myPlayer.cosmetics.SetPhantomRoleAlpha(0.3f);
        }
        else
        {
            PhantomRole? role = player.myPlayer.Data.Role as PhantomRole;
            if (role != null)
            {
                player.myPlayer.SetInvisibility(role.isInvisible);
            }
            else
            {
                player.myPlayer.cosmetics.SetPhantomRoleAlpha(1f);
                player.myPlayer.invisibilityAlpha = 1;
            }
        }
    }

    public static void Postfix(PlayerPhysics __instance)
    {
        if (__instance.AmOwner && ElysiumModMenuGUI.invertControls && __instance.body != null)
        {
            __instance.body.velocity = -__instance.body.velocity;
        }

        SeePlayerVent(__instance);
    }
}
[HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
public static class LobbyStart_ApplyLevelSpoof
{
    public static void Postfix()
    {
        if (!ElysiumModMenuGUI.isEditingLevel && uint.TryParse(ElysiumModMenuGUI.spoofLevelString, out uint parsedLvl))
        {
            uint targetLevel = parsedLvl > 0 ? parsedLvl - 1 : 0;
            try { AmongUs.Data.DataManager.Player.stats.level = targetLevel; }
            catch { try { AmongUs.Data.DataManager.Player.Stats.Level = targetLevel; } catch { } }
            AmongUs.Data.DataManager.Player.Save();
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
public static class RPCSniffer_Patch
{
    private static readonly HashSet<byte> VanillaRPCs = ElysiumModMenuGUI.VanillaRpcIds;

    private static readonly Dictionary<byte, (string Name, string Color)> KnownMods = new Dictionary<byte, (string, string)>
        {
            { 157, ("RockStar", "#800000") },
            { 121, ("RockStar / Chocoo", "#800000") },
            { 167, ("TuffMenu", "#008000") },
            { 164, ("Hydra / Sicko", "#FF0000") },
            { 176, ("HostGuard / TOH", "#008000") },
            { 195, ("Polar Client", "#FFFF00") },
            { 204, ("Polar Client", "#FFFF00") },
            { 154, ("GNC", "#FF0000") },
            { 85,  ("KillNet (Base)", "#FF0000") },
            { 150, ("KillNet (V2)", "#FF0000") },
            { 162, ("KNM", "#FF0000") },
            { 250, ("KillNet (Alt)", "#FF0000") },
            { 212, ("BanMod", "#008000") },
            { 213, ("BanMod", "#008000") },
            { 214, ("BanMod", "#008000") },
            { 215, ("BanMod", "#008000") },
            { 216, ("BanMod", "#008000") },
            { 217, ("BanMod", "#008000") },
            { 218, ("BanMod", "#008000") },
            { 219, ("BanMod", "#008000") },
            { 144, ("Gaff Menu", "#FF0000") },
            { 145, ("Gaff Menu", "#FF0000") },
            { 188, ("GMM", "#FF0000") },
            { 189, ("GMM", "#FF0000") },
            { 169, ("Malum", "#FF0000") },
            { 210, ("Eclipse", "#FFFF00") },
            { 173, ("Private Client", "#FF0000") },
            { 151, ("Better Among Us", "#008000") },
            { 152, ("Better Among Us", "#008000") },
            { 255, ("CrewMod", "#FFFF00") },
            { 111, ("AUM (BitCrackers)", "#FF0000") },
            { 231, ("SentinelAU", "#FF0000") },
            { 133, ("Lunar / ElysiumModMenu", "#00FFFF") },
            { 89,  ("ElysiumModMenu Old", "#008000") }
        };

    public static bool Prefix(PlayerControl __instance, byte callId, MessageReader reader)
    {
        if (__instance == null) return true;


        if (PlayerControl.LocalPlayer != null && __instance == PlayerControl.LocalPlayer) return true;

        ElysiumModMenuGUI.RecordPlayerRpc(__instance, callId);

        if (ElysiumModMenuGUI.LogAllRPCs)
        {

            if (!VanillaRPCs.Contains(callId))
            {
                string pNameSniff = (__instance.Data != null && !string.IsNullOrEmpty(__instance.Data.PlayerName)) ? __instance.Data.PlayerName : $"Player_{__instance.PlayerId}";


                if (KnownMods.TryGetValue(callId, out var modInfo))
                {
                    ElysiumModMenuGUI.ShowNotification($"<color=#00FFFF>[СНИФФЕР]</color> <b>{pNameSniff}</b>: <b><color={modInfo.Color}>{modInfo.Name}</color></b> <color=#FFFF00>({callId})</color>");
                }
                else
                {
                    ElysiumModMenuGUI.ShowNotification($"<color=#00FFFF>[СНИФФЕР]</color> <b>{pNameSniff}</b> кинул неизвестный RPC: <color=#FFFF00>{callId}</color>");
                }
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(HatManager), nameof(HatManager.Initialize))]
public static class UnlockCosmetics_HatManager_Initialize_Postfix
{
    public static void Postfix(HatManager __instance)
    {
        if (!ElysiumModMenuGUI.unlockCosmetics) return;

        foreach (var bundle in __instance.allBundles) bundle.Free = true;
        foreach (var hat in __instance.allHats) hat.Free = true;
        foreach (var nameplate in __instance.allNamePlates) nameplate.Free = true;
        foreach (var pet in __instance.allPets) pet.Free = true;
        foreach (var skin in __instance.allSkins) skin.Free = true;
        foreach (var visor in __instance.allVisors) visor.Free = true;
        foreach (var starBundle in __instance.allStarBundles) starBundle.price = 0;
    }
}

[HarmonyPatch(typeof(PlayerPurchasesData), nameof(PlayerPurchasesData.GetPurchase))]
public static class UnlockCosmetics_PlayerPurchasesData_GetPurchase_Prefix
{
    public static bool Prefix(ref bool __result)
    {
        if (!ElysiumModMenuGUI.unlockCosmetics) return true;
        __result = true;
        return false;
    }
}
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
public static class AutoChatEveryone_Start_Patch
{
    public static void Postfix()
    {
        ElysiumModMenuGUI.InitializeKillCooldownOnRoundStart();

        if (ElysiumModMenuGUI.autoChatEveryone && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            ElysiumModMenuGUI.pendingAutoMeeting = true;
            ElysiumModMenuGUI.autoMeetingTimer = 0f;
        }
    }
}
[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
public static class ChatController_AddChat_Patch
{
    public static bool Prefix(PlayerControl sourcePlayer, ref string chatText, bool censor, ChatController __instance)
    {
        if (string.IsNullOrEmpty(chatText)) return true;
        string lowerText = chatText.ToLower().Trim();

        if (ElysiumModMenuGUI.enableColorCommand && sourcePlayer != null)
        {
            string[] colorCommands = { "/color ", "!color ", "/col ", "!col ", "/c ", "!c " };
            string usedCmd = colorCommands.FirstOrDefault(cmd => lowerText.StartsWith(cmd));

            if (usedCmd != null)
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    string colorInput = lowerText.Substring(usedCmd.Length).Trim();
                    int colorId = -1;

                    if (int.TryParse(colorInput, out int parsedId)) { if (parsedId >= 0 && parsedId <= 18) colorId = parsedId; }
                    else colorId = ElysiumModMenuGUI.GetColorIdByName(colorInput);

                    if (colorId != -1)
                    {
                        if (colorId == 18 && ElysiumModMenuGUI.blockFortegreenChat)
                        {
                            if (HudManager.Instance?.Chat != null)
                                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Цвет Fortegreen запрещен хостом!");
                        }
                        else
                        {
                            sourcePlayer.RpcSetColor((byte)colorId);
                        }
                    }
                    else if (sourcePlayer == PlayerControl.LocalPlayer)
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Неверный цвет.");
                    }
                }
                return false;
            }

            if (lowerText == "/rainbow" || lowerText == "!rainbow" || lowerText == "/lgbt" || lowerText == "!lgbt")
            {
                if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                {
                    if (ElysiumModMenuGUI.blockRainbowChat)
                    {
                        if (HudManager.Instance?.Chat != null)
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "<color=#FF0000>[ОШИБКА]</color> Радуга запрещена хостом!");
                    }
                    else
                    {
                        if (ElysiumModMenuGUI.rainbowPlayers.Contains(sourcePlayer.PlayerId))
                        {
                            ElysiumModMenuGUI.rainbowPlayers.Remove(sourcePlayer.PlayerId);
                            ElysiumModMenuGUI.ShowNotification("<color=#FF00FF>[SERVER]</color> Радуга ВЫКЛ.");
                        }
                        else
                        {
                            ElysiumModMenuGUI.rainbowPlayers.Add(sourcePlayer.PlayerId);
                            ElysiumModMenuGUI.ShowNotification("<color=#FF00FF>[SERVER]</color> Радуга ВКЛ.");
                        }
                    }
                }
                return false;
            }
        }

        if (ShouldShowGhostMessage(sourcePlayer))
        {
            return ShowGhostMessage(sourcePlayer, chatText, censor, __instance);
        }

        return true;
    }

    private static bool ShouldShowGhostMessage(PlayerControl sourcePlayer)
    {
        try
        {
            if (!ElysiumModMenuGUI.readGhostChat && !ElysiumModMenuGUI.seeGhosts) return false;
            if (sourcePlayer == null || sourcePlayer.Data == null) return false;
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return false;
            if (PlayerControl.LocalPlayer.Data.IsDead) return false;

            return sourcePlayer.Data.IsDead;
        }
        catch { return false; }
    }

    private static bool ShowGhostMessage(PlayerControl sourcePlayer, string chatText, bool censor, ChatController chat)
    {
        if (chat == null) return true;

        ChatBubble pooledBubble = null;
        try
        {
            NetworkedPlayerInfo sourceData = sourcePlayer.Data;
            if (sourceData == null) return true;

            pooledBubble = chat.GetPooledBubble();
            pooledBubble.transform.SetParent(chat.scroller.Inner);
            pooledBubble.transform.localScale = Vector3.one;

            bool isLocal = sourcePlayer == PlayerControl.LocalPlayer;
            if (isLocal) pooledBubble.SetRight();
            else pooledBubble.SetLeft();

            bool didVote = MeetingHud.Instance != null && MeetingHud.Instance.DidVote(sourcePlayer.PlayerId);
            pooledBubble.SetCosmetics(sourceData);
            chat.SetChatBubbleName(pooledBubble, sourceData, sourceData.IsDead, didVote, PlayerNameColor.Get(sourceData), null);

            if (censor && AmongUs.Data.DataManager.Settings.Multiplayer.CensorChat)
            {
                chatText = BlockedWords.CensorWords(chatText, false);
            }

            pooledBubble.SetText($"<color={ElysiumModMenuGUI.GetGhostChatColorHex()}>{chatText}</color>");
            pooledBubble.AlignChildren();
            chat.AlignAllBubbles();

            if (!chat.IsOpenOrOpening && chat.notificationRoutine == null)
            {
                chat.notificationRoutine = chat.StartCoroutine(chat.BounceDot());
            }

            if (!isLocal && !chat.IsOpenOrOpening)
            {
                SoundManager.Instance.PlaySound(chat.messageSound, false).pitch = 0.5f + sourcePlayer.PlayerId / 15f;
                chat.chatNotification.SetUp(sourcePlayer, chatText);
            }

            return false;
        }
        catch
        {
            try
            {
                if (pooledBubble != null) chat.chatBubblePool.Reclaim(pooledBubble);
            }
            catch { }
            return true;
        }
    }



    public static void Postfix(GameStartManager __instance)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost || PlayerControl.LocalPlayer == null) return;
        if (ElysiumModMenuGUI.customStartTimer > 0f) return;

        if (ElysiumModMenuGUI.fakeStartCounterTroll)
        {
            try
            {
                sbyte[] arr = { -123, -100, -69, -42, 0, 42, 69, 100, 123 };
                sbyte b = arr[UnityEngine.Random.Range(0, arr.Length)];
                PlayerControl.LocalPlayer.RpcSetStartCounter((int)b);
                __instance.SetStartCounter(b);
            }
            catch { }
        }
        else if (ElysiumModMenuGUI.fakeStartCounterCustom && int.TryParse(ElysiumModMenuGUI.fakeStartInput, out int custom))
        {
            try
            {
                PlayerControl.LocalPlayer.RpcSetStartCounter(custom);
                __instance.SetStartCounter((sbyte)Mathf.Clamp(custom, -128, 127));
            }
            catch { }
        }
    }
}

[HarmonyPatch(typeof(GameContainer), nameof(GameContainer.SetupGameInfo))]
public static class MoreLobbyInfo_GameContainer_SetupGameInfo_Postfix
{
    public static void Postfix(GameContainer __instance)
    {
        if (!ElysiumModMenuGUI.moreLobbyInfo) return;

        var trueHostName = __instance.gameListing.TrueHostName;
        const string separator = "<#0000>000000000000000</color>";
        var age = __instance.gameListing.Age;
        var lobbyTime = $"Age: {age / 60}:{(age % 60 < 10 ? "0" : "")}{age % 60}";


        int platId = (int)__instance.gameListing.Platform;
        string platformStr = platId switch
        {
            1 => "Epic",
            2 => "Steam",
            3 => "Mac",
            4 => "Microsoft Store",
            5 => "Itch.io",
            6 => "iOS",
            7 => "Android",
            8 => "Nintendo Switch",
            9 => "Xbox",
            10 => "PlayStation",
            112 => "Starlight",
            _ => "Unknown"
        };

        string hexColor = ColorUtility.ToHtmlStringRGB(ElysiumModMenuGUI.currentAccentColor);

        __instance.capacity.text = $"<size=40%>{separator}\n{trueHostName}\n{__instance.capacity.text}\n" +
                                   $"<color=#{hexColor}>{GameCode.IntToGameName(__instance.gameListing.GameId)}</color>\n" +
                                   $"<color=#{hexColor}>{platformStr}</color>\n{lobbyTime}\n{separator}</size>";
    }
}

[HarmonyPatch(typeof(FindAGameManager), nameof(FindAGameManager.HandleList))]
public static class MoreLobbyInfo_FindAGameManager_HandleList_Postfix
{
    public static void Postfix(HttpMatchmakerManager.FindGamesListFilteredResponse response, FindAGameManager __instance)
    {
        if (!ElysiumModMenuGUI.moreLobbyInfo) return;

        __instance.TotalText.text = response.Metadata.AllGamesCount.ToString();
    }
}
[HarmonyPatch(typeof(PlatformSpecificData), nameof(PlatformSpecificData.Serialize))]
public static class PlatformSpooferPatch
{
    public static void Prefix(PlatformSpecificData __instance)
    {
        try
        {
            if (__instance != null)
            {
                if (!ElysiumModMenuGUI.enablePlatformSpoof) return;

                __instance.Platform = ElysiumModMenuGUI.platformValues[ElysiumModMenuGUI.currentPlatformIndex];
                __instance.PlatformName = "ElysiumModMenu by Meowchelo (and one <color=#FFA500>silly</color> guy :p) https://github.com/meowchelo/ElysiumModMenu";
            }
        }
        catch { }
    }
}

[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.Serialize))]
public static class FriendCodeSpooferPatch
{
    private static string serializeRestoreValue = null;

    public static void Prefix(NetworkedPlayerInfo __instance)
    {
        try
        {
            serializeRestoreValue = null;
            if (ElysiumModMenuGUI.PrepareLocalFriendCodeForSerialize(__instance, out serializeRestoreValue)) return;
            if (!ElysiumModMenuGUI.enableFriendCodeSpoof) return;
            if (__instance == null || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null) return;
            if (__instance.PlayerId != PlayerControl.LocalPlayer.PlayerId) return;

            string input = ElysiumModMenuGUI.spoofFriendCodeInput ?? "";
            string clean = "";
            foreach (char c in input.ToLowerInvariant())
            {
                if (char.IsWhiteSpace(c)) break;
                if (char.IsLetterOrDigit(c)) clean += c;
                if (clean.Length >= 10) break;
            }

            if (string.IsNullOrWhiteSpace(clean)) return;
            __instance.FriendCode = clean;
        }
        catch { }
    }

    public static void Postfix(NetworkedPlayerInfo __instance)
    {
        ElysiumModMenuGUI.RestoreLocalFriendCodeAfterSerialize(__instance, serializeRestoreValue);
        serializeRestoreValue = null;
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
public static class AmongUsClient_KickPlayer_BanList_Patch
{
    public static void Prefix(InnerNetClient __instance, int clientId, bool ban)
    {
        if (ban && PlayerControl.AllPlayerControls != null && AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            try
            {
                var pc = PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(p => p.OwnerId == clientId);
                if (pc != null && pc.Data != null)
                {
                    string fc = string.IsNullOrEmpty(pc.Data.FriendCode) ? "Unknown" : pc.Data.FriendCode;
                    string name = pc.Data.PlayerName ?? "Unknown";
                    string puid = "Unknown";

                    try
                    {
                        var client = AmongUsClient.Instance.GetClientFromCharacter(pc);
                        if (client != null) puid = ElysiumModMenuGUI.GetClientPuid(client);
                    }
                    catch { }

                    ElysiumModMenuGUI.AddToBanList(fc, puid, name, "Host ban");
                    ElysiumModMenuGUI.ShowNotification($"<color=#FF0000>[BAN]</color> {name} занесен в черный список!");
                }
            }
            catch { }
        }
    }
}
public static class Extra // another silly HostFuck class...
{
    public static ModPlayer Mod(this PlayerControl pc)
	{
		return pc.gameObject.EnsureComponent<ModPlayer>();
	}

	public static T EnsureComponent<T>(this GameObject obj) where T : Component
	{
		return obj.GetComponent<T>() ?? obj.AddComponent<T>();
	}

}
