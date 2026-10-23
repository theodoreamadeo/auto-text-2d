using System.Text.Json;
using Anthropic.Models.Messages;

namespace Orchestration.Api;

public static class ToolCatalog
{
    public static List<ToolUnion> BuildTools()
    {
        return
        [
            new ToolUnion(new Tool
            {
                Name = "create_line",
                Description = "Draw a straight line in the current AutoCAD drawing between two points. " +
                               "Call this when the user asks to draw, create, or add a line.",
                InputSchema = new()
                {
                    Properties = new Dictionary<string, JsonElement>
                    {
                        ["start"] = JsonSerializer.SerializeToElement(new
                        {
                            type = "array",
                            items = new { type = "number" },
                            description = "Start point [x, y] or [x, y, z] in drawing units.",
                        }),
                        ["end"] = JsonSerializer.SerializeToElement(new
                        {
                            type = "array",
                            items = new { type = "number" },
                            description = "End point [x, y] or [x, y, z] in drawing units.",
                        }),
                    },
                    Required = ["start", "end"],
                },
            }),
        ];
    }
}
