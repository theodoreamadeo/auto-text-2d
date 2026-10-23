using Anthropic;
using Orchestration.Api;

EnvLoader.LoadRepoRootEnv();

var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("ANTHROPIC_API_KEY is not set (checked .env and environment). Exiting.");
    return;
}

var model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? "claude-opus-5-5";
Console.WriteLine($"Using model: {model}");

var client = new AnthropicClient();
var bridge = new NamedPipeAutoCadBridge();
var session = new ChatSession(client, model, ToolCatalog.BuildTools(), bridge);

Console.WriteLine("AutoCAD Copilot terminal harness. Type a message, or Ctrl+C to exit.");

while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (line is null)
    {
        break;
    }

    if (string.IsNullOrWhiteSpace(line))
    {
        continue;
    }

    try
    {
        var reply = await session.SendAsync(line);
        Console.WriteLine(reply);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
    }
}
