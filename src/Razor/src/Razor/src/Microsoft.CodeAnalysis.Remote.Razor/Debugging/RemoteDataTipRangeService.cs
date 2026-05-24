// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Remote;
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
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        Position position,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetDataTipRangeAsync(context, position, cancellationToken),
            cancellationToken);
    }

    private async ValueTask<RemoteResponse<VSInternalDataTip?>> GetDataTipRangeAsync(
        RemoteDocumentContext context,
        Position position,
        CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
        var razorIndex = codeDocument.Source.Text.GetRequiredAbsoluteIndex(position);
        var csharpDocument = codeDocument.GetRequiredCSharpDocument();

        if (!_documentMappingService.TryMapToCSharpDocumentPosition(csharpDocument, razorIndex, out var csharpPosition, out _))
        {
            return NoFurtherHandling;
        }

        var generatedDocument = await context.Snapshot.GetGeneratedDocumentAsync(cancellationToken).ConfigureAwait(false);

        var csharpResult = await ExternalAccess.Razor.Cohost.Handlers.DataTipRange.GetDataTipRangeAsync(generatedDocument, csharpPosition, cancellationToken).ConfigureAwait(false);
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
