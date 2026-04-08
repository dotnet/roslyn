using Microsoft.Build.Locator;

namespace LspVGrepTool.Infrastructure;

internal static class MsBuildRegistration
{
    private static readonly object SyncRoot = new();
    private static bool s_registered;

    public static void EnsureRegistered()
    {
        if (s_registered || MSBuildLocator.IsRegistered)
        {
            s_registered = true;
            return;
        }

        lock (SyncRoot)
        {
            if (s_registered || MSBuildLocator.IsRegistered)
            {
                s_registered = true;
                return;
            }

            MSBuildLocator.RegisterDefaults();
            s_registered = true;
        }
    }
}
