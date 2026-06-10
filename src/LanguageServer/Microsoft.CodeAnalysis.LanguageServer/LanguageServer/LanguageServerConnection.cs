// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// A single transport connection between an editor (or thin client) and a language server: a pair of
/// input/output streams plus an optional resource to dispose when the server for this connection exits.
/// </summary>
internal readonly record struct LanguageServerConnection(Stream InputStream, Stream OutputStream, IDisposable? Resource = null);

/// <summary>
/// Produces the connections that a <see cref="LanguageServerConnectionManager"/> should run a language
/// server for. Both single-server mode (stdio / connect-out pipe) and daemon mode (a named-pipe listener
/// accepting many clients) are expressed as a connection source, so they share one run loop.
/// </summary>
internal interface ILanguageServerConnectionSource
{
    /// <summary>
    /// When <see langword="true"/>, a fault in one connection's language server is logged and isolated so
    /// other connections (and the daemon as a whole) are unaffected. When <see langword="false"/>, a fault
    /// propagates out of <see cref="LanguageServerConnectionManager.RunAsync"/> (single-server mode)
    /// </summary>
    bool IsolateConnectionFaults { get; }

    /// <summary>
    /// Yields connections to run language servers for. A finite source (stdio / connect-out-pipe mode)
    /// yields exactly one connection and then completes; the daemon listener yields connections indefinitely
    /// as clients connect, until <paramref name="cancellationToken"/> is signaled.
    /// </summary>
    IAsyncEnumerable<LanguageServerConnection> AcceptConnectionsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// A connection source that yields a single, already-established connection exactly once and then
/// completes. Used for stdio and connect-out-pipe (single-server) modes.
/// </summary>
internal sealed class SingleLanguageServerConnectionSource(LanguageServerConnection connection) : ILanguageServerConnectionSource
{
    public bool IsolateConnectionFaults => false;

    public IAsyncEnumerable<LanguageServerConnection> AcceptConnectionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return AsyncEnumerable.Repeat(connection, 1);
    }
}
