// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.Editor.InlineRename;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Editor.SmartRename;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename;

internal sealed partial class SmartRenameViewModel : INotifyPropertyChanged, IDisposable
{
#pragma warning disable CS0618 // Editor team use Obsolete attribute to mark potential changing API
    private readonly ISmartRenameSession _smartRenameSession;
#pragma warning restore CS0618

    private readonly IGlobalOptionService _globalOptionService;
    private readonly IThreadingContext _threadingContext;
    private readonly IAsynchronousOperationListener _asyncListener;

    /// <summary>
    /// Cancellation token source for <see cref="ISmartRenameSession.GetSuggestionsAsync(ImmutableDictionary{string,
    /// string[]}, CancellationToken)"/>. Each call uses a new instance. Mutliple calls are allowed only if previous
    /// call failed or was canceled. The request is canceled on UI thread through one of the following user
    /// interactions: 1. <see cref="BaseViewModelPropertyChanged"/> when user types in the text box. 2. <see
    /// cref="ToggleOrTriggerSuggestions"/> when user toggles the automatic suggestions. 3. <see cref="Dispose"/> when
    /// the dialog is closed.
    /// </summary>
    private CancellationTokenSource _cancellationTokenSource = new();
    private bool _isDisposed;
    private TimeSpan _semanticContextDelay;
    private bool _semanticContextError;
    private bool _semanticContextUsed;

    public event PropertyChangedEventHandler? PropertyChanged;

    public RenameFlyoutViewModel BaseViewModel { get; }

    public ObservableCollection<string> SuggestedNames { get; } = [];

    public bool IsAvailable => _smartRenameSession.IsAvailable;

    public bool HasSuggestions => _smartRenameSession.HasSuggestions;

    /// <summary>
    /// Indicates whether a request to get suggestions is in progress. The request to get suggestions is comprised of
    /// initial short delay, see AutomaticFetchDelay and call to <see
    /// cref="ISmartRenameSession.GetSuggestionsAsync(ImmutableDictionary{string, string[]}, CancellationToken)"/>. When
    /// <c>true</c>, the UI shows the progress bar, and prevents <see cref="FetchSuggestions(bool)"/> from making
    /// parallel request.
    /// </summary>
    public bool IsInProgress
    {
        get
        {
            _threadingContext.ThrowIfNotOnUIThread();
            return field;
        }
        set
        {
            _threadingContext.ThrowIfNotOnUIThread();
            field = value;
            NotifyPropertyChanged(nameof(IsInProgress));
        }
    } = false;

    public string StatusMessage => _smartRenameSession.StatusMessage;

    public bool StatusMessageVisibility => _smartRenameSession.StatusMessageVisibility;

    /// <summary>
    /// Determines whether smart rename is in automatic mode (if <c>true</c>) or explicit mode (if <c>false</c>).
    /// The mode is assigned based on feature flag / options.
    /// </summary>
    public bool SupportsAutomaticSuggestions { get; }

    /// <summary>
    /// When smart rename is in automatic mode and <see cref="SupportsAutomaticSuggestions"/> is set,
    /// developer gets to control whether the requests are made automatically on initialization.
    /// Developer can toggle this option using the keyboard shortcut or button click,
    /// both of which are handled in <see cref="ToggleOrTriggerSuggestions"/>."/>
    /// </summary>
    public bool IsAutomaticSuggestionsEnabled { get; private set; }

    /// <summary>
    /// Determines whether smart rename gets semantic context to augment the request for suggested names.
    /// </summary>
    public bool IsUsingSemanticContext { get; }

    /// <summary>
    /// The last selected name when user click one of the suggestions. <see langword="null"/> if user hasn't clicked any suggestions.
    /// </summary>
    public string? SelectedSuggestedName
    {
        get;
        set
        {
            if (field != value)
            {
                _threadingContext.ThrowIfNotOnUIThread();
                field = value;
                BaseViewModel.IdentifierText = value ?? string.Empty;
            }
        }
    }

    /// <summary>
    /// IsSuggestionsPanelExpanded is used to control the visibility of the suggestions panel.
    /// SupportsAutomaticSuggestions the flag to determine whether the SmartRename will generate suggestion automatically.
    /// When SupportsAutomaticSuggestions disenabled, the suggestions panel is supposed to always expanded once it's shown, thus users can see the suggestions.
    /// When SupportsAutomaticSuggestions enabled, the suggestions panel is supposed to react to the smart rename button click. If the button is clicked, IsAutomaticSuggestionsEnabled will be true, the panel will be expanded, Otherwise, it will be collapsed.
    /// </summary>
    public bool IsSuggestionsPanelExpanded => HasSuggestions && (!SupportsAutomaticSuggestions || IsAutomaticSuggestionsEnabled);

