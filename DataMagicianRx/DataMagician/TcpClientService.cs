using DataStructure;

namespace DataMagician;

internal class TcpClientService(ILogger logger, DataContainerService dataContainerService) : BackgroundService
{
    private readonly ILogger _logger = logger;
    private readonly DataContainerService dataContainerService = dataContainerService;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TcpClient client = new();

        try
        {
            await client.ConnectAsync(IPAddress.Loopback, Statics.Port);

            Pipe pipe = new();

            Task start_task = Task.Factory.StartNew(
                action: async () => await SendStart(pipe.Writer, stoppingToken),
                cancellationToken: stoppingToken,
                creationOptions: TaskCreationOptions.LongRunning,
                scheduler: TaskScheduler.Default);
            
            Task readingTask = Task.Factory.StartNew(
                action: async () => await ReceiveStart(pipe.Reader, stoppingToken),
                cancellationToken: stoppingToken,
                creationOptions: TaskCreationOptions.LongRunning,
                scheduler: TaskScheduler.Default);

            await Task.WhenAll(start_task, readingTask);
        }
        finally
        {
            client.Close();
        }
    }

    private async Task<int> JsonRpcSelectorAsync(int id, PipeWriter writer, CancellationToken ct)
    {
        JsonRpcRequest request;

        if (id % 3 == 0)
            request = new(new AMethod(), id);
        else if (id % 3 == 1)
            request = new(new BMethod(), id);
        else
            request = new(new CMethod(), id);

        string json_str = request.Serialize();

        _logger.LogInformation($"Sending request: {json_str}");

        json_str = json_str.Replace("\r", string.Empty).Replace("\n", string.Empty);
        byte[] bytes = Encoding.UTF8.GetBytes(json_str);
        await writer.WriteAsync(bytes, ct);

        id++;

        if (id == int.MaxValue)
            return 0;
        else
            return id;
    }

    private async Task SendStart(PipeWriter writer, CancellationToken ct)
    {
        int request_id = default;
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(Statics.TimerInterval));

        while (await timer.WaitForNextTickAsync(ct))
        {
            request_id = await JsonRpcSelectorAsync(request_id, writer, ct);
            await writer.FlushAsync(ct);
        }
    }

    private async Task ReceiveStart(PipeReader reader, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            ReadResult result = await reader.ReadAsync(ct);
            ReadOnlySequence<byte> buffer = result.Buffer;

            if (buffer.Length > 0)
            {
                string jsonResponse = Encoding.UTF8.GetString(buffer.ToArray());
                _logger.LogInformation($"Received response: {jsonResponse}");

                dataContainerService.RawDataSubject.OnNext(jsonResponse);
            }

            reader.AdvanceTo(buffer.End);

            if (result.IsCompleted)
                break;
        }
    }
}
