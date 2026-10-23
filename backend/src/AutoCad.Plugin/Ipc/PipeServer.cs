using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Shared.Ipc;

namespace AutoCad.Plugin.Ipc;

/// <summary>
/// Listens on a named pipe for newline-delimited JSON tool-call requests and writes back
/// newline-delimited JSON responses. Knows nothing about AutoCAD/Transactions - that's the
/// handler delegate's job (see AutoCadToolExecutor). One request per connection.
/// </summary>
public sealed class PipeServer
{
    private readonly string _pipeName;
    private readonly Func<ToolCallRequest, ToolCallResponse> _handler;
    private CancellationTokenSource? _cts;
    private Thread? _thread;

    public PipeServer(string pipeName, Func<ToolCallRequest, ToolCallResponse> handler)
    {
        _pipeName = pipeName;
        _handler = handler;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _thread = new Thread(() => RunLoop(_cts.Token))
        {
            IsBackground = true,
            Name = "AutoCadCopilot-PipeServer",
        };
        _thread.Start();
    }

    public void Stop()
    {
        _cts?.Cancel();
        _thread?.Join(TimeSpan.FromSeconds(2));
    }

    private void RunLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                RunOneConnectionAsync(token).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Expected on Stop() - exit the loop.
                break;
            }
            catch (ObjectDisposedException)
            {
                // Expected on Stop() cancelling a pending WaitForConnectionAsync - exit the loop.
                break;
            }
            catch
            {
                // Swallow anything else from the accept/connection plumbing itself so one bad
                // connection never takes the whole listener down; just try accepting again.
            }
        }
    }

    private async Task RunOneConnectionAsync(CancellationToken token)
    {
        using var server = new NamedPipeServerStream(
            _pipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);

        await server.WaitForConnectionAsync(token);

        using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        using var writer = new StreamWriter(server, new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n",
        };

        string? line;
        try
        {
            line = await reader.ReadLineAsync(token);
        }
        catch (Exception ex)
        {
            // Connection-level failure before we even got a line - nothing to write back to.
            _ = ex;
            return;
        }

        if (line is null)
        {
            return;
        }

        ToolCallResponse response;
        try
        {
            var request = JsonSerializer.Deserialize<ToolCallRequest>(line, JsonDefaults.Options)
                ?? throw new InvalidOperationException("Empty or invalid tool call request.");
            response = _handler(request);
        }
        catch (Exception ex)
        {
            response = ToolCallResponse.Fail(ex.Message);
        }

        try
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonDefaults.Options));
        }
        catch
        {
            // Client disconnected before we could respond - nothing more to do.
        }
    }
}
