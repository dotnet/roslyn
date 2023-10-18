// This is a copy of Metalama.Backstage.Utilities.MetalamaPathUtilities.

using System;
using System.IO;

namespace Metalama.Compiler;

internal static class MetalamaPathUtilities
{
    private static readonly string? s_overriddenTempPath;

    static MetalamaPathUtilities()
    {
        var overriddenTempPath = Environment.GetEnvironmentVariable("METALAMA_TEMP");
        s_overriddenTempPath = string.IsNullOrEmpty(overriddenTempPath) ? null : overriddenTempPath;
    }

    public static string GetTempPath() => s_overriddenTempPath ?? Path.GetTempPath();

    public static string GetTempFileName()
    {
        if (s_overriddenTempPath == null)
        {
            return Path.GetTempFileName();
        }

        // https://stackoverflow.com/a/10152460/4100001
        var attempt = 0;

        while (true)
        {
            var path = Path.Combine(s_overriddenTempPath, $"{Guid.NewGuid()}.tmp");

            try
            {
                using (var newFile = new FileStream(path, FileMode.Create))
                {
                    newFile.Close();
                }
            }
            catch (IOException) when (++attempt < 10) { continue; }

            return path;
        }
    }
}
