// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

/// <summary>
/// TODO: Ideally, we would export TypeScript service and delegate to an imported TypeScript service implementation.
/// However, TypeScript already exports the service so we would need to coordinate the change.
/// </summary>
internal abstract class VSTypeScriptDocumentHighlightsServiceBase : IDocumentHighlightsService
{
    protected abstract Task<ImmutableArray<DocumentHighlights>> GetDocumentHighlightsAsync(
        Document document, int position, IImmutableSet<Document> documentsToSearch, CancellationToken cancellationToken);

    Task<ImmutableArray<DocumentHighlights>> IDocumentHighlightsService.GetDocumentHighlightsAsync(
        Document document, int position, IImmutableSet<Document> documentsToSearch, HighlightingOptions options, CancellationToken cancellationToken)
        => GetDocumentHighlightsAsync(document, position, documentsToSearch, cancellationToken);
}
