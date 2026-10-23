using Autodesk.AutoCAD.ApplicationServices.Core;
using Shared.Ipc;

namespace AutoCad.Plugin.Ipc;

/// <summary>
/// Marshals a tool call from the pipe server's background thread onto AutoCAD's
/// main document thread, where Transaction/Database calls are actually safe to make.
/// </summary>
public sealed class AutoCadToolExecutor
{
    private readonly SynchronizationContext _autoCadContext;

    public AutoCadToolExecutor(SynchronizationContext autoCadContext)
    {
        _autoCadContext = autoCadContext;
    }

    public ToolCallResponse Execute(ToolCallRequest request)
    {
        object? resultData = null;
        Exception? error = null;
        using var done = new ManualResetEventSlim(false);

        _autoCadContext.Post(_ =>
        {
            try
            {
                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active AutoCAD drawing.");

                // This callback runs in application context, where AutoCAD does not
                // automatically lock the active document for database writes.
                using var documentLock = document.LockDocument();
                resultData = ToolDispatcher.Dispatch(request.Tool, request.Args);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                done.Set();
            }
        }, null);

        if (!done.Wait(TimeSpan.FromSeconds(10)))
        {
            return ToolCallResponse.Fail("Timed out waiting for AutoCAD to execute the tool call.");
        }

        return error is null ? ToolCallResponse.Ok(resultData) : ToolCallResponse.Fail(error.Message);
    }
}
