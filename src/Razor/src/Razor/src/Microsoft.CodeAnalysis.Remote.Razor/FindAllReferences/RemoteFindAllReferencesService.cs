// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.FindAllReferences;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<
    Roslyn.LanguageServer.Protocol.SumType<Roslyn.LanguageServer.Protocol.VSInternalReferenceItem, Roslyn.LanguageServer.Protocol.Location>[]?>;
using ExternalHandlers = Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteFindAllReferencesService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteFindAllReferencesService
{
    internal sealed class Factory : FactoryBase<IRemoteFindAllReferencesService>
    {
        protected override IRemoteFindAllReferencesService CreateService(in ServiceArgs args)
            => new RemoteFindAllReferencesService(in args);
    }

    private readonly IClientCapabilitiesService _clientCapabilitiesService = args.ExportProvider.GetExportedValue<IClientCapabilitiesService>();
    private readonly IWorkspaceProvider _workspaceProvider = args.WorkspaceProvider;
    private readonly IFilePathService _filePathService = args.ExportProvider.GetExportedValue<IFilePathService>();

    protected override IDocumentPositionInfoStrategy DocumentPositionInfoStrategy => PreferAttributeNameDocumentPositionInfoStrategy.Instance;

    public ValueTask<RemoteResponse<SumType<VSInternalReferenceItem, LspLocation>[]?>> FindAllReferencesAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        Position position,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => FindAllReferencesAsync(context, position, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<SumType<VSInternalReferenceItem, LspLocation>[]?>> FindAllReferencesAsync(
        RemoteDocumentContext context,
        Position position,
        CancellationToken cancellationToken)
    {
        var codeDocument = await context.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        if (!codeDocument.Source.Text.TryGetAbsoluteIndex(position, out var hostDocumentIndex))
        {
            return NoFurtherHandling;
        }

        // Adjust position if on a component end tag to use the start tag position
        hostDocumentIndex = codeDocument.AdjustPositionForComponentEndTag(hostDocumentIndex);

        var positionInfo = GetPositionInfo(codeDocument, hostDocumentIndex, preferCSharpOverHtml: true);

        if (positionInfo.LanguageKind is not RazorLanguageKind.CSharp)
        {
            return NoFurtherHandling;
        }

        // Finally, call into C#.
        var generatedDocument = await context.Snapshot
            .GetGeneratedDocumentAsync(cancellationToken)
            .ConfigureAwait(false);

        var results = await ExternalHandlers.FindAllReferences
            .FindReferencesAsync(
                _workspaceProvider.GetWorkspace(),
                generatedDocument,
                positionInfo.Position.ToLinePosition(),
                _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions,
                cancellationToken)
            .ConfigureAwait(false);

        if (results is null and not [])
        {
            // C# didn't return anything, so we're done.
            return NoFurtherHandling;
        }

        using var mappedResults = new PooledArrayBuilder<SumType<VSInternalReferenceItem, LspLocation>>(results.Length);

        // Map the C# locations back to the Razor file.
        foreach (var result in results)
        {
            var location = result.TryGetFirst(out var referenceItem)
                ? referenceItem.Location
                : result.Second;

            if (location is null)
            {
                continue;
            }

            var (mappedUri, mappedRange) = await DocumentMappingService.MapToHostDocumentUriAndRangeAsync(context.Snapshot, location.DocumentUri.GetRequiredParsedUri(), location.Range.ToLinePositionSpan(), cancellationToken).ConfigureAwait(false);

            if (_filePathService.IsVirtualCSharpFile(mappedUri))
            {
                // Couldn't map, so probably a hidden part of the code-gen, let's skip it.
                continue;
            }

            if (referenceItem is not null)
            {
                // Indicates the reference item is directly available in the code
                referenceItem.Origin = VSInternalItemOrigin.Exact;

                // If we're going to change the Uri, then also override the file paths
                if (mappedUri != location.DocumentUri.GetRequiredParsedUri())
                {
                    referenceItem.DisplayPath = mappedUri.AbsolutePath;
                    referenceItem.DocumentName = mappedUri.AbsolutePath;

                    var fixedResultText = await FindAllReferencesHelper.GetResultTextAsync(DocumentMappingService, context.GetSolutionQueryOperations(), mappedRange.Start.Line, mappedUri.GetDocumentFilePath(), cancellationToken).ConfigureAwait(false);
                    referenceItem.Text = fixedResultText ?? referenceItem.Text;
                }
            }

            location.DocumentUri = new(mappedUri);
            location.Range = mappedRange.ToRange();

            mappedResults.Add(result);
        }

        return Results(mappedResults.ToArrayAndClear());
    }
}
