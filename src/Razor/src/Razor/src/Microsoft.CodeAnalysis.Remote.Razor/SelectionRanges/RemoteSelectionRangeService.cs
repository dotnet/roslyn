// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteSelectionRangeService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteSelectionRangeService
{
    internal sealed class Factory : FactoryBase<IRemoteSelectionRangeService>
    {
        protected override IRemoteSelectionRangeService CreateService(in ServiceArgs args)
            => new RemoteSelectionRangeService(in args);
    }

    public ValueTask<SelectionRange[]?> GetSelectionRangesAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        Position[] positions,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetSelectionRangesAsync(context, positions, cancellationToken),
            cancellationToken);

    private async ValueTask<SelectionRange[]?> GetSelectionRangesAsync(
        RemoteDocumentContext context,
        Position[] positions,
        CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        // Each position can map to either the implementation or declaration C# document. Keep the
        // position info in request order so we can query each generated document separately and still
        // return selection ranges in the same order as the original request.
        var positionInfos = new DocumentPositionInfo[positions.Length];
        for (var i = 0; i < positions.Length; i++)
        {
            var position = positions[i];
            if (!codeDocument.Source.Text.TryGetAbsoluteIndex(position, out var hostDocumentIndex))
            {
                return null;
            }

            var positionInfo = GetPositionInfo(codeDocument, hostDocumentIndex, preferCSharpOverHtml: true);
            if (positionInfo.LanguageKind is not RazorLanguageKind.CSharp)
            {
                return null;
            }

            positionInfos[i] = positionInfo;
        }

        var selectionRanges = new SelectionRange[positions.Length];

        if (!await AddSelectionRangesAsync(inDeclDocument: false, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        if (!await AddSelectionRangesAsync(inDeclDocument: true, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return selectionRanges;

        async ValueTask<bool> AddSelectionRangesAsync(
            bool inDeclDocument,
            CancellationToken cancellationToken)
        {
            // Roslyn selection range requests are scoped to a single document, so collect just the
            // positions that belong to the requested generated C# document.
            using var linePositions = new PooledArrayBuilder<LinePosition>();
            foreach (var positionInfo in positionInfos)
            {
                if (positionInfo.InDeclDocument == inDeclDocument)
                {
                    linePositions.Add(positionInfo.Position.ToLinePosition());
                }
            }

            if (linePositions.Count == 0)
            {
                return true;
            }

            var generatedDocument = await context.Snapshot.GetGeneratedDocumentAsync(inDeclDocument, cancellationToken).ConfigureAwait(false);

            var csharpSelectionRanges = await SelectionRangeHandler.GetSelectionRangesAsync(generatedDocument, linePositions.ToImmutable(), cancellationToken).ConfigureAwait(false);
            if (csharpSelectionRanges is null)
            {
                return false;
            }

            var csharpDocument = codeDocument.GetRequiredCSharpDocument(inDeclDocument);
            var csharpSelectionRangeIndex = 0;

            // The Roslyn results match the filtered line position order. Walk the original positions
            // again so each mapped result goes back to its original request index.
            for (var i = 0; i < positionInfos.Length; i++)
            {
                if (positionInfos[i].InDeclDocument == inDeclDocument)
                {
                    selectionRanges[i] = MapSelectionRange(csharpDocument, csharpSelectionRanges[csharpSelectionRangeIndex], positions[i], isRoot: true)!;
                    csharpSelectionRangeIndex++;
                }
            }

            return true;
        }
    }

    private SelectionRange? MapSelectionRange(RazorCSharpDocument csharpDocument, SelectionRange? csharpSelectionRange, Position originalPosition, bool isRoot)
    {
        if (csharpSelectionRange is null)
        {
            return isRoot ? CreateEmptySelectionRange(originalPosition) : null;
        }

        var mappedParent = MapSelectionRange(csharpDocument, csharpSelectionRange.Parent, originalPosition, isRoot: false);
        if (!DocumentMappingService.TryMapToRazorDocumentRange(csharpDocument, csharpSelectionRange.Range, out var mappedRange))
        {
            return mappedParent;
        }

        if (mappedParent is not null && mappedParent.Range == mappedRange)
        {
            return mappedParent;
        }

        return new SelectionRange
        {
            Range = mappedRange,
            Parent = mappedParent
        };
    }

    private static SelectionRange CreateEmptySelectionRange(Position originalPosition)
        => new()
        {
            Range = LspFactory.CreateRange(originalPosition, originalPosition)
        };
}
