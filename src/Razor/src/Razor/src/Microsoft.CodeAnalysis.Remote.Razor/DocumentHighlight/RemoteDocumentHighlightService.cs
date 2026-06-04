// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Highlighting;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.DocumentHighlight;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Response = Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Microsoft.CodeAnalysis.Razor.Protocol.DocumentHighlight.RemoteDocumentHighlight[]?>;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed partial class RemoteDocumentHighlightService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteDocumentHighlightService
{
    internal sealed class Factory : FactoryBase<IRemoteDocumentHighlightService>
    {
        protected override IRemoteDocumentHighlightService CreateService(in ServiceArgs args)
            => new RemoteDocumentHighlightService(in args);
    }

    public ValueTask<Response> GetHighlightsAsync(
        RazorSolutionWrapper solutionInfo,
        DocumentId razorDocumentId,
        LinePosition position,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            razorDocumentId,
            context => GetHighlightsAsync(context, position, cancellationToken),
            cancellationToken);

    private async ValueTask<Response> GetHighlightsAsync(
        RemoteDocumentContext context,
        LinePosition position,
        CancellationToken cancellationToken)
    {
        var sourceText = await context.GetSourceTextAsync(cancellationToken).ConfigureAwait(false);
        if (!sourceText.TryGetAbsoluteIndex(position, out var index))
        {
            return Response.NoFurtherHandling;
        }

        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        var languageKind = codeDocument.GetLanguageKind(index, rightAssociative: true);
        if (languageKind is RazorLanguageKind.Html)
        {
            return Response.CallHtml;
        }
        else if (languageKind is RazorLanguageKind.Razor)
        {
            return Response.NoFurtherHandling;
        }

        if (!DocumentMappingService.TryMapToCSharpDocumentLinePosition(codeDocument, index, out _, out var csharpPosition, out var inDeclDocument))
        {
            return Response.NoFurtherHandling;
        }

        // Roslyn keyword highlighting only supports a single C# document, and we can't make two calls to Roslyn, one for each C#
        // document, because we would need to know a highlight location in the other document in order to know a position to ask for.
        // Fortunately none of the (at time of writing) keyword highlighters are for scenarios that would be valid across impl and decl
        // documents anyway.
        var document = await context.Snapshot.GetGeneratedDocumentAsync(inDeclDocument, cancellationToken).ConfigureAwait(false);
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var csharpDocument = codeDocument.GetRequiredCSharpDocument(inDeclDocument);

        var keywordHighlights = await GetKeywordHighlightsAsync(document, csharpDocument, text, csharpPosition, cancellationToken).ConfigureAwait(false);
        if (keywordHighlights.Length > 0)
        {
            return Response.Results(keywordHighlights);
        }

        // For reference highlights, we want to make sure to get references from both documents, as the user thinks they're just one, and
        // the results are much more useful. Fortunately Roslyn supports this, we just have to jump through a few little hoops when mapping
        // back to Razor positions.
        var otherDocument = await context.Snapshot.TryGetGeneratedDocumentAsync(!inDeclDocument, cancellationToken).ConfigureAwait(false);
        var otherCSharpDocument = codeDocument.GetCSharpDocument(!inDeclDocument);

        var referenceHighlights = await GetReferenceHighlightsAsync(document, otherDocument, csharpDocument, otherCSharpDocument, csharpPosition, cancellationToken).ConfigureAwait(false);
        if (referenceHighlights.Length > 0)
        {
            return Response.Results(referenceHighlights);
        }

        return Response.NoFurtherHandling;
    }

    // The below two methods are copied from Roslyn's DocumentHighlightHandler and modified for our needs, to map back to C# documents
    // and to support multiple documents for reference highlights, because given a scenario like:
    //
    // <div>@Title</div>
    //
    // @code {
    //    string Ti$$tle { get; set; }
    // }
    //
    // In this scenario Roslyn only sees the two Title references as being in two separate C# documents.

    private async Task<RemoteDocumentHighlight[]> GetKeywordHighlightsAsync(Document document, RazorCSharpDocument csharpDocument, SourceText text, int position, CancellationToken cancellationToken)
    {
        var highlightingService = document.Project.Solution.Services.ExportProvider.GetService<IHighlightingService>();
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var keywordSpans = new List<TextSpan>();
        highlightingService.AddHighlights(root, position, keywordSpans, cancellationToken);

        using var result = new PooledArrayBuilder<RemoteDocumentHighlight>();
        foreach (var span in keywordSpans)
        {
            if (DocumentMappingService.TryMapToRazorDocumentRange(csharpDocument, text.GetLinePositionSpan(span), out var mappedRange))
            {
                result.Add(new RemoteDocumentHighlight(mappedRange, DocumentHighlightKind.Text));
            }
        }

        return result.ToArrayAndClear();
    }

    private async Task<RemoteDocumentHighlight[]> GetReferenceHighlightsAsync(Document document, Document? otherDocument, RazorCSharpDocument csharpDocument, RazorCSharpDocument? otherCSharpDocument, int position, CancellationToken cancellationToken)
    {
        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var documentHighlightService = document.GetRequiredLanguageService<IDocumentHighlightsService>();
        var options = globalOptions.GetHighlightingOptions(document.Project.Language);

        var documentsToSearch = otherDocument is null
            ? ImmutableHashSet.Create(document)
            : [document, otherDocument];

        var highlights = await documentHighlightService.GetDocumentHighlightsAsync(document, position, documentsToSearch, options, cancellationToken).ConfigureAwait(false);
        if (highlights.IsDefaultOrEmpty)
        {
            return [];
        }

        using var result = new PooledArrayBuilder<RemoteDocumentHighlight>();
        foreach (var highlight in highlights)
        {
            RazorCSharpDocument mappedCSharpDocument;
            if (highlight.Document.Id == document.Id)
            {
                mappedCSharpDocument = csharpDocument;
            }
            else if (otherDocument is not null &&
                highlight.Document.Id == otherDocument.Id)
            {
                mappedCSharpDocument = otherCSharpDocument.AssumeNotNull("otherCSharpDocument should not be null if otherDocument isn't");
            }
            else
            {
                continue;
            }

            foreach (var span in highlight.HighlightSpans)
            {
                if (DocumentMappingService.TryMapToRazorDocumentRange(mappedCSharpDocument, mappedCSharpDocument.Text.GetLinePositionSpan(span.TextSpan), out var mappedRange))
                {
                    result.Add(new RemoteDocumentHighlight(mappedRange, ProtocolConversions.HighlightSpanKindToDocumentHighlightKind(span.Kind)));
                }
            }
        }

        return result.ToArrayAndClear();
    }
}
