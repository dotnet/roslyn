// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Razor.Protocol.DevTools;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Razor;
using Microsoft.VisualStudio.Razor.LanguageClient;
using Microsoft.VisualStudio.Razor.SyntaxVisualizer;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.RazorExtension.SyntaxVisualizer;

internal partial class SyntaxVisualizerControl : UserControl, IVsRunningDocTableEvents, IDisposable
{
    private static string s_baseTempPath = Path.Combine(Path.GetTempPath(), "RazorDevTools");

    private JoinableTaskFactory? _joinableTaskFactory;
    private LSPDocumentManager? _documentManager;
    private FileUriProvider? _fileUriProvider;
    private LanguageServerFeatureOptions? _languageServerFeatureOptions;
    private LSPRequestInvoker? _lspRequestInvoker;
    private IRemoteServiceInvoker? _remoteServiceInvoker;
    private uint _runningDocumentTableCookie;
    private IVsRunningDocumentTable? _runningDocumentTable;
    private IWpfTextView? _activeWpfTextView;
    private bool _isNavigatingFromTreeToSource;
    private bool _isNavigatingFromSourceToTree;

    private IVsRunningDocumentTable? RunningDocumentTable
    {
        get
        {
            if (_runningDocumentTable == null)
            {
                _runningDocumentTable = VSServiceHelpers.GetRequiredMefService<IVsRunningDocumentTable, SVsRunningDocumentTable>();
            }

            return _runningDocumentTable;
        }
    }

    public SyntaxVisualizerControl()
    {
        InitializeComponent();

        InitializeRunningDocumentTable();
    }

    [MemberNotNull(nameof(_joinableTaskFactory), nameof(_documentManager), nameof(_fileUriProvider), nameof(_languageServerFeatureOptions), nameof(_lspRequestInvoker), nameof(_remoteServiceInvoker))]
    private void EnsureInitialized()
    {
        if (_joinableTaskFactory is not null &&
            _documentManager is not null &&
            _fileUriProvider is not null &&
            _languageServerFeatureOptions is not null &&
            _lspRequestInvoker is not null &&
            _remoteServiceInvoker is not null)
        {
            return;
        }

        _joinableTaskFactory = VSServiceHelpers.GetRequiredMefService<JoinableTaskContext>().Factory;
        _documentManager = VSServiceHelpers.GetRequiredMefService<LSPDocumentManager>();
        _fileUriProvider = VSServiceHelpers.GetRequiredMefService<FileUriProvider>();
        _languageServerFeatureOptions = VSServiceHelpers.GetRequiredMefService<LanguageServerFeatureOptions>();
        _lspRequestInvoker = VSServiceHelpers.GetRequiredMefService<LSPRequestInvoker>();
        _remoteServiceInvoker = VSServiceHelpers.GetRequiredMefService<IRemoteServiceInvoker>();
    }

