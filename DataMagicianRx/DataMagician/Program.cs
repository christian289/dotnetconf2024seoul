using DataMagician;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddProvider(new JsonRpcColorConsoleLoggerProvider());
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<TcpClientService>();
    })
    .Build();

await host.RunAsync();