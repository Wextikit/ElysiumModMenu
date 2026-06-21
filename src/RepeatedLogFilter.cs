using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace ElysiumModMenu
{
    /// <summary>
    /// Places the standard BepInEx listeners behind a bounded queue. Keeping the
    /// original listeners means console and disk formatting remain unchanged.
    /// </summary>
    internal sealed class RepeatedLogFilter : ILogListener
    {
        private sealed class QueuedLog
        {
            public object Sender;
            public LogEventArgs EventArgs;
        }

        private static readonly object InstallLock = new object();
        private static RepeatedLogFilter instance;

        private const int OutputIntervalMilliseconds = 500;
        private const int MaximumQueuedLogs = 2000;

        private readonly object sync = new object();
        private readonly List<ILogListener> listeners;
        private readonly Queue<QueuedLog> queue = new Queue<QueuedLog>();
        private readonly Timer outputTimer;
        private int droppedLogs;
        private bool disposed;

        private RepeatedLogFilter(IEnumerable<ILogListener> originalListeners)
        {
            listeners = originalListeners.Where(listener => listener != null).ToList();
            outputTimer = new Timer(_ => OutputNext(), null, OutputIntervalMilliseconds, OutputIntervalMilliseconds);
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

            ElysiumModMenuGUI.ObserveRawDiagnosticLog(eventArgs);

            if (!ElysiumModMenuGUI.throttleDefaultLogs)
            {
                Forward(sender, eventArgs);
                return;
            }

            lock (sync)
            {
                if (disposed) return;

                if (queue.Count >= MaximumQueuedLogs)
                {
                    droppedLogs++;
                    return;
                }

                queue.Enqueue(new QueuedLog { Sender = sender, EventArgs = eventArgs });
            }
        }

        private void OutputNext()
        {
            QueuedLog next = null;
            int dropped = 0;

            lock (sync)
            {
                if (disposed) return;

                if (queue.Count > 0)
                {
                    next = queue.Dequeue();
                }
                else if (droppedLogs > 0)
                {
                    dropped = droppedLogs;
                    droppedLogs = 0;
                }
            }

            if (next != null)
            {
                Forward(next.Sender, next.EventArgs);
            }
            else if (dropped > 0)
            {
                System.Console.WriteLine($"[ElysiumModMenu] Log queue was full; {dropped} excessive messages were discarded to protect performance.");
            }
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
            List<QueuedLog> remaining;

            lock (sync)
            {
                if (disposed) return;
                disposed = true;
                outputTimer.Dispose();
                remaining = queue.ToList();
                queue.Clear();
            }

            foreach (QueuedLog item in remaining)
                Forward(item.Sender, item.EventArgs);

            foreach (ILogListener listener in listeners)
            {
                try { listener.Dispose(); }
                catch { }
            }
        }
    }
}
