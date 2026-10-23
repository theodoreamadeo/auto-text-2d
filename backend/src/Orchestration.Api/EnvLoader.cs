namespace Orchestration.Api;

public static class EnvLoader
{
    /// <summary>
    /// The real .env lives at the repo root, not next to this project, so search upward
    /// from both the current working directory and the app's base directory until we find it.
    /// </summary>
    public static void LoadRepoRootEnv()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            for (var i = 0; i < 12 && dir is not null; i++, dir = dir.Parent)
            {
                var envPath = Path.Combine(dir.FullName, ".env");
                if (File.Exists(envPath))
                {
                    DotNetEnv.Env.Load(envPath);
                    return;
                }
            }
        }

        Console.Error.WriteLine("Warning: no .env found searching upward from CWD/BaseDirectory.");
    }
}