    public string GetSuggestionsTooltip
        => SupportsAutomaticSuggestions
            ? EditorFeaturesWpfResources.Toggle_AI_suggestions
            : EditorFeaturesWpfResources.Get_AI_suggestions;

    public string SubmitTextOverride
        => SupportsAutomaticSuggestions
            ? EditorFeaturesWpfResources.Enter_to_rename_shift_enter_to_preview
            : EditorFeaturesWpfResources.Enter_to_rename_shift_enter_to_preview_ctrl_space_for_ai_suggestion;

    public static string GeneratingSuggestionsLabel => EditorFeaturesWpfResources.Generating_suggestions;

    public SmartRenameViewModel(
        IGlobalOptionService globalOptionService,
        IThreadingContext threadingContext,
        IAsynchronousOperationListenerProvider listenerProvider,
#pragma warning disable CS0618 // Editor team use Obsolete attribute to mark potential changing API
        ISmartRenameSession smartRenameSession,
#pragma warning restore CS0618,
        RenameFlyoutViewModel baseViewModel)
    {
        _globalOptionService = globalOptionService;
        _threadingContext = threadingContext;
        _asyncListener = listenerProvider.GetListener(FeatureAttribute.SmartRename);
        _smartRenameSession = smartRenameSession;
        _smartRenameSession.PropertyChanged += SessionPropertyChanged;

        BaseViewModel = baseViewModel;
        BaseViewModel.PropertyChanged += BaseViewModelPropertyChanged;
        BaseViewModel.IdentifierText = baseViewModel.IdentifierText;

        SetupTelemetry();

        this.SupportsAutomaticSuggestions = _globalOptionService.GetOption(InlineRenameUIOptionsStorage.GetSuggestionsAutomatically);
        this.IsUsingSemanticContext = _globalOptionService.GetOption(InlineRenameUIOptionsStorage.GetSuggestionsContext);
        // Use existing "CollapseSuggestionsPanel" option (true if user does not wish to get suggestions automatically) to honor user's choice.
        this.IsAutomaticSuggestionsEnabled = this.SupportsAutomaticSuggestions && !_globalOptionService.GetOption(InlineRenameUIOptionsStorage.CollapseSuggestionsPanel);
        if (this.IsAutomaticSuggestionsEnabled)
        {
            this.FetchSuggestions(isAutomaticOnInitialization: true);
        }
    }

    private void FetchSuggestions(bool isAutomaticOnInitialization)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        if (this.SuggestedNames.Count > 0 || _isDisposed || this.IsInProgress)
        {
            // Don't get suggestions again
            return;
        }

