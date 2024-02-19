using DataStructure;
using System.Reactive.Concurrency;

namespace DataMagician;

internal class TcpClientService(ILogger<TcpClientService> logger) : BackgroundService
{
    private readonly ILogger<TcpClientService> _logger = logger;
    private List<IDisposable> _observationList = [];

    private Subject<string> RawDataSubject { get; init; } = new Subject<string>();

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

        _logger.LogInformation("Observation Start~");
        MakeObservationMethodA();
        MakeObservationMethodB();
        MakeObservationMethodC();
        MakeObservationMethodA_();
        MakeObservationMethodB_();
        MakeObservationMethodB__();
        MakeObservationMethodC_();
        ConnectPipe(client);
    }

    private void MakeObservationMethodA()
    {
        // A Method에 대한 구독
        var observation = RawDataSubject
            .AsObservable()
            .ObserveOn(TaskPoolScheduler.Default)
            .Select(JsonConvert.DeserializeObject<JsonRpc>)
            .Where(rpc => rpc.Method == "A")
            .Subscribe(
                onNext: rpc => _logger.Log(LogLevel.Information, new EventId(), (ConsoleColor.Red, $"A Method: {rpc.Serialize()}"), null, (state, exception) => state.Item2),
                onError: ex => _logger.LogError(ex.Message),
                onCompleted: () => _logger.LogInformation("Observation A ByeBye~"));
        _observationList.Add(observation);
    }

    private void MakeObservationMethodB()
    {
        // B Method에 대한 구독
        var observation = RawDataSubject
            .AsObservable()
            .ObserveOn(TaskPoolScheduler.Default)
            .Select(JsonConvert.DeserializeObject<JsonRpc>)
            .Where(rpc => rpc.Method == "B")
            .Subscribe(
                onNext: rpc => _logger.Log(LogLevel.Information, new EventId(), (ConsoleColor.Green, $"B Method: {rpc.Serialize()}"), null, (state, exception) => state.Item2),
                onError: ex => _logger.LogError(ex.Message),
                onCompleted: () => _logger.LogInformation("Observation B ByeBye~"));
        _observationList.Add(observation);
    }

    private void MakeObservationMethodC()
    {
        // C Method에 대한 구독
        var observation = RawDataSubject
            .AsObservable()
            .ObserveOn(TaskPoolScheduler.Default)
            .Select(JsonConvert.DeserializeObject<JsonRpc>)
            .Where(rpc => rpc.Method == "C")
            .Subscribe(
                onNext: rpc => _logger.Log(LogLevel.Information, new EventId(), (ConsoleColor.Blue, $"C Method: {rpc.Serialize()}"), null, (state, exception) => state.Item2),
                onError: ex => _logger.LogError(ex.Message),
                onCompleted: () => _logger.LogInformation("Observation C ByeBye~"));
        _observationList.Add(observation);
    }

    private void MakeObservationMethodA_()
    {
        // A Method에 대한 구독을 10초만 한다.
        var stopObserve = Observable.Timer(TimeSpan.FromSeconds(10));
        var observation = RawDataSubject
            .AsObservable()
            .ObserveOn(TaskPoolScheduler.Default)
            .TakeUntil(stopObserve)
            .Select(JsonConvert.DeserializeObject<JsonRpc>)
            .Where(rpc => rpc.Method == "A")
            .Subscribe(
                onNext: rpc => _logger.Log(LogLevel.Information, new EventId(), (ConsoleColor.Yellow, $"A Method: {rpc.Serialize()}"), null, (state, exception) => state.Item2),
                onError: ex => _logger.LogError(ex.Message),
                onCompleted: () => _logger.LogInformation("10초 되었으니까 A 메서드 구독 그만~"));
        _observationList.Add(observation);
    }

    private void MakeObservationMethodB_()
    {
        // B Method에 대한 구독을 3번만 한다.
        var observation = RawDataSubject
            .AsObservable()
            .ObserveOn(TaskPoolScheduler.Default)
            .Select(JsonConvert.DeserializeObject<JsonRpc>)
            .Where(rpc => rpc.Method == "B")
            .Take(3)
            .Subscribe(
                onNext: rpc => _logger.Log(LogLevel.Information, new EventId(), (ConsoleColor.Magenta, $"B Method: {rpc.Serialize()}"), null, (state, exception) => state.Item2),
                onError: ex => _logger.LogError(ex.Message),
                onCompleted: () => _logger.LogInformation("B 메서드 3번만 구독하고 끝."));
        _observationList.Add(observation);
    }

    private void MakeObservationMethodB__()
    {
        // B Method에 대한 구독을 총 3번만 한다.
        var observation = RawDataSubject
            .AsObservable()
            .ObserveOn(TaskPoolScheduler.Default)
            .Take(3) // 여기다가 붙이면 구독 자체를 3번만 한다. Where 밑에 있을 때 Where 조건에 맞는 것을 3번만 한다.
            .Select(JsonConvert.DeserializeObject<JsonRpc>)
            .Where(rpc => rpc.Method == "B")
            .Subscribe(
                onNext: rpc => _logger.Log(LogLevel.Information, new EventId(), (ConsoleColor.DarkYellow, $"B Method: {rpc.Serialize()}"), null, (state, exception) => state.Item2),
                onError: ex => _logger.LogError(ex.Message),
                onCompleted: () => _logger.LogInformation("B 메서드 3번만 구독하고 끝."));
        _observationList.Add(observation);
    }

    private void MakeObservationMethodC_()
    {
        // 10초 마다 구독을 실행시켰을 때 Rx Stream의 데이터가 Where 절을 통과해야 구독이 발생되므로, 구독이 자주 발생되지 않는다.
        var observation = RawDataSubject
            .AsObservable()
            .ObserveOn(TaskPoolScheduler.Default)
            .Sample(TimeSpan.FromSeconds(10)) // 10초마다 구독한다.
            .Select(JsonConvert.DeserializeObject<JsonRpc>)
            .Where(rpc => rpc.Method == "C")
            .Subscribe(
                onNext: rpc =>
                {
                    _logger.Log(LogLevel.Information, new EventId(), (ConsoleColor.DarkCyan, $"C Method를 10초마다 구독."), null, (state, exception) => state.Item2);
                    _logger.Log(LogLevel.Information, new EventId(), (ConsoleColor.DarkCyan, $"C Method: {rpc.Serialize()}"), null, (state, exception) => state.Item2);
                },
                onError: ex => _logger.LogError(ex.Message),
                onCompleted: () => _logger.LogInformation("10초 마다 구독하는 C 메서드 구독 끝."));
        _observationList.Add(observation);
    }

    private void ConnectPipe(TcpClient client)
    {
        while (client.Connected)
        {
            Pipe pipe = new();
            JsonRpcPipeWriteStart(client, pipe.Writer);
            JsonRpcPipeReadAndSocketSendStart(client, pipe.Reader);
        }
    }

    private async void JsonRpcPipeWriteStart(TcpClient client, PipeWriter writer)
    {
        await Task.Factory.StartNew(
            action: async () =>
            {
                while (true)
                {
                    Memory<byte> memory = writer.GetMemory(Statics.ClientToServerBufferSize); // JsonRpcRequest 크기

                    try
                    {
                        int bytesRead = await client.Client.ReceiveAsync(memory);

                        if (bytesRead == 0)
                            continue;

                        writer.Advance(bytesRead);
                    }
                    catch (IOException ex) when (ex.InnerException is SocketException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);

                        break;
                    }

                    FlushResult result = await writer.FlushAsync();

                    if (result.IsCompleted)
                    {
                        await writer.CompleteAsync();

                        break;
                    }
                }
            },
            cancellationToken: CancellationToken.None,
            creationOptions: TaskCreationOptions.LongRunning,
            scheduler: TaskScheduler.Default);
    }

    private void JsonRpcPipeReadAndSocketSendStart(TcpClient client, PipeReader reader)
    {
        _ = Task.Factory.StartNew(
            action: async () =>
            {
                while (true)
                {
                    ReadResult result = await reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    if (buffer.Length > 0)
                    {
                        string jsonData = Encoding.UTF8.GetString(buffer.ToArray());
                        _logger.LogDebug($"Received Source Data: {jsonData}");
                        RawDataSubject.OnNext(jsonData);

                        reader.AdvanceTo(buffer.End);

                        if (result.IsCompleted)
                            break;
                    }
                }
            },
            cancellationToken: CancellationToken.None,
            creationOptions: TaskCreationOptions.LongRunning,
            scheduler: TaskScheduler.Default);
    }
}
