namespace DataMagician;

internal class RxMagicService(DataContainerService dataContainerService) : BackgroundService
{
    private readonly DataContainerService dataContainerService = dataContainerService;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}
