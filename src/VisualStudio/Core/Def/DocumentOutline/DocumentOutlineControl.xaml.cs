// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
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
    /// Interaction logic for SampleToolboxUserControl.xaml
    /// </summary>
    internal partial class DocumentOutlineControl : UserControl, IOleCommandTarget
    {
        [MemberNotNullWhen(true, nameof(SymbolsTreeItemsSource))]
        private bool SymbolTreeInitialized { get; set; }
        private List<DocumentSymbolViewModel>? SymbolsTreeItemsSource { get; set; }

        private ITextSnapshot? Snapshot { get; set; }
        private IWpfTextView TextView { get; set; }

        private readonly string? _filePath;
        private ResettableDelay? _delay;
        private readonly IAsynchronousOperationListener _asyncListener;

        public DocumentOutlineControl(ILanguageServiceBroker2 languageServiceBroker, IThreadingContext threadingContext, IAsynchronousOperationListener asyncListener)
        {
            InitializeComponent();

            _asyncListener = asyncListener;
            var textView = GetActiveTextView();
            RoslynDebug.AssertNotNull(textView);
            TextView = textView;
            _filePath = GetFilePath(textView);

            SymbolTreeInitialized = false;
            var textBuffer = TextView.TextBuffer;
            Snapshot = textBuffer.CurrentSnapshot;
            var isCSharpContentType = textBuffer.ContentType.IsOfType(ContentTypeNames.CSharpContentType);
            var isVisualBasicContentType = textBuffer.ContentType.IsOfType(ContentTypeNames.VisualBasicContentType);

            // Check required since ActiveDocumentChanged is called for many content types
            if (!isCSharpContentType && !isVisualBasicContentType)
            {
                symbolTree.ItemsSource = new List<DocumentSymbolViewModel>();
                return;
            }

            threadingContext.JoinableTaskFactory.RunAsync(async () =>
            {
                await UpdateAsync().ConfigureAwait(false);
            }).FileAndForget("Document Outline: Active Document Changed");


            TextView.Caret.PositionChanged += FollowCursor;
            TextView.TextBuffer.Changed += TextBuffer_Changed;

            void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
                => EnqueueUpdate();

            void EnqueueUpdate()
            {
                const int UpdateDelay = 500;
                var delay = _delay;
                if (delay == null)
                {
                    var newDelay = new ResettableDelay(UpdateDelay, _asyncListener);
                    if (Interlocked.CompareExchange(ref _delay, newDelay, null) == null)
                    {
                        var asyncToken = _asyncListener.BeginAsyncOperation("Updating Document Outline");
                        newDelay.Task.SafeContinueWithFromAsync(_ => UpdateAsync(), CancellationToken.None, TaskScheduler.Default)
                            .SafeContinueWith(_ => _delay = null, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default)
                            .CompletesAsyncOperation(asyncToken);
                    }

                    return;
                }

                delay.Reset();
            }

            async Task UpdateAsync()
            {
                var response = await DocumentSymbolsRequestAsync(textBuffer, languageServiceBroker).ConfigureAwait(false);
                if (response is not null && response.Response is not null)
                {
                    await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
                    var responseBody = response.Response.ToObject<DocumentSymbol[]>();
                    var documentSymbolModels = DocumentOutlineHelper.GetDocumentSymbols(responseBody);
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

        private void SortByName(object sender, EventArgs e)
        {
            if (SymbolTreeInitialized)
            {
                var sortedDocumentSymbolModels = DocumentOutlineHelper.Sort(SymbolsTreeItemsSource, SortOption.Name);
                SymbolsTreeItemsSource = sortedDocumentSymbolModels;
                symbolTree.ItemsSource = sortedDocumentSymbolModels;
            }
        }

        private void SortByOrder(object sender, EventArgs e)
        {
            if (SymbolTreeInitialized)
            {
                var sortedDocumentSymbolModels = DocumentOutlineHelper.Sort(SymbolsTreeItemsSource, SortOption.Order);
                SymbolsTreeItemsSource = sortedDocumentSymbolModels;
                symbolTree.ItemsSource = sortedDocumentSymbolModels;
            }
        }

        private void SortByType(object sender, EventArgs e)
        {
            if (SymbolTreeInitialized)
            {
                var sortedDocumentSymbolModels = DocumentOutlineHelper.Sort(SymbolsTreeItemsSource, SortOption.Type);
                SymbolsTreeItemsSource = sortedDocumentSymbolModels;
                symbolTree.ItemsSource = sortedDocumentSymbolModels;
            }
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
            /*if (snapshot is not null && textView is not null && symbolsTreeItemsSource is not null)
            {
                var caretPoint = textView.GetCaretPoint(snapshot.TextBuffer);
                if (caretPoint.HasValue)
                {
                    var documentSymbols = new List<DocumentSymbolViewModel>();
                    symbolsTreeItemsSource.ForEach(item => documentSymbols.Add(UnselectAllNodes(item)));
                    symbolTree.ItemsSource = documentSymbols;
                    caretPoint.Value.GetLineAndCharacter(out var lineNumber, out var characterIndex);
                    SelectNodeAtPosition(lineNumber, characterIndex);
                }
            }*/
        }

        /*private DocumentSymbolViewModel UnselectAllNodes(DocumentSymbolViewModel treeItem)
        {
            treeItem.IsSelected = false;
            foreach (var childItem in treeItem.Children.OfType<DocumentSymbolViewModel>())
            {
                UnselectAllNodes(childItem);
            }

            return treeItem;
        }

        private void SelectNodeAtPosition(int lineNumber, int characterIndex)
        {
            if (symbolsTreeItemsSource is not null)
            {
                var documentSymbols = symbolsTreeItemsSource;
                var selectedNodeIndex = -1;
                foreach (var node in documentSymbols)
                {
                    if (node.StartLine <= lineNumber && node.EndLine >= lineNumber)
                    {
                        selectedNodeIndex = documentSymbols.IndexOf(node);
                    }
                }

                if (selectedNodeIndex == -1)
                {
                    return;
                }

                symbolTree.SelectedItemChanged += SelectedNodeChanged;
                var newNode = SelectNode(documentSymbols[selectedNodeIndex], lineNumber, characterIndex);
                documentSymbols.Insert(selectedNodeIndex, newNode);
                documentSymbols.RemoveAt(selectedNodeIndex + 1);
                symbolTree.ItemsSource = documentSymbols;
            }
        }

        private void SelectedNodeChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var treeViewItem = symbolTree.ItemContainerGenerator.ContainerFromItem(symbolTree.SelectedItem) as TreeViewItem;
            if (treeViewItem is not null)
            {
                treeViewItem.Focus();
            }

            symbolTree.SelectedItemChanged -= SelectedNodeChanged;
        }

        private DocumentSymbolViewModel SelectNode(DocumentSymbolViewModel node, int lineNumber, int characterIndex)
        {
            if (node.Children.Count == 0)
            {
                node.IsSelected = true;
                return node;
            }

            var selectedNodeIndex = -1;
            foreach (var child in node.Children)
            {
                if (child.StartLine <= lineNumber && child.EndLine >= lineNumber)
                {
                    if (child.StartLine == child.EndLine)
                    {
                        if (child.StartChar <= characterIndex && child.EndChar >= characterIndex)
                        {
                            selectedNodeIndex = node.Children.IndexOf(child);
                        }
                    }
                    else
                    {
                        selectedNodeIndex = node.Children.IndexOf(child);
                    }
                }
            }

            if (selectedNodeIndex == -1)
            {
                node.IsSelected = true;
            }
            else
            {
                node.Children[selectedNodeIndex] = SelectNode(node.Children[selectedNodeIndex], lineNumber, characterIndex);
            }

            return node;
        }*/

        private static IWpfTextView? GetActiveTextView()
        {
            var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));
            if (monitorSelection == null)
            {
                return null;
            }

            if (ErrorHandler.Failed(monitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out var curDocument)))
            {
                return null;
            }

            if (curDocument is not IVsWindowFrame frame)
            {
                return null;
            }

            if (ErrorHandler.Failed(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out var docView)))
            {
                return null;
            }

            if (docView is IVsCodeWindow)
            {
                if (ErrorHandler.Failed(((IVsCodeWindow)docView).GetPrimaryView(out var textView)))
                {
                    return null;
                }

                var model = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
                var adapterFactory = model.GetService<IVsEditorAdaptersFactoryService>();
                var wpfTextView = adapterFactory.GetWpfTextView(textView);
                return wpfTextView;
            }

            return null;
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
