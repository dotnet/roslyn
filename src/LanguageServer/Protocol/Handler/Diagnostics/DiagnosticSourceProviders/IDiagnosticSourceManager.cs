// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;

/// <summary>
/// Provides centralized/singleton management of MEF based <see cref="IDiagnosticSourceProvider"/>s.
/// Consumers - like diagnostic handlers - use it to get diagnostics from one or more providers.
/// </summary>
internal interface IDiagnosticSourceManager
{
    /// <summary>
    /// Returns the names of document level <see cref="IDiagnosticSourceProvider"/>s.
    /// </summary>
    ImmutableArray<string> GetDocumentSourceProviderNames(ClientCapabilities clientCapabilities);

    /// <summary>
    /// Returns the names of workspace level <see cref="IDiagnosticSourceProvider"/>s.
    /// </summary>
    ImmutableArray<string> GetWorkspaceSourceProviderNames(ClientCapabilities clientCapabilities);

    /// <summary>
    /// Creates document diagnostic sources for the given <paramref name="providerName"/>.
    /// </summary>
    /// <param name="context"/>
    /// <param name="providerName">Optional provider name. If <see langword="null"/> then diagnostics from all providers are used.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the request processing.</param>
    ValueTask<ImmutableArray<IDiagnosticSource>> CreateDocumentDiagnosticSourcesAsync(RequestContext context, string? providerName, CancellationToken cancellationToken);

    /// <summary>
    /// Creates workspace diagnostic sources for the given <paramref name="providerName"/>.
    /// </summary>
    /// <param name="context"/>
    /// <param name="providerName">Optional provider name. If not specified then diagnostics from all providers are used.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the request processing.</param>
    ValueTask<ImmutableArray<IDiagnosticSource>> CreateWorkspaceDiagnosticSourcesAsync(RequestContext context, string? providerName, CancellationToken cancellationToken);
}
