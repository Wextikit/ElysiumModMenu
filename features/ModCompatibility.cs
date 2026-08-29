#nullable disable
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ElysiumModMenu
{
    public partial class ElysiumModMenuGUI : MonoBehaviour
    {
        private static bool modCompatibilityChecked;

        private static void ShowModCompatibilityWarnings()
        {
            if (modCompatibilityChecked) return;
            modCompatibilityChecked = true;

            List<string> mods = DetectInstalledMods();
            for (int i = 0; i < mods.Count; i++)
            {
                SendNotification(
                    "COMPATIBILITY WARNING",
                    $"{mods[i]} detected. ElysiumModMenu may not work correctly.",
                    7f);
            }
        }

        private static List<string> DetectInstalledMods()
        {
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string gameRoot = Directory.GetCurrentDirectory();
            string plugins = Path.Combine(gameRoot, "BepInEx", "plugins");

            CheckFolderEntries(gameRoot, SearchOption.TopDirectoryOnly, found);
            CheckFolderEntries(plugins, SearchOption.AllDirectories, found);
            CheckDllMetadata(gameRoot, SearchOption.TopDirectoryOnly, found);
            CheckDllMetadata(plugins, SearchOption.AllDirectories, found);
            CheckLoadedAssemblies(found);
            CheckLoadedPlugins(found);

            // Only the live console log counts; bundled or stale files do not.
            if (ConsoleLogContainsSicko(gameRoot))
                found.Add("SickoMenu");

            var mods = new List<string>();
            AddDetectedMod(found, mods, "Malum");
            AddDetectedMod(found, mods, "Hydra");
            AddDetectedMod(found, mods, "Onyx");
            AddDetectedMod(found, mods, "SickoMenu");
            AddDetectedMod(found, mods, "Other Mod");
            return mods;
        }

        private static void CheckFolderEntries(string folder, SearchOption option, HashSet<string> found)
        {
            try
            {
                if (!Directory.Exists(folder)) return;

                foreach (string entry in Directory.GetFileSystemEntries(folder, "*", option))
                    CheckKnownModText(Path.GetRelativePath(folder, entry), found);
            }
            catch (global::System.Exception __elysiumCaught223) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught223); }
        }

        private static void CheckDllMetadata(string folder, SearchOption option, HashSet<string> found)
        {
            try
            {
                if (!Directory.Exists(folder)) return;

                foreach (string file in Directory.GetFiles(folder, "*.dll", option))
                {
                    CheckKnownModText(Path.GetFileName(file), found);

                    try
                    {
                        AssemblyName name = AssemblyName.GetAssemblyName(file);
                        CheckKnownModText(name.FullName, found);
                    }
                    catch (global::System.Exception __elysiumCaught224) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught224); }

                    try
                    {
                        FileVersionInfo info = FileVersionInfo.GetVersionInfo(file);
                        CheckKnownModText(
                            string.Join("|", info.ProductName, info.FileDescription, info.InternalName, info.OriginalFilename, info.CompanyName),
                            found);
                    }
                    catch (global::System.Exception __elysiumCaught225) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught225); }
                }
            }
            catch (global::System.Exception __elysiumCaught226) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught226); }
        }

        private static void CheckLoadedAssemblies(HashSet<string> found)
        {
            try
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try { CheckKnownModText(assembly.FullName, found); }
                    catch (global::System.Exception __elysiumCaught227) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught227); }
                }
            }
            catch (global::System.Exception __elysiumCaught228) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught228); }
        }

        private static void CheckLoadedPlugins(HashSet<string> found)
        {
            try
            {
                IL2CPPChainloader loader = IL2CPPChainloader.Instance;
                if (loader == null || loader.Plugins == null) return;

                foreach (KeyValuePair<string, PluginInfo> pair in loader.Plugins)
                {
                    PluginInfo info = pair.Value;
                    if (info == null || info.Metadata == null) continue;
                    string guid = info.Metadata.GUID ?? pair.Key ?? string.Empty;
                    if (guid.Equals("com.elysiummodmenu.menu", StringComparison.OrdinalIgnoreCase)) continue;

                    string pluginText = string.Join("|", guid, info.Metadata.Name, info.Metadata.Version?.ToString());
                    CheckKnownModText(pluginText, found);
                    if (!ContainsKnownModName(pluginText)) found.Add("Other Mod");
                }
            }
            catch (global::System.Exception __elysiumCaught229) { global::ElysiumModMenu.ElysiumErrorLog.Capture(__elysiumCaught229); }
        }

        private static bool ContainsKnownModName(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return text.IndexOf("malum", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("hydra", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("onyx", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("sickomenu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("sicko menu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("sickolobby", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("sicko lobby", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void CheckKnownModText(string text, HashSet<string> found)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            if (text.IndexOf("malum", StringComparison.OrdinalIgnoreCase) >= 0) found.Add("Malum");
            if (text.IndexOf("hydra", StringComparison.OrdinalIgnoreCase) >= 0) found.Add("Hydra");
            if (text.IndexOf("onyx", StringComparison.OrdinalIgnoreCase) >= 0) found.Add("Onyx");
        }


        private static bool ConsoleLogContainsSicko(string gameRoot)
        {
            string logPath = Path.Combine(gameRoot, "BepInEx", "LogOutput.log");
            try
            {
                if (!File.Exists(logPath)) return false;
                using FileStream stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using StreamReader reader = new StreamReader(stream, Encoding.UTF8, true);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string value = line.ToLowerInvariant();
                    if (value.Contains("sicko") && !value.Contains("elysiummodmenu.sicko_"))
                        return true;
                }
            }
            catch (Exception error)
            {
                ElysiumErrorLog.Capture(error);
            }
            return false;
        }

        private static void AddDetectedMod(HashSet<string> found, List<string> mods, string name)
        {
            if (found.Contains(name)) mods.Add(name);
        }
    }
}
