// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

#pragma warning disable RS0030 // Do not use banned APIs
[Export(typeof(ILocationService))]
#pragma warning restore RS0030 // Do not use banned APIs
internal sealed class LocationService : ILocationService
{
    private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
    private readonly IGlobalOptionService _globalOptions;

#pragma warning disable RS0030 // Do not use banned APIs
    [ImportingConstructor]
#pragma warning restore RS0030 // Do not use banned APIs
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LocationService(IMetadataAsSourceFileService metadataAsSourceFileService, IGlobalOptionService globalOptions)
    {
        _metadataAsSourceFileService = metadataAsSourceFileService;
        _globalOptions = globalOptions;
    }

    public async Task<FileLinePositionSpan?> GetLocationAsync(TextDocument document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        if (document.FilePath is null)
        {
            return null;
        }

        var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var linePosSpan = sourceText.Lines.GetLinePositionSpan(textSpan);
        return new FileLinePositionSpan(document.FilePath, linePosSpan);
    }

    public async Task<FileLinePositionSpan[]> GetSymbolLocationsAsync(ISymbol symbol, Project project, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<FileLinePositionSpan>.GetInstance(out var locations);

        var items = NavigableItemFactory.GetItemsFromPreferredSourceLocations(project.Solution, symbol, displayTaggedParts: null, cancellationToken);
        if (items.Any())
        {
            foreach (var item in items)
            {
                var document = await item.Document.GetRequiredDocumentAsync(project.Solution, cancellationToken).ConfigureAwait(false);
                var location = await GetLocationAsync(document, item.SourceSpan, cancellationToken).ConfigureAwait(false);
                locations.AddIfNotNull(location);
            }
        }
        else
        {
            if (_metadataAsSourceFileService.IsNavigableMetadataSymbol(symbol))
            {
                var options = _globalOptions.GetMetadataAsSourceOptions();
                var declarationFile = await _metadataAsSourceFileService.GetGeneratedFileAsync(project.Solution.Workspace, project, symbol, signaturesOnly: true, options: options, cancellationToken: cancellationToken).ConfigureAwait(false);
                var linePosSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                locations.Add(new FileLinePositionSpan(declarationFile.FilePath, linePosSpan));
            }
        }

        return locations.ToArray();
    }
}
