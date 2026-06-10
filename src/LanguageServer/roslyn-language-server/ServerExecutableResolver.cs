// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

internal sealed class ServerExecutable
{
    public ServerExecutable(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException("Expected a non-empty server executable path.", nameof(fileName));

        FileName = fileName;
    }

    /// <summary>The full path to the bundled server executable (apphost) to launch.</summary>
    public string FileName { get; }

    /// <summary>
    /// Identifies the server build for pipe/mutex naming so only compatible thin clients and daemons connect. This is
    /// the server executable path (in a versioned NuGet cache directory).
    /// </summary>
    public string ToolIdentifier => FileName;

    /// <summary>
    /// Configures <paramref name="startInfo"/> to launch the bundled server: sets the executable path and ensures the
    /// child apphost can locate the .NET runtime.
    /// </summary>
    public void ConfigureStartInfo(ProcessStartInfo startInfo)
    {
        if (startInfo is null)
            throw new ArgumentNullException(nameof(startInfo));

        startInfo.FileName = FileName;

        // The bundled server is a framework-dependent apphost, which resolves the .NET runtime only via
        // DOTNET_ROOT[_<arch>], the registered install location, or the default install location (e.g.
        // /usr/share/dotnet) - never PATH. When this thin client was itself launched in any environment whose
        // only .NET install is reachable via PATH, the child apphost would otherwise fail to start.
        // Point the child at the runtime that is hosting us so it launches against the
        // same .NET.
        if (RuntimeHostInfo.GetToolDotNetRoot(logger: null) is { } dotNetRoot)
        {
            // Clear any inherited DOTNET_ROOT* variants (e.g. DOTNET_ROOT_X64) so they can't override the value we set.
            foreach (var key in startInfo.Environment.Keys
                .Where(static key => key.StartsWith(RuntimeHostInfo.DotNetRootEnvironmentName, StringComparison.OrdinalIgnoreCase))
                .ToArray())
            {
                startInfo.Environment.Remove(key);
            }

            startInfo.Environment[RuntimeHostInfo.DotNetRootEnvironmentName] = dotNetRoot;
        }
    }
}

internal static class ServerExecutableResolver
{
    private const string ServerBaseName = "Microsoft.CodeAnalysis.LanguageServer";
    private const string ThinClientBaseName = "roslyn-language-server";

    public static ServerExecutable Resolve()
    {
        // The server is always published with an apphost next to the thin client. The only configuration that omits
        // the apphost is .NET source-build, which these projects are not part of, so we always launch it directly
        // (no `dotnet <dll>` fallback needed).
        var appHostPath = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? ServerBaseName + ".exe" : ServerBaseName);

        if (!File.Exists(appHostPath))
        {
            throw new FileNotFoundException(
                $"Could not find the bundled language server executable next to the thin client. Expected '{appHostPath}'.");
        }

        return new ServerExecutable(appHostPath);
    }

    /// <summary>
    /// Resolves this thin client's own apphost (next to it), used to re-launch ourselves as the short-lived daemon
    /// bootstrap. Like the bundled server, the thin client is always published with an apphost (only .NET source-build
    /// omits it, which these projects are not part of), so we launch it directly. <see cref="ServerExecutable.ConfigureStartInfo"/>
    /// also points the relaunched apphost at the runtime hosting us, exactly as for the bundled server.
    /// </summary>
    public static ServerExecutable ResolveSelf()
    {
        var appHostPath = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? ThinClientBaseName + ".exe" : ThinClientBaseName);

        if (!File.Exists(appHostPath))
        {
            throw new FileNotFoundException(
                $"Could not find the thin client executable to re-launch as the daemon bootstrap. Expected '{appHostPath}'.");
        }

        return new ServerExecutable(appHostPath);
    }
}
