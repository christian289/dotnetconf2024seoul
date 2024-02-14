using DataStructure;

namespace DataGenerator;

internal class DataContainerService
{
    public DataContainerService(ILogger<DataContainerService> logger)
    {
        _logger = logger;

        RawDataSubject = new Subject<string>();
        Cts = new CancellationTokenSource();

        DataGenerate();
    }

    private readonly ILogger<DataContainerService> _logger;

    public Subject<string> RawDataSubject { get; init; }
    public CancellationTokenSource Cts { get; set; }

    private async void DataGenerate()
    {
        int request_id = default;
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(Statics.TimerInterval));

        while (await timer.WaitForNextTickAsync(Cts.Token))
            request_id = JsonRpcSelector(request_id);
    }

    private int JsonRpcSelector(int id)
    {
        JsonRpc request;

        if (id % 3 == 0)
            request = new(new AMethod(), id);
        else if (id % 3 == 1)
            request = new(new BMethod(), id);
        else
            request = new(new CMethod(), id);

        string json_str = request.Serialize();

        _logger.LogDebug($"Generated Json String Data:\r\n{json_str}");

        json_str = json_str.Replace("\r", string.Empty).Replace("\n", string.Empty);
        RawDataSubject.OnNext(json_str);

        id++;

        if (id == 100)
        {
            RawDataSubject.OnCompleted(); // 완료처리
            return 0;
        }
        //if (id == int.MaxValue)
        //    return 0;
        else
            return id;
    }
}