namespace Fluent.sudo
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Console;

    internal static class Log
    {
        private static readonly ILoggerFactory factory;
        static Log()
        {
#pragma warning disable 618 // TODO
            factory = new LoggerFactory()
                .AddConsole(LogLevel.Trace);
#pragma warning restore 618
        }

        public static ILogger<T> Get<T>() => factory.CreateLogger<T>();
    }
}