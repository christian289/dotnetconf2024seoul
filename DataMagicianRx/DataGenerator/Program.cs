using DataGenerator;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<DataContainerService>();
        services.AddHostedService<TcpServerService>();
    })
    .Build();

await host.RunAsync();