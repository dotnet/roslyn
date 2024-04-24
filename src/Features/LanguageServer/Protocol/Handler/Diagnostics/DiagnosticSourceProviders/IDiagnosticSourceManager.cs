// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;

/// <summary>
/// Manages the diagnostic sources that provide diagnostics for the language server.
/// </summary>
internal interface IDiagnosticSourceManager
{
    /// <summary>
    /// Returns the names of all the sources that provide diagnostics for the given <paramref name="isDocument"/>.
    /// </summary>
    /// <param name="isDocument"><see langword="true" /> for document sources and <see langword="false" /> for workspace sources.</param>
    ImmutableArray<string> GetSourceNames(bool isDocument);

    /// <summary>
    /// Creates document diagnostic sources for the given <paramref name="sourceName"/>.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="sourceName">Source name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<ImmutableArray<IDiagnosticSource>> CreateDocumentDiagnosticSourcesAsync(RequestContext context, string? sourceName, CancellationToken cancellationToken);

    /// <summary>
    /// Creates workspace diagnostic sources for the given <paramref name="sourceName"/>.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="sourceName">Source name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask<ImmutableArray<IDiagnosticSource>> CreateWorkspaceDiagnosticSourcesAsync(RequestContext context, string? sourceName, CancellationToken cancellationToken);
}
