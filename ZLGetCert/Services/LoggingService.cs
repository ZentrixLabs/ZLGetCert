using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;
using ZLGetCert.Enums;
using ZLGetCert.Models;

namespace ZLGetCert.Services
{
    /// <summary>
    /// Centralized logging service using NLog
    /// </summary>
    public class LoggingService
    {
        private static readonly Lazy<LoggingService> _instance = new Lazy<LoggingService>(() => new LoggingService());
        public static LoggingService Instance => _instance.Value;

        private Logger _logger;
        private bool _isInitialized;

        private LoggingService()
        {
            InitializeLogging();
        }

        /// <summary>
        /// Initialize logging configuration
        /// </summary>
        private void InitializeLogging()
        {
            try
            {
                var config = ConfigurationService.Instance.GetConfiguration();
                SetupLogging(config);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                // Fallback to basic console logging
                System.Diagnostics.Debug.WriteLine($"Failed to initialize logging: {ex.Message}");
                SetupBasicLogging();
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Setup logging based on configuration
        /// </summary>
        private void SetupLogging(AppConfiguration config)
        {
            var logConfig = new LoggingConfiguration();
            var logLevel = ConvertToNLogLevel(config.Logging.LogLevel);

            // File target
            if (config.Logging.LogToFile)
            {
                var fileTarget = new FileTarget("fileTarget")
                {
                    FileName = Path.Combine(config.FilePaths.LogPath, "ZLGetCert-${shortdate}.log"),
                    Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}",
                    ArchiveFileName = Path.Combine(config.FilePaths.LogPath, "ZLGetCert-{#}.log"),
                    ArchiveEvery = FileArchivePeriod.Day,
                    ArchiveSuffixFormat = "{#}",
                    MaxArchiveFiles = config.Logging.MaxLogFiles,
                    KeepFileOpen = false
                };

                logConfig.AddTarget(fileTarget);
                logConfig.AddRule(logLevel, NLog.LogLevel.Fatal, fileTarget, "*");
            }

            // Console target
            if (config.Logging.LogToConsole)
            {
                var consoleTarget = new ConsoleTarget("consoleTarget")
                {
                    Layout = "${time}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}"
                };

                logConfig.AddTarget(consoleTarget);
                logConfig.AddRule(logLevel, NLog.LogLevel.Fatal, consoleTarget, "*");
            }

            LogManager.Configuration = logConfig;
            _logger = LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        /// Setup basic console logging as fallback
        /// </summary>
        private void SetupBasicLogging()
        {
            var logConfig = new LoggingConfiguration();
            var consoleTarget = new ConsoleTarget("consoleTarget")
            {
                Layout = "${time}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}"
            };

            logConfig.AddTarget(consoleTarget);
            logConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, consoleTarget, "*");

            LogManager.Configuration = logConfig;
            _logger = LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        /// Convert application log level to NLog level
        /// </summary>
        private NLog.LogLevel ConvertToNLogLevel(Enums.LogLevel level)
        {
            switch (level)
            {
                case Enums.LogLevel.Trace:
                    return NLog.LogLevel.Trace;
                case Enums.LogLevel.Debug:
                    return NLog.LogLevel.Debug;
                case Enums.LogLevel.Information:
                    return NLog.LogLevel.Info;
                case Enums.LogLevel.Warning:
                    return NLog.LogLevel.Warn;
                case Enums.LogLevel.Error:
                    return NLog.LogLevel.Error;
                case Enums.LogLevel.Fatal:
                    return NLog.LogLevel.Fatal;
                default:
                    return NLog.LogLevel.Info;
            }
        }

        /// <summary>
        /// Log trace message
        /// </summary>
        public void LogTrace(string message, params object[] args)
        {
            if (_isInitialized)
                _logger.Trace(message, args);
        }

        /// <summary>
        /// Log debug message
        /// </summary>
        public void LogDebug(string message, params object[] args)
        {
            if (_isInitialized)
                _logger.Debug(message, args);
        }

        /// <summary>
        /// Log information message
        /// </summary>
        public void LogInfo(string message, params object[] args)
        {
            if (_isInitialized)
                _logger.Info(message, args);
        }

        /// <summary>
        /// Log warning message
        /// </summary>
        public void LogWarning(string message, params object[] args)
        {
            if (_isInitialized)
                _logger.Warn(message, args);
        }

        /// <summary>
        /// Log error message
        /// </summary>
        public void LogError(string message, params object[] args)
        {
            if (_isInitialized)
                _logger.Error(message, args);
        }

        /// <summary>
        /// Log error with exception
        /// </summary>
        public void LogError(Exception exception, string message, params object[] args)
        {
            if (_isInitialized)
                _logger.Error(exception, message, args);
        }

        /// <summary>
        /// Log fatal message
        /// </summary>
        public void LogFatal(string message, params object[] args)
        {
            if (_isInitialized)
                _logger.Fatal(message, args);
        }

        /// <summary>
        /// Log fatal with exception
        /// </summary>
        public void LogFatal(Exception exception, string message, params object[] args)
        {
            if (_isInitialized)
                _logger.Fatal(exception, message, args);
        }

        /// <summary>
        /// Reinitialize logging with new configuration
        /// </summary>
        public void Reinitialize()
        {
            _isInitialized = false;
            InitializeLogging();
        }
    }
}
