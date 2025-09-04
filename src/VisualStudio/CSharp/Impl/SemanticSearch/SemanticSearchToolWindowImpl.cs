// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SemanticSearch;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Extensibility.VSSdkCompatibility;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.RpcContracts.RemoteUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp;

using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;

[Shared]
[Export(typeof(SemanticSearchToolWindowImpl))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class SemanticSearchToolWindowImpl(
    IHostWorkspaceProvider hostWorkspaceProvider,
    IThreadingContext threadingContext,
    ITextEditorFactoryService textEditorFactory,
    ITextDocumentFactoryService textDocumentFactory,
    IContentTypeRegistryService contentTypeRegistry,
    IVsEditorAdaptersFactoryService vsEditorAdaptersFactoryService,
    IAsynchronousOperationListenerProvider listenerProvider,
    IGlobalOptionService globalOptions,
    Lazy<ISemanticSearchSolutionService> semanticSearchService,
    VisualStudioWorkspace workspace,
    IStreamingFindUsagesPresenter resultsPresenter,
    ITextUndoHistoryRegistry undoHistoryRegistry,
    IVsService<SVsUIShell, IVsUIShell> vsUIShellProvider,
    IPreviewFactoryService previewFactory) : IDisposable
{
    private const int ToolBarHeight = 26;
    private const int ToolBarButtonSize = 20;

    private static readonly Guid s_logOutputPainGuid = new("{4C4F1810-C865-493E-98A7-8E1120A9FDE4}");
    private const string LogOutputPaneName = "Semantic Search Log";

    private static readonly Lazy<ControlTemplate> s_buttonTemplate = new(CreateButtonTemplate);

    private readonly IContentType _contentType = contentTypeRegistry.GetContentType(CSharpSemanticSearchContentType.Name);
    private readonly IAsynchronousOperationListener _asyncListener = listenerProvider.GetListener(FeatureAttribute.SemanticSearch);

    private readonly Lazy<SemanticSearchEditorWorkspace> _semanticSearchWorkspace
        = new(() => new SemanticSearchEditorWorkspace(
            hostWorkspaceProvider.Workspace.Services.HostServices,
            semanticSearchService.Value,
            threadingContext,
            listenerProvider));

    // access interlocked:
    private volatile CancellationTokenSource? _pendingExecutionCancellationSource;

    // Create on UI thread only, access on any thread:
    private AsyncBatchingWorkQueue<string>? _lazyLogQueue;

    // Access on UI thread only:
    private Button? _executeButton;
    private Button? _cancelButton;
    private IWpfTextView? _textView;
    private ITextBuffer? _textBuffer;

    private IRemoteUserControl? _lazyContent;
    private IVsOutputWindowPane? _lazyLogOutputPane;

    public void Dispose()
    {
        _lazyContent?.Dispose();
    }

    public ISemanticSearchSolutionService SemanticSearchService
        => semanticSearchService.Value;

    public async Task<IRemoteUserControl> InitializeAsync(CancellationToken cancellationToken)
    {
        var content = _lazyContent;
        if (content != null)
        {
            return content;
        }

        var element = await CreateContentAsync(cancellationToken).ConfigureAwait(false);
        Interlocked.CompareExchange(ref _lazyContent, new WpfControlWrapper(element), null);
        return _lazyContent;
    }

    private async Task<FrameworkElement> CreateContentAsync(CancellationToken cancellationToken)
    {
        // TODO: replace with XAML once we can represent the editor as a XAML element
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1927626

        // TODO: Add toolbar and convert Execute and Cancel buttons to commands.
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1978331

        // The WPF control needs to be created on an UI thread
        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var vsUIShell = await vsUIShellProvider.GetValueAsync(cancellationToken).ConfigureAwait(false);

        var textViewHost = CreateTextViewHost(vsUIShell);
        var textViewControl = textViewHost.HostControl;
        _textView = textViewHost.TextView;
        _textBuffer = textViewHost.TextView.TextBuffer;

        // enable LSP:
        Contract.ThrowIfFalse(textDocumentFactory.TryGetTextDocument(_textBuffer, out var textDocument));
        textDocument.Rename(SemanticSearchService.GetQueryDocumentFilePath());

        var toolWindowGrid = new Grid();
        toolWindowGrid.ColumnDefinitions.Add(new ColumnDefinition());
        toolWindowGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(ToolBarHeight, GridUnitType.Pixel) });
        toolWindowGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
        toolWindowGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

        var toolbarGrid = new Grid();

        // Set dynamically, so that it gets refreshed when theme changes:
        toolbarGrid.SetResourceReference(Control.BackgroundProperty, EnvironmentColors.CommandBarGradientBrushKey);

        toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(ToolBarHeight, GridUnitType.Pixel) });
        toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(ToolBarHeight, GridUnitType.Pixel) });
        toolbarGrid.ColumnDefinitions.Add(new ColumnDefinition());

        var executeButton = CreateButton(
            KnownMonikers.Run,
            automationName: CSharpVSResources.RunQuery,
            acceleratorKey: "Ctrl+Enter",
            toolTip: CSharpVSResources.RunQueryCommandToolTip);

        executeButton.Click += (_, _) => RunQuery();
        _executeButton = executeButton;

        var cancelButton = CreateButton(
            KnownMonikers.Stop,
            automationName: CSharpVSResources.CancelQuery,
            acceleratorKey: "Escape",
            toolTip: CSharpVSResources.CancelQueryCommandToolTip);

        cancelButton.Click += (_, _) => CancelQuery();
        cancelButton.IsEnabled = false;
        _cancelButton = cancelButton;

        toolWindowGrid.Children.Add(toolbarGrid);

        toolWindowGrid.Children.Add(textViewControl);
        toolbarGrid.Children.Add(executeButton);
        toolbarGrid.Children.Add(cancelButton);

        // placement within the tool window grid:

        Grid.SetRow(toolbarGrid, 0);
        Grid.SetColumn(toolbarGrid, 0);

        Grid.SetRow(textViewControl, 2);
        Grid.SetColumn(textViewControl, 0);

        // placement within the toolbar grid:

        Grid.SetRow(executeButton, 0);
        Grid.SetColumn(executeButton, 0);

        Grid.SetRow(cancelButton, 0);
        Grid.SetColumn(cancelButton, 1);

        await TaskScheduler.Default;

        // TODO: add UI that allows to set target language (e.g. we can have two workspaces, one for for C# and one for VB, or just a combo box with C#/VB choice)
        await _semanticSearchWorkspace.Value.OpenQueryDocumentAsync(_textBuffer, targetLanguage: null, cancellationToken).ConfigureAwait(false);

        return toolWindowGrid;
    }

    private static Button CreateButton(
        Imaging.Interop.ImageMoniker moniker,
        string automationName,
        string acceleratorKey,
        string toolTip)
    {
        var image = new CrispImage()
        {
            Moniker = moniker,
            Width = ToolBarButtonSize - 4,
            Height = ToolBarButtonSize - 4,
            ToolTip = toolTip,
        };

        var holder = new ContentControl
        {
            Height = ToolBarButtonSize,
            Width = ToolBarButtonSize,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0, 0, 0, 0),
        };

        ImageThemingUtilities.SetImageBackgroundColor(holder, Colors.Transparent);
        holder.Content = image;

        var button = new Button()
        {
            Template = s_buttonTemplate.Value,
            Content = holder,
        };

        button.SetValue(AutomationProperties.NameProperty, automationName);
        button.SetValue(AutomationProperties.AcceleratorKeyProperty, acceleratorKey);

        image.SetBinding(CrispImage.GrayscaleProperty, new Binding(UIElement.IsEnabledProperty.Name)
        {
            Source = button,
            Converter = new NegateBooleanConverter()
        });

        return button;
    }

    private static ControlTemplate CreateButtonTemplate()
    {
        var context = new ParserContext();
        context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
        context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
        context.XmlnsDictionary.Add("vsui", "clr-namespace:Microsoft.VisualStudio.PlatformUI;assembly=Microsoft.VisualStudio.Shell.15.0");

        // CodeQL [SM02229] The string being passed to the deserialize method is practically constant and all the types listed are controlled by the VS platform.
        return (ControlTemplate)XamlReader.Parse($$$"""
            <ControlTemplate x:Key="ButtonTemplate" TargetType="{x:Type ButtonBase}">
                <Border x:Name="border" Background="Transparent" BorderThickness="0,0,0,0" SnapsToDevicePixels="true">
                    <ContentPresenter x:Name="contentPresenter" Focusable="False" RecognizesAccessKey="True"/>
                </Border>
                <ControlTemplate.Triggers>
                    <Trigger Property="Button.IsDefaulted" Value="true">
                        <Setter Property="BorderBrush" TargetName="border" Value="{DynamicResource {x:Static SystemColors.HighlightBrushKey} }"/>
                    </Trigger>
                    <Trigger Property="IsMouseOver" Value="true">
                        <Setter Property="BorderBrush" TargetName="border" Value="{DynamicResource {x:Static vsui:{{{nameof(EnvironmentColors)}}}.{{{nameof(EnvironmentColors.CommandBarBorderBrushKey)}}}}}"/>
                        <Setter Property="Background" TargetName="border" Value="{DynamicResource {x:Static vsui:{{{nameof(EnvironmentColors)}}}.{{{nameof(EnvironmentColors.CommandBarMouseOverBackgroundGradientBrushKey)}}}}}"/>
                        <Setter Property="TextElement.Foreground" TargetName="contentPresenter" Value="{DynamicResource {x:Static vsui:{{{nameof(EnvironmentColors)}}}.{{{nameof(EnvironmentColors.CommandBarTextHoverBrushKey)}}}}}"/>
                    </Trigger>
                    <Trigger Property="IsPressed" Value="true">
                        <Setter Property="BorderBrush" TargetName="border" Value="{DynamicResource {x:Static vsui:{{{nameof(EnvironmentColors)}}}.{{{nameof(EnvironmentColors.CommandBarBorderBrushKey)}}}}}"/>
                        <Setter Property="Background" TargetName="border" Value="{DynamicResource {x:Static vsui:{{{nameof(EnvironmentColors)}}}.{{{nameof(EnvironmentColors.CommandBarMouseDownBackgroundGradientBrushKey)}}}}}"/>
                        <Setter Property="TextElement.Foreground" TargetName="contentPresenter" Value="{DynamicResource {x:Static vsui:{{{nameof(EnvironmentColors)}}}.{{{nameof(EnvironmentColors.CommandBarTextMouseDownBrushKey)}}}}}"/>
                    </Trigger>
                    <Trigger Property="IsEnabled" Value="false">
                        <Setter Property="TextElement.Foreground" TargetName="contentPresenter" Value="{DynamicResource {x:Static vsui:{{{nameof(EnvironmentColors)}}}.{{{nameof(EnvironmentColors.CommandBarTextInactiveBrushKey)}}}}}"/>
                    </Trigger>
                </ControlTemplate.Triggers>
            </ControlTemplate>
            """, context);
    }

    private IWpfTextViewHost CreateTextViewHost(IVsUIShell vsUIShell)
    {
        Contract.ThrowIfFalse(threadingContext.JoinableTaskContext.IsOnMainThread);

        var toolWindowId = SemanticSearchToolWindow.Id;
        ErrorHandler.ThrowOnFailure(vsUIShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFrameOnly, ref toolWindowId, out var windowFrame));

        var commandUiGuid = VSConstants.GUID_TextEditorFactory;
        ErrorHandler.ThrowOnFailure(windowFrame.SetGuidProperty((int)__VSFPROPID.VSFPROPID_InheritKeyBindings, ref commandUiGuid));

        var roleSet = textEditorFactory.CreateTextViewRoleSet(
            PredefinedTextViewRoles.Analyzable,
            PredefinedTextViewRoles.Editable,
            PredefinedTextViewRoles.Interactive,
            PredefinedTextViewRoles.Zoomable);

        var oleServiceProvider = (OLE.Interop.IServiceProvider)Shell.Package.GetGlobalService(typeof(OLE.Interop.IServiceProvider));

        var bufferAdapter = vsEditorAdaptersFactoryService.CreateVsTextBufferAdapter(oleServiceProvider, _contentType);
        bufferAdapter.InitializeContent("", 0);

        var textViewAdapter = vsEditorAdaptersFactoryService.CreateVsTextViewAdapter(oleServiceProvider, roleSet);

        // set properties to behave like a code window:
        ErrorHandler.ThrowOnFailure(((IVsTextEditorPropertyCategoryContainer)textViewAdapter).GetPropertyCategory(DefGuidList.guidEditPropCategoryViewMasterSettings, out var propContainer));
        propContainer.SetProperty(VSEDITPROPID.VSEDITPROPID_ViewComposite_AllCodeWindowDefaults, true);
        propContainer.SetProperty(VSEDITPROPID.VSEDITPROPID_ViewGlobalOpt_AutoScrollCaretOnTextEntry, true);

        ErrorHandler.ThrowOnFailure(textViewAdapter.Initialize(
            (IVsTextLines)bufferAdapter,
            IntPtr.Zero,
            (uint)TextViewInitFlags.VIF_HSCROLL | (uint)TextViewInitFlags.VIF_VSCROLL | (uint)TextViewInitFlags3.VIF_NO_HWND_SUPPORT,
            [new INITVIEW { fSelectionMargin = 0, fWidgetMargin = 0, fVirtualSpace = 0, fDragDropMove = 1 }]));

        var textViewHost = vsEditorAdaptersFactoryService.GetWpfTextViewHost(textViewAdapter);
        Contract.ThrowIfNull(textViewHost);

        ErrorHandler.ThrowOnFailure(windowFrame.SetProperty((int)__VSFPROPID.VSFPROPID_ViewHelper, textViewAdapter));

        _ = new CommandFilter(this, textViewAdapter);

        return textViewHost;
    }

    private bool IsExecutingUIState()
    {
        Contract.ThrowIfFalse(threadingContext.JoinableTaskContext.IsOnMainThread);
        Contract.ThrowIfNull(_executeButton);

        return !_executeButton.IsEnabled;
    }

    private void UpdateUIState()
    {
        Contract.ThrowIfFalse(threadingContext.JoinableTaskContext.IsOnMainThread);
        Contract.ThrowIfNull(_executeButton);
        Contract.ThrowIfNull(_cancelButton);

        // reflect the actual state in UI:
        var isExecuting = _pendingExecutionCancellationSource != null;

        _executeButton.IsEnabled = !isExecuting;
        _cancelButton.IsEnabled = isExecuting;
    }

    /// <summary>
    /// Replaces current text buffer content with specified <paramref name="text"/>. Allow using Ctrl+Z to revert to the previous content.
    /// Must be invoked on UI thread.
    /// </summary>
    public void SetEditorText(string text)
    {
        Contract.ThrowIfFalse(threadingContext.JoinableTaskContext.IsOnMainThread);
        Contract.ThrowIfNull(_textBuffer);

        Contract.ThrowIfFalse(undoHistoryRegistry.TryGetHistory(_textBuffer, out var undoHistory));
        using var undoTransaction = undoHistory.CreateTransaction(FeaturesResources.SemanticSearch);

        using (var edit = _textBuffer.CreateEdit())
        {
            edit.Replace(0, _textBuffer.CurrentSnapshot.Length, text);
            edit.Apply();
        }

        undoTransaction.Complete();
    }

    private void CancelQuery()
    {
        Contract.ThrowIfFalse(threadingContext.JoinableTaskContext.IsOnMainThread);
        Contract.ThrowIfFalse(IsExecutingUIState());

        // The query might have been cancelled already but the UI may not be updated yet:
        var pendingExecutionCancellationSource = Interlocked.Exchange(ref _pendingExecutionCancellationSource, null);
        pendingExecutionCancellationSource?.Cancel();

        UpdateUIState();
    }

    /// <summary>
    /// Runs the current query.
    /// Must be invoked on UI thread.
    /// </summary>
    public void RunQuery()
    {
        Contract.ThrowIfFalse(threadingContext.JoinableTaskContext.IsOnMainThread);
        Contract.ThrowIfFalse(!IsExecutingUIState());
        Contract.ThrowIfNull(_textBuffer);

        var cancellationSource = new CancellationTokenSource();

        // Cancel execution that's in progress (if any) - may occur when UI state hasn't been updated yet based on the actual state.
        Interlocked.Exchange(ref _pendingExecutionCancellationSource, cancellationSource)?.Cancel();

        UpdateUIState();

        _lazyLogQueue ??= new(
            delay: TimeSpan.Zero,
            async (messages, cancellationToken) =>
            {
                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

                var pane = GetOrCreateLogOutputPane();

                foreach (var message in messages)
                {
                    pane.OutputStringThreadSafe(message + Environment.NewLine);
                }
            },
            _asyncListener,
            cancellationToken: CancellationToken.None);

        var (presenterContext, presenterCancellationToken) = resultsPresenter.StartSearch(ServicesVSResources.Semantic_search_results, StreamingFindUsagesPresenterOptions.Default);
        presenterCancellationToken.Register(() => cancellationSource?.Cancel());

        var querySolution = _semanticSearchWorkspace.Value.CurrentSolution;
        var queryDocument = querySolution.GetRequiredDocument(SemanticSearchService.GetQueryDocumentId(querySolution));

        var completionToken = _asyncListener.BeginAsyncOperation(nameof(SemanticSearchToolWindow) + ".Execute");
        _ = ExecuteAsync(cancellationSource.Token).ReportNonFatalErrorAsync().CompletesAsyncOperation(completionToken);

        async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await TaskScheduler.Default;

            try
            {
                var executor = new SemanticSearchQueryExecutor(presenterContext, message => _lazyLogQueue.AddWork(message), globalOptions);
                var oldSolution = workspace.CurrentSolution;
                var (newSolution, fileUpdates) = await executor.ExecuteAsync(query: null, queryDocument, oldSolution, cancellationToken).ConfigureAwait(false);

                var success = true;
                if (newSolution != oldSolution)
                {
                    var changedSolution = newSolution;

                    var previewDialogService = workspace.Services.GetService<IPreviewDialogService>();
                    if (previewDialogService != null &&
                        previewFactory.GetSolutionPreviews(oldSolution, newSolution, cancellationToken)?.ChangeSummary is { } changeSummary)
                    {
                        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

                        changedSolution = previewDialogService.PreviewChanges(
                            EditorFeaturesResources.Preview_Changes,
                            "vs.codefix.previewchanges",
                            "Updates",
                            EditorFeaturesResources.Changes,
                            Glyph.OpenFolder,
                            changeSummary.NewSolution,
                            changeSummary.OldSolution,
                            showCheckBoxes: false);

                        // TODO: report error
                        success = changedSolution != null && workspace.TryApplyChanges(changedSolution);
                    }
                    else
                    {
                        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

                        success = workspace.TryApplyChanges(changedSolution);
                    }
                }

                if (success)
                {
                    // TODO: parallelize, exceptions
                    foreach (var (filePath, newContent) in fileUpdates)
                    {
                        if (newContent == null)
                        {
                            File.Delete(filePath);
                        }
                        else
                        {
                            File.WriteAllText(filePath, newContent, Encoding.UTF8);
                        }
                    }
                }
            }
            finally
            {
                // Only clear pending source if it is the same as our source (otherwise, another execution has already kicked off):
                Interlocked.CompareExchange(ref _pendingExecutionCancellationSource, value: null, cancellationSource);

                // Dispose cancellation source and clear it, so that the presenterCancellationToken handler won't attempt to cancel:
                var source = Interlocked.Exchange(ref cancellationSource, null);
                Contract.ThrowIfNull(source);
                source.Dispose();

                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

                // Update UI:
                UpdateUIState();
            }
        }
    }

    public NavigableLocation GetNavigableLocation(TextSpan textSpan)
        => new(async (options, cancellationToken) =>
        {
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            Contract.ThrowIfNull(_textView);

            var textSnapshot = _textView.TextBuffer.CurrentSnapshot;
            var snapshotSpan = new SnapshotSpan(textSnapshot, textSpan.Start, textSpan.Length);

            _textView.Selection.Select(snapshotSpan, isReversed: false);
            _textView.ViewScroller.EnsureSpanVisible(snapshotSpan, EnsureSpanVisibleOptions.AlwaysCenter);

            // Moving the caret must be the last operation involving surfaceBufferSpan because 
            // it might update the version number of textView.TextSnapshot (VB does line commit
            // when the caret leaves a line which might cause pretty listing), which must be 
            // equal to surfaceBufferSpan.SnapshotSpan.Snapshot's version number.
            _textView.Caret.MoveTo(snapshotSpan.Start);

            _textView.VisualElement.Focus();

            return true;
        });

    private IVsOutputWindowPane GetOrCreateLogOutputPane()
    {
        if (_lazyLogOutputPane != null)
            return _lazyLogOutputPane;

        ThreadHelper.ThrowIfNotOnUIThread();

        var outputWindow = ServiceProvider.GlobalProvider.GetServiceOnMainThread<SVsOutputWindow, IVsOutputWindow>();

        // Try to get the pane, create if it doesn't exist
        var guid = s_logOutputPainGuid;
        if (outputWindow.GetPane(ref guid, out _lazyLogOutputPane) != VSConstants.S_OK || _lazyLogOutputPane == null)
        {
            outputWindow.CreatePane(ref guid, LogOutputPaneName, fInitVisible: 1, fClearWithSolution: 1);
            outputWindow.GetPane(ref guid, out _lazyLogOutputPane);
        }

        return _lazyLogOutputPane;
    }

    private sealed class CommandFilter : IOleCommandTarget
    {
        private readonly SemanticSearchToolWindowImpl _window;
        private readonly IOleCommandTarget _editorCommandTarget;

        public CommandFilter(SemanticSearchToolWindowImpl window, IVsTextView textView)
        {
            _window = window;
            ErrorHandler.ThrowOnFailure(textView.AddCommandFilter(this, out _editorCommandTarget));
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
            => _editorCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.OPENLINEABOVE:
                        if (!_window.IsExecutingUIState())
                        {
                            _window.RunQuery();
                            return VSConstants.S_OK;
                        }

                        break;

                    case VSConstants.VSStd2KCmdID.CANCEL:
                        if (_window.IsExecutingUIState())
                        {
                            _window.CancelQuery();
                            return VSConstants.S_OK;
                        }

                        break;
                }
            }

            return _editorCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }
    }
}
