// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename.HighlightTags;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.GoToBase;
using Microsoft.CodeAnalysis.GoToImplementation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;
using COMAsyncServiceProvider = Microsoft.VisualStudio.Shell.Interop.COMAsyncServiceProvider;
using IComponentModel = Microsoft.VisualStudio.ComponentModelHost.IComponentModel;
using IObjectWithSite = Microsoft.VisualStudio.OLE.Interop.IObjectWithSite;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using IPersistFile = Microsoft.VisualStudio.OLE.Interop.IPersistFile;
using SComponentModel = Microsoft.VisualStudio.ComponentModelHost.SComponentModel;
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;

namespace Microsoft.VisualStudio.Extensibility.Testing;

internal sealed partial class EditorInProcess : ITextViewWindowInProcess
{
    TestServices ITextViewWindowInProcess.TestServices => TestServices;

    Task<IWpfTextView> ITextViewWindowInProcess.GetActiveTextViewAsync(CancellationToken cancellationToken)
        => GetActiveTextViewAsync(cancellationToken);

    async Task<ITextBuffer?> ITextViewWindowInProcess.GetBufferContainingCaretAsync(IWpfTextView view, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        return view.GetBufferContainingCaret();
    }

    public async Task WaitForEditorOperationsAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var shell = await GetRequiredGlobalServiceAsync<SVsShell, IVsShell>(cancellationToken);
        if (shell.IsPackageLoaded(DefGuidList.guidEditorPkg, out var editorPackage) == VSConstants.S_OK)
        {
            var asyncPackage = (AsyncPackage)editorPackage;
            var collection = asyncPackage.GetPropertyValue<JoinableTaskCollection>("JoinableTaskCollection");
            await collection.JoinTillEmptyAsync(cancellationToken);
        }
    }

    public async Task<bool> IsSavedAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var vsView = await GetActiveVsTextViewAsync(cancellationToken);
        ErrorHandler.ThrowOnFailure(vsView.GetBuffer(out var buffer));

        // From CVsDocument::get_Saved
        if (buffer is IVsPersistDocData persistDocData)
        {
            ErrorHandler.ThrowOnFailure(persistDocData.IsDocDataDirty(out var dirty));
            return dirty == 0;
        }
        else if (buffer is IPersistFile persistFile)
        {
            return persistFile.IsDirty() == 0;
        }
        else if (buffer is IPersistFileFormat persistFileFormat)
        {
            ErrorHandler.ThrowOnFailure(persistFileFormat.IsDirty(out var dirty));
            return dirty == 0;
        }
        else
        {
            throw new InvalidOperationException("Unsupported document");
        }
    }

    public async Task<Document?> GetActiveDocumentAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        return view.TextBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
    }

    public async Task SetTextAsync(string text, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        var textSnapshot = view.TextSnapshot;
        var replacementSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
        view.TextBuffer.Replace(replacementSpan, text);
    }

    public async Task<string> GetTextAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
        var bufferPosition = view.Caret.Position.BufferPosition;
        return bufferPosition.Snapshot.GetText();
    }

    public async Task ReplaceTextAsync(string oldText, string newText, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
        await SelectTextInCurrentDocumentAsync(oldText, cancellationToken);
        var replacementSpan = new SnapshotSpan(view.TextSnapshot, view.Selection.Start.Position, view.Selection.End.Position - view.Selection.Start.Position);
        view.TextBuffer.Replace(replacementSpan, newText);
    }

    public async Task<string> GetCurrentLineTextAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
        var bufferPosition = view.Caret.Position.BufferPosition;
        var line = bufferPosition.GetContainingLine();
        return line.GetText();
    }

    public async Task<string> GetSelectedTextAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
        var subjectBuffer = view.GetBufferContainingCaret();
        Contract.ThrowIfNull(subjectBuffer);

        var selectedSpan = view.Selection.SelectedSpans[0];
        return subjectBuffer.CurrentSnapshot.GetText(selectedSpan);
    }

    public async Task<string> GetLineTextBeforeCaretAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
        var bufferPosition = view.Caret.Position.BufferPosition;
        var line = bufferPosition.GetContainingLine();
        var lineText = line.GetText();
        var lineTextBeforeCaret = lineText[..(bufferPosition.Position - line.Start)];
        return lineTextBeforeCaret;
    }

    public async Task<string> GetLineTextAfterCaretAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
        var bufferPosition = view.Caret.Position.BufferPosition;
        var line = bufferPosition.GetContainingLine();
        var lineText = line.GetText();
        var lineTextAfterCaret = lineText[(bufferPosition.Position - line.Start)..];
        return lineTextAfterCaret;
    }

    public async Task MoveCaretAsync(int position, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        var point = new SnapshotPoint(view.TextSnapshot, position);

        view.Caret.MoveTo(point);
    }

    public async Task SetMultiSelectionAsync(ImmutableArray<TextSpan> positions, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);

        var subjectBuffer = view.GetBufferContainingCaret();
        Assumes.Present(subjectBuffer);

        view.SetMultiSelection(positions.Select(p => new SnapshotSpan(subjectBuffer.CurrentSnapshot, p.Start, p.Length)));
    }

    public async Task SetSelectionAsync(TextSpan span, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);

        var subjectBuffer = view.GetBufferContainingCaret();
        Assumes.Present(subjectBuffer);
        view.SetSelection(new SnapshotSpan(subjectBuffer.CurrentSnapshot, span.Start, span.Length));
    }

    public async Task SelectTextInCurrentDocumentAsync(string text, CancellationToken cancellationToken)
    {
        await this.PlaceCaretAsync(text, charsOffset: -1, occurrence: 0, extendSelection: false, selectBlock: false, cancellationToken);
        await this.PlaceCaretAsync(text, charsOffset: 0, occurrence: 0, extendSelection: true, selectBlock: false, cancellationToken);
    }

    public async Task DeleteTextAsync(string text, CancellationToken cancellationToken)
    {
        await SelectTextInCurrentDocumentAsync(text, cancellationToken);
        await TestServices.Input.SendAsync(VirtualKeyCode.DELETE, cancellationToken);
    }

    public async Task PasteAsync(string text, CancellationToken cancellationToken)
    {
        var provider = await TestServices.Shell.GetComponentModelServiceAsync<IAsynchronousOperationListenerProvider>(cancellationToken);
        var waiter = (IAsynchronousOperationWaiter)provider.GetListener(FeatureAttribute.AddImportsOnPaste);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy], cancellationToken);
        Clipboard.SetText(text);
        await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd97CmdID.Paste, cancellationToken);

        await waiter.ExpeditedWaitAsync();
    }

    public async Task<ClassificationSpan[]> GetLightBulbPreviewClassificationsAsync(string menuText, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        var broker = await GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);
        var classifierAggregatorService = await GetComponentModelServiceAsync<IViewClassifierAggregatorService>(cancellationToken);

        await LightBulbHelper.WaitForLightBulbSessionAsync(TestServices, broker, view, cancellationToken).ConfigureAwait(true);

        var bufferType = view.TextBuffer.ContentType.DisplayName;
        if (!broker.IsLightBulbSessionActive(view))
        {
            throw new Exception($"No Active Smart Tags in View!  Buffer content type='{bufferType}'");
        }

        var activeSession = broker.GetSession(view);
        if (activeSession == null || !activeSession.IsExpanded)
        {
            throw new InvalidOperationException($"No expanded light bulb session found after View.ShowSmartTag.  Buffer content type='{bufferType}'");
        }

        if (!string.IsNullOrEmpty(menuText))
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (activeSession.TryGetSuggestedActionSets(out var actionSets) != QuerySuggestedActionCompletionStatus.Completed)
            {
                actionSets = [];
            }
