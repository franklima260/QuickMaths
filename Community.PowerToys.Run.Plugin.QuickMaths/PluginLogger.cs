using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Community.PowerToys.Run.Plugin.QuickMaths
{
    /// <summary>
    /// Minimal file-based logger. Zero external dependencies — immune to ManagedCommon version mismatches.
    /// Writes to %LocalAppData%\Microsoft\PowerToys\PowerToys Run\Logs\QuickMaths\YYYY-MM-DD.txt
    /// </summary>
    internal static class PluginLogger
    {
        private static string? _logPath;
        private static readonly object _lock = new();

        public static void Initialize()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "PowerToys", "PowerToys Run", "Logs", "QuickMaths");
            Directory.CreateDirectory(dir);
            _logPath = Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd}.txt");
            Info("Logger initialized.");
        }

        public static void Info(string message, [CallerMemberName] string caller = "") =>
            Write("INFO ", caller, message);

        public static void Warn(string message, [CallerMemberName] string caller = "") =>
            Write("WARN ", caller, message);

        public static void Error(string message, Exception? ex = null, [CallerMemberName] string caller = "") =>
            Write("ERROR", caller, ex == null ? message : $"{message}\n{ex}");

        public static void Debug(string message, [CallerMemberName] string caller = "") =>
            Write("DEBUG", caller, message);

        private static void Write(string level, string caller, string message)
        {
            if (_logPath == null) return;
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] [{caller}] {message}";
            try
            {
                lock (_lock)
                    File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch { /* never crash the plugin due to logging */ }
        }
    }
}
