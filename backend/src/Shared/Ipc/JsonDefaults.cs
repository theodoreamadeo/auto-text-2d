using System.Text.Json;

namespace Shared.Ipc;

/// <summary>
/// Shared JSON policy for the pipe wire protocol: camelCase on the wire (matches the
/// documented {"tool":"create_line","args":{...}} shape), case-insensitive on read so
/// casing mismatches between hand-written test clients and C# PascalCase properties don't
/// silently produce null/default values.
/// </summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };
}
