using System.Text.Json;

namespace Shared.Ipc;

public sealed record ToolCallRequest(string Tool, JsonElement Args);
