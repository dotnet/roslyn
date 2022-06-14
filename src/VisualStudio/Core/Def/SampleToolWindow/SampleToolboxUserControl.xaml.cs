// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Text;
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

namespace Microsoft.VisualStudio.LanguageServices
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    /// <summary>
    /// Interaction logic for SampleToolboxUserControl.xaml
    /// </summary>
    internal partial class SampleToolboxUserControl : UserControl, IOleCommandTarget
    {
        public SampleToolboxUserControl(Workspace workspace, IDocumentTrackingService documentTrackingService, ILanguageServiceBroker2 languageServiceBroker, IThreadingContext threadingContext)
        {
            InitializeComponent();
            InitializeIfNeeded(workspace, documentTrackingService, languageServiceBroker, threadingContext);
        }

        private Workspace? workspace { get; set; }
        private DocumentId? lastDocumentId { get; set; }

        [MemberNotNullWhen(true, nameof(symbolsTreeItemsSource))]
        private bool symbolTreeInitialized { get; set; }
        private ObservableCollection<DocumentSymbolViewModel>? symbolsTreeItemsSource { get; set; }

        private ITextSnapshot? snapshot { get; set; }
        private IWpfTextView? textView { get; set; }

        private void InitializeIfNeeded(Workspace workspace, IDocumentTrackingService documentTrackingService, ILanguageServiceBroker2 languageServiceBroker, IThreadingContext threadingContext)
        {
            this.workspace = workspace;
            documentTrackingService.ActiveDocumentChanged += DocumentTrackingService_ActiveDocumentChanged;

            this.textView = GetActiveTextView();
            if (this.textView is not null)
            {
                this.textView.Caret.PositionChanged += FollowCursor;
            }

            searchBox.GotFocus += RemoveText;
            searchBox.LostFocus += AddText;

            void DocumentTrackingService_ActiveDocumentChanged(object sender, DocumentId? documentId)
            {
                threadingContext.JoinableTaskFactory.RunAsync(async () =>
                {
                    this.symbolTreeInitialized = false;
                    if (documentId == this.lastDocumentId)
                    {
                        return;
                    }

                    this.lastDocumentId = documentId;
                    var document = workspace.CurrentSolution.GetDocument(documentId);
                    if (document is null)
                    {
                        symbolTree.ItemsSource = new List<DocumentSymbolViewModel>();
                        return;
                    }

                    document.TryGetText(out var text);
                    if (text is null)
                    {
                        symbolTree.ItemsSource = new List<DocumentSymbolViewModel>();
                        return;
                    }

                    var textBuffer = text.Container.GetTextBuffer();
                    this.snapshot = textBuffer.CurrentSnapshot;
                    if (!textBuffer.ContentType.IsOfType(ContentTypeNames.RoslynContentType))
                    {
                        symbolTree.ItemsSource = new List<DocumentSymbolViewModel>();
                        return;
                    }

                    var response = DocumentSymbolsRequest(document, textBuffer, languageServiceBroker, threadingContext);
                    if (response is not null && response.Response is not null)
                    {
                        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
                        var responseBody = response.Response.ToObject<DocumentSymbol[]>();
                        var documentSymbolModels = DocumentOutlineHelper.GetDocumentSymbols(responseBody);
                        this.symbolTreeInitialized = true;
                        this.symbolsTreeItemsSource = documentSymbolModels;
                        symbolTree.ItemsSource = documentSymbolModels;
                    }
                    else
                    {
                        symbolTree.ItemsSource = new List<DocumentSymbolViewModel>();
                    }
                }).FileAndForget("Document Outline: Active Document Changed");
            }
        }

        private ManualInvocationResponse? DocumentSymbolsRequest(Document document, ITextBuffer textBuffer, ILanguageServiceBroker2 languageServiceBroker, IThreadingContext threadingContext)
        {
            ManualInvocationResponse? response = null;
            threadingContext.JoinableTaskFactory.RunAsync(async () =>
            {
                var parameterFactory = new DocumentSymbolParams()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = document.GetURI()
                    }
                };

                var serializedParams = JToken.FromObject(parameterFactory);
                JToken ParameterFactory(ITextSnapshot _)
                {
                    return serializedParams;
                }

                // TODO: proper workaround such that context.ClientCapabilities?.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true
                response = await languageServiceBroker.RequestAsync(
                    textBuffer: textBuffer,
                    method: Methods.TextDocumentDocumentSymbolName,
                    capabilitiesFilter: (JToken x) => true,
                    languageServerName: WellKnownLspServerKinds.AlwaysActiveVSLspServer.ToUserVisibleString(),
                    parameterFactory: ParameterFactory,
                    cancellationToken: CancellationToken.None).ConfigureAwait(false);
            }).FileAndForget("Document Outline: Document Symbols Request");
            return response;
        }

        private void ExpandAll(object sender, RoutedEventArgs e)
        {
            if (this.symbolTreeInitialized)
            {
                var expandedNodes = SetIsExpanded(this.symbolsTreeItemsSource, true);
                this.symbolsTreeItemsSource = expandedNodes;
                symbolTree.ItemsSource = expandedNodes;
            }
        }

        private void CollapseAll(object sender, RoutedEventArgs e)
        {
            if (this.symbolTreeInitialized)
            {
                var collapsedNodes = SetIsExpanded(this.symbolsTreeItemsSource, false);
                this.symbolsTreeItemsSource = collapsedNodes;
                symbolTree.ItemsSource = collapsedNodes;
            }
        }

        private ObservableCollection<DocumentSymbolViewModel> SetIsExpanded(ObservableCollection<DocumentSymbolViewModel> documentSymbolModels, bool isExpanded)
        {
            foreach (var documentSymbolModel in documentSymbolModels)
            {
                documentSymbolModel.IsExpanded = isExpanded;
                documentSymbolModel.Children = SetIsExpanded(documentSymbolModel.Children, isExpanded);
            }

            return documentSymbolModels;
        }

        private void RemoveText(object sender, EventArgs e)
        {
            if (searchBox.Text == ServicesVSResources.Search_Document_Outline)
            {
                searchBox.Text = string.Empty;
            }
        }

        private void AddText(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchBox.Text))
                searchBox.Text = ServicesVSResources.Search_Document_Outline;
        }

        private void Search(object sender, EventArgs e)
        {
            if (this.symbolsTreeItemsSource is not null)
            {
                if (searchBox.Text == ServicesVSResources.Search_Document_Outline || string.IsNullOrWhiteSpace(searchBox.Text))
                {
                    symbolTree.ItemsSource = this.symbolsTreeItemsSource;
                }
                else
                {
                    var documentSymbols = new List<DocumentSymbolViewModel>();
                    foreach (var item in this.symbolsTreeItemsSource)
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
            if (this.symbolTreeInitialized)
            {
                var sortedDocumentSymbolModels = DocumentOutlineHelper.Sort(this.symbolsTreeItemsSource, SortOption.Name);
                this.symbolsTreeItemsSource = sortedDocumentSymbolModels;
                symbolTree.ItemsSource = sortedDocumentSymbolModels;
            }
        }

        private void SortByOrder(object sender, EventArgs e)
        {
            if (this.symbolTreeInitialized)
            {
                var sortedDocumentSymbolModels = DocumentOutlineHelper.Sort(this.symbolsTreeItemsSource, SortOption.Order);
                this.symbolsTreeItemsSource = sortedDocumentSymbolModels;
                symbolTree.ItemsSource = sortedDocumentSymbolModels;
            }
        }

        private void SortByType(object sender, EventArgs e)
        {
            if (this.symbolTreeInitialized)
            {
                var sortedDocumentSymbolModels = DocumentOutlineHelper.Sort(this.symbolsTreeItemsSource, SortOption.Type);
                this.symbolsTreeItemsSource = sortedDocumentSymbolModels;
                symbolTree.ItemsSource = sortedDocumentSymbolModels;
            }
        }

        // When node clicked, selects corresponding code
        private void JumpToContent(object sender, EventArgs e)
        {
            if (this.textView is not null && this.snapshot is not null &&
                sender is StackPanel panel && panel.DataContext is DocumentSymbolViewModel symbol)
            {
                var snapshot = this.snapshot;
                if (symbol.StartLine >= 0 && symbol.StartLine < snapshot.LineCount)
                {
                    var position = snapshot.GetLineFromLineNumber(symbol.StartLine).Start.Position;
                    if (position >= 0 && position <= snapshot.Length)
                    {
                        this.textView.Caret.PositionChanged -= FollowCursor;
                        var point = new SnapshotPoint(this.snapshot, position);
                        var snapshotSpan = new SnapshotSpan(point, point);
                        this.textView.SetSelection(snapshotSpan);
                        this.textView.ViewScroller.EnsureSpanVisible(snapshotSpan);
                        this.textView.Caret.PositionChanged += FollowCursor;
                    }
                }
            }
        }

        private void FollowCursor(object sender, EventArgs e)
        {
            /*if (this.snapshot is not null && this.textView is not null && this.symbolsTreeItemsSource is not null)
            {
                var caretPoint = this.textView.GetCaretPoint(this.snapshot.TextBuffer);
                if (caretPoint.HasValue)
                {
                    var documentSymbols = new List<DocumentSymbolViewModel>();
                    this.symbolsTreeItemsSource.ForEach(item => documentSymbols.Add(UnselectAllNodes(item)));
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
            if (this.symbolsTreeItemsSource is not null)
            {
                var documentSymbols = this.symbolsTreeItemsSource;
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
