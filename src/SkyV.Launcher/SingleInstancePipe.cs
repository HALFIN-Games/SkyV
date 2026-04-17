using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkyV.Launcher;

public sealed class SingleInstancePipe : IDisposable
{
    private readonly string pipeName;
    private readonly Action<string[]> onArgs;
    private readonly CancellationTokenSource cts = new();
    private Task? serverTask;

    public SingleInstancePipe(string pipeName, Action<string[]> onArgs)
    {
        this.pipeName = pipeName;
        this.onArgs = onArgs;
    }

    public void Start()
    {
        serverTask = Task.Run(ServerLoopAsync);
    }

    public static bool TrySend(string pipeName, string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
            client.Connect(250);
            using var sw = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };
            sw.WriteLine(string.Join('\u001F', args ?? Array.Empty<string>()));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task ServerLoopAsync()
    {
        while (!cts.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(cts.Token);
                using var sr = new StreamReader(server, new UTF8Encoding(false));
                var line = await sr.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var split = line.Split('\u001F', StringSplitOptions.None);
                    onArgs(split);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
            }
        }
    }

    public void Dispose()
    {
        cts.Cancel();
        try { serverTask?.Wait(250); } catch { }
        cts.Dispose();
    }
}

