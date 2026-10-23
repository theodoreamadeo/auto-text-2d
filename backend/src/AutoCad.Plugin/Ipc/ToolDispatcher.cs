using System.Text.Json;
using AutoCad.Plugin.Tools;
using Autodesk.AutoCAD.Geometry;
using Shared.Ipc;
using Shared.Tools;

namespace AutoCad.Plugin.Ipc;

public static class ToolDispatcher
{
    public static object Dispatch(string tool, JsonElement args) => tool switch
    {
        "create_line" => DispatchCreateLine(args),
        _ => throw new InvalidOperationException($"Unknown tool '{tool}'."),
    };

    private static CreateLineResult DispatchCreateLine(JsonElement args)
    {
        var parsed = args.Deserialize<CreateLineArgs>(JsonDefaults.Options)
            ?? throw new InvalidOperationException("create_line: missing or invalid arguments.");

        if (parsed.Start.Length < 2 || parsed.End.Length < 2)
        {
            throw new InvalidOperationException("create_line: 'start' and 'end' each need at least [x, y].");
        }

        var start = new Point3d(parsed.Start[0], parsed.Start[1], parsed.Start.Length > 2 ? parsed.Start[2] : 0.0);
        var end = new Point3d(parsed.End[0], parsed.End[1], parsed.End.Length > 2 ? parsed.End[2] : 0.0);

        return LineTools.CreateLine(start, end);
    }
}
