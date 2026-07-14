#nullable disable
#pragma warning disable CS0162, CS0108, CS0219, CS0661, CS0660, CS8632, CS0168, CS0659
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
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {

private static readonly string[] menuTranslationFixKeys =
        {
            "ANTI CHEAT", "AUTO HOST", "LOBBY CONTROLS", "ROLE MANAGER", "PUNISHMENT SYSTEM", "Mode:",
            "RPC PROTECTIONS", "Block Spoof RPC", "Block Sabotage & Meetings", "Block Game RPC in Lobby",
            "Auto-Ban Platform Spoof (Host)", "Ban Custom Platforms From TXT", "Block Meeting RPC Flood",
            "Block Chat RPC Flood", "OTHER PROTECTIONS", "Disable Vote Kicks (Host)", "Auto-Kick Fortegreen",
            "Auto-Ban Broken FriendCode (Host)", "BAN LIST", "Auto-Ban Blacklisted Players", "Enter Friend Code",
            "ADD", "Ban list is empty."
        };

private static readonly Dictionary<string, Dictionary<string, string>> menuExtraTranslations = new Dictionary<string, Dictionary<string, string>>
        {
            ["de"] = new Dictionary<string, string> { ["GAME RULES"]="SPIELREGELN", ["Rainbow All"]="Regenbogen alle", ["All Color"]="Alle Farbe", ["Color:"]="Farbe:", ["Applied lobby color."]="Lobby-Farbe angewendet." },
            ["fr"] = new Dictionary<string, string> { ["GAME RULES"]="RÈGLES DU JEU", ["Rainbow All"]="Arc-en-ciel tous", ["All Color"]="Couleur tous", ["Color:"]="Couleur :", ["Applied lobby color."]="Couleur du lobby appliquée." },
            ["es"] = new Dictionary<string, string> { ["GAME RULES"]="REGLAS DEL JUEGO", ["Rainbow All"]="Arcoíris todos", ["All Color"]="Color todos", ["Color:"]="Color:", ["Applied lobby color."]="Color del lobby aplicado." },
            ["it"] = new Dictionary<string, string> { ["GAME RULES"]="REGOLE DI GIOCO", ["Rainbow All"]="Arcobaleno tutti", ["All Color"]="Colore tutti", ["Color:"]="Colore:", ["Applied lobby color."]="Colore lobby applicato." },
            ["pt"] = new Dictionary<string, string> { ["GAME RULES"]="REGRAS DO JOGO", ["Rainbow All"]="Arco-íris todos", ["All Color"]="Cor todos", ["Color:"]="Cor:", ["Applied lobby color."]="Cor do lobby aplicada." },
            ["pl"] = new Dictionary<string, string> { ["GAME RULES"]="ZASADY GRY", ["Rainbow All"]="Tęcza wszyscy", ["All Color"]="Kolor wszystkich", ["Color:"]="Kolor:", ["Applied lobby color."]="Kolor lobby zastosowany." },
            ["nl"] = new Dictionary<string, string> { ["GAME RULES"]="SPELREGELS", ["Rainbow All"]="Regenboog allen", ["All Color"]="Kleur allen", ["Color:"]="Kleur:", ["Applied lobby color."]="Lobbykleur toegepast." },
            ["tr"] = new Dictionary<string, string> { ["GAME RULES"]="OYUN KURALLARI", ["Rainbow All"]="Herkese gökkuşağı", ["All Color"]="Herkes renk", ["Color:"]="Renk:", ["Applied lobby color."]="Lobi rengi uygulandı." },
            ["cs"] = new Dictionary<string, string> { ["GAME RULES"]="PRAVIDLA HRY", ["Rainbow All"]="Duha všem", ["All Color"]="Barva všem", ["Color:"]="Barva:", ["Applied lobby color."]="Barva lobby použita." },
            ["ro"] = new Dictionary<string, string> { ["GAME RULES"]="REGULI JOC", ["Rainbow All"]="Curcubeu toți", ["All Color"]="Culoare toți", ["Color:"]="Culoare:", ["Applied lobby color."]="Culoarea lobby-ului aplicată." },
            ["hu"] = new Dictionary<string, string> { ["GAME RULES"]="JÁTÉKSZABÁLYOK", ["Rainbow All"]="Szivárvány mind", ["All Color"]="Mindenki színe", ["Color:"]="Szín:", ["Applied lobby color."]="Lobby színe alkalmazva." },
            ["sv"] = new Dictionary<string, string> { ["GAME RULES"]="SPELREGLER", ["Rainbow All"]="Regnbåge alla", ["All Color"]="Färg alla", ["Color:"]="Färg:", ["Applied lobby color."]="Lobbyfärg tillämpad." },
            ["da"] = new Dictionary<string, string> { ["GAME RULES"]="SPILREGLER", ["Rainbow All"]="Regnbue alle", ["All Color"]="Farve alle", ["Color:"]="Farve:", ["Applied lobby color."]="Lobbyfarve anvendt." },
            ["fi"] = new Dictionary<string, string> { ["GAME RULES"]="PELISÄÄNNÖT", ["Rainbow All"]="Sateenkaari kaikille", ["All Color"]="Väri kaikille", ["Color:"]="Väri:", ["Applied lobby color."]="Lobbyn väri käytetty." },
            ["no"] = new Dictionary<string, string> { ["GAME RULES"]="SPILLREGLER", ["Rainbow All"]="Regnbue alle", ["All Color"]="Farge alle", ["Color:"]="Farge:", ["Applied lobby color."]="Lobbyfarge brukt." },
            ["uk"] = new Dictionary<string, string> { ["GAME RULES"]="ПРАВИЛА ГРИ", ["Rainbow All"]="Райдуга всім", ["All Color"]="Колір усім", ["Color:"]="Колір:", ["Applied lobby color."]="Колір лобі застосовано." },
            ["el"] = new Dictionary<string, string> { ["GAME RULES"]="ΚΑΝΟΝΕΣ ΠΑΙΧΝΙΔΙΟΥ", ["Rainbow All"]="Ουράνιο τόξο όλοι", ["All Color"]="Χρώμα όλοι", ["Color:"]="Χρώμα:", ["Applied lobby color."]="Το χρώμα lobby εφαρμόστηκε." },
            ["zh"] = new Dictionary<string, string> { ["GAME RULES"]="游戏规则", ["Rainbow All"]="全员彩虹", ["All Color"]="全员颜色", ["Color:"]="颜色:", ["Applied lobby color."]="大厅颜色已应用。" },
            ["ja"] = new Dictionary<string, string> { ["GAME RULES"]="ゲームルール", ["Rainbow All"]="全員レインボー", ["All Color"]="全員カラー", ["Color:"]="色:", ["Applied lobby color."]="ロビーの色を適用しました。" },
            ["ko"] = new Dictionary<string, string> { ["GAME RULES"]="게임 규칙", ["Rainbow All"]="모두 무지개", ["All Color"]="모두 색상", ["Color:"]="색상:", ["Applied lobby color."]="로비 색상이 적용되었습니다." }
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
            ["ja"] = new[] { "アンチチート", "自動ホスト", "ロビー制御", "ロール管理", "処罰システム", "モード:", "RPC保護", "Spoof RPCをブロック", "サボタージュと会議をブロック", "ロビー中のゲームRPCをブロック", "プラットフォーム偽装を自動BAN (ホスト)", "TXTのカスタムプラットフォームをBAN", "会議RPCフラッドをブロック", "チャットRPCフラッドをブロック", "その他の保護", "投票キックを無効化 (ホスト)", "Fortegreenを自動キック", "壊れたFriendCodeを自動BAN (ホスト)", "BANリスト", "BANリストのプレイヤーを自動BAN", "Friend Codeを入力", "追加", "BANリストは空です。" },
            ["ko"] = new[] { "안티치트", "자동 호스트", "로비 컨트롤", "역할 관리자", "처벌 시스템", "모드:", "RPC 보호", "Spoof RPC 차단", "사보타주와 회의 차단", "로비에서 게임 RPC 차단", "플랫폼 위장 자동 밴 (호스트)", "TXT 커스텀 플랫폼 밴", "회의 RPC 플러드 차단", "채팅 RPC 플러드 차단", "기타 보호", "투표 킥 비활성화 (호스트)", "Fortegreen 자동 킥", "손상된 FriendCode 자동 밴 (호스트)", "밴 목록", "목록의 플레이어 자동 밴", "Friend Code 입력", "추가", "밴 목록이 비어 있습니다." }
        };

public static float resetingDataLimit;

public static byte selectedMorphTargetId = 255;

public static bool unlockCosmetics = true;

public static bool unlockCosmicubes = true;

public static bool activateCompletedCosmicubes = false;

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

                string autoLanguage = ResolveAutoMenuLanguageCode();
                if (autoLanguage != "en")
                    return TryTranslateMenuText(autoLanguage, eng, autoLanguage == "ru" ? rus : null);
            }
            catch { }
            return eng;
        }

