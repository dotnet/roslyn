// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    /// Upper bound on how long the bootstrap waits for the daemon to become ready before terminating it. The
    /// bootstrap normally exits as soon as the daemon signals readiness (within a few seconds); this only caps a
    /// pathologically slow or hung startup so the bootstrap process itself never lingers indefinitely.
    /// </summary>
    internal static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(60);

    /// <summary>Whether <paramref name="args"/> request the daemon bootstrap stage.</summary>
    public static bool IsBootstrapRequested(string[] args)
        => Array.IndexOf(args, BootstrapArgument) >= 0;

    /// <summary>
    /// Launches the real daemon detached from this process tree, forwards its startup diagnostics until it is ready
    /// (or fails), and returns an exit code. Returning causes this bootstrap to exit, which orphans the running daemon.
    /// </summary>
    public static async Task<int> RunAsync(string[] args)
    {
        if (!TryGetPipeName(args, out var pipeName, out var error))
        {
            Console.Error.WriteLine(error);
            return ExitCodes.BadArguments;
        }

        // The daemon command line: our args, but the thin-client bootstrap marker becomes the server's --daemon flag.
        var daemonArguments = Array.ConvertAll(args, static arg => arg == BootstrapArgument ? DaemonArgument : arg);

        var executable = ServerExecutable.ResolveLanguageServer();

        var daemonProcess = executable.StartWithStandardHandleInheritanceSuppressed(daemonArguments);

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

        var readiness = await WaitForReadyOrExitAsync(daemonProcess, pipeName).ConfigureAwait(false);

        if (readiness.State == DaemonReadinessState.Exited)
        {
            // The daemon exited during startup (e.g. it failed to compose, or another daemon already owns the pipe).
            // Flush its diagnostics so the failure reason surfaces, then propagate its exit code.
            await FlushForwardersAsync(drainStandardOutput, forwardStandardError).ConfigureAwait(false);
            var exitCode = readiness.ExitCode!.Value;
            Console.Error.WriteLine($"The language server daemon exited during startup with code {exitCode}.");
            daemonProcess.Dispose();
            return exitCode;
        }

        if (readiness.State == DaemonReadinessState.TimedOut)
        {
            Console.Error.WriteLine($"Timed out waiting {ReadyTimeout} for the language server daemon to become ready; terminating it.");
            await TerminateDaemonAsync(daemonProcess).ConfigureAwait(false);
            await FlushForwardersAsync(drainStandardOutput, forwardStandardError).ConfigureAwait(false);
            daemonProcess.Dispose();
            return ExitCodes.DaemonReadyTimeout;
        }

        // The daemon is ready. Return immediately to orphan it; it now owns its own lifetime and logs to its files.
        // We intentionally do not wait on the forwarders here so the orphaning isn't delayed.
        forwardingCancellation.Cancel();
        daemonProcess.Dispose();
        return ExitCodes.Success;
    }

    /// <summary>
    /// Polls until the daemon signals readiness by acquiring its server mutex, exits, or the ready timeout elapses.
    /// </summary>
    private static async Task<DaemonReadinessResult> WaitForReadyOrExitAsync(Process daemonProcess, string pipeName)
    {
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            if (daemonProcess.HasExited)
                return DaemonReadinessResult.Exited(daemonProcess.ExitCode);

            // The daemon holds its server mutex for its whole lifetime once it is ready to accept clients, so its
            // existence is our readiness signal.
            if (DaemonServerMutex.IsRunning(pipeName))
                return DaemonReadinessResult.Ready;

            if (stopwatch.Elapsed >= ReadyTimeout)
                return DaemonReadinessResult.TimedOut;

            await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
        }
    }

    private static async Task TerminateDaemonAsync(Process daemonProcess)
    {
        try
        {
            if (!daemonProcess.HasExited)
                daemonProcess.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
        }

        await daemonProcess.WaitForExitAsync().ConfigureAwait(false);
    }

    private static async Task FlushForwardersAsync(Task drainStandardOutput, Task forwardStandardError)
    {
        // The caller invokes this only after the daemon exited or was killed, so both redirected streams should reach
        // EOF. Wait briefly for forwarding to finish, but cap the wait in case a stream fails to complete.
        var flushed = Task.WhenAll(drainStandardOutput, forwardStandardError);
        await Task.WhenAny(flushed, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
    }

    private static bool TryGetPipeName(string[] args, [NotNullWhen(true)] out string? pipeName, [NotNullWhen(false)] out string? error)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == PipeArgument)
            {
                pipeName = args[i + 1];
                if (string.IsNullOrWhiteSpace(pipeName))
                {
                    error = "Expected a non-empty value for --pipe.";
                    pipeName = null;
                    return false;
                }

                error = null;
                return true;
            }
        }

        pipeName = null;
        error = "Expected --pipe <name> when launching the daemon bootstrap.";
        return false;
    }

    private enum DaemonReadinessState
    {
        Ready,
        Exited,
        TimedOut,
    }

    private readonly record struct DaemonReadinessResult(DaemonReadinessState State, int? ExitCode = null)
    {
        public static DaemonReadinessResult Ready { get; } = new(DaemonReadinessState.Ready);
        public static DaemonReadinessResult TimedOut { get; } = new(DaemonReadinessState.TimedOut);

        public static DaemonReadinessResult Exited(int exitCode)
            => new(DaemonReadinessState.Exited, exitCode);
    }
}
