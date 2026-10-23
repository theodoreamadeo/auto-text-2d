using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Shared.Tools;

namespace AutoCad.Plugin.Tools;

public static class LineTools
{
    public static CreateLineResult CreateLine(Point3d start, Point3d end)
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        var db = doc.Database;

        string handle;

        using (var tr = db.TransactionManager.StartTransaction())
        {
            var blockTable = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var modelSpace = (BlockTableRecord)tr.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            using var line = new Line(start, end);
            modelSpace.AppendEntity(line);
            tr.AddNewlyCreatedDBObject(line, true);

            tr.Commit();

            handle = line.ObjectId.Handle.ToString();
        }

        return new CreateLineResult(
            handle,
            [start.X, start.Y, start.Z],
            [end.X, end.Y, end.Z]);
    }
}
