// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.SymbolMapping;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal class DefinitionContextConnectionListener : IWpfTextViewConnectionListener
    {
        private readonly HashSet<ITextView> _subscribedViews = new HashSet<ITextView>();
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
        private readonly ICodeDefinitionWindowService _codeDefinitionWindowService;

        private CancellationTokenSource _currentUpdateCancellationToken;

        [ImportingConstructor]
        public DefinitionContextConnectionListener(
            IMetadataAsSourceFileService metadataAsSourceFileService,
            ICodeDefinitionWindowService codeDefinitionWindowService)
        {
            _metadataAsSourceFileService = metadataAsSourceFileService;
            _codeDefinitionWindowService = codeDefinitionWindowService;
        }

        void IWpfTextViewConnectionListener.SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (!_subscribedViews.Contains(textView) && !textView.Roles.Contains(PredefinedTextViewRoles.CodeDefinitionView))
            {
                _subscribedViews.Add(textView);
                textView.Caret.PositionChanged += OnTextViewCaretPositionChanged;
                var fireAndForget = UpdateDefinitionContext(textView, textView.Caret.Position);
            }
        }

        void IWpfTextViewConnectionListener.SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
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
            var fireAndForget = UpdateDefinitionContext(e.TextView, e.NewPosition);
        }

        private async Task UpdateDefinitionContext(ITextView textView, CaretPosition caretPosition)
        {
            // Cancel any pending update for this view
            _currentUpdateCancellationToken?.Cancel();

            // See if we moved somewhere else in a projection that we care about
            var pointInRoslynSnapshot = caretPosition.Point.GetPoint(tb => tb.ContentType.IsOfType(ContentTypeNames.RoslynContentType), caretPosition.Affinity);
            if (pointInRoslynSnapshot == null)
            {
                return;
            }

            // After a delay in case the caret moves again, find the symbol under the caret and update the context
            _currentUpdateCancellationToken = new CancellationTokenSource();

            var foregroundTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            var locations = await Task.Run(
                () => GetContextFromPointAfterDelay(pointInRoslynSnapshot.Value, foregroundTaskScheduler, _currentUpdateCancellationToken.Token),
                _currentUpdateCancellationToken.Token).ConfigureAwait(true);

            if (!_currentUpdateCancellationToken.Token.IsCancellationRequested)
            {
                _codeDefinitionWindowService.SetContext(locations);
            }
        }

        private async Task<ImmutableArray<CodeDefinitionWindowLocation>> GetContextFromPointAfterDelay(
            SnapshotPoint pointInRoslynSnapshot, TaskScheduler foregroundTaskScheduler, CancellationToken cancellationToken)
        {
            // TODO: Does this allocate too many tasks - should we switch to a queue like the classifier uses?
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
            var document = pointInRoslynSnapshot.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return ImmutableArray<CodeDefinitionWindowLocation>.Empty;
            }

            var workspace = document.Project.Solution.Workspace;
            if (!document.SupportsSemanticModel)
            {
                var goToDefinitionService = document.GetLanguageService<IGoToDefinitionService>();
                var navigableItems = await goToDefinitionService.FindDefinitionsAsync(document, pointInRoslynSnapshot.Position, cancellationToken).ConfigureAwait(false);
                if (navigableItems != null)
                {
                    var navigationService = workspace.Services.GetService<IDocumentNavigationService>();

                    var builder = new ArrayBuilder<CodeDefinitionWindowLocation>();
                    foreach (var item in navigableItems)
                    {
                        if (navigationService.CanNavigateToPosition(workspace, item.Document.Id, item.SourceSpan.Start))
                        {
                            var text = await item.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                            var linePositionSpan = text.Lines.GetLinePositionSpan(item.SourceSpan);
                            builder.Add(new CodeDefinitionWindowLocation(item.DisplayString, item.Document.FilePath, linePositionSpan));
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
                    pointInRoslynSnapshot.Position,
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
                    ?  ImmutableArray<CodeDefinitionWindowLocation>.Empty
                    : await GetLocationsOfSymbolAsync(
                        mappingResult.Symbol, mappingResult.Project, foregroundTaskScheduler, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<ImmutableArray<CodeDefinitionWindowLocation>> GetLocationsOfSymbolAsync(
            ISymbol symbol, Project project, TaskScheduler foregroundTaskScheduler, CancellationToken cancellationToken)
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
            string filePath = null;
            int lineNumber = 0;
            int charOffset = 0;
            var symbolNavigationService = solution.Workspace.Services.GetService<ISymbolNavigationService>();
            var wouldNavigate = false;
            await Task.Factory.StartNew(
                () => wouldNavigate = symbolNavigationService.WouldNavigateToSymbol(symbol, solution, out filePath, out lineNumber, out charOffset),
                cancellationToken,
                TaskCreationOptions.None,
                foregroundTaskScheduler).ConfigureAwait(false);

            var results = new ArrayBuilder<CodeDefinitionWindowLocation>();
            if (wouldNavigate)
            {
                results.Add(new CodeDefinitionWindowLocation(symbol.ToDisplayString(), filePath, lineNumber, charOffset));
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
                    var declarationFile = await _metadataAsSourceFileService.GetGeneratedFileAsync(project, symbol, cancellationToken).ConfigureAwait(false);
                    var identifierSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                    results.Add(new CodeDefinitionWindowLocation(symbol.ToDisplayString(), declarationFile.FilePath, identifierSpan));
                }
            }

            return results.ToImmutable();
        }
    }
}
