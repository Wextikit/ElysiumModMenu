using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ElysiumModMenu
{
    internal sealed class RepeatedLogFilter : ILogListener
    {
        private static readonly object InstallLock = new object();
        private static RepeatedLogFilter instance;

        private readonly object sync = new object();
        private readonly List<ILogListener> listeners;
        private bool disposed;

        private RepeatedLogFilter(IEnumerable<ILogListener> originalListeners)
        {
            listeners = originalListeners.Where(listener => listener != null).ToList();
        }

        public LogLevel LogLevelFilter
        {
            get
            {
                LogLevel levels = LogLevel.None;
                foreach (ILogListener listener in listeners)
                    levels |= listener.LogLevelFilter;
                return levels;
            }
        }

        public static void Install()
        {
            lock (InstallLock)
            {
                if (instance != null) return;

                PropertyInfo listenersProperty = typeof(Logger).GetProperty(
                    "Listeners",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                object listenersObject = listenersProperty?.GetValue(null);
                if (!(listenersObject is ICollection<ILogListener> globalListeners)) return;

                List<ILogListener> originalListeners = globalListeners
                    .Where(listener => listener != null && !(listener is RepeatedLogFilter))
                    .ToList();
                if (originalListeners.Count == 0) return;

                var filter = new RepeatedLogFilter(originalListeners);
                foreach (ILogListener listener in originalListeners)
                    globalListeners.Remove(listener);
                globalListeners.Add(filter);
                instance = filter;
            }
        }

        public void LogEvent(object sender, LogEventArgs eventArgs)
        {
            if (eventArgs == null) return;

            bool isImportant = (eventArgs.Level & (LogLevel.Warning | LogLevel.Error | LogLevel.Fatal)) != 0;
            if (!ElysiumModMenuGUI.detailedLogsEnabled)
            {
                if (IsKnownVerboseNoise(eventArgs)) return;
                if (!isImportant && !IsOwnLog(sender, eventArgs)) return;
            }

            Forward(sender, eventArgs);
        }

        private static bool IsOwnLog(object sender, LogEventArgs eventArgs)
        {
            string src = GetSrc(sender);
            if (string.IsNullOrEmpty(src))
                src = GetSrc(eventArgs);
            if (!string.IsNullOrEmpty(src))
                return src.Contains("Elysium") || src.Contains("NetGuard");

            string msg = eventArgs.Data as string ?? eventArgs.Data?.ToString();
            return msg != null && (msg.Contains("Elysium") || msg.Contains("NetGuard") || msg.StartsWith("Raw RPC "));
        }

        private static string GetSrc(object obj)
        {
            if (obj == null) return null;
            try
            {
                var t = obj.GetType();
                foreach (string n in new[] { "SourceName", "Source", "Name" })
                {
                    var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance);
                    object v = p?.GetValue(obj);
                    if (v != null) return v.ToString();
                }
            }
            catch { }
            try { return obj.ToString(); } catch { return null; }
        }

        private static bool IsKnownVerboseNoise(LogEventArgs eventArgs)
        {
            string message = eventArgs.Data as string ?? eventArgs.Data?.ToString();
            if (message == null) return false;

            if ((eventArgs.Level & LogLevel.Warning) == 0 &&
                (message.StartsWith("Registered mono type ElysiumModMenu.", System.StringComparison.Ordinal) ||
                 message.StartsWith("Registered mono type ElysiumNetGuard.", System.StringComparison.Ordinal)))
                return true;

            if ((eventArgs.Level & LogLevel.Warning) == 0) return false;

            return message.StartsWith("Delay spawn for unowned ", System.StringComparison.Ordinal) ||
                message.StartsWith("Stored data for ", System.StringComparison.Ordinal) ||
                (message.StartsWith("[Server] > ", System.StringComparison.Ordinal) &&
                 message.Contains(" has SendMode set to Everything"));
        }

        private void Forward(object sender, LogEventArgs eventArgs)
        {
            foreach (ILogListener listener in listeners)
            {
                try
                {
                    if ((listener.LogLevelFilter & eventArgs.Level) != 0)
                        listener.LogEvent(sender, eventArgs);
                }
                catch { }
            }
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (disposed) return;
                disposed = true;
            }

            foreach (ILogListener listener in listeners)
            {
                try { listener.Dispose(); }
                catch { }
            }
        }
    }
}

