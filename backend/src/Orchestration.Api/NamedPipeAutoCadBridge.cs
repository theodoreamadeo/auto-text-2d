using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Shared.Ipc;

namespace Orchestration.Api;

/// <summary>
/// The pipe client half of the IPC - opens a new named-pipe connection per tool call and
/// does one request/response round trip, mirroring PipeServer's one-request-per-connection loop.
/// </summary>
public sealed class NamedPipeAutoCadBridge
{
    private const int ConnectTimeoutMs = 5000;

    public async Task<ToolCallResponse> SendAsync(ToolCallRequest request, CancellationToken ct = default)
    {
        using var client = new NamedPipeClientStream(
            ".", PipeConstants.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        try
        {
            await client.ConnectAsync(ConnectTimeoutMs, ct);
        }
        catch (Exception ex)
        {
            return ToolCallResponse.Fail(
                $"Could not reach the AutoCAD plugin (pipe '{PipeConstants.PipeName}'): {ex.Message}. " +
                "Is AutoCAD running with the plugin NETLOADed?");
        }

        using var writer = new StreamWriter(client, new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n",
        };
        using var reader = new StreamReader(client, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);

        await writer.WriteLineAsync(JsonSerializer.Serialize(request, JsonDefaults.Options));

        var line = await reader.ReadLineAsync(ct);
        if (line is null)
        {
            return ToolCallResponse.Fail("Pipe closed before a response was received.");
        }

        return JsonSerializer.Deserialize<ToolCallResponse>(line, JsonDefaults.Options)
            ?? ToolCallResponse.Fail("Empty or invalid tool response.");
    }
}
