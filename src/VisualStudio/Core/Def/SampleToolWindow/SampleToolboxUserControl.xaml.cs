// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json;
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
        private List<DocumentSymbolViewModel>? originalTree { get; set; }
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

            async void DocumentTrackingService_ActiveDocumentChanged(object sender, DocumentId? documentId)
            {
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
                var isCorrectType = textBuffer.ContentType.IsOfType(ContentTypeNames.RoslynContentType);

                if (!isCorrectType)
                {
                    symbolTree.ItemsSource = new List<DocumentSymbolViewModel>();
                    return;
                }

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

                // make LSP request
                var languageServerName = WellKnownLspServerKinds.AlwaysActiveVSLspServer.ToUserVisibleString();
                var lspService = languageServiceBroker;
                var capabilitiesFilter = (JToken x) => true;
                var method = Methods.TextDocumentDocumentSymbolName;
                var cancellationToken = CancellationToken.None;

                // TODO: proper workaround such that context.ClientCapabilities?.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true
                var response = await lspService.RequestAsync(
                    textBuffer: textBuffer,
                    method: method,
                    capabilitiesFilter: capabilitiesFilter,
                    languageServerName: languageServerName,
                    parameterFactory: ParameterFactory,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (response is not null && response.Response is not null)
                {
                    await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    var body = response.Response.ToObject<DocumentSymbol[]>();
                    var docSymbols = DocumentOutlineHelper.GetDocumentSymbols(body);
                    this.originalTree = docSymbols;
                    symbolTree.ItemsSource = docSymbols;
                }
                else
                {
                    symbolTree.ItemsSource = new List<DocumentSymbolViewModel>();
                }
            }
        }

        private void ExpandAll(object sender, RoutedEventArgs e)
        {
            var documentSymbols = new List<DocumentSymbolViewModel>();
            foreach (var item in (List<DocumentSymbolViewModel>)symbolTree.ItemsSource)
            {
                documentSymbols.Add(ExpandAllNodes(item));
            }

            symbolTree.ItemsSource = documentSymbols;
        }

        private DocumentSymbolViewModel ExpandAllNodes(DocumentSymbolViewModel treeItem)
        {
            treeItem.IsExpanded = true;
            foreach (var childItem in treeItem.Children.OfType<DocumentSymbolViewModel>())
            {
                ExpandAllNodes(childItem);
            }

            return treeItem;
        }

        private void CollapseAll(object sender, RoutedEventArgs e)
        {
            var documentSymbols = new List<DocumentSymbolViewModel>();
            foreach (var item in (List<DocumentSymbolViewModel>)symbolTree.ItemsSource)
            {
                documentSymbols.Add(CollapseAllNodes(item));
            }

            symbolTree.ItemsSource = documentSymbols;
        }

        private DocumentSymbolViewModel CollapseAllNodes(DocumentSymbolViewModel treeItem)
        {
            treeItem.IsExpanded = false;
            foreach (var childItem in treeItem.Children.OfType<DocumentSymbolViewModel>())
            {
                CollapseAllNodes(childItem);
            }

            return treeItem;
        }

        private void RemoveText(object sender, EventArgs e)
        {
            if (searchBox.Text == "Search Document Outline")
            {
                searchBox.Text = "";
            }
        }

        private void AddText(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchBox.Text))
                searchBox.Text = "Search Document Outline";
        }

        private void Search(object sender, EventArgs e)
        {
            if (searchBox.Text == "Search Document Outline" || string.IsNullOrWhiteSpace(searchBox.Text))
            {
                if (this.originalTree is not null)
                {
                    symbolTree.ItemsSource = this.originalTree;
                }
            }
            else
            {
                var documentSymbols = new List<DocumentSymbolViewModel>();
                if (this.originalTree is not null)
                {
                    foreach (var item in this.originalTree)
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
            var items = this.originalTree;
            var documentSymbols = items.OrderBy(x => x.Name).ToList();
            for (var i = 0; i < documentSymbols.Count; i++)
            {
                documentSymbols[i].Children = DocumentOutlineHelper.Sort(documentSymbols[i].Children, SortOption.Name);
            }

            symbolTree.ItemsSource = documentSymbols;
        }

        private void SortByOrder(object sender, EventArgs e)
        {
            var items = this.originalTree;
            var documentSymbols = items.OrderBy(x => x.StartLine).ThenBy(x => x.StartChar).ToList();
            for (var i = 0; i < documentSymbols.Count; i++)
            {
                documentSymbols[i].Children = DocumentOutlineHelper.Sort(documentSymbols[i].Children, SortOption.Order);
            }

            symbolTree.ItemsSource = documentSymbols;
        }

        private void SortByType(object sender, EventArgs e)
        {
            var items = this.originalTree;
            var documentSymbols = items.OrderBy(x => x.SymbolKind).ThenBy(x => x.Name).ToList();
            for (var i = 0; i < documentSymbols.Count; i++)
            {
                documentSymbols[i].Children = DocumentOutlineHelper.Sort(documentSymbols[i].Children, SortOption.Type);
            }

            symbolTree.ItemsSource = documentSymbols;
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
            /*if (this.snapshot is not null && this.textView is not null && this.originalTree is not null)
            {
                var caretPoint = this.textView.GetCaretPoint(this.snapshot.TextBuffer);
                if (caretPoint.HasValue)
                {
                    var documentSymbols = new List<DocumentSymbolViewModel>();
                    this.originalTree.ForEach(item => documentSymbols.Add(UnselectAllNodes(item)));
                    symbolTree.ItemsSource = documentSymbols;
                    caretPoint.Value.GetLineAndCharacter(out var lineNumber, out var characterIndex);
                    SelectNodeAtPosition(lineNumber, characterIndex);
                }
            }*/
        }

        private DocumentSymbolViewModel UnselectAllNodes(DocumentSymbolViewModel treeItem)
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
            if (this.originalTree is not null)
            {
                var documentSymbols = this.originalTree;
                var selectedNodeIndex = -1;
                documentSymbols.ForEach(node =>
                {
                    if (node.StartLine <= lineNumber && node.EndLine >= lineNumber)
                    {
                        selectedNodeIndex = documentSymbols.IndexOf(node);
                    }
                });

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
        }

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
