// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Peek;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Peek
{
    internal sealed class PeekableItemSource : IPeekableItemSource
    {
        private readonly ITextBuffer _textBuffer;
        private readonly IPeekableItemFactory _peekableItemFactory;
        private readonly IPeekResultFactory _peekResultFactory;
        private readonly IThreadingContext _threadingContext;
        private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;

        public PeekableItemSource(
            ITextBuffer textBuffer,
            IPeekableItemFactory peekableItemFactory,
            IPeekResultFactory peekResultFactory,
            IThreadingContext threadingContext,
            IUIThreadOperationExecutor uiThreadOperationExecutor)
        {
            _textBuffer = textBuffer;
            _peekableItemFactory = peekableItemFactory;
            _peekResultFactory = peekResultFactory;
            _threadingContext = threadingContext;
            _uiThreadOperationExecutor = uiThreadOperationExecutor;
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

            _uiThreadOperationExecutor.Execute(EditorFeaturesResources.Peek, EditorFeaturesResources.Loading_Peek_information, allowCancellation: true, showProgress: false, action: context =>
            {
                _threadingContext.JoinableTaskFactory.Run(() => AugumentPeekSessionAsync(peekableItems, context, triggerPoint.Value, document));
            });
        }

        private async Task AugumentPeekSessionAsync(
            IList<IPeekableItem> peekableItems, IUIThreadOperationContext context, SnapshotPoint triggerPoint, Document document)
        {
            var cancellationToken = context.UserCancellationToken;
            var services = document.Project.Solution.Services;

            if (!document.SupportsSemanticModel)
            {
                // For documents without semantic models, just try to use the goto-def service
                // as a reasonable place to peek at.
                var service = document.GetLanguageService<INavigableItemsService>();
                if (service == null)
                    return;

                var navigableItems = await service.GetNavigableItemsAsync(document, triggerPoint.Position, cancellationToken).ConfigureAwait(false);
                await foreach (var item in GetPeekableItemsForNavigableItemsAsync(
                    navigableItems, document.Project, _peekResultFactory, cancellationToken).ConfigureAwait(false))
                {
                    peekableItems.Add(item);
                }
            }
            else
            {
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var semanticInfo = await SymbolFinder.GetSemanticInfoAtPositionAsync(
                    semanticModel,
                    triggerPoint.Position,
                    services,
                    cancellationToken).ConfigureAwait(false);
                var symbol = semanticInfo.GetAnySymbol(includeType: true);
                if (symbol == null)
                {
                    return;
                }

                symbol = symbol.GetOriginalUnreducedDefinition();

                // Get the symbol back from the originating workspace
                var symbolMappingService = services.GetRequiredService<ISymbolMappingService>();

                var mappingResult = await symbolMappingService.MapSymbolAsync(document, symbol, cancellationToken).ConfigureAwait(false);

                mappingResult ??= new SymbolMappingResult(document.Project, symbol);

                peekableItems.AddRange(await _peekableItemFactory.GetPeekableItemsAsync(
                    mappingResult.Symbol, mappingResult.Project, _peekResultFactory, cancellationToken).ConfigureAwait(false));
            }
        }

        private static async IAsyncEnumerable<IPeekableItem> GetPeekableItemsForNavigableItemsAsync(
            IEnumerable<INavigableItem>? navigableItems, Project project,
            IPeekResultFactory peekResultFactory,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (navigableItems != null)
            {
                var workspace = project.Solution.Workspace;
                var navigationService = workspace.Services.GetRequiredService<IDocumentNavigationService>();

                foreach (var item in navigableItems)
                {
                    var document = item.Document;
                    if (await navigationService.CanNavigateToPositionAsync(
                            workspace, document.Id, item.SourceSpan.Start, cancellationToken).ConfigureAwait(false))
                    {
                        var text = await document.GetTextAsync(project.Solution, cancellationToken).ConfigureAwait(false);
                        var linePositionSpan = text.Lines.GetLinePositionSpan(item.SourceSpan);
                        if (document.FilePath != null)
                        {
                            yield return new ExternalFilePeekableItem(
                                new FileLinePositionSpan(document.FilePath, linePositionSpan),
                                PredefinedPeekRelationships.Definitions, peekResultFactory);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