    private void InitializeRunningDocumentTable()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (RunningDocumentTable != null)
        {
            RunningDocumentTable.AdviseRunningDocTableEvents(this, out _runningDocumentTableCookie);
        }
    }

    void IDisposable.Dispose()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        try
        {
            if (Directory.Exists(s_baseTempPath))
            {
                Directory.Delete(s_baseTempPath, recursive: true);
            }
        }
        catch
        {
        }

        if (_runningDocumentTableCookie != 0)
        {
            _runningDocumentTable?.UnadviseRunningDocTableEvents(_runningDocumentTableCookie);
            _runningDocumentTableCookie = 0;
        }
    }

    public void ShowFormattingDocument()
    {
        if (_activeWpfTextView is null)
        {
            return;
        }

        EnsureInitialized();

        if (_fileUriProvider.TryGet(_activeWpfTextView.TextBuffer, out var hostDocumentUri))
        {
            ShowGeneratedCode(_activeWpfTextView.TextBuffer, hostDocumentUri, GeneratedDocumentKind.Formatting);
        }
    }

    public void ShowGeneratedCode()
    {
        if (_activeWpfTextView is null)
        {
            return;
        }

        EnsureInitialized();

        if (_fileUriProvider.TryGet(_activeWpfTextView.TextBuffer, out var hostDocumentUri))
        {
            ShowGeneratedCode(_activeWpfTextView.TextBuffer, hostDocumentUri, GeneratedDocumentKind.CSharp);
        }
    }

    private void OpenVirtualDocuments<T>(ITextBuffer hostDocumentBuffer) where T : VirtualDocumentSnapshot
    {
        EnsureInitialized();

        if (_fileUriProvider.TryGet(hostDocumentBuffer, out var hostDocumentUri) &&
            _documentManager.TryGetDocument(hostDocumentUri, out var hostDocument) &&
            hostDocument.TryGetAllVirtualDocuments<T>(out var virtualDocuments))
        {
            foreach (var doc in virtualDocuments)
            {
                OpenGeneratedCode(doc.Uri.AbsolutePath, doc.Snapshot.GetText());
            }
        }
    }

    private void OpenGeneratedCode(string filePath, string generatedCode)
    {
        var tempFileName = GetTempFileName(filePath);

        // Ignore any I/O errors
        try
        {
            File.WriteAllText(tempFileName, generatedCode);
            VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, tempFileName);
        }
        catch
        {
        }
    }

    public void ShowGeneratedHtml()
    {
        if (_activeWpfTextView is null)
        {
            return;
        }

        OpenVirtualDocuments<HtmlVirtualDocumentSnapshot>(_activeWpfTextView.TextBuffer);
    }

    private void ShowGeneratedCode(ITextBuffer textBuffer, Uri hostDocumentUri, GeneratedDocumentKind kind)
    {
        EnsureInitialized();

        var request = DocumentContentsRequest.Create(hostDocumentUri, kind);

        var response = _joinableTaskFactory.Run(async () =>
        {
            var lspResponse = await _lspRequestInvoker.ReinvokeRequestOnServerAsync<DocumentContentsRequest, string>(
                textBuffer,
                "razor/generatedDocumentContents",
                RazorLSPConstants.RoslynLanguageServerName,
                request,
                CancellationToken.None);

            return lspResponse?.Response;
        });

        var extension = kind switch
        {
            GeneratedDocumentKind.CSharp => ".g.cs",
            GeneratedDocumentKind.Html => ".g.html",
            GeneratedDocumentKind.Formatting => ".formatting.cs",
            _ => null
        };

        if (response != null)
        {
            OpenGeneratedCode(hostDocumentUri.AbsolutePath + extension, response);
        }
    }

    private void ShowSerializedTagHelpers(Uri hostDocumentUri, TagHelperDisplayMode displayKind)
    {
        EnsureInitialized();

        var tagHelpersKind = displayKind switch
        {
            TagHelperDisplayMode.All => TagHelpersKind.All,
            TagHelperDisplayMode.InScope => TagHelpersKind.InScope,
            TagHelperDisplayMode.Referenced => TagHelpersKind.Referenced,
            _ => TagHelpersKind.All
        };

        var tagHelpers = _joinableTaskFactory.Run(async () =>
        {
            var workspace = VSServiceHelpers.GetRequiredMefService<VisualStudioWorkspace>();
            var solution = workspace.CurrentSolution;
            var tagHelpersJson = await SyntaxVisualizerHelper.GetTagHelperDescriptorsAsync(_remoteServiceInvoker, hostDocumentUri, tagHelpersKind, solution, CancellationToken.None).ConfigureAwait(false);

            if (tagHelpersJson is null)
            {
                return "";
            }

            return tagHelpersJson;
        });

        ShowSerializedTagHelpers(displayKind, tagHelpers);
    }

    public void ShowSerializedTagHelpers(TagHelperDisplayMode displayKind)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        EnsureInitialized();

        if (_activeWpfTextView is not null &&
            _fileUriProvider.TryGet(_activeWpfTextView.TextBuffer, out var hostDocumentUri))
        {
            ShowSerializedTagHelpers(hostDocumentUri, displayKind);
        }
    }

    private static void ShowSerializedTagHelpers(TagHelperDisplayMode displayKind, string tagHelpers)
    {
        var tempFileName = GetTempFileName(displayKind.ToString() + "TagHelpers.json");

        File.WriteAllText(tempFileName, tagHelpers);

        VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, tempFileName);
    }

    private static string GetTempFileName(string originalFilePath)
    {
        var fileName = Path.GetFileName(originalFilePath);
        var tempPath = Path.Combine(s_baseTempPath, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);
        var tempFileName = Path.Combine(tempPath, fileName);
        return tempFileName;
    }

    private void SyntaxVisualizerControl_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshSyntaxVisualizer();
    }

    private void SyntaxVisualizerControl_Unloaded(object sender, RoutedEventArgs e)
    {
        Clear();
    }

    // Copied from roslyn-sdk.. not sure this works
    private void SyntaxVisualizerControl_GotFocus(object sender, RoutedEventArgs e)
    {
        if (_activeWpfTextView != null && !_activeWpfTextView.Properties.ContainsProperty("BackupOpacity"))
        {
            var selectionLayer = _activeWpfTextView.GetAdornmentLayer(PredefinedAdornmentLayers.Selection);

            // Backup current selection opacity value.
            _activeWpfTextView.Properties.AddProperty("BackupOpacity", selectionLayer.Opacity);

            // Set selection opacity to a high value. This ensures that the text selection is visible
            // even when the code editor loses focus (i.e. when user is changing the text selection by
            // clicking on nodes in the TreeView).
            selectionLayer.Opacity = 1;
        }
    }

    private void SyntaxVisualizerControl_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_activeWpfTextView != null && _activeWpfTextView.Properties.ContainsProperty("BackupOpacity"))
        {
            var selectionLayer = _activeWpfTextView.GetAdornmentLayer(PredefinedAdornmentLayers.Selection);

            // Restore backed up selection opacity value.
            selectionLayer.Opacity = (double)_activeWpfTextView.Properties.GetProperty("BackupOpacity");
            _activeWpfTextView.Properties.RemoveProperty("BackupOpacity");
        }
    }

    int IVsRunningDocTableEvents.OnBeforeDocumentWindowShow(uint docCookie, int isFirstShow, IVsWindowFrame vsWindowFrame)
    {
        if (IsVisible && isFirstShow == 0)
        {
            var wpfTextView = GetWpfTextView(vsWindowFrame);
            if (wpfTextView != null)
            {
                var contentType = wpfTextView.TextBuffer.ContentType;
                if (contentType.IsOfType(RazorConstants.RazorLSPContentTypeName))
                {
                    if (_activeWpfTextView != wpfTextView)
                    {
                        Clear();
                        _activeWpfTextView = wpfTextView;
                        _activeWpfTextView.TextBuffer.Changed += HandleTextBufferChanged;
                        _activeWpfTextView.Selection.SelectionChanged += HandleSelectionChanged;

                        RefreshSyntaxVisualizer();
                    }
                    else if (treeView.Items.Count == 0)
                    {
                        // even if we're already tracking this document, if we didn't have a tree yet, then try again
                        RefreshSyntaxVisualizer();
                    }
                }
            }
        }

        return VSConstants.S_OK;
    }

    // Handle the case where the user closes the current code document / switches to a different code document.
    int IVsRunningDocTableEvents.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame vsWindowFrame)
    {
        if (IsVisible && _activeWpfTextView != null)
        {
            var wpfTextView = GetWpfTextView(vsWindowFrame);
            if (wpfTextView == _activeWpfTextView)
            {
                Clear();
            }
        }

        return VSConstants.S_OK;
    }

    internal void Clear()
    {
        if (_activeWpfTextView != null)
        {
            _activeWpfTextView.TextBuffer.Changed -= HandleTextBufferChanged;
            _activeWpfTextView.Selection.SelectionChanged -= HandleSelectionChanged;
            _activeWpfTextView = null;
        }

        treeView.Items.Clear();
    }

    private void HandleTextBufferChanged(object sender, TextContentChangedEventArgs e)
    {
        RefreshSyntaxVisualizer();
    }

    private void HandleSelectionChanged(object sender, EventArgs e)
    {
        if (_isNavigatingFromTreeToSource)
        {
            return;
        }

        if (treeView.Items.Count == 0)
        {
            return;
        }

        NavigateToCaret();
    }

    private void NavigateToCaret()
    {
        if (_activeWpfTextView is null)
        {
            return;
        }

        var caret = _activeWpfTextView.Selection.StreamSelectionSpan.SnapshotSpan.Span.Start;

        var node = FindNodeForPosition((TreeViewItem)treeView.Items[0], caret);
        if (node is null)
        {
            return;
        }

        _isNavigatingFromSourceToTree = true;
        ExpandPathTo(node);
        node.IsSelected = true;
        _isNavigatingFromSourceToTree = false;
    }

    private void ExpandPathTo(TreeViewItem? item)
    {
        if (item != null)
        {
            item.IsExpanded = true;
            ExpandPathTo(item.Parent as TreeViewItem);
            item.BringIntoView();
        }
    }

    private TreeViewItem? FindNodeForPosition(TreeViewItem item, int caret)
    {
        if (item.Tag is not RazorSyntaxNode node)
        {
            return null;
        }

        foreach (TreeViewItem child in item.Items)
        {
            var childNode = FindNodeForPosition(child, caret);
            if (childNode is not null)
            {
                return childNode;
            }
        }

        if (caret >= node.SpanStart && caret <= node.SpanEnd)
        {
            return item;
        }

        return null;
    }

    private void RefreshSyntaxVisualizer()
    {
        if (!IsVisible || _activeWpfTextView is null)
        {
            return;
        }

        EnsureInitialized();

        if (_activeWpfTextView is not null &&
            _fileUriProvider.TryGet(_activeWpfTextView.TextBuffer, out var hostDocumentUri))
        {
            var rootNode = _joinableTaskFactory.Run(async () =>
            {
                var workspace = VSServiceHelpers.GetRequiredMefService<VisualStudioWorkspace>();
                var solution = workspace.CurrentSolution;
                return await SyntaxVisualizerHelper.GetSyntaxRootAsync(_remoteServiceInvoker, hostDocumentUri, solution, CancellationToken.None);
            });

            if (rootNode is not null)
            {
                ShowSyntaxTree(rootNode);
            }
        }
    }

    private void ShowSyntaxTree(RazorSyntaxNode rootNode)
    {
        AddNode(rootNode, parent: null);

        NavigateToCaret();
    }

    private void AddNode(RazorSyntaxNode node, TreeViewItem? parent)
    {
        var item = new TreeViewItem()
        {
            Tag = node,
            IsExpanded = parent == null,
            ToolTip = node.ToString(),
            Header = $"{node.Kind} [{node.SpanStart}-{node.SpanEnd}]"
        };

        item.Selected += new RoutedEventHandler((sender, e) =>
        {
            if (!_isNavigatingFromSourceToTree)
            {
                _isNavigatingFromTreeToSource = true;

                if (IsVisible && _activeWpfTextView != null)
                {
                    var snapShotSpan = new SnapshotSpan(_activeWpfTextView.TextBuffer.CurrentSnapshot, node.SpanStart, node.SpanLength);

                    _activeWpfTextView.Selection.Select(snapShotSpan, false);
                    _activeWpfTextView.ViewScroller.EnsureSpanVisible(snapShotSpan);
                }

                _isNavigatingFromTreeToSource = false;
            }

            e.Handled = true;
        });

        if (parent == null)
        {
            treeView.Items.Clear();
            treeView.Items.Add(item);
        }
        else
        {
            parent.Items.Add(item);
        }

        foreach (var child in node.Children)
        {
            AddNode(child, item);
        }
    }

    #region Unused IVsRunningDocTableEvents

    int IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock(uint docCookie, uint lockType, uint readLocksRemaining, uint editLocksRemaining)
    {
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
    {
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents.OnAfterFirstDocumentLock(uint docCookie, uint lockType, uint readLocksRemaining, uint editLocksRemaining)
    {
        return VSConstants.S_OK;
    }

    int IVsRunningDocTableEvents.OnAfterSave(uint docCookie)
    {
        return VSConstants.S_OK;
    }

    #endregion

    private static IWpfTextView? GetWpfTextView(IVsWindowFrame vsWindowFrame)
    {
        IWpfTextView? wpfTextView = null;
        var vsTextView = VsShellUtilities.GetTextView(vsWindowFrame);

        if (vsTextView != null)
        {
            // TODO: Work out what dependency to bump, and use DefGuidList.guidIWpfTextViewHost
            var guidTextViewHost = new Guid("8C40265E-9FDB-4f54-A0FD-EBB72B7D0476");
            if (((IVsUserData)vsTextView).GetData(ref guidTextViewHost, out var textViewHost) == VSConstants.S_OK &&
                textViewHost != null)
            {
                wpfTextView = ((IWpfTextViewHost)textViewHost).TextView;
            }
        }

        return wpfTextView;
    }

    private void treeView_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != System.Windows.Input.Key.Enter)
        {
            return;
        }

        if (!IsVisible || _activeWpfTextView is null)
        {
            return;
        }

        if (treeView.SelectedItem is not TreeViewItem item)
        {
            return;
        }

        if (item.Tag is not RazorSyntaxNode node)
        {
            return;
        }

        var caretPoint = new SnapshotPoint(_activeWpfTextView.TextBuffer.CurrentSnapshot, node.SpanEnd);

        // When we activate a node, we don't move the caret, because its a bit weird, but its equally weird to move focus
        // to the editor, and not move the caret.
        _isNavigatingFromTreeToSource = true;
        _activeWpfTextView.Caret.MoveTo(caretPoint);
        _activeWpfTextView.VisualElement.Focus();
        _isNavigatingFromTreeToSource = false;
    }

    internal enum TagHelperDisplayMode
    {
        All,
        InScope,
        Referenced
    }
}
