using DataStructure;

namespace DataGenerator;

internal class TcpServerService(ILogger<TcpServerService> logger) : BackgroundService
{
    private readonly ILogger<TcpServerService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<Task> connTasks = []; // new(); 가 아니라 []; 이건...새로운 문법인가...?

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                for (int start_port = Statics.Port; start_port < Statics.MaxPort; start_port++)
                {
                    TcpListener listener = new(IPAddress.Loopback, start_port);
                    listener.Start();
                    _logger.LogInformation($"[{IPAddress.Loopback}:{start_port}] Listening...");
                    connTasks.Add(AcceptClientsAsync(listener, stoppingToken));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

                throw;
            }

            await Task.WhenAll(connTasks);
        }
    }

    private async Task AcceptClientsAsync(TcpListener listener, CancellationToken stoppingToken)
    {
        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
            _logger.LogInformation($"[{client.Client.LocalEndPoint}] Accepted!");

            try
            {
                _ = HandleClientAsync(client.GetStream(), stoppingToken);
            }
            catch (SocketException)
            {
                listener.Stop();
                listener.Start();
            }
        }
    }

    private async Task HandleClientAsync(NetworkStream stream, CancellationToken stoppingToken)
    {
        Pipe pipe = new();
        //Task writingTask = WriteToPipeAsync(stream, pipe.Writer, stoppingToken);
        Task writingTask = FillPipeAsync(stream, pipe.Writer, stoppingToken);
        Task readingTask = ReadFromPipeAsync(pipe.Reader, stoppingToken);

        await Task.WhenAll(writingTask, readingTask);
        stream.Close();
    }

    private async Task FillPipeAsync(NetworkStream stream, PipeWriter writer, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Memory<byte> memory = writer.GetMemory(Statics.ClientToServerBufferSize); // JsonRpcRequest 크기

            try
            {
                int bytesRead = await stream.ReadAsync(memory, stoppingToken);

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

            FlushResult result = await writer.FlushAsync(stoppingToken);

            if (result.IsCompleted)
                break;
        }

        writer.Complete();
    }

    private async Task WriteToPipeAsync(NetworkStream stream, PipeWriter writer, CancellationToken stoppingToken)
    {
        Stopwatch stopwatch = new();

        while (!stoppingToken.IsCancellationRequested)
        {
            stopwatch.Restart();

            try
            {
                string jsonData = GenerateRandomJsonString(3, 500);
                byte[] data = Encoding.UTF8.GetBytes(jsonData);

                Memory<byte> memory = writer.GetMemory(data.Length);
                data.CopyTo(memory);
                writer.Advance(data.Length);

                FlushResult result = await writer.FlushAsync(stoppingToken);

                if (result.IsCompleted)
                    break;
            }
            catch
            {
                break;
            }

            stopwatch.Stop();

            int delay = 100 - (int)stopwatch.ElapsedMilliseconds;

            if (delay > 0)
                await Task.Delay(delay, stoppingToken);
        }

        await writer.CompleteAsync();
    }

    private async Task ReadFromPipeAsync(PipeReader reader, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ReadResult result = await reader.ReadAsync(stoppingToken);
            ReadOnlySequence<byte> buffer = result.Buffer;

            reader.AdvanceTo(buffer.End);

            if (result.IsCompleted)
                break;
        }

        await reader.CompleteAsync();
    }

    private string GenerateRandomJsonString(int depth, int stringLength)
    {
        var random = new Random();
        var data = new Dictionary<string, object>();

        for (int i = 0; i < stringLength; i++)
        {
            string key = "Key" + i;
            data[key] = depth > 1 ? GenerateRandomJsonString(depth - 1, stringLength / 2) : random.Next(1000, 9999).ToString();
        }

        return JsonConvert.SerializeObject(data);
    }
}
