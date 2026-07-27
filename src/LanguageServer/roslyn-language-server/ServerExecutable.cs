// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

internal sealed class ServerExecutable
{
    private const string ServerBaseName = "Microsoft.CodeAnalysis.LanguageServer";
    private const string ThinClientBaseName = "roslyn-language-server";

    private ServerExecutable(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException("Expected a non-empty server executable path.", nameof(fileName));

        FileName = fileName;
    }

    /// <summary>The full path to the executable apphost to launch.</summary>
    public string FileName { get; }

    public static ServerExecutable ResolveLanguageServer()
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
    /// omits it, which these projects are not part of), so we launch it directly and point the relaunched apphost at
    /// the runtime hosting us, exactly as for the bundled server.
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

    /// <summary>Starts this executable with all standard streams redirected.</summary>
    public Process Start(IReadOnlyList<string> arguments)
        => Start(arguments, suppressStandardHandleInheritance: false);

    /// <summary>
    /// Starts this executable with all standard streams redirected while preventing this process's standard handles
    /// from being inherited. Used for both stages of the daemon launch so the daemon cannot retain the editor's LSP
    /// stdio pipes.
    /// </summary>
    public Process StartWithStandardHandleInheritanceSuppressed(IReadOnlyList<string> arguments)
        => Start(arguments, suppressStandardHandleInheritance: true);

    private Process Start(IReadOnlyList<string> arguments, bool suppressStandardHandleInheritance)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = FileName,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

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

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        if (!suppressStandardHandleInheritance)
            return StartCore();

        DaemonHandleInheritance.SetStandardHandlesInheritable(false);
        try
        {
            return StartCore();
        }
        finally
        {
            DaemonHandleInheritance.SetStandardHandlesInheritable(true);
        }

        Process StartCore()
            => Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start '{FileName}'.");
    }
}
