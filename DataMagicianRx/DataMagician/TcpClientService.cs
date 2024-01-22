using DataStructure;

namespace DataMagician;

internal class TcpClientService(ILogger<TcpClientService> logger, DataContainerService dataContainerService) : BackgroundService
{
    private readonly ILogger<TcpClientService> _logger = logger;
    private readonly DataContainerService dataContainerService = dataContainerService;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TcpClient client = new();

        bool isRetry = false;
        CancellationTokenSource connectionTokenSource = new(TimeSpan.FromSeconds(Statics.SocketConnectionTimeout));

        for (int current_port = Statics.Port; current_port < Statics.MaxPort;)
        {
            try
            {
                await client.ConnectAsync(IPAddress.Loopback, current_port, connectionTokenSource.Token);

                break;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError($"IP: {IPAddress.Loopback}, Port: {current_port}, Timeout.");

                if (!isRetry)
                {
                    isRetry = true;
                    _logger.LogInformation($"Retry...");

                    continue;
                }

                _logger.LogInformation($"Port Number Up: {++current_port}");

                if (current_port > Statics.MaxPort)
                {
                    _logger.LogInformation($"재시도 포트가 모두 사용되었습니다...프로그램을 종료합니다.");

                    return;
                }

                isRetry = false;
                continue;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message} // 연결 실패.");

                return;
            }
        }

        NetworkStream stream = client.GetStream();
        Pipe pipe = new();

        JsonRpcPipeWriteStart(pipe.Writer, stoppingToken);
        JsonRpcPipeReadAndSocketSendStart(stream, pipe.Reader, stoppingToken);
    }

    private int JsonRpcSelector(int id, PipeWriter writer)
    {
        JsonRpcRequest request;

        if (id % 3 == 0)
            request = new(new AMethod(), id);
        else if (id % 3 == 1)
            request = new(new BMethod(), id);
        else
            request = new(new CMethod(), id);

        string json_str = request.Serialize();

        _logger.LogDebug($"Write Pipe Data: {json_str}");

        json_str = json_str.Replace("\r", string.Empty).Replace("\n", string.Empty);
        byte[] bytes = Encoding.UTF8.GetBytes(json_str);
        writer.Write(bytes);

        id++;

        if (id == int.MaxValue)
            return 0;
        else
            return id;
    }

    private void JsonRpcPipeWriteStart(PipeWriter writer, CancellationToken ct)
    {
        _ = Task.Factory.StartNew(
            action: async () =>
            {
                int request_id = default;
                using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(Statics.TimerInterval));

                while (await timer.WaitForNextTickAsync(ct))
                {
                    request_id = JsonRpcSelector(request_id, writer);
                    await writer.FlushAsync(ct);
                }
            },
            cancellationToken: ct,
            creationOptions: TaskCreationOptions.LongRunning,
            scheduler: TaskScheduler.Default);
    }

    private void JsonRpcPipeReadAndSocketSendStart(NetworkStream stream, PipeReader reader, CancellationToken ct)
    {
        _ = Task.Factory.StartNew(
            action: async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    ReadResult result = await reader.ReadAsync(ct);
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    if (buffer.Length > 0)
                    {
                        if (stream.CanWrite)
                        {
                            string jsonResponse = Encoding.UTF8.GetString(buffer.ToArray());
                            stream.Write(buffer.ToArray());
                            _logger.LogInformation($"Send Data: {jsonResponse}");
                            dataContainerService.RawDataSubject.OnNext(jsonResponse);
                        }
                    }

                    reader.AdvanceTo(buffer.End);

                    if (result.IsCompleted)
                        break;
                }
            },
            cancellationToken: ct,
            creationOptions: TaskCreationOptions.LongRunning,
            scheduler: TaskScheduler.Default);
    }
}
