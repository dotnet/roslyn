// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Produces the connections that a <see cref="LanguageServerConnectionManager"/> should run a language
/// server for. Both single-server mode (stdio / connect-out pipe) and daemon mode (a named-pipe listener
/// accepting many clients) are expressed as a connection source, so they share one run loop.
/// </summary>
internal interface ILanguageServerConnectionSource
{
    /// <summary>
    /// When <see langword="true"/>, the connection manager logs and isolates a fault in one connection's language
    /// server so other connections (and the daemon as a whole) are unaffected. When <see langword="false"/>, a fault
    /// propagates out of <see cref="LanguageServerConnectionManager.RunAsync"/> (single-server mode).
    /// </summary>
    bool ShouldIsolateConnectionFaults { get; }

    /// <summary>
    /// Yields connections to run language servers for. A <see cref="SingleLanguageServerConnectionSource"/>
    /// yields exactly one connection and then completes; the daemon listener yields connections indefinitely
    /// as clients connect, until <paramref name="cancellationToken"/> is signaled.
    /// </summary>
    IAsyncEnumerable<LanguageServerConnection> AcceptConnectionsAsync(CancellationToken cancellationToken);
}
