using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using System.Collections.ObjectModel;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using SVsServiceProvider = Microsoft.VisualStudio.Shell.SVsServiceProvider;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Editor.SymbolMapping;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal class DefinitionContextConnectionListener : IWpfTextViewConnectionListener
    {
        private readonly Dictionary<ITextView, CancellationTokenSource> _views = new Dictionary<ITextView, CancellationTokenSource>();
        private readonly IVsCodeDefView _vsCodeDefView;
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

        [ImportingConstructor]
        public DefinitionContextConnectionListener(IMetadataAsSourceFileService metadataAsSourceFileService, SVsServiceProvider serviceProvider)
        {
            _metadataAsSourceFileService = metadataAsSourceFileService;
            _vsCodeDefView = (IVsCodeDefView)serviceProvider.GetService(typeof(SVsCodeDefView));
        }

        void IWpfTextViewConnectionListener.SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (!_views.ContainsKey(textView) && !textView.Roles.Contains(PredefinedTextViewRoles.CodeDefinitionView))
            {
                _views.Add(textView, new CancellationTokenSource());
                textView.Caret.PositionChanged += OnTextViewCaretPositionChanged;
                var fireAndForget = UpdateDefinitionContext(textView, textView.Caret.Position);
            }
        }

        void IWpfTextViewConnectionListener.SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            if (reason == ConnectionReason.TextViewLifetime ||
                !textView.BufferGraph.GetTextBuffers(b => b.ContentType.IsOfType(ContentTypeNames.RoslynContentType)).Any())
            {
                CancellationTokenSource cancellationTokenSource;
                if (_views.TryGetValue(textView, out cancellationTokenSource))
                {
                    cancellationTokenSource.Cancel();
                    _views.Remove(textView);
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
            _views[textView].Cancel();

            // No point calculating the new context if the window isn't visible
            if (_vsCodeDefView.IsVisible() != VSConstants.S_OK)
            {
                return;
            }

            // See if we moved somewhere else in a projection that we care about
            var pointInRoslynSnapshot = caretPosition.Point.GetPoint(tb => tb.ContentType.IsOfType(ContentTypeNames.RoslynContentType), caretPosition.Affinity);
            if (pointInRoslynSnapshot == null)
            {
                return;
            }

            // After a delay in case the caret moves again, find the symbol under the caret and update the context
            var cancellationTokenSource = new CancellationTokenSource();
            _views[textView] = cancellationTokenSource;
            var cancellationToken = cancellationTokenSource.Token;
            var context = await Task.Run(
                async () => await GetContextFromPoint(pointInRoslynSnapshot, cancellationToken).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(true);

            if (context != null)
            {
                Marshal.ThrowExceptionForHR(_vsCodeDefView.SetContext(context));
            }
        }

        private async Task<Context> GetContextFromPoint(SnapshotPoint? pointInRoslynSnapshot, CancellationToken cancellationToken)
        {
            // TODO: Does this allocate too many tasks - should we switch to a queue like the classifier uses?
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
            var document = pointInRoslynSnapshot?.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return null;
            }

            var workspace = document.Project.Solution.Workspace;
            if (!document.SupportsSemanticModel)
            {
                var goToDefinitionService = document.GetLanguageService<IGoToDefinitionService>();
                var navigableItems = await goToDefinitionService.FindDefinitionsAsync(document, pointInRoslynSnapshot.Value.Position, cancellationToken).ConfigureAwait(false);
                if (navigableItems != null)
                {
                    var navigationService = workspace.Services.GetService<IDocumentNavigationService>();

                    var builder = new ArrayBuilder<Location>();
                    foreach (var item in navigableItems)
                    {
                        if (navigationService.CanNavigateToPosition(workspace, item.Document.Id, item.SourceSpan.Start))
                        {
                            var text = await item.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                            var linePositionSpan = text.Lines.GetLinePositionSpan(item.SourceSpan);
                            builder.Add(new Location(item.DisplayString, item.Document.FilePath, linePositionSpan.Start.Line, linePositionSpan.Start.Character));
                        }
                    }

                    return new Context(builder.ToImmutable());
                }

                return null;
            }
            else
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var symbol = SymbolFinder.FindSymbolAtPosition(
                    semanticModel,
                    pointInRoslynSnapshot.Value.Position,
                    workspace, bindLiteralsToUnderlyingType: true,
                    cancellationToken: cancellationToken);

                if (symbol == null)
                {
                    return null;
                }

                symbol = symbol.GetOriginalUnreducedDefinition();
                var project = document.Project;

                // Get the symbol back from the originating workspace

                var symbolMappingService = document.Project.Solution.Workspace.Services.GetService<ISymbolMappingService>();
                var mappingResult = await symbolMappingService.MapSymbolAsync(document, symbol, cancellationToken).ConfigureAwait(false);
                if (mappingResult != null)
                {
                    symbol = mappingResult.Symbol;
                    project = mappingResult.Project;
                }

                var solution = project.Solution;
                var sourceDefinition = await SymbolFinder.FindSourceDefinitionAsync(mappingResult.Symbol, mappingResult.Project.Solution, cancellationToken).ConfigureAwait(false);
                if (sourceDefinition != null)
                {
                    var originatingProject = solution.GetProject(sourceDefinition.ContainingAssembly, cancellationToken);
                    project = originatingProject ?? project;
                }

                var results = new ArrayBuilder<Location>();

                string filePath;
                int lineNumber;
                int charOffset;
                var symbolNavigationService = solution.Workspace.Services.GetService<ISymbolNavigationService>();
                //if (symbolNavigationService.WouldNavigateToSymbol(symbol, solution, out filePath, out lineNumber, out charOffset))
                //{
                //    results.Add(new Location(symbol.ToDisplayString(), filePath, lineNumber, charOffset));
                //}
                //else
                {
                    var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                    var sourceLocations = symbol.Locations.Where(l => l.IsInSource).ToList();

                    if (!sourceLocations.Any())
                    {
                        // It's a symbol from metadata, so we want to go produce it from metadata
                        var declarationFile = await _metadataAsSourceFileService.GetGeneratedFileAsync(project, symbol, cancellationToken).ConfigureAwait(false);
                        var identifierSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                        results.Add(new Location(symbol.ToDisplayString(), declarationFile.FilePath, identifierSpan.Start.Line, identifierSpan.Start.Character));
                    }

                    foreach (var declaration in sourceLocations)
                    {
                        var declarationLocation = declaration.GetLineSpan();
                        results.Add(new Location(symbol.ToDisplayString(), declarationLocation.Path, declarationLocation.Span.Start.Line, declarationLocation.Span.Start.Character));
                    }
                }

                return new Context(results.ToImmutable());
            }
        }

        private struct Location
        {
            public string DisplayName { get; }
            public string FilePath { get; }
            public int Line { get; }
            public int Character { get; }

            public Location(string displayName, string filePath, int line, int character)
            {
                DisplayName = displayName;
                FilePath = filePath;
                Line = line;
                Character = character;
            }
        }

        private class Context : IVsCodeDefViewContext
        {
            private readonly ImmutableArray<Location> _locations = ImmutableArray<Location>.Empty;

            public Context(ImmutableArray<Location> locations)
            {
                _locations = locations;
            }

            int IVsCodeDefViewContext.GetCount(out uint pcItems)
            {
                pcItems = (uint)_locations.Length;
                return VSConstants.S_OK;
            }

            int IVsCodeDefViewContext.GetSymbolName(uint iItem, out string pbstrSymbolName)
            {
                var index = (int)iItem;
                if (index < 0 || index >= _locations.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(iItem));
                }

                pbstrSymbolName = _locations[index].DisplayName;
                return VSConstants.S_OK;
            }

            int IVsCodeDefViewContext.GetFileName(uint iItem, out string pbstrFilename)
            {
                var index = (int)iItem;
                if (index < 0 || index >= _locations.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(iItem));
                }

                pbstrFilename = _locations[index].FilePath;
                return VSConstants.S_OK;
            }

            int IVsCodeDefViewContext.GetLine(uint iItem, out uint piLine)
            {
                var index = (int)iItem;
                if (index < 0 || index >= _locations.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(iItem));
                }

                piLine = (uint)_locations[index].Line;
                return VSConstants.S_OK;
            }

            int IVsCodeDefViewContext.GetCol(uint iItem, out uint piCol)
            {
                var index = (int)iItem;
                if (index < 0 || index >= _locations.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(iItem));
                }

                piCol = (uint)_locations[index].Character;
                return VSConstants.S_OK;
            }
        }
    }
}
