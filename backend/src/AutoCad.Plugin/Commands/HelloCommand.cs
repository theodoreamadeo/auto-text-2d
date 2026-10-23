using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AutoCad.Plugin.Tools;

namespace AutoCad.Plugin.Commands;

public class HelloCommand
{
    [CommandMethod("COPILOT_HELLO")]
    public void CreateTestLine()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        if (doc is null)
        {
            return;
        }

        var result = LineTools.CreateLine(new Point3d(0, 0, 0), new Point3d(100, 100, 0));

        doc.Editor.WriteMessage(
            $"\nCOPILOT_HELLO: line created from (0,0) to (100,100), handle={result.Handle}.\n");
    }
}
