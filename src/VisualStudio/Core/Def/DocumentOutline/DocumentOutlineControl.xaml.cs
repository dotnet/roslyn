// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Interaction logic for DocumentOutlineControl.xaml
    /// </summary>
    internal partial class DocumentOutlineControl : UserControl, IOleCommandTarget
    {
        private readonly AsyncBatchingWorkQueue _uiUpdateQueue;

        private bool SymbolTreeItemsSourceInitialized { get; set; }
        private ImmutableArray<DocumentSymbolViewModel> SymbolsTreeItemsSource { get; set; }

        private ITextSnapshot? Snapshot { get; set; }
        private IWpfTextView TextView { get; set; }

        private readonly string? _filePath;

        public DocumentOutlineControl(
            IWpfTextView textView,
            ITextBuffer textBuffer,
            ILanguageServiceBroker2 languageServiceBroker,
            IThreadingContext threadingContext,
            IAsynchronousOperationListener asyncListener)
        {
            InitializeComponent();

            TextView = textView;
            _filePath = GetFilePath(textView);
            Snapshot = textBuffer.CurrentSnapshot;
            SymbolTreeItemsSourceInitialized = false;

            _uiUpdateQueue = new AsyncBatchingWorkQueue(
                    DelayTimeSpan.Short,
                    UpdateAsync,
                    asyncListener,
                    CancellationToken.None);

            async ValueTask UpdateAsync(CancellationToken cancellationToken)
            {
                var response = await DocumentSymbolsRequestAsync(textBuffer, languageServiceBroker).ConfigureAwait(false);
                if (response?.Response is not null)
                {
                    await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    var responseBody = response.Response.ToObject<DocumentSymbol[]>();
                    var documentSymbols = DocumentOutlineHelper.GetNestedDocumentSymbols(responseBody);
                    SymbolTreeItemsSourceInitialized = true;
                    SymbolsTreeItemsSource = DocumentOutlineHelper.GetDocumentSymbolModels(documentSymbols);
                    symbolTree.ItemsSource = SymbolsTreeItemsSource;
                }
                else
                {
                    SymbolTreeItemsSourceInitialized = false;
                    symbolTree.ItemsSource = ImmutableArray<DocumentSymbolViewModel>.Empty;
                    SymbolsTreeItemsSource = ImmutableArray<DocumentSymbolViewModel>.Empty;
                }
            }

            void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
                => _uiUpdateQueue.AddWork();

            TextView.Caret.PositionChanged += FollowCursor;
            TextView.TextBuffer.Changed += TextBuffer_Changed;

            threadingContext.JoinableTaskFactory.RunAsync(async () =>
            {
                await UpdateAsync(CancellationToken.None).ConfigureAwait(false);
            }).FileAndForget("Document Outline: Active Document Changed");
        }

        private async Task<ManualInvocationResponse?> DocumentSymbolsRequestAsync(ITextBuffer textBuffer, ILanguageServiceBroker2 languageServiceBroker)
        {
            var parameterFactory = new DocumentSymbolParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = new Uri(_filePath)
                }
            };

            // TODO: proper workaround such that context.ClientCapabilities?.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true
            return await languageServiceBroker.RequestAsync(
                textBuffer: textBuffer,
                method: Methods.TextDocumentDocumentSymbolName,
                capabilitiesFilter: (JToken x) => true,
                languageServerName: WellKnownLspServerKinds.AlwaysActiveVSLspServer.ToUserVisibleString(),
                parameterFactory: _ => JToken.FromObject(parameterFactory),
                cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }

        private static string? GetFilePath(IWpfTextView textView)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (textView.TextBuffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer bufferAdapter) &&
                bufferAdapter is IPersistFileFormat persistFileFormat &&
                ErrorHandler.Succeeded(persistFileFormat.GetCurFile(out var filePath, out _)))
            {
                return filePath;
            }

            return null;
        }

        private void ExpandAll(object sender, RoutedEventArgs e)
        {
            SetIsExpanded((IEnumerable<DocumentSymbolViewModel>)symbolTree.ItemsSource, true);
        }

        private void CollapseAll(object sender, RoutedEventArgs e)
        {
            SetIsExpanded((IEnumerable<DocumentSymbolViewModel>)symbolTree.ItemsSource, false);
        }

        private void SetIsExpanded(IEnumerable<DocumentSymbolViewModel> documentSymbolModels, bool isExpanded)
        {
            foreach (var documentSymbolModel in documentSymbolModels)
            {
                documentSymbolModel.IsExpanded = isExpanded;
                SetIsExpanded(documentSymbolModel.Children, isExpanded);
            }
        }

        private void Search(object sender, EventArgs e)
        {
            if (SymbolTreeItemsSourceInitialized)
            {
                if (searchBox.Text == ServicesVSResources.Search_Document_Outline || string.IsNullOrWhiteSpace(searchBox.Text))
                {
                    symbolTree.ItemsSource = SymbolsTreeItemsSource;
                }
                else
                {
                    var documentSymbols = ArrayBuilder<DocumentSymbolViewModel>.GetInstance();
                    foreach (var item in SymbolsTreeItemsSource)
                    {
                        if (DocumentOutlineHelper.SearchNodeTree(item, searchBox.Text))
                            documentSymbols.Add(item);
                    }

                    symbolTree.ItemsSource = documentSymbols.ToImmutableAndFree();
                }
            }
        }

        private void Sort(SortOption sortOption, FunctionId functionId)
        {
            if (SymbolTreeItemsSourceInitialized)
            {
                Logger.Log(functionId);
                // If there is an active search, only sort the filtered nodes
                if (!(searchBox.Text == ServicesVSResources.Search_Document_Outline || string.IsNullOrWhiteSpace(searchBox.Text)))
                {
                    var filteredDocumentSymbolModels = ArrayBuilder<DocumentSymbolViewModel>.GetInstance();
                    foreach (DocumentSymbolViewModel documentSymbolModel in symbolTree.ItemsSource)
                        filteredDocumentSymbolModels.Add(documentSymbolModel);

                    symbolTree.ItemsSource = DocumentOutlineHelper.Sort(filteredDocumentSymbolModels.ToImmutableAndFree(), sortOption);
                    SymbolsTreeItemsSource = DocumentOutlineHelper.Sort(SymbolsTreeItemsSource, sortOption);
                }
                else
                {
                    SymbolsTreeItemsSource = DocumentOutlineHelper.Sort(SymbolsTreeItemsSource, sortOption);
                    symbolTree.ItemsSource = SymbolsTreeItemsSource;
                }
            }
        }

        private void SortByName(object sender, EventArgs e)
        {
            Sort(SortOption.Name, FunctionId.DocumentOutline_SortByName);
        }

        private void SortByOrder(object sender, EventArgs e)
        {
            Sort(SortOption.Order, FunctionId.DocumentOutline_SortByOrder);
        }

        private void SortByType(object sender, EventArgs e)
        {
            Sort(SortOption.Type, FunctionId.DocumentOutline_SortByType);
        }

        // When symbol node clicked, selects corresponding code
        private void JumpToContent(object sender, EventArgs e)
        {
            RoslynDebug.AssertNotNull(Snapshot);
            if (sender is StackPanel panel && panel.DataContext is DocumentSymbolViewModel symbol)
            {
                if (symbol.StartLine >= 0 && symbol.StartLine < Snapshot.LineCount)
                {
                    var position = Snapshot.GetLineFromLineNumber(symbol.StartLine).Start.Position;
                    if (position >= 0 && position <= Snapshot.Length)
                    {
                        TextView.Caret.PositionChanged -= FollowCursor;
                        var point = new SnapshotPoint(Snapshot, position);
                        var snapshotSpan = new SnapshotSpan(point, point);
                        TextView.SetSelection(snapshotSpan);
                        TextView.ViewScroller.EnsureSpanVisible(snapshotSpan);
                        TextView.Caret.PositionChanged += FollowCursor;
                    }
                }
            }
        }

        private void FollowCursor(object sender, EventArgs e)
        {
            if (Snapshot is not null && TextView is not null)
            {
                var caretPoint = TextView.GetCaretPoint(Snapshot.TextBuffer);
                if (caretPoint.HasValue)
                {
                    caretPoint.Value.GetLineAndCharacter(out var lineNumber, out var characterIndex);
                    UnselectAll((IEnumerable<DocumentSymbolViewModel>)symbolTree.ItemsSource);
                    foreach (DocumentSymbolViewModel documentSymbolModel in symbolTree.ItemsSource)
                        SelectNode(documentSymbolModel, lineNumber, characterIndex);
                }
            }

            static void UnselectAll(IEnumerable<DocumentSymbolViewModel> documentSymbolModels)
            {
                foreach (var documentSymbolModel in documentSymbolModels)
                {
                    documentSymbolModel.IsSelected = false;
                    UnselectAll(documentSymbolModel.Children);
                }
            }
        }

        private void SelectNode(DocumentSymbolViewModel documentSymbol, int lineNumber, int characterIndex)
        {
            var selectedNodeIndex = -1;
            foreach (var child in documentSymbol.Children)
            {
                if (child.StartLine <= lineNumber && child.EndLine >= lineNumber)
                {
                    if (child.StartLine == child.EndLine)
                    {
                        if (child.StartChar <= characterIndex && child.EndChar >= characterIndex)
                        {
                            selectedNodeIndex = documentSymbol.Children.IndexOf(child);
                        }
                    }
                    else
                    {
                        selectedNodeIndex = documentSymbol.Children.IndexOf(child);
                    }
                }
            }

            if (selectedNodeIndex != -1)
            {
                SelectNode(documentSymbol.Children[selectedNodeIndex], lineNumber, characterIndex);
            }
            else
            {
                documentSymbol.IsSelected = documentSymbol.StartLine <= lineNumber && documentSymbol.EndLine >= lineNumber;
            }
        }

        internal const int OLECMDERR_E_NOTSUPPORTED = unchecked((int)0x80040100);

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            // we don't support any commands like rename/undo in this view yet
            return OLECMDERR_E_NOTSUPPORTED;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return VSConstants.S_OK;
        }
    }
}
