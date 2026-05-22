// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.DocumentHighlight;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
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
        RazorPinnedSolutionInfoWrapper solutionInfo,
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

        var csharpDocument = codeDocument.GetRequiredCSharpDocument();
        if (DocumentMappingService.TryMapToCSharpDocumentPosition(csharpDocument, index, out var mappedPosition, out _))
        {
            var generatedDocument = await context.Snapshot
                .GetGeneratedDocumentAsync(cancellationToken)
                .ConfigureAwait(false);

            var highlights = await DocumentHighlights.GetHighlightsAsync(generatedDocument, mappedPosition, cancellationToken).ConfigureAwait(false);

            if (highlights is not null)
            {
                using var results = new PooledArrayBuilder<RemoteDocumentHighlight>();

                foreach (var highlight in highlights)
                {
                    if (DocumentMappingService.TryMapToRazorDocumentRange(csharpDocument, highlight.Range.ToLinePositionSpan(), out var mappedRange))
                    {
                        highlight.Range = mappedRange.ToRange();
                        results.Add(RemoteDocumentHighlight.FromLspDocumentHighlight(highlight));
                    }
                }

                return Response.Results(results.ToArray());
            }
        }

        return Response.NoFurtherHandling;
    }
}
