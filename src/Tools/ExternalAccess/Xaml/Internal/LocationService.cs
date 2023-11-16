// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

[Export(typeof(ILocationService))]
internal class LocationService : ILocationService
{
    private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LocationService(IMetadataAsSourceFileService metadataAsSourceFileService, IGlobalOptionService globalOptions)
    {
        _metadataAsSourceFileService = metadataAsSourceFileService;
        _globalOptions = globalOptions;
    }

    public Task<LSP.Location?> GetLocationAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        => ProtocolConversions.TextSpanToLocationAsync(document, textSpan, isStale: false, cancellationToken);

    public async Task<LSP.Location[]> GetSymbolDefinitionLocationsAsync(ISymbol symbol, Project project, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<LSP.Location>.GetInstance(out var locations);

        var items = NavigableItemFactory.GetItemsFromPreferredSourceLocations(project.Solution, symbol, displayTaggedParts: null, cancellationToken);
        if (items.Any())
        {
            foreach (var item in items)
            {
                var document = await item.Document.GetRequiredDocumentAsync(project.Solution, cancellationToken).ConfigureAwait(false);
                var location = await ProtocolConversions.TextSpanToLocationAsync(
                    document, item.SourceSpan, item.IsStale, cancellationToken).ConfigureAwait(false);
                locations.AddIfNotNull(location);
            }
        }
        else
        {
            if (_metadataAsSourceFileService.IsNavigableMetadataSymbol(symbol))
            {
                var options = _globalOptions.GetMetadataAsSourceOptions(project.Services);
                var declarationFile = await _metadataAsSourceFileService.GetGeneratedFileAsync(project.Solution.Workspace, project, symbol, signaturesOnly: true, options, cancellationToken).ConfigureAwait(false);
                var linePosSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                locations.Add(new LSP.Location
                {
                    Uri = ProtocolConversions.CreateAbsoluteUri(declarationFile.FilePath),
                    Range = ProtocolConversions.LinePositionToRange(linePosSpan),
                });
            }
        }

        return locations.ToArray();
    }
}
