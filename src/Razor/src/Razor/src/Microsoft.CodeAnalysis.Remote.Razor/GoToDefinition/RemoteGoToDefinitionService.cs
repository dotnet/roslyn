// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.GoToDefinition;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Response = Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Microsoft.CodeAnalysis.Razor.Remote.GoToDefinitionResponse?>;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteGoToDefinitionService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteGoToDefinitionService
{
    internal sealed class Factory : FactoryBase<IRemoteGoToDefinitionService>
    {
        protected override IRemoteGoToDefinitionService CreateService(in ServiceArgs args)
            => new RemoteGoToDefinitionService(in args);
    }

    private readonly IDefinitionService _definitionService = args.ExportProvider.GetExportedValue<IDefinitionService>();
    private readonly IWorkspaceProvider _workspaceProvider = args.WorkspaceProvider;

    protected override IDocumentPositionInfoStrategy DocumentPositionInfoStrategy => PreferAttributeNameDocumentPositionInfoStrategy.Instance;

    private static Task<LspLocation[]?> GetSourceDefinitionsAsync(
        Workspace workspace,
        Document document,
        bool typeOnly,
        LinePosition linePosition,
        CancellationToken cancellationToken)
    {
        var globalOptions = document.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();

        // Metadata-as-source relies on host-only services such as SourceLink. Passing null keeps
        // this lookup source-only; navigable metadata symbols are sent back to the cohost endpoint below.
        return AbstractGoToDefinitionHandler.GetDefinitionsAsync(
            globalOptions,
            metadataAsSourceFileService: null,
            workspace,
            document,
            typeOnly,
            linePosition,
            cancellationToken);
    }

    public ValueTask<Response> GetDefinitionsAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        Position position,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            snapshot => GetDefinitionsAsync(snapshot, position, cancellationToken),
            cancellationToken);

    private async ValueTask<Response> GetDefinitionsAsync(
        RemoteDocumentSnapshot snapshot,
        Position position,
        CancellationToken cancellationToken)
    {
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        if (!codeDocument.Source.Text.TryGetAbsoluteIndex(position, out var hostDocumentIndex))
        {
            return Response.NoFurtherHandling;
        }

        // Adjust position if on a component end tag to use the start tag position
        hostDocumentIndex = codeDocument.AdjustPositionForComponentEndTag(hostDocumentIndex);

        var positionInfo = GetPositionInfo(codeDocument, hostDocumentIndex, preferCSharpOverHtml: true);

        // First, see if this is a tag helper. We ignore component attributes here, because they're better served by the C# handler.
        var componentLocations = await _definitionService.GetDefinitionAsync(
            snapshot,
            positionInfo,
            snapshot.ProjectSnapshot.SolutionSnapshot,
            includeMvcTagHelpers: true,
            cancellationToken)
            .ConfigureAwait(false);

        if (componentLocations is { Length: > 0 })
        {
            return Response.Results(GoToDefinitionResponse.FromLocations(componentLocations));
        }

        // Check if we're in a string literal with a file path (before calling C# which would navigate to String class)
        if (positionInfo.LanguageKind is RazorLanguageKind.CSharp)
        {
            var stringLiteralLocations = await _definitionService.TryGetDefinitionFromStringLiteralAsync(
                snapshot,
                positionInfo.Position,
                positionInfo.InDeclDocument,
                cancellationToken)
                .ConfigureAwait(false);

            if (stringLiteralLocations is { Length: > 0 })
            {
                return Response.Results(GoToDefinitionResponse.FromLocations(stringLiteralLocations));
            }
        }

        if (positionInfo.LanguageKind is RazorLanguageKind.Html or RazorLanguageKind.Razor)
        {
            // If it isn't a Razor construct, and it isn't C#, let the server know to delegate to HTML.
            return Response.CallHtml;
        }

        // Finally, call into C#.
        var generatedDocument = await snapshot
            .GetGeneratedDocumentAsync(positionInfo.InDeclDocument, cancellationToken)
            .ConfigureAwait(false);

        var projectedPosition = positionInfo.Position.ToLinePosition();
        var locations = await GetSourceDefinitionsAsync(
            _workspaceProvider.GetWorkspace(),
            generatedDocument,
            typeOnly: false,
            projectedPosition,
            cancellationToken).ConfigureAwait(false);

        if (locations is null)
        {
            // C# didn't return anything, so we're done.
            return Response.NoFurtherHandling;
        }

        if (locations.Length == 0)
        {
            // Resolving the symbol requires a semantic model and SymbolFinder, so keep this fallback
            // after source lookup rather than adding that work to every direct-source navigation.
            if (!await IsNavigableMetadataSymbolAsync(generatedDocument, projectedPosition, cancellationToken).ConfigureAwait(false))
            {
                return Response.NoFurtherHandling;
            }

            return Response.Results(GoToDefinitionResponse.FromCSharpRequest(
                new TextDocumentPositionParams
                {
                    TextDocument = new TextDocumentIdentifier { DocumentUri = generatedDocument.GetURI() },
                    Position = positionInfo.Position,
                }));
        }

        // Map the C# locations back to the Razor file.
        using var mappedLocations = new PooledArrayBuilder<LspLocation>(locations.Length);
        using var _ = HashSetPool<(DocumentUri DocumentUri, LinePositionSpan Range)>.GetPooledObject(out var seenLocations);

        foreach (var location in locations)
        {
            var (uri, range) = location;

            var (mappedDocumentUri, mappedRange) = await DocumentMappingService
                .MapToHostDocumentUriAndRangeAsync(snapshot, uri, range.ToLinePositionSpan(), cancellationToken)
                .ConfigureAwait(false);

            // Impl and decl generated documents can both contain a generated class declaration that maps to the same Razor location.
            if (!seenLocations.Add((mappedDocumentUri, mappedRange)))
            {
                continue;
            }

            var mappedLocation = LspFactory.CreateLocation(mappedDocumentUri, mappedRange);

            mappedLocations.Add(mappedLocation);
        }

        return Response.Results(GoToDefinitionResponse.FromLocations(mappedLocations.ToArray()));
    }

    private static async Task<bool> IsNavigableMetadataSymbolAsync(
        Document document,
        LinePosition linePosition,
        CancellationToken cancellationToken)
    {
        var metadataAsSourceFileService = document.Project.Solution.Services.ExportProvider.GetService<IMetadataAsSourceFileService>();
        if (metadataAsSourceFileService is null)
        {
            return false;
        }

        var position = await document.GetPositionFromLinePositionAsync(linePosition, cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(
            semanticModel,
            position,
            document.Project.Solution.Services,
            includeType: true,
            cancellationToken).ConfigureAwait(false);

        return symbol is not null && metadataAsSourceFileService.IsNavigableMetadataSymbol(symbol);
    }

    internal static class TestAccessor
    {
        public static Task<LspLocation[]?> GetDefinitionsAsync(
            Workspace workspace,
            Document document,
            bool typeOnly,
            LinePosition linePosition,
            CancellationToken cancellationToken)
            => GetSourceDefinitionsAsync(workspace, document, typeOnly, linePosition, cancellationToken);
    }
}
