using DataStructure;
using System.Reactive.Threading.Tasks;

namespace DataGenerator;

internal class TcpServerService(ILogger<TcpServerService> logger, DataContainerService dataContainerService) : BackgroundService
{
    private readonly ILogger<TcpServerService> _logger = logger;
    private readonly DataContainerService dataContainerService = dataContainerService;
    private Dictionary<Task, CancellationTokenSource> connTasks = []; // 소켓 커넥션 리스트

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    for (int start_port = Statics.Port; start_port < Statics.MaxPort; start_port++)
                    {
                        TcpListener listener = new(IPAddress.Loopback, start_port);
                        _logger.LogInformation($"[{IPAddress.Loopback}:{start_port}] Listening...");
                        listener.Start();
                        var cts = new CancellationTokenSource();
                        connTasks.Add(AcceptClientsAsync(listener, cts.Token), cts);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);

                    throw;
                }

                await Task.WhenAll(connTasks.Keys);
            }
        }
        finally
        {
            connTasks.Values.ToList().ForEach(cts => cts.Cancel());
        }
    }

    private async Task AcceptClientsAsync(TcpListener listener, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
            _logger.LogInformation($"[{client.Client.LocalEndPoint}] Accepted!");
            _ = HandleClientAsync(client.GetStream(), stoppingToken);
        }
    }

    private async Task HandleClientAsync(NetworkStream stream, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Pipe pipe = new();
            Task<byte[]> writingTask = WriteToPipe(pipe.Writer, stoppingToken);
            Task readingTask = ReadFromPipeAsync(stream, pipe.Reader, stoppingToken);
            await Task.WhenAll(writingTask, readingTask);
        }
    }

    private Task<byte[]> WriteToPipe(PipeWriter writer, CancellationToken stoppingToken)
    {
        Task<byte[]> observableTask = dataContainerService.RawDataSubject
            .AsObservable() // Linq 형태로 변환, DataTable에 대해서 AsEnemerable()과 비슷한 역할
            .Sample(TimeSpan.FromMilliseconds(Statics.Delay)) // 1.1초 간격으로 데이터를 샘플링
            .ObserveOn(TaskPoolScheduler.Default) // ThreadPool에서 실행 (여기선 별 의미 없음)
            .Select(Encoding.UTF8.GetBytes) // dataContainerService에서 발생된 데이터를 byte[]로 변환
            .Catch<byte[], Exception>(ex =>
            {
                _logger.LogError(ex.Message);
                return Observable.Empty<byte[]>();
            }) // Select하다가 Exception이 발생하면 빈 Observable<byte[]>을 반환하면서 로그 기록
            .Do(
                onNext: async bytearr =>
                {
                    Memory<byte> memory = writer.GetMemory(bytearr.Length);
                    bytearr.CopyTo(memory);
                    writer.Advance(memory.Length); // 이 예제는 데이터가 짧기 때문에 여러번 Advance()를 반복해서 호출할 필요가 없으며, 바로 FlushAsync()를 호출해도 무방.

                    FlushResult result = await writer.FlushAsync(stoppingToken);

                    if (result.IsCompleted) // PipeWriter에 Flush할 게 없으면 완료 처리되며 PipeWriter가 종료되며, 더이상 Write할 수 없고 Pipe를 새로 생성해야 함.
                        await writer.CompleteAsync();
                },
                onError: (ex) =>
                {
                    // 구독하다가 에러가 발생했을 때의 처리.
                },
                onCompleted: () =>
                {
                    // 구독이 완료되었을 때의 처리. dataContainerService.RawDataSubject에서 id가 100이 되면 OnComplete()가 호출되면서 여기에 신호가 도달.
                    writer.Complete();
                })
            .ToTask();

        return observableTask;
    }

    private async Task ReadFromPipeAsync(NetworkStream stream, PipeReader reader, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ReadResult result = await reader.ReadAsync(stoppingToken);
            ReadOnlySequence<byte> buffer = result.Buffer;

            if (buffer.Length > 0)
            {
                if (stream.CanWrite)
                {
                    stream.Write(buffer.ToArray());
                    _logger.LogDebug($"PipeReader: {Encoding.UTF8.GetString(buffer.ToArray())}");
                }
            }

            reader.AdvanceTo(buffer.End);

            if (result.IsCompleted) // PipeWriter에서 Flush된게 더 이상 없으면 완료 처리되며 PipeReader가 종료되며, 더이상 Read할 수 없고 Pipe를 새로 생성해야 함.
                break;
        }

        await reader.CompleteAsync();
    }
}
