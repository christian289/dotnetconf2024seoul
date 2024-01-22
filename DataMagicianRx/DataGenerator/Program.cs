using DataGenerator;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<TcpServerService>();
    })
    .Build();

await host.RunAsync();