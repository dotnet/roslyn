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
using Microsoft.CodeAnalysis.Editor.Peek;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Peek
{
    [Export(typeof(IPeekableItemFactory))]
    internal class PeekableItemFactory : IPeekableItemFactory
    {
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PeekableItemFactory(IMetadataAsSourceFileService metadataAsSourceFileService, IGlobalOptionService globalOptions)
        {
            _metadataAsSourceFileService = metadataAsSourceFileService;
            _globalOptions = globalOptions;
        }

        public async Task<IEnumerable<IPeekableItem>> GetPeekableItemsAsync(
            ISymbol symbol, Project project,
            IPeekResultFactory peekResultFactory,
            CancellationToken cancellationToken)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (peekResultFactory == null)
            {
                throw new ArgumentNullException(nameof(peekResultFactory));
            }

            var results = new List<IPeekableItem>();

            var solution = project.Solution;
            var sourceDefinition = await SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

            // And if our definition actually is from source, then let's re-figure out what project it came from
            if (sourceDefinition != null)
            {
                var originatingProject = solution.GetProject(sourceDefinition.ContainingAssembly, cancellationToken);

                project = originatingProject ?? project;
            }

            var symbolNavigationService = solution.Workspace.Services.GetService<ISymbolNavigationService>();
            var definitionItem = symbol.ToNonClassifiedDefinitionItem(solution, includeHiddenLocations: true);

            var result = await symbolNavigationService.GetExternalNavigationSymbolLocationAsync(definitionItem, cancellationToken).ConfigureAwait(false);
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
                        results.Add(new DefinitionPeekableItem(solution.Workspace, project.Id, symbolKey, peekResultFactory, _metadataAsSourceFileService, _globalOptions));
                    }
                }
            }

            return results;
        }
    }
}
