// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Roslyn.LanguageServer.Protocol.Location[]?>;

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal sealed class RemoteGoToImplementationService(in ServiceArgs args) : RazorDocumentServiceBase(in args), IRemoteGoToImplementationService
{
    internal sealed class Factory : FactoryBase<IRemoteGoToImplementationService>
    {
        protected override IRemoteGoToImplementationService CreateService(in ServiceArgs args)
            => new RemoteGoToImplementationService(in args);
    }

    protected override IDocumentPositionInfoStrategy DocumentPositionInfoStrategy => PreferAttributeNameDocumentPositionInfoStrategy.Instance;

    private readonly IClientCapabilitiesService _clientCapabilitiesService = args.ExportProvider.GetExportedValue<IClientCapabilitiesService>();

    public ValueTask<RemoteResponse<LspLocation[]?>> GetImplementationAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        Position position,
        CancellationToken cancellationToken)
        => RunServiceAsync(
            solutionInfo,
            documentId,
            snapshot => GetImplementationAsync(snapshot, position, cancellationToken),
            cancellationToken);

    private async ValueTask<RemoteResponse<LspLocation[]?>> GetImplementationAsync(
        RemoteDocumentSnapshot snapshot,
        Position position,
        CancellationToken cancellationToken)
    {
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        if (!codeDocument.Source.Text.TryGetAbsoluteIndex(position, out var hostDocumentIndex))
        {
            return NoFurtherHandling;
        }

        var positionInfo = GetPositionInfo(codeDocument, hostDocumentIndex, preferCSharpOverHtml: true);

        if (positionInfo.LanguageKind is RazorLanguageKind.Razor)
        {
            return NoFurtherHandling;
        }

        if (positionInfo.LanguageKind is RazorLanguageKind.Html)
        {
            return CallHtml;
        }

        // Finally, call into C#.
        var generatedDocument = await snapshot
            .GetGeneratedDocumentAsync(positionInfo.InDeclDocument, cancellationToken)
            .ConfigureAwait(false);

        var supportsVisualStudioExtensions = _clientCapabilitiesService.ClientCapabilities.SupportsVisualStudioExtensions;
        var globalOptions = generatedDocument.Project.Solution.Services.ExportProvider.GetService<IGlobalOptionService>();
        var classificationOptions = globalOptions.GetClassificationOptionsProvider();
        var locations = await FindImplementationsHandler.FindImplementationsAsync(
            generatedDocument,
            positionInfo.Position.ToLinePosition(),
            classificationOptions,
            supportsVisualStudioExtensions,
            cancellationToken).ConfigureAwait(false);

        if (locations is null and not [])
        {
            // C# didn't return anything, so we're done.
            return NoFurtherHandling;
        }

        // Map the C# locations back to the Razor file.
        using var mappedLocations = new PooledArrayBuilder<LspLocation>(locations.Length);
        var seenLocations = new HashSet<(DocumentUri DocumentUri, LinePositionSpan Range)>();

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

        return Results(mappedLocations.ToArray());
    }
}
