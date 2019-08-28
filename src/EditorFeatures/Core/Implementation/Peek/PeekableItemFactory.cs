// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Peek;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Peek
{
    [Export(typeof(IPeekableItemFactory))]
    internal class PeekableItemFactory : IPeekableItemFactory
    {
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

        [ImportingConstructor]
        public PeekableItemFactory(IMetadataAsSourceFileService metadataAsSourceFileService)
        {
            _metadataAsSourceFileService = metadataAsSourceFileService;
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
            var definitionItem = symbol.ToNonClassifiedDefinitionItem(project, includeHiddenLocations: true);

            if (symbolNavigationService.WouldNavigateToSymbol(
                    definitionItem, solution, cancellationToken,
                    out var filePath, out var lineNumber, out var charOffset))
            {
                var position = new LinePosition(lineNumber, charOffset);
                results.Add(new ExternalFilePeekableItem(new FileLinePositionSpan(filePath, position, position), PredefinedPeekRelationships.Definitions, peekResultFactory));
            }
            else
            {
                var symbolKey = SymbolKey.Create(symbol, cancellationToken);

                var firstLocation = symbol.Locations.FirstOrDefault();
                if (firstLocation != null)
                {
                    if (firstLocation.IsInSource || _metadataAsSourceFileService.IsNavigableMetadataSymbol(symbol))
                    {
                        results.Add(new DefinitionPeekableItem(solution.Workspace, project.Id, symbolKey, peekResultFactory, _metadataAsSourceFileService));
                    }
                }
            }

            return results;
        }
    }
}