#pragma warning restore CS0618 // Type or member is obsolete

            var set = actionSets.SelectMany(s => s.Actions).FirstOrDefault(a => a.DisplayText == menuText);
            if (set == null)
            {
                throw new InvalidOperationException(
                    $"ISuggestionAction '{menuText}' not found.  Buffer content type='{bufferType}'");
            }

            IWpfTextView? preview = null;
            var pane = await set.GetPreviewAsync(CancellationToken.None).ConfigureAwait(true);
            if (pane is UserControl control)
            {
                var container = control.FindName("PreviewDockPanel") as DockPanel;
                var host = container.FindDescendants<UIElement>().OfType<IWpfTextViewHost>().LastOrDefault();
                preview = host?.TextView;
            }

            if (preview == null)
            {
                throw new InvalidOperationException(string.Format("Could not find light bulb preview.  Buffer content type={0}", bufferType));
            }

            activeSession.Collapse();
            var classifier = classifierAggregatorService.GetClassifier(preview);
            var classifiedSpans = classifier.GetClassificationSpans(new SnapshotSpan(preview.TextBuffer.CurrentSnapshot, 0, preview.TextBuffer.CurrentSnapshot.Length));
            return [.. classifiedSpans];
        }

        activeSession.Collapse();
        return [];
    }

    public async Task<string[]> GetCurrentClassificationsAsync(CancellationToken cancellationToken)
    {
        IClassifier? classifier = null;
        try
        {
            var textView = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
            var selectionSpan = textView.Selection.StreamSelectionSpan.SnapshotSpan;
            if (selectionSpan.Length == 0)
            {
                var textStructureNavigatorSelectorService = await TestServices.Shell.GetComponentModelServiceAsync<ITextStructureNavigatorSelectorService>(cancellationToken);
                selectionSpan = textStructureNavigatorSelectorService
                    .GetTextStructureNavigator(textView.TextBuffer)
                    .GetExtentOfWord(selectionSpan.Start).Span;
            }

            var classifierAggregatorService = await TestServices.Shell.GetComponentModelServiceAsync<IViewClassifierAggregatorService>(cancellationToken);
            classifier = classifierAggregatorService.GetClassifier(textView);
            var classifiedSpans = classifier.GetClassificationSpans(selectionSpan);
            return [.. classifiedSpans.Select(x => x.ClassificationType.Classification)];
        }
        finally
        {
            if (classifier is IDisposable classifierDispose)
            {
                classifierDispose.Dispose();
            }
        }
    }

    public async Task<int> GetVisibleColumnCountAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
        return (int)Math.Ceiling(view.ViewportWidth / Math.Max(view.FormattedLineSource.ColumnWidth, 1));
    }

    public async Task ActivateAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        dte.ActiveDocument.Activate();
    }

    public async Task SendExplicitFocusAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var textView = await GetActiveVsTextViewAsync(cancellationToken);
        textView.SendExplicitFocus();
    }

    public async Task<bool> IsUseSuggestionModeOnAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var textView = await GetActiveTextViewAsync(cancellationToken);
        return await IsUseSuggestionModeOnAsync(forDebuggerTextView: IsDebuggerTextView(textView), cancellationToken);
    }

    public async Task<bool> IsUseSuggestionModeOnAsync(bool forDebuggerTextView, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var editorOptionsFactory = await GetComponentModelServiceAsync<IEditorOptionsFactoryService>(cancellationToken);
        var options = editorOptionsFactory.GlobalOptions;

        EditorOptionKey<bool> optionKey;
        bool defaultOption;
        if (forDebuggerTextView)
        {
            optionKey = new EditorOptionKey<bool>(PredefinedCompletionNames.SuggestionModeInDebuggerCompletionOptionName);
            defaultOption = true;
        }
        else
        {
            optionKey = new EditorOptionKey<bool>(PredefinedCompletionNames.SuggestionModeInCompletionOptionName);
            defaultOption = false;
        }

        if (!options.IsOptionDefined(optionKey, localScopeOnly: false))
        {
            return defaultOption;
        }

        return options.GetOptionValue(optionKey);
    }

    public async Task SetUseSuggestionModeAsync(bool value, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var textView = await GetActiveTextViewAsync(cancellationToken);
        await SetUseSuggestionModeAsync(forDebuggerTextView: IsDebuggerTextView(textView), value, cancellationToken);
    }

    public async Task SetUseSuggestionModeAsync(bool forDebuggerTextView, bool value, CancellationToken cancellationToken)
    {
        if (await IsUseSuggestionModeOnAsync(forDebuggerTextView, cancellationToken) != value)
        {
            await UpdateUseSuggestionModeAsync();
            var useSuggestionMode = await IsUseSuggestionModeOnAsync(forDebuggerTextView, cancellationToken);
            if (useSuggestionMode != value)
            {
                throw new InvalidOperationException($"Failed to update suggestion mode to {value} (current: {useSuggestionMode})");
            }
        }

        async Task UpdateUseSuggestionModeAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var editorOptionsFactory = await GetComponentModelServiceAsync<IEditorOptionsFactoryService>(cancellationToken);
            var options = editorOptionsFactory.GlobalOptions;

            EditorOptionKey<bool> optionKey;
            if (forDebuggerTextView)
            {
                optionKey = new EditorOptionKey<bool>(PredefinedCompletionNames.SuggestionModeInDebuggerCompletionOptionName);
            }
            else
            {
                optionKey = new EditorOptionKey<bool>(PredefinedCompletionNames.SuggestionModeInCompletionOptionName);
            }

            options.SetOptionValue<bool>(optionKey, value);
        }
    }

    public async Task<ImmutableArray<TagSpan<ITextMarkerTag>>> GetRenameTagsAsync(CancellationToken cancellationToken)
    {
        await TestServices.Workspace.WaitForRenameAsync(cancellationToken);
        var tags = await GetTagsAsync<ITextMarkerTag>(cancellationToken);
        return tags.WhereAsArray(tag => tag.Tag.Type == RenameFieldBackgroundAndBorderTag.TagId);
    }

    public async Task<ImmutableArray<TagSpan<TTag>>> GetTagsAsync<TTag>(CancellationToken cancellationToken)
        where TTag : ITag
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var view = await GetActiveTextViewAsync(cancellationToken);
        var viewTagAggregatorFactory = await GetComponentModelServiceAsync<IViewTagAggregatorFactoryService>(cancellationToken);

        var aggregator = viewTagAggregatorFactory.CreateTagAggregator<TTag>(view);
        var tags = aggregator.GetTags(new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length));

        return tags.SelectAsArray(tag => new TagSpan<TTag>(tag.Span.GetSpans(view.TextBuffer).Single(), tag.Tag));
    }

    private static bool IsDebuggerTextView(ITextView textView)
        => textView.Roles.Contains("DEBUGVIEW");

    public async Task<ImmutableArray<string>> GetF1KeywordsAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var vsView = await GetActiveVsTextViewAsync(cancellationToken);
        ErrorHandler.ThrowOnFailure(vsView.GetBuffer(out var textLines));
        ErrorHandler.ThrowOnFailure(textLines.GetLanguageServiceID(out var languageServiceGuid));

        var comServiceProvider = await GetRequiredGlobalServiceAsync<SAsyncServiceProvider, COMAsyncServiceProvider.IAsyncServiceProvider>(cancellationToken);
        var languageService = await new AsyncServiceProvider(comServiceProvider).QueryServiceAsync(languageServiceGuid).WithCancellation(cancellationToken);
        Assumes.Present(languageService);

        var languageContextProvider = (IVsLanguageContextProvider)languageService;
        var monitorUserContext = await GetRequiredGlobalServiceAsync<SVsMonitorUserContext, IVsMonitorUserContext>(cancellationToken);
        ErrorHandler.ThrowOnFailure(monitorUserContext.CreateEmptyContext(out var emptyUserContext));
        ErrorHandler.ThrowOnFailure(vsView.GetCaretPos(out var line, out var column));

        var span = new TextManager.Interop.TextSpan()
        {
            iStartLine = line,
            iStartIndex = column,
            iEndLine = line,
            iEndIndex = column,
        };

        ErrorHandler.ThrowOnFailure(languageContextProvider.UpdateLanguageContext(dwHint: 0, textLines, [span], emptyUserContext));
        ErrorHandler.ThrowOnFailure(emptyUserContext.CountAttributes("keyword", fIncludeChildren: Convert.ToInt32(true), out var count));
        var results = ImmutableArray.CreateBuilder<string>(count);
        for (var i = 0; i < count; i++)
        {
            emptyUserContext.GetAttribute(i, "keyword", fIncludeChildren: Convert.ToInt32(true), pbstrName: out _, out var value);
            results.Add(value);
        }

        return results.MoveToImmutable();
    }

    #region Navigation bars

    public async Task ExpandNavigationBarAsync(NavigationBarDropdownKind index, CancellationToken cancellationToken)
    {
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.NavigationBar, cancellationToken);

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        var combobox = (await GetNavigationBarComboBoxesAsync(view, cancellationToken))[(int)index];
        FocusManager.SetFocusedElement(FocusManager.GetFocusScope(combobox), combobox);
        combobox.IsDropDownOpen = true;
    }

    public async Task<ImmutableArray<string>> GetNavigationBarItemsAsync(NavigationBarDropdownKind index, CancellationToken cancellationToken)
    {
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.NavigationBar, cancellationToken);

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        var combobox = (await GetNavigationBarComboBoxesAsync(view, cancellationToken))[(int)index];
        return combobox.Items.OfType<object>().SelectAsArray(i => $"{i}");
    }

    public async Task<string?> GetNavigationBarSelectionAsync(NavigationBarDropdownKind index, CancellationToken cancellationToken)
    {
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.NavigationBar, cancellationToken);

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        var combobox = (await GetNavigationBarComboBoxesAsync(view, cancellationToken))[(int)index];
        return combobox.SelectedItem?.ToString();
    }

    public async Task SelectNavigationBarItemAsync(NavigationBarDropdownKind index, string item, CancellationToken cancellationToken)
    {
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.NavigationBar, cancellationToken);

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var itemIndex = await GetNavigationBarItemIndexAsync(index, item, cancellationToken);
        if (itemIndex < 0)
        {
            Assert.Contains(item, await GetNavigationBarItemsAsync(index, cancellationToken));
            throw ExceptionUtilities.Unreachable();
        }

        await ExpandNavigationBarAsync(index, cancellationToken);
        await TestServices.Input.SendAsync(VirtualKeyCode.HOME, cancellationToken);
        for (var i = 0; i < itemIndex; i++)
        {
            await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, cancellationToken);
        }

        await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, cancellationToken);

        // Navigation and/or code generation following selection is tracked under FeatureAttribute.NavigationBar
        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.NavigationBar, cancellationToken);
    }

    public async Task<int> GetNavigationBarItemIndexAsync(NavigationBarDropdownKind index, string item, CancellationToken cancellationToken)
    {
        var items = await GetNavigationBarItemsAsync(index, cancellationToken);
        return items.IndexOf(item);
    }

    public async Task<bool> IsNavigationBarEnabledAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        return (await GetNavigationBarMarginAsync(view, cancellationToken)) is not null;
    }

    private async Task<List<Microsoft.VisualStudio.Shell.Controls.ComboBox>> GetNavigationBarComboBoxesAsync(IWpfTextView textView, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var margin = await GetNavigationBarMarginAsync(textView, cancellationToken);
        try
        {
            return margin.GetFieldValue<List<Microsoft.VisualStudio.Shell.Controls.ComboBox>>("_combos");
        }
        catch (FieldAccessException)
        {
            return margin.GetFieldValue<List<Microsoft.VisualStudio.Shell.Controls.ComboBox>>("Combos");
        }
    }

    private async Task<UIElement?> GetNavigationBarMarginAsync(IWpfTextView textView, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var editorAdaptersFactoryService = await GetComponentModelServiceAsync<IVsEditorAdaptersFactoryService>(cancellationToken);
        var viewAdapter = editorAdaptersFactoryService.GetViewAdapter(textView);
        Assumes.Present(viewAdapter);

        // Make sure we have the top pane
        //
        // The docs are wrong. When a secondary view exists, it is the secondary view which is on top. The primary
        // view is only on top when there is no secondary view.
        var codeWindow = TryGetCodeWindow(viewAdapter);
        Assumes.Present(codeWindow);

        if (ErrorHandler.Succeeded(codeWindow.GetSecondaryView(out var secondaryViewAdapter)))
        {
            viewAdapter = secondaryViewAdapter;
        }

        var textViewHost = editorAdaptersFactoryService.GetWpfTextViewHost(viewAdapter);
        Assumes.Present(textViewHost);

        var dropDownMargin = textViewHost.GetTextViewMargin("DropDownMargin");
        if (dropDownMargin != null)
        {
            return ((Decorator)dropDownMargin.VisualElement).Child;
        }

        return null;

        static IVsCodeWindow? TryGetCodeWindow(IVsTextView textView)
        {
            if (textView is not IObjectWithSite objectWithSite)
            {
                return null;
            }

            var riid = typeof(IOleServiceProvider).GUID;
            objectWithSite.GetSite(ref riid, out var ppvSite);
            if (ppvSite == IntPtr.Zero)
            {
                return null;
            }

            IOleServiceProvider? oleServiceProvider = null;
            try
            {
                oleServiceProvider = Marshal.GetObjectForIUnknown(ppvSite) as IOleServiceProvider;
            }
            finally
            {
                Marshal.Release(ppvSite);
            }

            if (oleServiceProvider == null)
            {
                return null;
            }

            var guidService = typeof(SVsWindowFrame).GUID;
            riid = typeof(IVsWindowFrame).GUID;
            if (ErrorHandler.Failed(oleServiceProvider.QueryService(ref guidService, ref riid, out var ppvObject)) || ppvObject == IntPtr.Zero)
            {
                return null;
            }

            IVsWindowFrame? frame = null;
            try
            {
                frame = (IVsWindowFrame)Marshal.GetObjectForIUnknown(ppvObject);
            }
            finally
            {
                Marshal.Release(ppvObject);
            }

            riid = typeof(IVsCodeWindow).GUID;
            if (ErrorHandler.Failed(frame.QueryViewInterface(ref riid, out ppvObject)) || ppvObject == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Marshal.GetObjectForIUnknown(ppvObject) as IVsCodeWindow;
            }
            finally
            {
                Marshal.Release(ppvObject);
            }
        }
    }

    #endregion

    public async Task DismissLightBulbSessionAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);

        var broker = await GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);
        broker.DismissSession(view);
    }

    public async Task DismissCompletionSessionsAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await WaitForCompletionSetAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);

        var broker = await GetComponentModelServiceAsync<ICompletionBroker>(cancellationToken);
        broker.DismissAllSessions(view);
    }

    public async Task<Completion> GetCurrentCompletionItemAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await WaitForCompletionSetAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        var broker = await GetComponentModelServiceAsync<ICompletionBroker>(cancellationToken);
        var sessions = broker.GetSessions(view);
        if (sessions.Count != 1)
        {
            throw new InvalidOperationException($"Expected exactly one session in the completion list, but found {sessions.Count}");
        }

        var selectedCompletionSet = sessions[0].SelectedCompletionSet;
        return selectedCompletionSet.SelectionStatus.Completion;
    }

    public async Task<bool> IsCompletionActiveAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await WaitForCompletionSetAsync(cancellationToken);

        // It's not known why WaitForCompletionSetAsync fails to stabilize calls to IsCompletionActive.
        await Task.Delay(TimeSpan.FromSeconds(1));

        var view = await GetActiveTextViewAsync(cancellationToken);
        if (view is null)
            return false;

        var broker = await TestServices.Shell.GetComponentModelServiceAsync<ICompletionBroker>(cancellationToken);
        return broker.IsCompletionActive(view);
    }

    public async Task InvokeSignatureHelpAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.ParameterInfo, cancellationToken);
        await WaitForSignatureHelpAsync(cancellationToken);
    }

    public async Task<bool> IsSignatureHelpActiveAsync(CancellationToken cancellationToken)
    {
        await WaitForSignatureHelpAsync(cancellationToken);
        var view = await GetActiveTextViewAsync(cancellationToken);
        var broker = await GetComponentModelServiceAsync<ISignatureHelpBroker>(cancellationToken);
        return broker.IsSignatureHelpActive(view);
    }

    public async Task<string[]> GetLightBulbActionsAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);

        var broker = await GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);
        return [.. (await GetLightBulbActionsAsync(broker, view, cancellationToken)).Select(a => a.DisplayText)];
    }

    public async Task<bool> ApplyLightBulbActionAsync(string actionName, FixAllScope? fixAllScope, bool blockUntilComplete, CancellationToken cancellationToken)
    {
        var lightBulbAction = GetLightBulbApplicationAction(actionName, fixAllScope, blockUntilComplete);

        var listenerProvider = await GetComponentModelServiceAsync<IAsynchronousOperationListenerProvider>(cancellationToken);
        var listener = listenerProvider.GetListener(FeatureAttribute.LightBulb);

        var task = JoinableTaskFactory.RunAsync(async () =>
        {
            using var _ = listener.BeginAsyncOperation(nameof(ApplyLightBulbActionAsync));

            await JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);

            var activeTextView = await GetActiveTextViewAsync(cancellationToken);
            return await lightBulbAction(activeTextView, cancellationToken);
        });

        if (blockUntilComplete)
        {
            var result = await task.JoinAsync(cancellationToken);
            await DismissLightBulbSessionAsync(cancellationToken);
            return result;
        }

        return true;
    }

    private Func<IWpfTextView, CancellationToken, Task<bool>> GetLightBulbApplicationAction(string actionName, FixAllScope? fixAllScope, bool willBlockUntilComplete)
    {
        return async (view, cancellationToken) =>
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var broker = await GetComponentModelServiceAsync<ILightBulbBroker>(cancellationToken);

            var actions = (await GetLightBulbActionsAsync(broker, view, cancellationToken)).ToArray();
            var action = actions.FirstOrDefault(a => a.DisplayText == actionName);

            if (action == null)
            {
                var sb = new StringBuilder();
                foreach (var item in actions)
                {
                    sb.AppendLine("Actual ISuggestedAction: " + item.DisplayText);
                }

                var bufferType = view.TextBuffer.ContentType.DisplayName;
                throw new InvalidOperationException(
                    $"ISuggestedAction {actionName} not found.  Buffer content type={bufferType}\r\nActions: {sb}");
            }

            if (fixAllScope != null)
            {
                if (!action.HasActionSets)
                {
                    throw new InvalidOperationException($"Suggested action '{action.DisplayText}' does not support FixAllOccurrences.");
                }

                var actionSetsForAction = await action.GetActionSetsAsync(cancellationToken);
                var fixAllAction = await GetFixAllSuggestedActionAsync(actionSetsForAction!, fixAllScope.Value, cancellationToken);
                if (fixAllAction == null)
                {
                    throw new InvalidOperationException($"Unable to find FixAll in {fixAllScope} code fix for suggested action '{action.DisplayText}'.");
                }

                action = fixAllAction;

                if (willBlockUntilComplete
                    && action is EditorSuggestedActionForRefactorOrFixAll fixAllSuggestedAction)
                {
                    // Ensure the preview changes dialog will not be shown. Since the operation 'willBlockUntilComplete',
                    // the caller would not be able to interact with the preview changes dialog, and the tests would
                    // either timeout or deadlock.
                    fixAllSuggestedAction.CodeAction.GetTestAccessor().ShowPreviewChangesDialog = false;
                }

                if (string.IsNullOrEmpty(actionName))
                {
                    return false;
                }

                // Dismiss the lightbulb session as we not invoking the original code fix.
                broker.DismissSession(view);
            }

            if (action is not EditorSuggestedAction suggestedAction)
                return true;

            broker.DismissSession(view);
            var threadOperationExecutor = await GetComponentModelServiceAsync<IUIThreadOperationExecutor>(cancellationToken);
            var guardedOperations = await GetComponentModelServiceAsync<IGuardedOperations2>(cancellationToken);
            threadOperationExecutor.Execute(
                title: "Execute Suggested Action",
                defaultDescription: Accelerator.StripAccelerators(action.DisplayText, '_'),
                allowCancellation: true,
                showProgress: true,
                action: context =>
                {
                    guardedOperations.CallExtensionPoint(
                        errorSource: suggestedAction,
                        call: () => suggestedAction.Invoke(context),
                        exceptionGuardFilter: e => e is not OperationCanceledException);
                });

            return true;
        };
    }

    private async Task<IEnumerable<ISuggestedAction>> GetLightBulbActionsAsync(ILightBulbBroker broker, IWpfTextView view, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        if (!broker.IsLightBulbSessionActive(view))
        {
            var bufferType = view.TextBuffer.ContentType.DisplayName;
            throw new Exception($"No light bulb session in View!  Buffer content type={bufferType}");
        }

        var activeSession = broker.GetSession(view);
        if (activeSession == null)
        {
            var bufferType = view.TextBuffer.ContentType.DisplayName;
            throw new InvalidOperationException($"No expanded light bulb session found after View.ShowSmartTag.  Buffer content type={bufferType}");
        }

        var actionSets = await LightBulbHelper.WaitForItemsAsync(TestServices, broker, view, cancellationToken);
        return await SelectActionsAsync(actionSets, cancellationToken);
    }

    private async Task<IEnumerable<ISuggestedAction>> SelectActionsAsync(IEnumerable<SuggestedActionSet> actionSets, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var actions = new List<ISuggestedAction>();

        if (actionSets != null)
        {
            foreach (var actionSet in actionSets)
            {
                if (actionSet.Actions != null)
                {
                    foreach (var action in actionSet.Actions)
                    {
                        actions.Add(action);
                        if (action.HasActionSets)
                        {
                            var nestedActionSets = await action.GetActionSetsAsync(cancellationToken);
                            var nestedActions = await SelectActionsAsync(nestedActionSets!, cancellationToken);
                            actions.AddRange(nestedActions);
                        }
                    }
                }
            }
        }

        return actions;
    }

    private async Task<EditorSuggestedActionForRefactorOrFixAll?> GetFixAllSuggestedActionAsync(IEnumerable<SuggestedActionSet> actionSets, FixAllScope fixAllScope, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        foreach (var actionSet in actionSets)
        {
            foreach (var action in actionSet.Actions)
            {
                if (action is EditorSuggestedActionForRefactorOrFixAll fixAllSuggestedAction &&
                    fixAllSuggestedAction.CodeAction.RefactorOrFixAllState.Scope == fixAllScope)
                {
                    return fixAllSuggestedAction;
                }

                if (action.HasActionSets)
                {
                    var nestedActionSets = await action.GetActionSetsAsync(cancellationToken);
                    var fixAllCodeAction = await GetFixAllSuggestedActionAsync(nestedActionSets!, fixAllScope, cancellationToken);
                    if (fixAllCodeAction != null)
                    {
                        return fixAllCodeAction;
                    }
                }
            }
        }

        return null;
    }

    public async Task<int> GetCaretColumnAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        var startOfLine = view.Caret.ContainingTextViewLine.Start.Position;
        var caretVirtualPosition = view.Caret.Position.VirtualBufferPosition;
        return caretVirtualPosition.Position - startOfLine + caretVirtualPosition.VirtualSpaces;
    }

    public async Task<bool> IsCaretOnScreenAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);
        var caret = view.Caret;

        return caret.Left >= view.ViewportLeft
            && caret.Right <= view.ViewportRight
            && caret.Top >= view.ViewportTop
            && caret.Bottom <= view.ViewportBottom;
    }

    public async Task<ISignature> GetCurrentSignatureAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var view = await GetActiveTextViewAsync(cancellationToken);

        await WaitForSignatureHelpAsync(cancellationToken);
        var broker = await GetComponentModelServiceAsync<ISignatureHelpBroker>(cancellationToken);
        var sessions = broker.GetSessions(view);
        if (sessions.Count != 1)
        {
            throw new InvalidOperationException($"Expected exactly one session in the signature help, but found {sessions.Count}");
        }

        return sessions[0].SelectedSignature;
    }

    public async Task GoToDefinitionAsync(CancellationToken cancellationToken)
    {
        await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd97CmdID.GotoDefn, cancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [FeatureAttribute.Workspace, FeatureAttribute.NavigateTo, FeatureAttribute.GoToDefinition],
            cancellationToken);
    }

    public async Task GoToBaseAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await TestServices.Shell.ExecuteCommandAsync(EditorConstants.EditorCommandID.GoToBase, cancellationToken);

        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, cancellationToken);

        await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.GoToBase, cancellationToken);
        await TestServices.Editor.WaitForEditorOperationsAsync(cancellationToken);
    }

    public async Task ConfigureAsyncNavigation(AsyncNavigationKind kind, CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task>? delayHook = kind switch
        {
            AsyncNavigationKind.Default => null,
            AsyncNavigationKind.Synchronous => static cancellationToken => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken),
            AsyncNavigationKind.Asynchronous => static _ => Task.CompletedTask,
            _ => throw ExceptionUtilities.UnexpectedValue(kind),
        };

        var componentModelService = await GetRequiredGlobalServiceAsync<SComponentModel, IComponentModel>(cancellationToken);

        var goToImplementation = componentModelService.DefaultExportProvider.GetExportedValue<GoToImplementationNavigationService>();
        goToImplementation.GetTestAccessor().DelayHook = delayHook;

        var goToBase = componentModelService.DefaultExportProvider.GetExportedValue<GoToBaseNavigationService>();
        goToBase.GetTestAccessor().DelayHook = delayHook;
    }

    public async Task GoToImplementationAsync(CancellationToken cancellationToken)
    {
        await TestServices.Shell.ExecuteCommandAsync(WellKnownCommands.Edit.GoToImplementation, cancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [FeatureAttribute.Workspace, FeatureAttribute.GoToImplementation],
            cancellationToken);
    }

    public async Task<ImmutableArray<(bool Collapsed, TextSpan Span)>> GetOutliningSpansAsync(CancellationToken cancellationToken)
    {
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [FeatureAttribute.Workspace, FeatureAttribute.Outlining],
            cancellationToken);

        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var componentModelService = await GetRequiredGlobalServiceAsync<SComponentModel, IComponentModel>(cancellationToken);
        var view = await GetActiveTextViewAsync(cancellationToken);
        var manager = componentModelService.GetService<IOutliningManagerService>().GetOutliningManager(view);
        var span = new SnapshotSpan(view.TextSnapshot, 0, view.TextSnapshot.Length);
        var regions = manager.GetAllRegions(span);
        return regions
                .OrderBy(s => s.Extent.GetStartPoint(view.TextSnapshot))
                .SelectAsArray(r =>
                {
                    var span = r.Extent.GetSpan(view.TextSnapshot);
                    return (r.IsCollapsed, TextSpan.FromBounds(span.Start.Position, span.End.Position));
                });
    }

    public Task FormatDocumentAsync(CancellationToken cancellationToken)
    {
        return TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd2KCmdID.FORMATDOCUMENT, cancellationToken);
    }

    public Task FormatSelectionAsync(CancellationToken cancellationToken)
    {
        return TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd2KCmdID.FORMATSELECTION, cancellationToken);
    }

    private Task WaitForSignatureHelpAsync(CancellationToken cancellationToken)
        => TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.SignatureHelp, cancellationToken);

    private Task WaitForCompletionSetAsync(CancellationToken cancellationToken)
        => TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.CompletionSet, cancellationToken);

    public async Task AddWinFormButtonAsync(string buttonName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        var designerHost = (IDesignerHost)dte.ActiveWindow.Object;
        var componentChangeService = (IComponentChangeService)designerHost;

        var waitHandle = new AsyncManualResetEvent(false);

        void ComponentAdded(object sender, ComponentEventArgs e)
        {
            var control = (System.Windows.Forms.Control)e.Component;
            if (control.Name == buttonName)
            {
                waitHandle.Set();
            }
        }

        componentChangeService.ComponentAdded += ComponentAdded;

        try
        {
            var mainForm = (System.Windows.Forms.Form)designerHost.RootComponent;
            var newControl = (System.Windows.Forms.Button)designerHost.CreateComponent(typeof(System.Windows.Forms.Button), buttonName);
            newControl.Parent = mainForm;
            await waitHandle.WaitAsync(cancellationToken);
        }
        finally
        {
            componentChangeService.ComponentAdded -= ComponentAdded;
        }
    }

    public async Task DeleteWinFormButtonAsync(string buttonName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        var designerHost = (IDesignerHost)dte.ActiveWindow.Object;
        var componentChangeService = (IComponentChangeService)designerHost;

        var waitHandle = new AsyncManualResetEvent(false);

        void ComponentRemoved(object sender, ComponentEventArgs e)
        {
            var control = (System.Windows.Forms.Control)e.Component;
            if (control.Name == buttonName)
            {
                waitHandle.Set();
            }
        }

        componentChangeService.ComponentRemoved += ComponentRemoved;

        try
        {
            designerHost.DestroyComponent(designerHost.Container.Components[buttonName]);
            await waitHandle.WaitAsync(cancellationToken);
        }
        finally
        {
            componentChangeService.ComponentRemoved -= ComponentRemoved;
        }
    }

    public Task EditWinFormButtonPropertyAsync(string buttonName, string propertyName, string propertyValue, CancellationToken cancellationToken)
        => EditWinFormButtonPropertyAsync(buttonName, propertyName, propertyValue, propertyTypeName: null, cancellationToken);

    public async Task EditWinFormButtonPropertyAsync(string buttonName, string propertyName, string propertyValue, string? propertyTypeName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        var designerHost = (IDesignerHost)dte.ActiveWindow.Object;
        var componentChangeService = (IComponentChangeService)designerHost;

        var waitHandle = new AsyncManualResetEvent(false);

        object GetEnumPropertyValue(string typeName, string value)
        {
            var type = Type.GetType(typeName);
            var converter = new EnumConverter(type);
            return converter.ConvertFromInvariantString(value);
        }

        bool EqualToPropertyValue(object newValue)
        {
            if (propertyTypeName == null)
            {
                return (newValue as string)?.Equals(propertyValue) == true;
            }
            else
            {
                var enumPropertyValue = GetEnumPropertyValue(propertyTypeName, propertyValue);
                return newValue?.Equals(enumPropertyValue) == true;
            }
        }

        void ComponentChanged(object sender, ComponentChangedEventArgs e)
        {
            if (e.Member.Name == propertyName && EqualToPropertyValue(e.NewValue))
            {
                waitHandle.Set();
            }
        }

        componentChangeService.ComponentChanged += ComponentChanged;

        try
        {
            var button = designerHost.Container.Components[buttonName];
            var properties = TypeDescriptor.GetProperties(button);
            var property = properties[propertyName];
            if (propertyTypeName == null)
            {
                property.SetValue(button, propertyValue);
            }
            else
            {
                var enumPropertyValue = GetEnumPropertyValue(propertyTypeName, propertyValue);
                property.SetValue(button, enumPropertyValue);
            }

            await waitHandle.WaitAsync(cancellationToken);
        }
        finally
        {
            componentChangeService.ComponentChanged -= ComponentChanged;
        }
    }

    public async Task EditWinFormButtonEventAsync(string buttonName, string eventName, string eventHandlerName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        var designerHost = (IDesignerHost)dte.ActiveWindow.Object;
        var componentChangeService = (IComponentChangeService)designerHost;

        var waitHandle = new AsyncManualResetEvent(false);

        void ComponentChanged(object sender, ComponentChangedEventArgs e)
        {
            if (e.Member.Name == eventName)
            {
                waitHandle.Set();
            }
        }

        componentChangeService.ComponentChanged += ComponentChanged;

        try
        {
            var button = designerHost.Container.Components[buttonName];
            var eventBindingService = (IEventBindingService)button.Site.GetService(typeof(IEventBindingService));
            var events = TypeDescriptor.GetEvents(button);
            var eventProperty = eventBindingService.GetEventProperty(events.Find(eventName, ignoreCase: true));
            eventProperty.SetValue(button, eventHandlerName);

            await waitHandle.WaitAsync(cancellationToken);
        }
        finally
        {
            componentChangeService.ComponentChanged -= ComponentChanged;
        }
    }

    public async Task<string?> GetWinFormButtonPropertyValueAsync(string buttonName, string propertyName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        var designerHost = (IDesignerHost)dte.ActiveWindow.Object;
        var button = designerHost.Container.Components[buttonName];
        var properties = TypeDescriptor.GetProperties(button);
        return properties[propertyName].GetValue(button) as string;
    }
}
