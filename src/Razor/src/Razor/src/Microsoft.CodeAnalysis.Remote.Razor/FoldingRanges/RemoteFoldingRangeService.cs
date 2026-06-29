// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Protocol.Folding;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.FoldingRanges;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteFoldingRangeService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteFoldingRangeService
{
    internal sealed class Factory : FactoryBase<IRemoteFoldingRangeService>
    {
        protected override IRemoteFoldingRangeService CreateService(in ServiceArgs args)
            => new RemoteFoldingRangeService(in args);
    }

    private readonly IFoldingRangeService _foldingRangeService = args.ExportProvider.GetExportedValue<IFoldingRangeService>();
    private readonly IClientCapabilitiesService _clientCapabilitiesService = args.ExportProvider.GetExportedValue<IClientCapabilitiesService>();

    public ValueTask<ImmutableArray<RemoteFoldingRange>> GetFoldingRangesAsync(
        RazorSolutionWrapper solutionInfo,
        DocumentId documentId,
        ImmutableArray<RemoteFoldingRange> htmlRanges,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetFoldingRangesAsync(context, htmlRanges, cancellationToken),
            cancellationToken);

    private async ValueTask<ImmutableArray<RemoteFoldingRange>> GetFoldingRangesAsync(
        RemoteDocumentContext context,
        ImmutableArray<RemoteFoldingRange> htmlRanges,
        CancellationToken cancellationToken)
    {
        var lineFoldingOnly = _clientCapabilitiesService.ClientCapabilities.TextDocument?.FoldingRange?.LineFoldingOnly ?? false;
        var globalOptions = context.Snapshot.TextDocument.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();

        var generatedDocument = await context.Snapshot.GetGeneratedDocumentAsync(declarationDocument: false, cancellationToken).ConfigureAwait(false);
        var csharpRanges = await FoldingRangesHandler.GetFoldingRangesAsync(globalOptions, generatedDocument, lineFoldingOnly, cancellationToken).ConfigureAwait(false);

        FoldingRange[]? declCSharpRanges = null;
        if (await context.Snapshot.TryGetGeneratedDocumentAsync(declarationDocument: true, cancellationToken).ConfigureAwait(false) is SourceGeneratedDocument declGeneratedDocument)
        {
            declCSharpRanges = await FoldingRangesHandler.GetFoldingRangesAsync(globalOptions, declGeneratedDocument, lineFoldingOnly, cancellationToken).ConfigureAwait(false);
        }

        var convertedHtml = htmlRanges.SelectAsArray(RemoteFoldingRange.ToLspFoldingRange);

        var codeDocument = await context.Snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        return _foldingRangeService.GetFoldingRanges(codeDocument, csharpRanges, declCSharpRanges, convertedHtml, cancellationToken)
            .SelectAsArray(RemoteFoldingRange.FromLspFoldingRange);
    }
}
