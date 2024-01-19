using DataStructure;

namespace DataGenerator;

internal class TcpServerService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TcpListener listener = new(IPAddress.Loopback, Statics.Port);
        listener.Start();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);
                    _ = HandleClientAsync(client.GetStream(), stoppingToken);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(NetworkStream stream, CancellationToken stoppingToken)
    {
        Pipe pipe = new();
        Task writingTask = WriteToPipeAsync(stream, pipe.Writer, stoppingToken);
        Task readingTask = ReadFromPipeAsync(pipe.Reader, stoppingToken);

        await Task.WhenAll(writingTask, readingTask);
        stream.Close();
    }

    private async Task FillPipeAsync(NetworkStream stream, PipeWriter writer, CancellationToken stoppingToken)
    {
        Stopwatch stopwatch = new();

        while (!stoppingToken.IsCancellationRequested)
        {
            stopwatch.Restart();

            Memory<byte> memory = writer.GetMemory(Statics.BufferSize);

            try
            {
                int bytesRead = await stream.ReadAsync(memory, stoppingToken);

                if (bytesRead == 0)
                    break;

                writer.Advance(bytesRead);
            }
            catch (Exception ex)
            {
                //LogError(ex);

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
