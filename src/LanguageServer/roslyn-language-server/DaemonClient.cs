// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using Microsoft.CodeAnalysis.LanguageServer.Daemon;

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

internal sealed class DaemonConnectResult : IDisposable
{
    private DaemonConnectResult(bool daemonConnected, Stream? stream)
    {
        DaemonConnected = daemonConnected;
        Stream = stream;
    }

    [MemberNotNullWhen(true, nameof(Stream))]
    public bool DaemonConnected { get; }

    public Stream? Stream { get; }

    public static DaemonConnectResult Connected(Stream stream)
        => new(daemonConnected: true, stream);

    public static DaemonConnectResult FallbackToNonDaemon()
        => new(daemonConnected: false, stream: null);

    public void Dispose()
        => Stream?.Dispose();
}

internal static class DaemonClient
{
    private const int DaemonMutexTimeoutMs = 20_000;
    private const int ExistingDaemonConnectTimeoutMs = 5_000;
    private const int NewDaemonConnectTimeoutMs = 20_000;

    public static Task<DaemonConnectResult> ConnectAsync(
        ServerExecutable executable,
        IReadOnlyList<string> serverArguments)
    {
        var pipeName = GetDaemonPipeName(executable);

        using var clientMutex = new DaemonClientMutex(pipeName, out _);
        if (!clientMutex.IsLocked && !clientMutex.TryLock(DaemonMutexTimeoutMs))
        {
            Console.Error.WriteLine($"Timed out waiting for the daemon startup mutex for pipe '{pipeName}'. Falling back to non-daemon mode.");
            return Task.FromResult(DaemonConnectResult.FallbackToNonDaemon());
        }

        var launchedDaemon = false;
        var serverWasRunning = DaemonServerMutex.IsRunning(pipeName);
        if (!serverWasRunning)
        {
            LaunchDaemon(pipeName, serverArguments);
            launchedDaemon = true;
        }

        var pipeClient = NamedPipeUtil.CreateClient(serverName: ".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        try
        {
            var connectTimeoutMs = launchedDaemon ? NewDaemonConnectTimeoutMs : ExistingDaemonConnectTimeoutMs;
            pipeClient.Connect(connectTimeoutMs);
            return Task.FromResult(DaemonConnectResult.Connected(pipeClient));
        }
        catch
        {
            pipeClient.Dispose();
            throw;
        }
    }

    private static string GetDaemonPipeName(ServerExecutable executable)
    {
        // Honor an explicit override so independent instances (chiefly end-to-end tests) can run isolated
        // daemons. Normal clients leave it unset and derive the name from the bundled server path so only
        // version-compatible clients share a daemon.
        var pipeNameOverride = Environment.GetEnvironmentVariable(DaemonPipeName.PipeNameOverrideEnvironmentVariable);
        return string.IsNullOrEmpty(pipeNameOverride)
            ? DaemonPipeName.GetPipeName(executable.ToolIdentifier)
            : pipeNameOverride;
    }

    private static void LaunchDaemon(
        string pipeName,
        IReadOnlyList<string> serverArguments)
    {
        // Launch a second copy of THIS thin client as the daemon bootstrap. It re-launches the real server --daemon
        // and then exits, orphaning the daemon out of our (and so the editor's) process tree. See DaemonBootstrap.
        var bootstrapExecutable = ServerExecutableResolver.ResolveSelf();

        var bootstrapArguments = new List<string>(serverArguments.Count + 3)
        {
            DaemonBootstrap.BootstrapArgument,
            "--pipe",
            pipeName,
        };
        bootstrapArguments.AddRange(serverArguments);

        // The daemon is shared across clients and must outlive whichever client launches it. Rather than launch the
        // daemon directly - which would leave it a child of this thin client and so vulnerable to an editor tearing
        // down this client's process tree (process-tree teardowns walk parent/child links, which job breakaway and
        // setsid don't change) - we launch a short-lived "bootstrap" (a second copy of this thin client). The bootstrap
        // launches the real daemon and then exits, orphaning the daemon so it is no longer in this client's process
        // tree. See DaemonBootstrap. We forward the bootstrap's standard error to ours so daemon startup diagnostics
        // (including a failure to find the .NET runtime) still reach the host, and drain its standard output and input
        // so it can't corrupt the editor's LSP channel in stdio mode.
        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
        };
        bootstrapExecutable.ConfigureStartInfo(startInfo);
        foreach (var argument in bootstrapArguments)
            startInfo.ArgumentList.Add(argument);

        // On Windows, mark our standard handles non-inheritable across the launch. A redirected Process.Start uses
        // CreateProcess(bInheritHandles: true), which would otherwise leak our own standard handles - the editor's LSP
        // stdio pipes - to the bootstrap and, transitively, to the long-lived daemon it spawns. A daemon holding those
        // pipes keeps them from ever reaching EOF after we exit (so the editor's WaitForExit/output draining hangs) and
        // in stdio mode corrupts the editor's LSP channel. The bootstrap applies the same protection when it launches
        // the daemon. A no-op off Windows.
        Process process;
        DaemonHandleInheritance.SetStandardHandlesInheritable(false);
        try
        {
            process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start the language server daemon bootstrap process.");
        }
        finally
        {
            DaemonHandleInheritance.SetStandardHandlesInheritable(true);
        }

        // The bootstrap reads nothing from its stdin; close our write end so it sees EOF if it ever does.
        try
        {
            process.StandardInput.Close();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
        }

        // Drain the bootstrap's stdout so it never blocks on a full pipe, and forward its stderr onto ours (never our
        // stdout, which carries LSP in stdio mode). Both end on their own once the bootstrap exits, shortly after it has
        // launched the daemon. The daemon then logs to its own files.
        _ = ProcessUtilities.ForwardStreamAsync(process.StandardOutput.BaseStream, Stream.Null, CancellationToken.None);
        _ = ForwardAndDisposeStandardErrorAsync(process.StandardError.BaseStream);
        Console.Error.WriteLine($"Started language server daemon bootstrap (pid {process.Id}): {ProcessUtilities.GetCommandLineForDisplay(bootstrapExecutable, bootstrapArguments)}");
    }

    private static Task ForwardAndDisposeStandardErrorAsync(Stream daemonStandardError)
    {
        return Task.Run(async () =>
        {
            try
            {
                await ProcessUtilities.CopyStreamAsync(daemonStandardError, Console.OpenStandardError(), CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                await daemonStandardError.DisposeAsync().ConfigureAwait(false);
            }
        }, CancellationToken.None);
    }
}
