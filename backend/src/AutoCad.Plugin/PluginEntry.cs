using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.Runtime;
using AutoCad.Plugin.Ipc;
using Shared.Ipc;

[assembly: ExtensionApplication(typeof(AutoCad.Plugin.PluginEntry))]
[assembly: CommandClass(typeof(AutoCad.Plugin.Commands.HelloCommand))]

namespace AutoCad.Plugin;

public class PluginEntry : IExtensionApplication
{
    private PipeServer? _pipeServer;

    public void Initialize()
    {
        var doc = Application.DocumentManager.MdiActiveDocument;
        doc?.Editor.WriteMessage("\nAutoCAD Copilot plugin loaded. Try the COPILOT_HELLO command.\n");

        var autoCadContext = Autodesk.AutoCAD.Runtime.SynchronizationContext.Current;
        if (autoCadContext is null)
        {
            doc?.Editor.WriteMessage("\nAutoCAD Copilot could not start its pipe listener: no AutoCAD UI synchronization context was available.\n");
            return;
        }

        var executor = new AutoCadToolExecutor(autoCadContext);
        _pipeServer = new PipeServer(PipeConstants.PipeName, executor.Execute);
        _pipeServer.Start();
    }

    public void Terminate()
    {
        _pipeServer?.Stop();
    }
}
