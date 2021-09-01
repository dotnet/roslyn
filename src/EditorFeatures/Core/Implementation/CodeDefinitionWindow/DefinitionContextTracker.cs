// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CodeDefinitionWindow
{
    [Export(typeof(ITextViewConnectionListener))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal class DefinitionContextTracker : ITextViewConnectionListener
    {
        private readonly HashSet<ITextView> _subscribedViews = new HashSet<ITextView>();
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
        private readonly ICodeDefinitionWindowService _codeDefinitionWindowService;
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListener _asyncListener;

        private CancellationTokenSource _currentUpdateCancellationToken;

#pragma warning disable RS0033 // Importing constructor should be marked with 'ObsoleteAttribute'
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be marked with 'ObsoleteAttribute'
        public DefinitionContextTracker(
            IMetadataAsSourceFileService metadataAsSourceFileService,
            ICodeDefinitionWindowService codeDefinitionWindowService,
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _metadataAsSourceFileService = metadataAsSourceFileService;
            _codeDefinitionWindowService = codeDefinitionWindowService;
            _threadingContext = threadingContext;
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.CodeDefinitionWindow);
        }

        void ITextViewConnectionListener.SubjectBuffersConnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
        {
            if (!_subscribedViews.Contains(textView) && !textView.Roles.Contains(PredefinedTextViewRoles.CodeDefinitionView))
            {
                _subscribedViews.Add(textView);
                textView.Caret.PositionChanged += OnTextViewCaretPositionChanged;
                QueueUpdateForCaretPosition(textView.Caret.Position);
            }
        }

        void ITextViewConnectionListener.SubjectBuffersDisconnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
        {
            if (reason == ConnectionReason.TextViewLifetime ||
                !textView.BufferGraph.GetTextBuffers(b => b.ContentType.IsOfType(ContentTypeNames.RoslynContentType)).Any())
            {
                if (_subscribedViews.Contains(textView))
                {
                    _subscribedViews.Remove(textView);
                    textView.Caret.PositionChanged -= OnTextViewCaretPositionChanged;
                }
            }
        }

        private void OnTextViewCaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            QueueUpdateForCaretPosition(e.NewPosition);
        }

        private void QueueUpdateForCaretPosition(CaretPosition caretPosition)
        {
            Contract.ThrowIfFalse(_threadingContext.JoinableTaskContext.IsOnMainThread);

            // Cancel any pending update for this view
            _currentUpdateCancellationToken?.Cancel();

            // See if we moved somewhere else in a projection that we care about
            var pointInRoslynSnapshot = caretPosition.Point.GetPoint(tb => tb.ContentType.IsOfType(ContentTypeNames.RoslynContentType), caretPosition.Affinity);
            if (pointInRoslynSnapshot == null)
            {
                return;
            }

            _currentUpdateCancellationToken = new CancellationTokenSource();

            var cancellationToken = _currentUpdateCancellationToken.Token;
            var asyncToken = _asyncListener.BeginAsyncOperation(nameof(DefinitionContextTracker) + "." + nameof(QueueUpdateForCaretPosition));
            UpdateForCaretPositionAsync(pointInRoslynSnapshot.Value, cancellationToken).CompletesAsyncOperation(asyncToken);
        }

        private async Task UpdateForCaretPositionAsync(SnapshotPoint pointInRoslynSnapshot, CancellationToken cancellationToken)
        {
            var locations = await GetContextFromPointAfterDelayAsync(pointInRoslynSnapshot, cancellationToken).ConfigureAwait(true);

            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _codeDefinitionWindowService.SetContext(locations);
        }

        private async Task<ImmutableArray<CodeDefinitionWindowLocation>> GetContextFromPointAfterDelayAsync(
            SnapshotPoint pointInRoslynSnapshot, CancellationToken cancellationToken)
        {
            // TODO: Does this allocate too many tasks - should we switch to a queue like the classifier uses?
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);

            var document = pointInRoslynSnapshot.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return ImmutableArray<CodeDefinitionWindowLocation>.Empty;
            }

            return await GetContextFromPointAsync(document, pointInRoslynSnapshot.Position, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Internal for testing purposes.
        /// </summary>
        internal async Task<ImmutableArray<CodeDefinitionWindowLocation>> GetContextFromPointAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            if (!document.SupportsSemanticModel)
            {
                var goToDefinitionService = document.GetLanguageService<IGoToDefinitionService>();
                var navigableItems = await goToDefinitionService.FindDefinitionsAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (navigableItems != null)
                {
                    var navigationService = workspace.Services.GetService<IDocumentNavigationService>();

                    var builder = new ArrayBuilder<CodeDefinitionWindowLocation>();
                    foreach (var item in navigableItems)
                    {
                        if (navigationService.CanNavigateToPosition(workspace, item.Document.Id, item.SourceSpan.Start, cancellationToken))
                        {
                            var text = await item.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                            var linePositionSpan = text.Lines.GetLinePositionSpan(item.SourceSpan);
                            builder.Add(new CodeDefinitionWindowLocation(item.DisplayTaggedParts.JoinText(), item.Document.FilePath, linePositionSpan));
                        }
                    }

                    return builder.ToImmutable();
                }

                return ImmutableArray<CodeDefinitionWindowLocation>.Empty;
            }
            else
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var symbol = await SymbolFinder.FindSymbolAtPositionAsync(
                    semanticModel,
                    position,
                    workspace,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (symbol == null)
                {
                    return ImmutableArray<CodeDefinitionWindowLocation>.Empty;
                }

                symbol = symbol.GetOriginalUnreducedDefinition();

                // Get the symbol back from the originating workspace
                var symbolMappingService = document.Project.Solution.Workspace.Services.GetService<ISymbolMappingService>();
                var mappingResult = await symbolMappingService.MapSymbolAsync(document, symbol, cancellationToken).ConfigureAwait(false);

                return mappingResult == null
                    ? ImmutableArray<CodeDefinitionWindowLocation>.Empty
                    : await GetLocationsOfSymbolAsync(
                        mappingResult.Symbol, mappingResult.Project, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<ImmutableArray<CodeDefinitionWindowLocation>> GetLocationsOfSymbolAsync(
            ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            var solution = project.Solution;
            var sourceDefinition = await SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
            if (sourceDefinition != null)
            {
                var originatingProject = solution.GetProject(sourceDefinition.ContainingAssembly, cancellationToken);
                project = originatingProject ?? project;
            }

            // Three choices here:
            // 1. Another language (like XAML) will take over via ISymbolNavigationService
            // 2. There are locations in source, so we'll use those
            // 3. There are no locations from source, so we'll try to generate a metadata as source file and use that
            var symbolNavigationService = solution.Workspace.Services.GetService<ISymbolNavigationService>();
            var definitionItem = symbol.ToNonClassifiedDefinitionItem(solution, includeHiddenLocations: false);
            var result = await symbolNavigationService.WouldNavigateToSymbolAsync(definitionItem, cancellationToken).ConfigureAwait(false);

            var results = new ArrayBuilder<CodeDefinitionWindowLocation>();
            if (result != null)
            {
                results.Add(new CodeDefinitionWindowLocation(symbol.ToDisplayString(), result.Value.filePath, result.Value.lineNumber, result.Value.charOffset));
            }
            else
            {
                var sourceLocations = symbol.Locations.Where(l => l.IsInSource).ToList();
                if (sourceLocations.Any())
                {
                    foreach (var declaration in sourceLocations)
                    {
                        var declarationLocation = declaration.GetLineSpan();
                        results.Add(new CodeDefinitionWindowLocation(symbol.ToDisplayString(), declarationLocation));
                    }
                }
                else if (_metadataAsSourceFileService.IsNavigableMetadataSymbol(symbol))
                {
                    // Don't allow decompilation when generating, since we don't have a good way to prompt the user
                    // without a modal dialog.
                    var declarationFile = await _metadataAsSourceFileService.GetGeneratedFileAsync(project, symbol, allowDecompilation: false, cancellationToken).ConfigureAwait(false);
                    var identifierSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                    results.Add(new CodeDefinitionWindowLocation(symbol.ToDisplayString(), declarationFile.FilePath, identifierSpan));
                }
            }

            return results.ToImmutable();
        }
    }
}