private static string ResolveAutoMenuLanguageCode()
        {
            try
            {
                if (DestroyableSingleton<TranslationController>.InstanceExists)
                {
                    string currentLang = DestroyableSingleton<TranslationController>.Instance.currentLanguage.ToString().ToLowerInvariant();
                    if (currentLang.Contains("russian") || currentLang.Contains("рус") || currentLang == "ru") return "ru";
                    if (currentLang.Contains("ukrainian") || currentLang.Contains("укр") || currentLang == "uk") return "uk";
                    if (currentLang.Contains("german") || currentLang.Contains("deutsch") || currentLang == "de") return "de";
                    if (currentLang.Contains("french") || currentLang.Contains("fran") || currentLang == "fr") return "fr";
                    if (currentLang.Contains("spanish") || currentLang.Contains("espa") || currentLang == "es") return "es";
                    if (currentLang.Contains("italian") || currentLang == "it") return "it";
                    if (currentLang.Contains("portugu") || currentLang == "pt") return "pt";
                    if (currentLang.Contains("polish") || currentLang == "pl") return "pl";
                    if (currentLang.Contains("dutch") || currentLang.Contains("neder") || currentLang == "nl") return "nl";
                    if (currentLang.Contains("turkish") || currentLang == "tr") return "tr";
                    if (currentLang.Contains("czech") || currentLang == "cs") return "cs";
                    if (currentLang.Contains("romanian") || currentLang == "ro") return "ro";
                    if (currentLang.Contains("hungarian") || currentLang == "hu") return "hu";
                    if (currentLang.Contains("swedish") || currentLang == "sv") return "sv";
                    if (currentLang.Contains("danish") || currentLang == "da") return "da";
                    if (currentLang.Contains("finnish") || currentLang == "fi") return "fi";
                    if (currentLang.Contains("norwegian") || currentLang == "no") return "no";
                    if (currentLang.Contains("greek") || currentLang == "el") return "el";
                    if (currentLang.Contains("chinese") || currentLang == "zh") return "zh";
                    if (currentLang.Contains("japanese") || currentLang == "ja") return "ja";
                    if (currentLang.Contains("korean") || currentLang == "ko") return "ko";
                }
            }
            catch { }

            try
            {
                string gray = Palette.GetColorName(15);
                if (!string.IsNullOrEmpty(gray) && gray.Any(c => c >= '\u0400' && c <= '\u04FF')) return "ru";
            }
            catch { }

            return "en";
        }

