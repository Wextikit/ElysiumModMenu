#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace ElysiumModMenu
{
    internal static class ElysiumErrorLog
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>(StringComparer.Ordinal);

        internal static void Capture(Exception error, [CallerMemberName] string member = "", [CallerFilePath] string file = "")
        {
            if (error == null) return;
            try
            {
                string source = Path.GetFileNameWithoutExtension(file) + "." + member;
                string key = source + "|" + error.GetType().FullName + "|" + error.Message;
                int count;
                lock (Sync)
                {
                    Counts.TryGetValue(key, out count);
                    count++;
                    Counts[key] = count;
                }
                if (count <= 3 || count % 100 == 0)
                    Plugin.Instance?.Log?.LogWarning((object)$"[RECOVERED ERROR] {source}: {error.GetType().Name}: {error.Message} (x{count})");
            }
            catch (Exception loggingError)
            {
                System.Diagnostics.Debug.WriteLine(loggingError);
            }
        }
    }
}
