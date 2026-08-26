// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Roslyn.LanguageServer.Protocol.VSInternalDataTip?>;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteDataTipRangeService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteDataTipRangeService
{
    internal sealed class Factory : FactoryBase<IRemoteDataTipRangeService>
    {
        protected override IRemoteDataTipRangeService CreateService(in ServiceArgs args)
            => new RemoteDataTipRangeService(in args);
    }

    private readonly IDocumentMappingService _documentMappingService = args.ExportProvider.GetExportedValue<IDocumentMappingService>();

    public ValueTask<RemoteResponse<VSInternalDataTip?>> GetDataTipRangeAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        Position position,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(
            solutionInfo,
            documentId,
            snapshot => GetDataTipRangeAsync(snapshot, position, cancellationToken),
            cancellationToken);
    }

    private async ValueTask<RemoteResponse<VSInternalDataTip?>> GetDataTipRangeAsync(
        RemoteDocumentSnapshot snapshot,
        Position position,
        CancellationToken cancellationToken)
    {
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        var razorIndex = codeDocument.Source.Text.GetRequiredAbsoluteIndex(position);

        if (!_documentMappingService.TryMapToCSharpDocumentLinePosition(codeDocument, razorIndex, out var csharpPosition, out _, out var inDeclDocument))
        {
            return NoFurtherHandling;
        }

        var csharpDocument = codeDocument.GetRequiredCSharpDocument(inDeclDocument);
        var generatedDocument = await snapshot.GetGeneratedDocumentAsync(inDeclDocument, cancellationToken).ConfigureAwait(false);

        var csharpResult = await DataTipRangeHandler.GetDataTipRangeAsync(generatedDocument, csharpPosition, cancellationToken).ConfigureAwait(false);
        if (csharpResult?.ExpressionRange is null)
        {
            return NoFurtherHandling;
        }

        if (!DocumentMappingService.TryMapToRazorDocumentRange(csharpDocument, csharpResult.HoverRange, out var razorHoverRange)
            || !DocumentMappingService.TryMapToRazorDocumentRange(csharpDocument, csharpResult.ExpressionRange, out var razorExpressionRange))
        {
            return NoFurtherHandling;
        }

        var razorResult = new VSInternalDataTip()
        {
            HoverRange = razorHoverRange,
            ExpressionRange = razorExpressionRange,
            DataTipTags = csharpResult.DataTipTags,
        };

        return Results(razorResult);
    }
}