private static string TryTranslateMenuText(string languageCode, string englishText, string fallback)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(languageCode) || string.IsNullOrEmpty(englishText))
                    return fallback ?? englishText;

                if (languageCode == "en")
                    return englishText;

                if (menuExtraTranslations.TryGetValue(languageCode, out Dictionary<string, string> extraTranslations) &&
                    extraTranslations.TryGetValue(englishText, out string extraTranslated) &&
                    !string.IsNullOrWhiteSpace(extraTranslated))
                    return extraTranslated;

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

private string[] generalSubTabs => new string[] { L("INFORMATION", "ИНФОРМАЦИЯ"), L("KEYBINDS", "БИНДЫ") }

;

private string[] generalInfoSubTabs => new string[] { L("WELCOME", "WELCOME"), L("CREDITS", "АВТОРЫ") }

;

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

public static KeyCode bindReviveAll = KeyCode.None;

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

public static bool isWaitBindReviveAll = false;

public static bool SpoofMenuEnabled = false;

public static int selectedSpoofMenuIndex = 0;

private float uiSpoofTimer = 0f;

public static bool noClip = false;

public static bool tpToCursor = false;

public static bool dragToCursor = false;

private const float CursorMoveRpcIntervalSeconds = 0.2f;

private const float CursorMoveRpcMinDistance = 0.35f;

