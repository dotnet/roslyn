// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Razor.Extensions;
using Microsoft.VisualStudio.Razor.LanguageClient;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using IServiceProvider = System.IServiceProvider;

namespace Microsoft.VisualStudio.Razor;

// The entire purpose of this class is to workaround quirks in Visual Studio's core editor handling. In Razor scenarios
// we can have a multitude of content types that represents a Razor file:
//
// ** Content Type Mappings **
// RazorCSharp = .NET Framework Razor editor
// RazorCoreCSharp = .NET Core Legacy Razor editor
// Razor = .NET Core Razor editor (LSP / new)
//
// Because we have these content types that are applied based on what project the user is operating in we have to workaround
// quirks on the core editor side to ensure that language services for our "Razor" content type properly get applied. For
// instance we need to set a language service ID, we need to update options and we need to hookup data tip filters for
// debugging. Typically all of this would be handled for us but due to bugs on the platform front we need to manually do this.
// That is what this classes purpose is.
[Export(typeof(ITextViewConnectionListener))]
[TextViewRole(PredefinedTextViewRoles.Document)]
[ContentType(RazorConstants.RazorLSPContentTypeName)]
[method: ImportingConstructor]
internal sealed partial class RazorLSPTextViewConnectionListener(
    [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
    IVsEditorAdaptersFactoryService editorAdaptersFactory,
    ILspEditorFeatureDetector editorFeatureDetector,
    IEditorOptionsFactoryService editorOptionsFactory,
    IClientSettingsManager editorSettingsManager,
    JoinableTaskContext joinableTaskContext,
    [ImportMany] IEnumerable<IInterceptedCommand> interceptedCommands) : ITextViewConnectionListener
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory = editorAdaptersFactory;
    private readonly ILspEditorFeatureDetector _editorFeatureDetector = editorFeatureDetector;
    private readonly IEditorOptionsFactoryService _editorOptionsFactory = editorOptionsFactory;
    private readonly IClientSettingsManager _editorSettingsManager = editorSettingsManager;
    private readonly JoinableTaskContext _joinableTaskContext = joinableTaskContext;
    private readonly ImmutableArray<IInterceptedCommand> _interceptedCommands = [.. interceptedCommands];
    private IVsTextManager4? _textManager;

    /// <summary>
    /// Protects concurrent modifications to _activeTextViews and _textBuffer's
    /// property bag.
    /// </summary>
    private readonly object _lock = new();

    #region protected by _lock
    private readonly List<ITextView> _activeTextViews = [];

    private ITextBuffer? _textBuffer;
    #endregion

    /// <summary>
    /// Gets instance of <see cref="IVsTextManager4"/>. This accesses COM object and requires to be called on the UI thread.
    /// </summary>
    private IVsTextManager4 TextManager
    {
        get
        {
            _joinableTaskContext.AssertUIThread();
            return _textManager ??= (IVsTextManager4)_serviceProvider.GetService(typeof(SVsTextManager));
        }
    }

    public void SubjectBuffersConnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
    {
        if (textView is null)
        {
            throw new ArgumentNullException(nameof(textView));
        }

        var vsTextView = _editorAdaptersFactory.GetViewAdapter(textView);

        Assumes.NotNull(vsTextView);

        // In remote client scenarios there's a custom language service applied to buffers in order to enable delegation of interactions.
        // Because of this we don't want to break that experience so we ensure not to "set" a language service for remote clients.
        if (!_editorFeatureDetector.IsRemoteClient())
        {
            vsTextView.GetBuffer(out var vsBuffer);
            vsBuffer.SetLanguageServiceID(RazorConstants.RazorLanguageServiceGuid);
        }

        RazorLSPTextViewFilter.CreateAndRegister(vsTextView, textView, _joinableTaskContext.Factory, _interceptedCommands);

        if (!textView.TextBuffer.IsRazorLSPBuffer())
        {
            return;
        }

        lock (_lock)
        {
            _activeTextViews.Add(textView);

            if (!textView.TextBuffer.Properties.ContainsProperty(RazorLSPConstants.WebToolsWrapWithTagServerNameProperty))
            {
                // We have to tell web tools which language server to send requests to for this buffer, but that changes
                // if cohosting is enabled.
                textView.TextBuffer.Properties[RazorLSPConstants.WebToolsWrapWithTagServerNameProperty] = RazorLSPConstants.RoslynLanguageServerName;
            }

            // Initialize the user's options and start listening for changes.
            // We only want to attach the option changed event once so we don't receive multiple
            // notifications if there is more than one TextView active.
            if (!textView.TextBuffer.Properties.ContainsProperty(typeof(RazorEditorOptionsTracker)))
            {
                // We assume there is ever only one TextBuffer at a time and thus all active
                // TextViews have the same TextBuffer.
                _textBuffer = textView.TextBuffer;

                var bufferOptions = _editorOptionsFactory.GetOptions(_textBuffer);
                var viewOptions = _editorOptionsFactory.GetOptions(textView);

                Assumes.Present(bufferOptions);
                Assumes.Present(viewOptions);

                // All TextViews share the same options, so we only need to listen to changes for one.
                // We need to keep track of and update both the TextView and TextBuffer options. Updating
                // the TextView's options is necessary so 'SPC'/'TABS' in the bottom right corner of the
                // view displays the right setting. Updating the TextBuffer is necessary since it's where
                // LSP pulls settings from when sending us requests.
                var optionsTracker = new RazorEditorOptionsTracker(TrackedView: textView, viewOptions, bufferOptions);
                _textBuffer.Properties[typeof(RazorEditorOptionsTracker)] = optionsTracker;

                // Initialize TextView options. We only need to do this once per TextView, as the options should
                // automatically update and they aren't options we care about keeping track of.
                Assumes.Present(TextManager);
                InitializeRazorTextViewOptions(TextManager, optionsTracker);

                // A change in Tools->Options settings only kicks off an options changed event in the view
                // and not the buffer, i.e. even if we listened for TextBuffer option changes, we would never
                // be notified. As a workaround, we listen purely for TextView changes, and update the
                // TextBuffer options in the TextView listener as well.
                RazorOptions_OptionChanged(null, null);
                viewOptions.OptionChanged += RazorOptions_OptionChanged;
            }
        }
    }

    public void SubjectBuffersDisconnected(ITextView textView, ConnectionReason reason, IReadOnlyCollection<ITextBuffer> subjectBuffers)
    {
        // When the TextView goes away so does the filter.  No need to do anything more.
        // However, we do need to detach from listening for option changes to avoid leaking.
        // We should switch to listening to a different TextView if the one we're listening
        // to is disconnected.
        Assumes.NotNull(_textBuffer);

        if (!textView.TextBuffer.IsRazorLSPBuffer())
        {
            return;
        }

        lock (_lock)
        {
            _activeTextViews.Remove(textView);

            // Is the tracked TextView where we listen for option changes the one being disconnected?
            // If so, see if another view is available.
            if (_textBuffer.Properties.TryGetProperty(
                typeof(RazorEditorOptionsTracker), out RazorEditorOptionsTracker optionsTracker) &&
                optionsTracker.TrackedView == textView)
            {
                _textBuffer.Properties.RemoveProperty(typeof(RazorEditorOptionsTracker));
                optionsTracker.ViewOptions.OptionChanged -= RazorOptions_OptionChanged;

                // If there's another text view we can use to listen for options, start tracking it.
                if (_activeTextViews.Count != 0)
                {
                    var newTrackedView = _activeTextViews[0];
                    var newViewOptions = _editorOptionsFactory.GetOptions(newTrackedView);
                    Assumes.Present(newViewOptions);

                    // We assume the TextViews all have the same TextBuffer, so we can reuse the
                    // buffer options from the old TextView.
                    var newOptionsTracker = new RazorEditorOptionsTracker(
                        newTrackedView, newViewOptions, optionsTracker.BufferOptions);
                    _textBuffer.Properties[typeof(RazorEditorOptionsTracker)] = newOptionsTracker;

                    newViewOptions.OptionChanged += RazorOptions_OptionChanged;
                }
            }
        }
    }

    private void RazorOptions_OptionChanged(object? sender, EditorOptionChangedEventArgs? e)
    {
        Assumes.NotNull(_textBuffer);

        if (!_textBuffer.Properties.TryGetProperty(typeof(RazorEditorOptionsTracker), out RazorEditorOptionsTracker optionsTracker))
        {
            return;
        }

        // Retrieve current space/tabs settings from from Tools->Options and update options in
        // the actual editor.
        (ClientSpaceSettings ClientSpaceSettings, ClientCompletionSettings ClientCompletionSettings) settings = UpdateRazorEditorOptions(TextManager, optionsTracker);

        // Keep track of accurate settings on the client side so we can easily retrieve the
        // options later when the server sends us a workspace/configuration request.
        _editorSettingsManager.Update(settings.ClientSpaceSettings);
        _editorSettingsManager.Update(settings.ClientCompletionSettings);
    }

    private static void InitializeRazorTextViewOptions(IVsTextManager4 textManager, RazorEditorOptionsTracker optionsTracker)
    {
        var langPrefs3 = new LANGPREFERENCES3[] { new LANGPREFERENCES3() { guidLang = RazorConstants.RazorLanguageServiceGuid } };
        if (VSConstants.S_OK != textManager.GetUserPreferences4(null, langPrefs3, null))
        {
            return;
        }

        // General options
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewOptions.UseVirtualSpaceName, Convert.ToBoolean(langPrefs3[0].fVirtualSpace));

        var wordWrapStyle = WordWrapStyles.None;
        if (Convert.ToBoolean(langPrefs3[0].fWordWrap))
        {
            wordWrapStyle |= WordWrapStyles.WordWrap | WordWrapStyles.AutoIndent;
            if (Convert.ToBoolean(langPrefs3[0].fWordWrapGlyphs))
            {
                wordWrapStyle |= WordWrapStyles.VisibleGlyphs;
            }
        }

        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewOptions.WordWrapStyleName, wordWrapStyle);
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginName, Convert.ToBoolean(langPrefs3[0].fLineNumbers));
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewOptions.DisplayUrlsAsHyperlinksName, Convert.ToBoolean(langPrefs3[0].fHotURLs));
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewOptions.BraceCompletionEnabledOptionName, Convert.ToBoolean(langPrefs3[0].fBraceCompletion));
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionName, Convert.ToBoolean(langPrefs3[0].fCutCopyBlanks));

        // Completion options
        optionsTracker.ViewOptions.SetOptionValue(DefaultLanguageOptions.ShowCompletionOnTypeCharName, Convert.ToBoolean(langPrefs3[0].fAutoListMembers));

        // Scroll bar options
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewHostOptions.HorizontalScrollBarName, Convert.ToBoolean(langPrefs3[0].fShowHorizontalScrollBar));
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewHostOptions.VerticalScrollBarName, Convert.ToBoolean(langPrefs3[0].fShowVerticalScrollBar));
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewHostOptions.ShowScrollBarAnnotationsOptionName, Convert.ToBoolean(langPrefs3[0].fShowAnnotations));
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewHostOptions.ShowChangeTrackingMarginOptionName, Convert.ToBoolean(langPrefs3[0].fShowChanges));
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewHostOptions.ShowMarksOptionName, Convert.ToBoolean(langPrefs3[0].fShowMarks));
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewHostOptions.ShowErrorsOptionName, Convert.ToBoolean(langPrefs3[0].fShowErrors));
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewHostOptions.ShowCaretPositionOptionName, Convert.ToBoolean(langPrefs3[0].fShowCaretPosition));
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewHostOptions.ShowEnhancedScrollBarOptionName, Convert.ToBoolean(langPrefs3[0].fUseMapMode));
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewHostOptions.ShowPreviewOptionName, Convert.ToBoolean(langPrefs3[0].fShowPreview));
        optionsTracker.ViewOptions.SetOptionValue(DefaultTextViewHostOptions.PreviewSizeOptionName, (int)langPrefs3[0].uOverviewWidth);
    }

    private static (ClientSpaceSettings, ClientCompletionSettings) UpdateRazorEditorOptions(IVsTextManager4 textManager, RazorEditorOptionsTracker optionsTracker)
    {
        var insertSpaces = true;
        var tabSize = 4;

        var langPrefs3 = new LANGPREFERENCES3[] { new LANGPREFERENCES3() { guidLang = RazorConstants.RazorLanguageServiceGuid } };
        if (VSConstants.S_OK != textManager.GetUserPreferences4(null, langPrefs3, null))
        {
            return (new ClientSpaceSettings(IndentWithTabs: !insertSpaces, tabSize), ClientCompletionSettings.Default);
        }

        // Tabs options
        insertSpaces = !Convert.ToBoolean(langPrefs3[0].fInsertTabs);
        tabSize = (int)langPrefs3[0].uTabSize;

        // Completion options
        var autoShowCompletion = Convert.ToBoolean(langPrefs3[0].fAutoListMembers);
        var autoListParams = Convert.ToBoolean(langPrefs3[0].fAutoListParams);

        optionsTracker.ViewOptions.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, insertSpaces);
        optionsTracker.ViewOptions.SetOptionValue(DefaultOptions.TabSizeOptionId, tabSize);

        // We need to update both the TextView and TextBuffer options for tabs/spaces settings. Updating the TextView
        // is necessary so 'SPC'/'TABS' in the bottom right corner of the view displays the right setting. Updating the
        // TextBuffer is necessary since it's where LSP pulls settings from when sending us requests.
        optionsTracker.BufferOptions.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, insertSpaces);
        optionsTracker.BufferOptions.SetOptionValue(DefaultOptions.TabSizeOptionId, tabSize);

        return (new ClientSpaceSettings(IndentWithTabs: !insertSpaces, tabSize), new ClientCompletionSettings(autoShowCompletion, autoListParams));
    }

    private record RazorEditorOptionsTracker(ITextView TrackedView, IEditorOptions ViewOptions, IEditorOptions BufferOptions);
}
