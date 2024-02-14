namespace DataMagician
{
    internal class JsonRpcColorConsoleLoggerProvider : ILoggerProvider
    {
        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new JsonRpcColorConsoleLogger(categoryName);
        }
    }
}