private static float nextCursorMoveRpcAt = 0f;

private static Vector2 lastCursorMoveRpcPosition = Vector2.zero;

private static bool hasLastCursorMoveRpcPosition = false;

public static float walkSpeed = 1f;

public static bool DetailedJoinInfo = true;

private static List<int> lastPlayerClientIds = new List<int>();

private static Dictionary<int, float> pendingJoinTimers = new Dictionary<int, float>();

private static Dictionary<int, float> pendingJoinWaitTimes = new Dictionary<int, float>();

private const float JoinLevelMaxWaitSeconds = 5f;

private static float nextPlayerHistoryUpdateAt;

private static float lastPlayerHistoryUpdateAt;

private static Dictionary<byte, string> playerHistoryKeysById = new Dictionary<byte, string>();

private static Dictionary<int, string> playerHistoryKeysByClientId = new Dictionary<int, string>();

private sealed class SafePlayerIdentitySnapshot
        {
            public int ClientId;
            public byte PlayerId = byte.MaxValue;
            public string Name = "Unknown";
            public string FriendCode = "Hidden";
            public string Puid = "Unknown";
            public string Platform = "Unknown";
            public string CustomPlatform = "";
            public int Level = 0;
        }

private static readonly Dictionary<int, SafePlayerIdentitySnapshot> safeIdentityByClientId = new Dictionary<int, SafePlayerIdentitySnapshot>();

private static readonly Dictionary<byte, SafePlayerIdentitySnapshot> safeIdentityByPlayerId = new Dictionary<byte, SafePlayerIdentitySnapshot>();

private static readonly Dictionary<int, int> safeIdentityCaptureAttempts = new Dictionary<int, int>();

private static readonly Dictionary<int, float> safeIdentityNextCaptureAt = new Dictionary<int, float>();

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
            public List<string> PreviousNames = new List<string>();
            public List<byte> RpcCalls = new List<byte>();
        }

private static List<PlayerHistoryEntry> playerHistoryEntries = new List<PlayerHistoryEntry>();

private sealed class PlayerHistoryViewRow
        {
            public string Header;
            public string Identity;
            public string Times;
            public string Platform;
            public string Rpc;
        }

private static readonly Dictionary<string, PlayerHistoryEntry> playerHistoryEntryLookup = new Dictionary<string, PlayerHistoryEntry>();

private static readonly List<PlayerHistoryViewRow> playerHistoryViewRows = new List<PlayerHistoryViewRow>();

private static bool playerHistoryViewDirty = true;

private const float PlayerHistoryRowHeight = 92f;

private static bool playerHistoryLoaded = false;

private Vector2 playersHistoryScroll = Vector2.zero;

private int currentPlayersSubTab = 0;

private string[] playersSubTabs = { "ACTIONS", "PLAYER HISTORY", "CLONES" };

private Vector2 playersClonesScroll = Vector2.zero;

private int cloneFormationIdx = 0;

