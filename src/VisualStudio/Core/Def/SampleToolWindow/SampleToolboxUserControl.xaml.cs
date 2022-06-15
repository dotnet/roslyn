// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServer;
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
    internal partial class SampleToolboxUserControl : UserControl, IOleCommandTarget
    {
        [MemberNotNullWhen(true, nameof(symbolsTreeItemsSource))]
        private bool symbolTreeInitialized { get; set; }
        private ObservableCollection<DocumentSymbolViewModel>? symbolsTreeItemsSource { get; set; }

        private ITextSnapshot? snapshot { get; set; }
        private IWpfTextView textView { get; set; }

        public SampleToolboxUserControl(ILanguageServiceBroker2 languageServiceBroker, IThreadingContext threadingContext)
        {
            InitializeComponent();

            var textView = GetActiveTextView();
            RoslynDebug.AssertNotNull(textView);
            this.textView = textView;
            this.textView.Caret.PositionChanged += FollowCursor;
            //this.textView.TextBuffer.Changed += TextBuffer_Changed;

            /*void TextBuffer_Changed(object sender, TextContentChangedEventArgs e)
            {
            }*/

            threadingContext.JoinableTaskFactory.RunAsync(async () =>
            {
                this.symbolTreeInitialized = false;
                var textBuffer = this.textView.TextBuffer;
                this.snapshot = textBuffer.CurrentSnapshot;
                var isCSharpContentType = textBuffer.ContentType.IsOfType(ContentTypeNames.CSharpContentType);
                var isVisualBasicContentType = textBuffer.ContentType.IsOfType(ContentTypeNames.VisualBasicContentType);

                // Check required since ActiveDocumentChanged is called for many content types
                if (!isCSharpContentType && !isVisualBasicContentType)
                {
                    symbolTree.ItemsSource = new List<DocumentSymbolViewModel>();
                    return;
                }

                var response = await DocumentSymbolsRequestAsync(textBuffer, languageServiceBroker).ConfigureAwait(false);
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
                    symbolTree.ItemsSource = new ObservableCollection<DocumentSymbolViewModel>();
                    this.symbolsTreeItemsSource = new ObservableCollection<DocumentSymbolViewModel>();
                }
            }).FileAndForget("Document Outline: Active Document Changed");
        }

        private async Task<ManualInvocationResponse?> DocumentSymbolsRequestAsync(ITextBuffer textBuffer, ILanguageServiceBroker2 languageServiceBroker)
        {
            var parameterFactory = new DocumentSymbolParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = new Uri(GetFilePath(this.textView))
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
            if (this.symbolTreeInitialized)
            {
                var documentSymbolModels = new ObservableCollection<DocumentSymbolViewModel>();
                foreach (var documentSymbolModel in this.symbolsTreeItemsSource)
                {
                    documentSymbolModels.Add(SetIsExpanded(documentSymbolModel, true));
                }

                this.symbolsTreeItemsSource = documentSymbolModels;
                symbolTree.ItemsSource = documentSymbolModels;
            }
        }

        private void CollapseAll(object sender, RoutedEventArgs e)
        {
            if (this.symbolTreeInitialized)
            {
                var documentSymbolModels = new ObservableCollection<DocumentSymbolViewModel>();
                foreach (var documentSymbolModel in this.symbolsTreeItemsSource)
                {
                    documentSymbolModels.Add(SetIsExpanded(documentSymbolModel, false));
                }

                this.symbolsTreeItemsSource = documentSymbolModels;
                symbolTree.ItemsSource = documentSymbolModels;
            }
        }

        private DocumentSymbolViewModel SetIsExpanded(DocumentSymbolViewModel documentSymbolModel, bool isExpanded)
        {
            documentSymbolModel.IsExpanded = isExpanded;
            var documentSymbolModelChildren = new ObservableCollection<DocumentSymbolViewModel>();
            foreach (var documentSymbolModelChild in documentSymbolModel.Children)
            {
                documentSymbolModelChildren.Add(SetIsExpanded(documentSymbolModelChild, isExpanded));
            }

            documentSymbolModel.Children = documentSymbolModelChildren;
            return documentSymbolModel;
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

        // When symbol node clicked, selects corresponding code
        private void JumpToContent(object sender, EventArgs e)
        {
            RoslynDebug.AssertNotNull(this.snapshot);
            if (sender is StackPanel panel && panel.DataContext is DocumentSymbolViewModel symbol)
            {
                if (symbol.StartLine >= 0 && symbol.StartLine < this.snapshot.LineCount)
                {
                    var position = this.snapshot.GetLineFromLineNumber(symbol.StartLine).Start.Position;
                    if (position >= 0 && position <= this.snapshot.Length)
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
