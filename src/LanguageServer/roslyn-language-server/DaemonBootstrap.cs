// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Daemon;

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

/// <summary>
/// The short-lived "bootstrap" stage of the shared daemon's double launch, run as a second copy of this thin client.
/// <para>
/// The thin client cannot launch the daemon directly: the daemon would be a descendant of the thin client and so be
/// torn down when an editor kills the thin client's process tree (process-tree teardowns walk parent/child links,
/// which neither Windows job-object breakaway nor Unix <c>setsid</c> change). Instead, when a client needs a daemon it
/// launches this bootstrap - a second, short-lived copy of the thin client (see <see cref="DaemonClient"/>) - which
/// launches the real <c>Microsoft.CodeAnalysis.LanguageServer --daemon</c> and then exits, orphaning the daemon out of
/// the editor's process tree. Keeping all of this process-launch plumbing in the thin client lets the language server
/// stay a plain LSP server that only understands <c>--daemon</c>.
/// </para>
/// </summary>
internal static class DaemonBootstrap
{
    /// <summary>Marker the thin client passes to request this bootstrap stage; replaced by <c>--daemon</c> for the server.</summary>
    public const string BootstrapArgument = "--daemon-launch";
    private const string DaemonArgument = "--daemon";
    private const string PipeArgument = "--pipe";

    /// <summary>
    /// Upper bound on how long the bootstrap waits for the daemon to become ready before orphaning it anyway. The
    /// bootstrap normally exits as soon as the daemon signals readiness (within a few seconds); this only caps a
    /// pathologically slow or hung startup so the bootstrap process itself never lingers indefinitely.
    /// </summary>
    private static readonly TimeSpan s_readyTimeout = TimeSpan.FromSeconds(60);

    /// <summary>Whether <paramref name="args"/> request the daemon bootstrap stage.</summary>
    public static bool IsBootstrapRequested(string[] args)
        => Array.IndexOf(args, BootstrapArgument) >= 0;

    /// <summary>
    /// Launches the real daemon detached from this process tree, forwards its startup diagnostics until it is ready
    /// (or fails), and returns an exit code. Returning causes this bootstrap to exit, which orphans the running daemon.
    /// </summary>
    public static async Task<int> RunAsync(string[] args)
    {
        // The daemon command line: our args, but the thin-client bootstrap marker becomes the server's --daemon flag.
        var daemonArguments = Array.ConvertAll(args, static arg => arg == BootstrapArgument ? DaemonArgument : arg);

        var executable = ServerExecutableResolver.Resolve();

        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        // Resolve the executable path and the environment (notably DOTNET_ROOT, so the bundled apphost finds the
        // runtime) exactly as a normal server launch would.
        executable.ConfigureStartInfo(startInfo);
        foreach (var argument in daemonArguments)
            startInfo.ArgumentList.Add(argument);

        var daemonProcess = StartDaemon(startInfo);

        // The daemon serves clients over its named pipe, never stdin; close our write end so it sees EOF if it reads.
        try
        {
            daemonProcess.StandardInput.Close();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
        }

        // While we wait below, drain the daemon's stdout (verbose trace, also written to its log files) so it can't
        // block on a full pipe during startup, and forward its stderr to ours - which our parent thin client in turn
        // forwards - so the daemon's startup diagnostics still reach the editor's output. Keeping these pipes attached
        // until the daemon is ready also means it isn't writing to a closed stream while it still logs to the console
        // during startup; once it's ready it logs to its files instead.
        using var forwardingCancellation = new CancellationTokenSource();
        var drainStandardOutput = ProcessUtilities.CopyStreamAsync(daemonProcess.StandardOutput.BaseStream, Stream.Null, forwardingCancellation.Token);
        var forwardStandardError = ProcessUtilities.CopyStreamAsync(daemonProcess.StandardError.BaseStream, Console.OpenStandardError(), forwardingCancellation.Token);

        var daemonExitCode = await WaitForReadyOrExitAsync(daemonProcess, GetPipeName(args)).ConfigureAwait(false);

        forwardingCancellation.Cancel();

        if (daemonExitCode is int exitCode)
        {
            // The daemon exited during startup (e.g. it failed to compose, or another daemon already owns the pipe).
            // Flush its diagnostics so the failure reason surfaces, then propagate its exit code.
            await FlushForwardersAsync(drainStandardOutput, forwardStandardError).ConfigureAwait(false);
            Console.Error.WriteLine($"The language server daemon exited during startup with code {exitCode}.");
            return exitCode;
        }

        // The daemon is ready (or we hit the cap). Return immediately to orphan it; it now owns its own lifetime and
        // logs to its files. We intentionally do not wait on the forwarders here so the orphaning isn't delayed.
        return ExitCodes.Success;
    }

    private static Process StartDaemon(ProcessStartInfo startInfo)
    {
        // On Windows, mark our standard handles non-inheritable across the launch so the long-lived daemon doesn't
        // inherit (copies of) our standard handles - which, through the editor -> thin client -> us launch chain, are
        // the editor's LSP stdio pipes. A daemon holding those would keep them from reaching EOF after we exit and, in
        // stdio mode, corrupt the editor's LSP channel. A no-op off Windows.
        DaemonHandleInheritance.SetStandardHandlesInheritable(false);
        try
        {
            return Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start the language server daemon process.");
        }
        finally
        {
            DaemonHandleInheritance.SetStandardHandlesInheritable(true);
        }
    }

    /// <summary>
    /// Polls until the daemon signals readiness by acquiring its server mutex, exits, or the ready timeout elapses.
    /// Returns the daemon's exit code if it exited, or <see langword="null"/> if it became ready (or the cap elapsed).
    /// </summary>
    private static async Task<int?> WaitForReadyOrExitAsync(Process daemonProcess, string? pipeName)
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            if (daemonProcess.HasExited)
                return daemonProcess.ExitCode;

            // The daemon holds its server mutex for its whole lifetime once it is ready to accept clients, so its
            // existence is our readiness signal. (We always pass --pipe; if it is somehow absent we can't detect
            // readiness and simply fall through to the timeout cap.)
            if (pipeName is not null && DaemonServerMutex.IsRunning(pipeName))
                return null;

            if (stopwatch.Elapsed >= s_readyTimeout)
                return null;

            await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
        }
    }

    private static async Task FlushForwardersAsync(Task drainStandardOutput, Task forwardStandardError)
    {
        // The daemon has exited, so its streams are at EOF and the forwarders will complete on their own; bound the
        // wait so a stuck stream can't hang the bootstrap.
        var flushed = Task.WhenAll(drainStandardOutput, forwardStandardError);
        await Task.WhenAny(flushed, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
    }

    private static string? GetPipeName(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == PipeArgument)
                return args[i + 1];
        }

        return null;
    }
}
