using Microsoft.Extensions.Logging;

namespace Logging
{
    public class AppLogger
    {
        private static ILogger? _logger;

        /// <summary>
        /// Creates a new logger is one has not been created yet
        /// </summary>
        /// <returns></returns>
        private static ILogger CreateLogger()
        {
            var loggerFactory = LoggerFactory.Create(loggingBuilder => loggingBuilder
            .SetMinimumLevel(LogLevel.Debug)
            .AddConsole());
            var logger = loggerFactory.CreateLogger<AppLogger>();
            return logger;
        }

        /// <summary>
        /// Gets the global logger component.
        /// </summary>
        public static ILogger Logger => _logger ??= CreateLogger();
    }
}