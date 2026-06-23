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

        using var mappedPositions = new PooledArrayBuilder<LinePosition>();
        foreach (var position in positions)
        {
            if (!codeDocument.Source.Text.TryGetAbsoluteIndex(position, out var hostDocumentIndex))
            {
                return null;
            }

            var positionInfo = GetPositionInfo(codeDocument, hostDocumentIndex, preferCSharpOverHtml: true);
            if (positionInfo.LanguageKind is not RazorLanguageKind.CSharp)
            {
                return null;
            }

            mappedPositions.Add(positionInfo.Position.ToLinePosition());
        }

        var generatedDocument = await context.Snapshot
            .GetGeneratedDocumentAsync(cancellationToken)
            .ConfigureAwait(false);

        var linePositions = mappedPositions.ToImmutable();
        var csharpSelectionRanges = await SelectionRangeHandler.GetSelectionRangesAsync(generatedDocument, linePositions, cancellationToken)
            .ConfigureAwait(false);

        if (csharpSelectionRanges is null)
        {
            return null;
        }

        var csharpDocument = codeDocument.GetRequiredCSharpDocument();
        var selectionRanges = new SelectionRange[csharpSelectionRanges.Length];
        for (var i = 0; i < csharpSelectionRanges.Length; i++)
        {
            selectionRanges[i] = MapSelectionRange(csharpDocument, csharpSelectionRanges[i], positions[i], isRoot: true)!;
        }

        return selectionRanges;
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
