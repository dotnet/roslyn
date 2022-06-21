// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer;
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
        [MemberNotNullWhen(true, nameof(SymbolsTreeItemsSource))]
        private bool SymbolTreeInitialized { get; set; }

        private readonly AsyncBatchingWorkQueue _uiUpdateQueue;

        private List<DocumentSymbolViewModel>? SymbolsTreeItemsSource { get; set; }

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
            SymbolTreeInitialized = false;

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
                    var documentSymbolModels = DocumentOutlineHelper.GetDocumentSymbolModels(documentSymbols);
                    SymbolTreeInitialized = true;
                    SymbolsTreeItemsSource = documentSymbolModels;
                    symbolTree.ItemsSource = documentSymbolModels;
                }
                else
                {
                    symbolTree.ItemsSource = new List<DocumentSymbolViewModel>();
                    SymbolsTreeItemsSource = new List<DocumentSymbolViewModel>();
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

            var serializedParams = JToken.FromObject(parameterFactory);
            JToken ParameterFactory(ITextSnapshot _)
            {
                return serializedParams;
            }

            // TODO: proper workaround such that context.ClientCapabilities?.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true
            return await languageServiceBroker.RequestAsync(
                textBuffer: textBuffer,
                method: Methods.TextDocumentDocumentSymbolName,
                capabilitiesFilter: (JToken x) => true,
                languageServerName: WellKnownLspServerKinds.AlwaysActiveVSLspServer.ToUserVisibleString(),
                parameterFactory: ParameterFactory,
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
            if (SymbolTreeInitialized)
            {
                var documentSymbolModels = new List<DocumentSymbolViewModel>();
                foreach (var documentSymbolModel in SymbolsTreeItemsSource)
                {
                    documentSymbolModels.Add(SetIsExpanded(documentSymbolModel, true));
                }

                SymbolsTreeItemsSource = documentSymbolModels;
                symbolTree.ItemsSource = documentSymbolModels;
            }
        }

        private void CollapseAll(object sender, RoutedEventArgs e)
        {
            if (SymbolTreeInitialized)
            {
                var documentSymbolModels = new List<DocumentSymbolViewModel>();
                foreach (var documentSymbolModel in SymbolsTreeItemsSource)
                {
                    documentSymbolModels.Add(SetIsExpanded(documentSymbolModel, false));
                }

                SymbolsTreeItemsSource = documentSymbolModels;
                symbolTree.ItemsSource = documentSymbolModels;
            }
        }

        private DocumentSymbolViewModel SetIsExpanded(DocumentSymbolViewModel documentSymbolModel, bool isExpanded)
        {
            documentSymbolModel.IsExpanded = isExpanded;
            var documentSymbolModelChildren = new List<DocumentSymbolViewModel>();
            foreach (var documentSymbolModelChild in documentSymbolModel.Children)
            {
                documentSymbolModelChildren.Add(SetIsExpanded(documentSymbolModelChild, isExpanded));
            }

            documentSymbolModel.Children = documentSymbolModelChildren;
            return documentSymbolModel;
        }

        private void Search(object sender, EventArgs e)
        {
            if (SymbolsTreeItemsSource is not null)
            {
                if (searchBox.Text == ServicesVSResources.Search_Document_Outline || string.IsNullOrWhiteSpace(searchBox.Text))
                {
                    symbolTree.ItemsSource = SymbolsTreeItemsSource;
                }
                else
                {
                    var documentSymbols = new List<DocumentSymbolViewModel>();
                    foreach (var item in SymbolsTreeItemsSource)
                    {
                        if (DocumentOutlineHelper.SearchNodeTree(item, searchBox.Text))
                        {
                            documentSymbols.Add(item);
                        }
                    }

                    symbolTree.ItemsSource = documentSymbols;
                }
            }
        }

        private void Sort(SortOption sortOption, FunctionId functionId)
        {
            if (SymbolTreeInitialized)
            {
                Logger.Log(functionId);
                // If there is an active search, only sort the filtered nodes
                if (!(searchBox.Text == ServicesVSResources.Search_Document_Outline || string.IsNullOrWhiteSpace(searchBox.Text)))
                {
                    symbolTree.ItemsSource = DocumentOutlineHelper.Sort(
                        new List<DocumentSymbolViewModel>((IEnumerable<DocumentSymbolViewModel>)symbolTree.ItemsSource), sortOption);
                }
                else
                {
                    var sortedDocumentSymbolModels = DocumentOutlineHelper.Sort(SymbolsTreeItemsSource, sortOption);
                    SymbolsTreeItemsSource = sortedDocumentSymbolModels;
                    symbolTree.ItemsSource = sortedDocumentSymbolModels;
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
            if (Snapshot is not null && TextView is not null && SymbolsTreeItemsSource is not null)
            {
                var caretPoint = TextView.GetCaretPoint(Snapshot.TextBuffer);
                if (caretPoint.HasValue)
                {
                    caretPoint.Value.GetLineAndCharacter(out var lineNumber, out var characterIndex);
                    SymbolsTreeItemsSource.ForEach(node => SelectNode(node, lineNumber, characterIndex));
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
            else if (documentSymbol.StartLine <= lineNumber && documentSymbol.EndLine >= lineNumber)
            {
                documentSymbol.IsSelected = true;
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
