// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

/// <summary>
/// Provides diagnostic sources.
/// </summary>
internal interface IDiagnosticSourceProvider
{
    /// <summary>
    /// <see langword="true"/> if this provider is for documents, <see langword="false"/> if it is for workspace.
    /// </summary>
    bool IsDocument { get; }

    /// <summary>
    /// Provider's name. Each should have a unique name within <see cref="IsDocument"/> scope.
    /// </summary>
    string Name { get; }

    bool IsEnabled(ClientCapabilities clientCapabilities);

    /// <summary>
    /// Creates the diagnostic sources.
    /// </summary>
    /// <param name="context"/>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the request processing.</param>
    ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken);
}

