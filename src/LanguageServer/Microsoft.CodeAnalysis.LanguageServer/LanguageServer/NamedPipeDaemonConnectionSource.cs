// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias MSBuildWorkspaces;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServer.Daemon;
using Microsoft.Extensions.Logging;

// Reuse the compiler server's named-pipe helper (Asynchronous | WriteThrough | CurrentUserOnly,
// MaxAllowedServerInstances, and Unix /tmp socket-path handling). It is source-linked into
// Microsoft.CodeAnalysis.Workspaces.MSBuild, which this project already references under the
// MSBuildWorkspaces alias, so we use that already-compiled copy rather than source-linking another
// copy into this assembly (which would collide with the MSBuild build host's copy of the same type).
using NamedPipeUtil = MSBuildWorkspaces::Microsoft.CodeAnalysis.NamedPipeUtil;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// A connection source for daemon mode: owns the server mutex (which signals "a daemon is running" for
/// this pipe) and accepts client connections on a named pipe, handing each a dedicated, independent
/// <see cref="System.IO.Pipes.NamedPipeServerStream"/>.
/// </summary>
internal sealed class NamedPipeDaemonConnectionSource : ILanguageServerConnectionSource, IDisposable
{
    private readonly string _pipeName;
    private readonly ILogger _logger;
    private readonly Mutex _serverMutex;

    private NamedPipeDaemonConnectionSource(string pipeName, Mutex serverMutex, ILogger logger)
    {
        _pipeName = pipeName;
        _serverMutex = serverMutex;
        _logger = logger;
    }

    public bool IsolateConnectionFaults => true;

    /// <summary>
    /// Attempts to become the daemon for <paramref name="pipeName"/> by acquiring the server mutex.
    /// Returns <see langword="false"/> (without creating a source) if another daemon already owns it.
    /// </summary>
    public static bool TryCreate(string pipeName, ILogger logger, [NotNullWhen(true)] out NamedPipeDaemonConnectionSource? source)
    {
        if (!DaemonServerMutex.TryAcquire(pipeName, out var serverMutex))
        {
            logger.LogWarning(
                "A language server daemon already owns pipe '{pipeName}'; this instance will exit so clients use the existing daemon.",
                pipeName);
            source = null;
            return false;
        }

        source = new NamedPipeDaemonConnectionSource(pipeName, serverMutex, logger);
        return true;
    }

    public async IAsyncEnumerable<LanguageServerConnection> AcceptConnectionsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pipeStream = NamedPipeUtil.CreateServer(_pipeName);

            // Wait for a client (outside any 'yield return', which C# disallows inside a try/catch). On success
            // the stream's ownership passes to the yielded connection; on failure we dispose it here.
            var connected = false;
            try
            {
                await pipeStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Daemon accepted a new client connection.");
                connected = true;
            }
            catch (OperationCanceledException)
            {
                await pipeStream.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            catch (Exception ex)
            {
                // Failing to accept one connection shouldn't take down the daemon; log and try again.
                _logger.LogError(ex, "Daemon encountered an error while waiting for a client connection.");
                await pipeStream.DisposeAsync().ConfigureAwait(false);
            }

            if (connected)
            {
                // The accepted stream is both input and output, and is disposed when its language server exits.
                yield return new LanguageServerConnection(pipeStream, pipeStream, pipeStream);
            }
        }
    }

    public void Dispose()
        => _serverMutex.Dispose();
}
