using System.Text.Json;

namespace Shared.Ipc;

public sealed record ToolCallResponse(bool Success, JsonElement? Result, string? Error)
{
    public static ToolCallResponse Ok(object? result) =>
        new(true, result is null ? null : JsonSerializer.SerializeToElement(result, JsonDefaults.Options), null);

    public static ToolCallResponse Fail(string error) => new(false, null, error);
}