private int cloneFormationCount = 8;

private int currentRoleBuffSubTab = 0;

public static float engineSpeed = 1f;

public static bool invertControls = false;

public static bool autoFollowCursor = false;

public static int fakeRoleIdx = 0;

public static RoleTypes[] forceRoleOptions = { RoleTypes.Crewmate, RoleTypes.Impostor, RoleTypes.Engineer, RoleTypes.Scientist, RoleTypes.Shapeshifter, (RoleTypes)9, (RoleTypes)18, RoleTypes.GuardianAngel };

public static RoleTypes[] roleAssignOptions = {
            RoleTypes.Crewmate, RoleTypes.Impostor, RoleTypes.Engineer, RoleTypes.Scientist, RoleTypes.Shapeshifter, RoleTypes.GuardianAngel,
            (RoleTypes)8, (RoleTypes)9, (RoleTypes)10, (RoleTypes)12, (RoleTypes)18, RoleTypes.Crewmate, RoleTypes.Impostor
        };

public static string[] roleAssignNames = {
            "Crewmate", "Impostor", "Engineer", "Scientist", "Shapeshifter", "Guardian Angel",
            "Noisemaker", "Phantom", "Tracker", "Detective", "Viper", "Ghost", "Ghost Imp"
        };

private int targetRoleAssignIdx = 0;

private int selectedPlayerReportReasonIdx = 0;

private static readonly ReportReasons[] selectedPlayerReportReasons = {
            ReportReasons.InappropriateName,
            ReportReasons.InappropriateChat,
            ReportReasons.Cheating_Hacking,
            ReportReasons.Harassment_Misconduct
        };

private static readonly string[] selectedPlayerReportReasonNames = {
            "Inappropriate Name",
            "Inappropriate Chat",
            "Cheating / Hacking",
            "Harassment / Misconduct"
        };

private int allPlayersRoleAssignIdx = 0;

public static bool NoShapeshiftAnim = false;

public static bool EndlessTracking = false;

public static bool NoTrackingCooldown = false;

public static bool UnlimitedInterrogateRange = false;

public static bool allowTasksAsImpostor = false;

public static bool killWhileVanishedHostOnly = false;

public static bool roleBuffImmortality = false;

private const int ImmortalityCustomVentId = 50;

private static bool immortalityVentStateApplied = false;

public static bool noTaskMode = false;

public static bool killAuraHostOnly = false;

public static bool hostAutoKillRandom = false;

public static bool hostAutoKillTarget = false;

private static byte hostAutoKillTargetId = byte.MaxValue;

public static bool bugRoomAutoAngel = false;

public static bool bugRoomAutoKillShield = false;

public static bool noKillCooldownHostOnly = false;

public static bool disableEndGameSafeMode = false;

public static bool disableMapSafeMode = false;

public static bool spamReportBodies = false;

private float killAuraTimer = 0f;

private float hostAutoKillTimer = 0f;

private float hostAutoKillTargetTimer = 0f;

private float bugRoomAngelTimer = 0f;

private float bugRoomShieldKillTimer = 0f;

private float bugRoomTimedAutoRunTimer = 0f;

private bool bugRoomTimedAutoRunDone = false;

public static bool enableColorCommand = false;

public static bool hostChatColor = false;

public static Color hostChatColorValue = new Color32(0, 128, 128, 255);

public static bool showMenu = false;

public static Rect windowRect = new Rect(100, 100, 750, 480);

public static bool freecam = false;

private static bool _freecamActive = false;

public static bool cameraZoom = false;

public static bool RevealVotesEnabled = false;

private static bool hudZoomBaseCaptured = false;

private static Vector3 hudZoomBaseDistance = Vector3.zero;

private static bool zoomResolutionRefreshNeeded = false;

public static Color currentAccentColor = new Color(1f, 0.549f, 0f, 1f);

public static bool rgbMenuMode = false;

public static bool rgbMenuText = false;

public static bool boldMenuText = true;

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

