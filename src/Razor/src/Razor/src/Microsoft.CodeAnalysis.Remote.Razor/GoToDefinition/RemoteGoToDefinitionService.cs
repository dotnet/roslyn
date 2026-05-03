// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.GoToDefinition;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Roslyn.LanguageServer.Protocol.Location[]?>;
using ExternalHandlers = Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

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

    public ValueTask<RemoteResponse<LspLocation[]?>> GetDefinitionAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        Position position,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            context => GetDefinitionAsync(context, position, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<LspLocation[]?>> GetDefinitionAsync(
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

        // First, see if this is a tag helper. We ignore component attributes here, because they're better served by the C# handler.
        var componentLocations = await _definitionService.GetDefinitionAsync(
            context.Snapshot,
            positionInfo,
            context.GetSolutionQueryOperations(),
            includeMvcTagHelpers: true,
            cancellationToken)
            .ConfigureAwait(false);

        if (componentLocations is { Length: > 0 })
        {
            return Results(componentLocations);
        }

        // Check if we're in a string literal with a file path (before calling C# which would navigate to String class)
        if (positionInfo.LanguageKind is RazorLanguageKind.CSharp)
        {
            var stringLiteralLocations = await _definitionService.TryGetDefinitionFromStringLiteralAsync(
                context.Snapshot,
                positionInfo.Position,
                cancellationToken)
                .ConfigureAwait(false);

            if (stringLiteralLocations is { Length: > 0 })
            {
                return Results(stringLiteralLocations);
            }
        }

        if (positionInfo.LanguageKind is RazorLanguageKind.Html or RazorLanguageKind.Razor)
        {
            // If it isn't a Razor construct, and it isn't C#, let the server know to delegate to HTML.
            return CallHtml;
        }

        // Finally, call into C#.
        var generatedDocument = await context.Snapshot
            .GetGeneratedDocumentAsync(cancellationToken)
            .ConfigureAwait(false);

        var locations = await ExternalHandlers.GoToDefinition
            .GetDefinitionsAsync(
                _workspaceProvider.GetWorkspace(),
                generatedDocument,
                typeOnly: false,
                positionInfo.Position.ToLinePosition(),
                cancellationToken)
            .ConfigureAwait(false);

        if (locations is null and not [])
        {
            // C# didn't return anything, so we're done.
            return NoFurtherHandling;
        }

        // Map the C# locations back to the Razor file.
        using var mappedLocations = new PooledArrayBuilder<LspLocation>(locations.Length);

        foreach (var location in locations)
        {
            var (uri, range) = location;

            var (mappedDocumentUri, mappedRange) = await DocumentMappingService
                .MapToHostDocumentUriAndRangeAsync(context.Snapshot, uri, range.ToLinePositionSpan(), cancellationToken)
                .ConfigureAwait(false);

            var mappedLocation = LspFactory.CreateLocation(mappedDocumentUri, mappedRange);

            mappedLocations.Add(mappedLocation);
        }

        return Results(mappedLocations.ToArray());
    }
}
