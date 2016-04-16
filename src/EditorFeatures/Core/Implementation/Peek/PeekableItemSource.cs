// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Editor.Peek;
using Microsoft.CodeAnalysis.Editor.SymbolMapping;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Peek
{
    internal sealed class PeekableItemSource : IPeekableItemSource
    {
        private readonly ITextBuffer _textBuffer;
        private readonly IPeekableItemFactory _peekableItemFactory;
        private readonly IPeekResultFactory _peekResultFactory;
        private readonly IMetadataAsSourceFileService _metadataAsSourceService;
        private readonly IWaitIndicator _waitIndicator;

        public PeekableItemSource(
            ITextBuffer textBuffer,
            IPeekableItemFactory peekableItemFactory,
            IPeekResultFactory peekResultFactory,
            IMetadataAsSourceFileService metadataAsSourceService,
            IWaitIndicator waitIndicator)
        {
            _textBuffer = textBuffer;
            _peekableItemFactory = peekableItemFactory;
            _peekResultFactory = peekResultFactory;
            _metadataAsSourceService = metadataAsSourceService;
            _waitIndicator = waitIndicator;
        }

        public void AugmentPeekSession(IPeekSession session, IList<IPeekableItem> peekableItems)
        {
            if (!string.Equals(session.RelationshipName, PredefinedPeekRelationships.Definitions.Name, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var triggerPoint = session.GetTriggerPoint(_textBuffer.CurrentSnapshot);
            if (!triggerPoint.HasValue)
            {
                return;
            }

            var document = triggerPoint.Value.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            _waitIndicator.Wait(EditorFeaturesResources.Peek, EditorFeaturesResources.LoadingPeekInformation, allowCancel: true, action: context =>
            {
                var cancellationToken = context.CancellationToken;

                IEnumerable<IPeekableItem> results;

                if (!document.SupportsSemanticModel)
                {
                    // For documents without semantic models, just try to use the goto-def service
                    // as a reasonable place to peek at.
                    var goToDefinitionService = document.GetLanguageService<IGoToDefinitionService>();
                    var navigableItems = goToDefinitionService.FindDefinitionsAsync(document, triggerPoint.Value.Position, cancellationToken)
                                                              .WaitAndGetResult(cancellationToken);

                    results = GetPeekableItemsForNavigableItems(navigableItems, document.Project, _peekResultFactory, cancellationToken);
                }
                else
                {
                    var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);
                    var symbol = SymbolFinder.FindSymbolAtPositionAsync(semanticModel,
                                                                   triggerPoint.Value.Position,
                                                                   document.Project.Solution.Workspace,
                                                                   bindLiteralsToUnderlyingType: true,
                                                                   cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);

                    if (symbol == null)
                    {
                        return;
                    }

                    symbol = symbol.GetOriginalUnreducedDefinition();

                    // Get the symbol back from the originating workspace
                    var symbolMappingService = document.Project.Solution.Workspace.Services.GetService<ISymbolMappingService>();
                    var mappingResult = symbolMappingService.MapSymbolAsync(document, symbol, cancellationToken)
                                                            .WaitAndGetResult(cancellationToken);

                    mappingResult = mappingResult ?? new SymbolMappingResult(document.Project, symbol);

                    results = _peekableItemFactory.GetPeekableItemsAsync(mappingResult.Symbol, mappingResult.Project, _peekResultFactory, cancellationToken)
                                                 .WaitAndGetResult(cancellationToken);
                }

                peekableItems.AddRange(results);
            });
        }

        private static IEnumerable<IPeekableItem> GetPeekableItemsForNavigableItems(
            IEnumerable<INavigableItem> navigableItems, Project project,
            IPeekResultFactory peekResultFactory,
            CancellationToken cancellationToken)
        {
            if (navigableItems != null)
            {
                var workspace = project.Solution.Workspace;
                var navigationService = workspace.Services.GetService<IDocumentNavigationService>();

                foreach (var item in navigableItems)
                {
                    var document = item.Document;
                    if (navigationService.CanNavigateToPosition(workspace, document.Id, item.SourceSpan.Start))
                    {
                        var text = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);
                        var linePositionSpan = text.Lines.GetLinePositionSpan(item.SourceSpan);
                        yield return new ExternalFilePeekableItem(
                            new FileLinePositionSpan(document.FilePath, linePositionSpan),
                            PredefinedPeekRelationships.Definitions, peekResultFactory);
                    }
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