        var listenerToken = _asyncListener.BeginAsyncOperation(nameof(_smartRenameSession.GetSuggestionsAsync));
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        GetSuggestionsTaskAsync(isAutomaticOnInitialization, _cancellationTokenSource.Token).CompletesAsyncOperation(listenerToken);
    }

    /// <summary>
    /// The request for rename suggestions. It's made of three parts:
    /// 1. Short delay of duration AutomaticFetchDelay.
    /// 2. Get definition and references if <see cref="IsUsingSemanticContext"/> is set.
    /// 3. Call to <see cref="ISmartRenameSession.GetSuggestionsAsync(ImmutableDictionary{string, string[]}, CancellationToken)"/>.
    /// </summary>
    private async Task GetSuggestionsTaskAsync(bool isAutomaticOnInitialization, CancellationToken cancellationToken)
    {
        RoslynDebug.Assert(!this.IsInProgress);
        this.IsInProgress = true;
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        try
        {
            if (isAutomaticOnInitialization)
            {
                await Task.Delay(_smartRenameSession.AutomaticFetchDelay, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (cancellationToken.IsCancellationRequested || _isDisposed)
            {
                return;
            }

            if (IsUsingSemanticContext)
            {
                var stopwatch = SharedStopwatch.StartNew();
                _semanticContextUsed = true;
                var document = this.BaseViewModel.Session.TriggerDocument;
                var smartRenameContext = ImmutableDictionary<string, string[]>.Empty;
                try
                {
                    var editorRenameService = document.GetRequiredLanguageService<IEditorInlineRenameService>();
                    var renameLocations = await this.BaseViewModel.Session.AllRenameLocationsTask.JoinAsync(cancellationToken)
                        .ConfigureAwait(false);
                    var context = await editorRenameService.GetRenameContextAsync(this.BaseViewModel.Session.RenameInfo, renameLocations, cancellationToken)
                        .ConfigureAwait(false);
                    smartRenameContext = ImmutableDictionary.CreateRange(
                        context.Select(n => KeyValuePair.Create(n.Key, n.Value.SelectMany(t => new[] { t.filePath, t.content }).ToArray())));
                    _semanticContextDelay = stopwatch.Elapsed;
                }
                catch (Exception e) when (FatalError.ReportAndCatch(e, ErrorSeverity.Diagnostic))
                {
                    _semanticContextError = true;
                    // use empty smartRenameContext
                }

                _ = await _smartRenameSession.GetSuggestionsAsync(smartRenameContext, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _ = await _smartRenameSession.GetSuggestionsAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            // cancellationToken might be already canceled. Fallback to the disposal token.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(_threadingContext.DisposalToken);
            this.IsInProgress = false;
        }
    }

    private void SessionPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var listenerToken = _asyncListener.BeginAsyncOperation(nameof(SessionPropertyChanged));
        var sessionPropertyChangedTask = SessionPropertyChangedAsync(sender, e).CompletesAsyncOperation(listenerToken);
    }

    private async Task SessionPropertyChangedAsync(object sender, PropertyChangedEventArgs e)
    {
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

        // _smartRenameSession.SuggestedNames is a normal list. We need to convert it to ObservableCollection to bind to UI Element.
        if (e.PropertyName == nameof(_smartRenameSession.SuggestedNames))
        {
            var textInputBackup = BaseViewModel.IdentifierText;

            SuggestedNames.Clear();
            // Set limit of 3 results
            foreach (var name in _smartRenameSession.SuggestedNames.Take(3))
            {
                SuggestedNames.Add(name);
            }

            // Changing the list may have changed the text in the text box. We need to restore it.
            BaseViewModel.IdentifierText = textInputBackup;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSuggestionsPanelExpanded)));
            return;
        }

        // For the rest of the property, like HasSuggestions, IsAvailable and etc. Just forward it has changed to subscriber
        PropertyChanged?.Invoke(this, e);
    }

    public string? ScrollSuggestions(string currentIdentifier, bool down)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        if (!HasSuggestions)
        {
            return null;
        }

        // ↑ and ↓ would navigate via the Suggested list.
        // The previous element of first element is the last one. And the next element of the last element is the first one.
        var currentIndex = SuggestedNames.IndexOf(currentIdentifier);
        currentIndex += down ? 1 : -1;
        var count = this.SuggestedNames.Count;
        currentIndex = (currentIndex + count) % count;
        return SuggestedNames[currentIndex];
    }

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
        // It's needed by editor-side telemetry.
        _smartRenameSession.OnCancel();
        PostTelemetry(isCommit: false);
    }

    public void Commit(string finalIdentifierName)
    {
        // It's needed by editor-side telemetry.
        _smartRenameSession.OnSuccess(finalIdentifierName);
        PostTelemetry(isCommit: true);
    }

    public void Dispose()
    {
        _threadingContext.ThrowIfNotOnUIThread();
        _isDisposed = true;
        _smartRenameSession.PropertyChanged -= SessionPropertyChanged;
        BaseViewModel.PropertyChanged -= BaseViewModelPropertyChanged;
        _smartRenameSession.Dispose();
        _cancellationTokenSource.Cancel();
    }

    /// <summary>
    /// When smart rename operates in explicit mode, this method gets the suggestions.
    /// When smart rename operates in automatic mode, this method toggles the automatic suggestions:
    /// gets the suggestions if it was just enabled, and cancels the ongoing request if it was just disabled.
    /// </summary>
    public void ToggleOrTriggerSuggestions()
    {
        _threadingContext.ThrowIfNotOnUIThread();
        if (this.SupportsAutomaticSuggestions)
        {
            this.IsAutomaticSuggestionsEnabled = !this.IsAutomaticSuggestionsEnabled;
            if (this.IsAutomaticSuggestionsEnabled)
            {
                this.FetchSuggestions(isAutomaticOnInitialization: false);
            }
            else
            {
                _cancellationTokenSource.Cancel();
            }
            NotifyPropertyChanged(nameof(IsSuggestionsPanelExpanded));
            NotifyPropertyChanged(nameof(IsAutomaticSuggestionsEnabled));
            // Use existing "CollapseSuggestionsPanel" option (true if user does not wish to get suggestions automatically) to honor user's choice.
            _globalOptionService.SetGlobalOption(InlineRenameUIOptionsStorage.CollapseSuggestionsPanel, !IsAutomaticSuggestionsEnabled);
        }
        else
        {
            this.FetchSuggestions(isAutomaticOnInitialization: false);
        }
    }

    private void NotifyPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void BaseViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        if (e.PropertyName == nameof(BaseViewModel.IdentifierText))
        {
            // User is typing the new identifier name, cancel the ongoing request to get suggestions.
            _cancellationTokenSource.Cancel();
        }
    }
}
