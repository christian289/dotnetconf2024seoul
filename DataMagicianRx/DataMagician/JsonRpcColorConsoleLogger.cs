namespace DataMagician
{

    enum MethodType
    {
        A,
        B,
        C
    }



    internal sealed class JsonRpcColorConsoleLogger(string name) : ILogger
    {
        private readonly string _name = name;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var (color, message) = ParseState(state);
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"{logLevel}: {_name}: {formatter(state, exception)}");
            Console.ForegroundColor = originalColor;
        }

        private (ConsoleColor, string) ParseState<TState>(TState state)
        {
            if (state is ValueTuple<ConsoleColor, string> tuple)
            {
                return tuple;
            }

            return (ConsoleColor.White, state?.ToString() ?? string.Empty);
        }
    }
}
