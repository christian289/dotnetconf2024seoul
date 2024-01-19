using DataGenerator;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<TcpServerService>();
    })
    .Build();

await host.RunAsync();