private string[] tabNames => new string[] { L("GENERAL", "ОБЩИЕ"), L("SELF", "ИГРОК"), L("VISUALS", "ВИЗУАЛ"), L("PLAYERS", "ИГРОКИ"), L("SABOTAGES", "САБОТАЖИ"), L("HOST ONLY", "ХОСТ"), L("VOTEKICK", "КИК"), L("MENU", "МЕНЮ") }

;

private int currentSabotageSubTab = 0;

private string[] sabotageSubTabs => new string[] { L("SABOTAGES", "САБОТАЖИ"), L("ANIMATIONS", "АНИМАЦИИ") }

;

public static float speedMultiplier = 1f;

public static bool noSettingLimit = false;

public static float globalRoomColorId = 0f;

private int currentHostOnlySubTab = 0;

private string[] hostOnlySubTabs => new string[] { L("LOBBY CONTROLS", "КОНТРОЛЬ ЛОББИ"), L("ROLE MANAGER", "МЕНЕДЖЕР РОЛЕЙ"), L("ANTI CHEAT", "АНТИ-ЧИТ"), L("AUTO HOST", "АВТО ХОСТ"), "BUG ROOM", L("MAPS", "КАРТЫ") }

;

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
                if (HushWhisper.TryHandle(__instance)) return false;

                string text = __instance.freeChatField.Text;
                if (ElysiumModMenuGUI.enableChatHistory && !string.IsNullOrWhiteSpace(text))
                    ElysiumModMenuGUI.ChatHistory.Remember(text.Trim());

                if (ElysiumModMenuGUI.autoChatEveryone)
                {
                    string msg = CensorUrlsAndEmails(text);
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        InjectChatSpawnForAll();
                        if (PlayerControl.AllPlayerControls != null)
                        {
                            foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                            {
                                try { if (pc != null) pc.RpcSendChat(msg); } catch { }
                            }
                        }
                        __instance.freeChatField.textArea.SetText(string.Empty, string.Empty);
                    }
                    return false;
                }

                if (!ElysiumModMenuGUI.allowLinksAndSymbols) return true;

                if (!string.IsNullOrWhiteSpace(text))
                {
                    PlayerControl.LocalPlayer.RpcSendChat(text);
                    __instance.freeChatField.textArea.SetText(string.Empty, string.Empty);
                }

                return false;
            }

            private static string CensorUrlsAndEmails(string text)
            {
                string pattern = "(http[s]?://)?([a-zA-Z0-9-]+\\.)+[a-zA-Z]{2,6}(/[\\w-./?%&=]*)?|([a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\\.[a-zA-Z0-9-.]+)";
                Regex regex = new Regex(pattern);
                return regex.Replace(text ?? string.Empty, match => match.Value.Replace('.', ','));
            }

            private static void InjectChatSpawnForAll()
            {
                if (AmongUsClient.Instance == null || PlayerControl.AllPlayerControls == null) return;
                if (!((InnerNetClient)AmongUsClient.Instance).AmHost) return;

                try
                {
                    MessageWriter wr = MessageWriter.Get(SendOption.Reliable);
                    wr.StartMessage(6);
                    wr.Write(AmongUsClient.Instance.GameId);
                    wr.WritePacked(-1);

                    foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc == null) continue;
                        wr.StartMessage(4);
                        wr.WritePacked(69u);
                        wr.WritePacked(-2);
                        wr.Write(0);
                        wr.WritePacked(1);
                        wr.WritePacked(pc.NetId);
                        wr.StartMessage(1);
                        wr.EndMessage();
                        wr.EndMessage();
                    }

                    wr.EndMessage();
                    ((Connection)((InnerNetClient)AmongUsClient.Instance).connection).Send(wr);
                    wr.Recycle();
                }
                catch { }
            }
        }

public static bool autoKickBugs = false;

public static float autoKickTimer = 5f;

public static Dictionary<byte, float> fortegreenTimer = new Dictionary<byte, float>();
}
}
