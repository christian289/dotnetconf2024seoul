using DataMagician;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton<DataContainerService>();
        services.AddHostedService<TcpClientService>();
        //services.AddHostedService<RxMagicService>();
    })
    .Build();

await host.RunAsync();