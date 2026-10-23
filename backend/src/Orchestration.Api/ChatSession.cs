using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Shared.Ipc;

namespace Orchestration.Api;

/// <summary>
/// Owns the conversation history and runs the manual Claude tool-use loop for one chat session:
/// call the model, execute any tool_use blocks via the named-pipe bridge to the AutoCAD plugin,
/// feed the results back, and repeat until Claude returns a turn with no tool calls.
/// </summary>
public sealed class ChatSession
{
    private const int MaxToolRoundsPerTurn = 10;

    private readonly AnthropicClient _client;
    private readonly string _model;
    private readonly List<ToolUnion> _tools;
    private readonly NamedPipeAutoCadBridge _bridge;
    private readonly List<MessageParam> _history = [];

    public ChatSession(AnthropicClient client, string model, List<ToolUnion> tools, NamedPipeAutoCadBridge bridge)
    {
        _client = client;
        _model = model;
        _tools = tools;
        _bridge = bridge;
    }

    public async Task<string> SendAsync(string userText, CancellationToken ct = default)
    {
        _history.Add(new MessageParam { Role = Role.User, Content = userText });

        for (var round = 0; round < MaxToolRoundsPerTurn; round++)
        {
            var response = await _client.Messages.Create(new MessageCreateParams
            {
                Model = _model,
                MaxTokens = 16000,
                Tools = _tools,
                Messages = _history,
            }, ct);

            var finalText = new StringBuilder();
            List<ContentBlockParam> assistantContent = [];
            List<ContentBlockParam> toolResults = [];

            foreach (var block in response.Content)
            {
                if (block.TryPickText(out TextBlock? text))
                {
                    assistantContent.Add(new TextBlockParam { Text = text.Text });
                    finalText.Append(text.Text);
                }
                else if (block.TryPickToolUse(out ToolUseBlock? toolUse))
                {
                    assistantContent.Add(new ToolUseBlockParam
                    {
                        ID = toolUse.ID,
                        Name = toolUse.Name,
                        Input = toolUse.Input,
                    });

                    var (content, isError) = await ExecuteToolAsync(toolUse, ct);
                    toolResults.Add(new ToolResultBlockParam
                    {
                        ToolUseID = toolUse.ID,
                        Content = content,
                        IsError = isError,
                    });
                }
            }

            _history.Add(new MessageParam { Role = Role.Assistant, Content = assistantContent });

            if (toolResults.Count == 0)
            {
                return finalText.ToString();
            }

            _history.Add(new MessageParam { Role = Role.User, Content = toolResults });
        }

        return "(stopped: too many tool-call rounds in a single turn)";
    }

    private async Task<(string Content, bool IsError)> ExecuteToolAsync(ToolUseBlock toolUse, CancellationToken ct)
    {
        var argsElement = JsonSerializer.SerializeToElement(toolUse.Input);
        var request = new ToolCallRequest(toolUse.Name, argsElement);

        ToolCallResponse response;
        try
        {
            response = await _bridge.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            return ($"{{\"error\": \"IPC failure: {ex.Message}\"}}", true);
        }

        if (!response.Success)
        {
            return ($"{{\"error\": {JsonSerializer.Serialize(response.Error)}}}", true);
        }

        var resultJson = response.Result is { } r ? r.GetRawText() : "null";
        return (resultJson, false);
    }
}
