// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Highlighting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(DocumentHighlightsHandler)), Shared]
[Method(Methods.TextDocumentDocumentHighlightName)]
internal sealed class DocumentHighlightsHandler : ILspServiceDocumentRequestHandler<TextDocumentPositionParams, DocumentHighlight[]?>
{
    private readonly IHighlightingService _highlightingService;
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DocumentHighlightsHandler(IHighlightingService highlightingService, IGlobalOptionService globalOptions)
    {
        _highlightingService = highlightingService;
        _globalOptions = globalOptions;
    }

    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(TextDocumentPositionParams request) => request.TextDocument;

    public async Task<DocumentHighlight[]?> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.Document;
        if (document == null)
            return null;

        var position = ProtocolConversions.PositionToLinePosition(request.Position);
        return await GetHighlightsAsync(_globalOptions, _highlightingService, document, position, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<DocumentHighlight[]?> GetHighlightsAsync(IGlobalOptionService globalOptions, IHighlightingService highlightingService, Document document, LinePosition linePosition, CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var position = await document.GetPositionFromLinePositionAsync(linePosition, cancellationToken).ConfigureAwait(false);

        // First check if this is a keyword that needs highlighting.
        var keywordHighlights = await GetKeywordHighlightsAsync(highlightingService, document, text, position, cancellationToken).ConfigureAwait(false);
        if (keywordHighlights.Any())
        {
            return [.. keywordHighlights];
        }

        // Not a keyword, check if it is a reference that needs highlighting.
        var referenceHighlights = await GetReferenceHighlightsAsync(globalOptions, document, text, position, cancellationToken).ConfigureAwait(false);
        if (referenceHighlights.Any())
        {
            return [.. referenceHighlights];
        }

        // No keyword or references to highlight at this location.
        return [];
    }

    private static async Task<ImmutableArray<DocumentHighlight>> GetKeywordHighlightsAsync(IHighlightingService highlightingService, Document document, SourceText text, int position, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var keywordSpans = new List<TextSpan>();
        highlightingService.AddHighlights(root, position, keywordSpans, cancellationToken);

        return keywordSpans.SelectAsArray(highlight => new DocumentHighlight
        {
            Kind = DocumentHighlightKind.Text,
            Range = ProtocolConversions.TextSpanToRange(highlight, text)
        });
    }

    private static async Task<ImmutableArray<DocumentHighlight>> GetReferenceHighlightsAsync(IGlobalOptionService globalOptions, Document document, SourceText text, int position, CancellationToken cancellationToken)
    {
        var documentHighlightService = document.GetRequiredLanguageService<IDocumentHighlightsService>();
        var options = globalOptions.GetHighlightingOptions(document.Project.Language);
        var highlights = await documentHighlightService.GetDocumentHighlightsAsync(
            document,
            position,
            ImmutableHashSet.Create(document),
            options,
            cancellationToken).ConfigureAwait(false);

        if (!highlights.IsDefaultOrEmpty)
        {
            // LSP requests are only for a single document. So just get the highlights for the requested document.
            var highlightsForDocument = highlights.FirstOrDefault(h => h.Document.Id == document.Id);

            return highlightsForDocument.HighlightSpans.SelectAsArray(h => new DocumentHighlight
            {
                Range = ProtocolConversions.TextSpanToRange(h.TextSpan, text),
                Kind = ProtocolConversions.HighlightSpanKindToDocumentHighlightKind(h.Kind),
            });
        }

        return [];
    }
}
