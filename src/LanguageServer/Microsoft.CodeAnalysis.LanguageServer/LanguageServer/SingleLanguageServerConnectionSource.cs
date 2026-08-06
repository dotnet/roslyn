// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// A connection source that yields a single, already-established connection exactly once and then
/// completes. Used for stdio and connect-out-pipe (single-server) modes.
/// </summary>
internal sealed class SingleLanguageServerConnectionSource(LanguageServerConnection connection) : ILanguageServerConnectionSource
{
    public bool ShouldIsolateConnectionFaults => false;

    public IAsyncEnumerable<LanguageServerConnection> AcceptConnectionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return AsyncEnumerable.Repeat(connection, 1);
    }
}
