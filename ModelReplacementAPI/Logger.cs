using BepInEx.Logging;

namespace ModelReplacement
{
    public enum LogLevel
    {
        Fatal = 1 << 0,
        Error = 1 << 1,
        Warning = 1 << 2,
        Info = 1 << 3,
        Debug = 1 << 4,
    }

    public class Logger
    {
        private ManualLogSource source;
        private LogLevel level;

        public Logger(string name, LogLevel logLevel = LogLevel.Info)
        {
            source = BepInEx.Logging.Logger.CreateLogSource(name);
            level = logLevel;
        }

        // So they decided it would be a good idea to NOT implement a enum option for logging
        public void Log(LogLevel logLevel, string message)
        {
            if ((level & logLevel) != 0)
            {
                switch (logLevel)
                {
                    case LogLevel.Fatal:
                        source.LogFatal(message);
                        break;
                    case LogLevel.Error:
                        source.LogError(message);
                        break;
                    case LogLevel.Warning:
                        source.LogWarning(message);
                        break;
                    case LogLevel.Info:
                        source.LogInfo(message);
                        break;
                    case LogLevel.Debug:
                        source.LogDebug(message);
                        break;
                }
            }
        }

        public void LogFatal(string message)
        {
            Log(LogLevel.Fatal, message);
        }

        public void LogError(string message)
        {
            Log(LogLevel.Error, message);
        }

        public void LogWarning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public void LogInfo(string message)
        {
            Log(LogLevel.Info, message);
        }

        public void LogDebug(string message)
        {
            Log(LogLevel.Debug, message);
        }
    }
}
