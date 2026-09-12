// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Peek;

[Export]
internal class PeekableItemFactory
{
    private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
    private readonly IGlobalOptionService _globalOptions;
    private readonly IThreadingContext _threadingContext;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PeekableItemFactory(
        IMetadataAsSourceFileService metadataAsSourceFileService,
        IGlobalOptionService globalOptions,
        IThreadingContext threadingContext)
    {
        _metadataAsSourceFileService = metadataAsSourceFileService;
        _globalOptions = globalOptions;
        _threadingContext = threadingContext;
    }

    public async Task<IEnumerable<IPeekableItem>> GetPeekableItemsAsync(
        ISymbol symbol,
        Project project,
        IPeekResultFactory peekResultFactory,
        CancellationToken cancellationToken)
    {
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));

        if (project == null)
            throw new ArgumentNullException(nameof(project));

        if (peekResultFactory == null)
            throw new ArgumentNullException(nameof(peekResultFactory));

        var solution = project.Solution;
        symbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).ConfigureAwait(false) ?? symbol;
        symbol = await GoToDefinitionFeatureHelpers.TryGetPreferredSymbolAsync(solution, symbol, cancellationToken).ConfigureAwait(false);
        if (symbol is null)
            return [];

        // if we mapped the symbol, then get the new project it is contained in.
        var originatingProject = solution.GetProject(symbol.ContainingAssembly, cancellationToken);
        project = originatingProject ?? project;

        var definitionItem = await symbol.ToNonClassifiedDefinitionItemAsync(
            solution, includeHiddenLocations: true, cancellationToken).ConfigureAwait(false);

        var symbolNavigationService = solution.Services.GetService<ISymbolNavigationService>();
        var result = await symbolNavigationService.GetExternalNavigationSymbolLocationAsync(definitionItem, cancellationToken).ConfigureAwait(false);
        result ??= await GetCrossLanguageFileLocationAsync(solution, symbol, cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<IPeekableItem>.GetInstance(out var results);
        if (result is var (filePath, linePosition))
        {
            results.Add(new ExternalFilePeekableItem(new FileLinePositionSpan(filePath, linePosition, linePosition), PredefinedPeekRelationships.Definitions, peekResultFactory));
        }
        else
        {
            var symbolKey = SymbolKey.Create(symbol, cancellationToken);

            var firstLocation = symbol.Locations.FirstOrDefault();
            if (firstLocation != null)
            {
                if (firstLocation.IsInSource || _metadataAsSourceFileService.IsNavigableMetadataSymbol(symbol))
                {
                    results.Add(new DefinitionPeekableItem(
                        solution.Workspace, project.Id, symbolKey, peekResultFactory, _metadataAsSourceFileService, _globalOptions, _threadingContext));
                }
            }
        }

        return results.ToImmutableAndClear();
    }

    /// <summary>
    /// The source file of a symbol another .Net language owns, for example F#.  It is metadata to us, and
    /// metadata-as-source would show it decompiled, so that language is asked first - as navigating to the
    /// symbol asks it in <c>VisualStudioSymbolNavigationService</c>.
    /// </summary>
    private async Task<(string filePath, LinePosition linePosition)?> GetCrossLanguageFileLocationAsync(
        Solution solution, ISymbol symbol, CancellationToken cancellationToken)
    {
        if (symbol.Locations.Any(static location => location.IsInSource) ||
            !_metadataAsSourceFileService.IsNavigableMetadataSymbol(symbol))
        {
            return null;
        }

        var docCommentId = symbol.GetDocumentationCommentId();
        var assemblyName = symbol.ContainingAssembly.Identity.Name;
        if (docCommentId == null || assemblyName == null)
            return null;

        foreach (var lazyService in solution.Services.ExportProvider.GetExports<ICrossLanguageSymbolNavigationService>())
        {
            var location = await lazyService.Value.TryGetNavigableFileLocationAsync(
                assemblyName, docCommentId, cancellationToken).ConfigureAwait(false);
            if (location != null)
                return location;
        }

        return null;
    }
}
