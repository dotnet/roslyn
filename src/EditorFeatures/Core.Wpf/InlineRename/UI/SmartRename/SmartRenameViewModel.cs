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
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.Editor.InlineRename;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.EditorFeatures.Lightup;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename;

internal sealed partial class SmartRenameViewModel : INotifyPropertyChanged, IDisposable
{
#pragma warning disable CS0618 // Editor team use Obsolete attribute to mark potential changing API
    private readonly ISmartRenameSessionWrapper _smartRenameSession;
#pragma warning restore CS0618

    private readonly IGlobalOptionService _globalOptionService;
    private readonly IThreadingContext _threadingContext;
    private readonly IAsynchronousOperationListenerProvider _listenerProvider;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isDisposed;
    private TimeSpan AutomaticFetchDelay => _smartRenameSession.AutomaticFetchDelay;
    private Task _getSuggestionsTask = Task.CompletedTask;

    public event PropertyChangedEventHandler? PropertyChanged;

    public RenameFlyoutViewModel BaseViewModel { get; }

    public ObservableCollection<string> SuggestedNames { get; } = [];

    public bool IsAvailable => _smartRenameSession.IsAvailable;

    public bool HasSuggestions => _smartRenameSession.HasSuggestions;

    public bool IsInProgress => _smartRenameSession.IsInProgress;

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
    public bool IsUsingContext { get; }

    private string? _selectedSuggestedName;

    /// <summary>
    /// The last selected name when user click one of the suggestions. <see langword="null"/> if user hasn't clicked any suggestions.
    /// </summary>
    public string? SelectedSuggestedName
    {
        get => _selectedSuggestedName;
        set
        {
            if (_selectedSuggestedName != value)
            {
                _threadingContext.ThrowIfNotOnUIThread();
                _selectedSuggestedName = value;
                BaseViewModel.IdentifierText = value ?? string.Empty;
            }
        }
    }

    public bool IsSuggestionsPanelExpanded => HasSuggestions;

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
        ISmartRenameSessionWrapper smartRenameSession,
#pragma warning restore CS0618,
        RenameFlyoutViewModel baseViewModel)
    {
        _globalOptionService = globalOptionService;
        _threadingContext = threadingContext;
        _listenerProvider = listenerProvider;
        _smartRenameSession = smartRenameSession;
        _smartRenameSession.PropertyChanged += SessionPropertyChanged;

        BaseViewModel = baseViewModel;
        BaseViewModel.PropertyChanged += BaseViewModelPropertyChanged;
        BaseViewModel.IdentifierText = baseViewModel.IdentifierText;

        SetupTelemetry();

        this.SupportsAutomaticSuggestions = _globalOptionService.GetOption(InlineRenameUIOptionsStorage.GetSuggestionsAutomatically);
        this.IsUsingContext = _globalOptionService.GetOption(InlineRenameUIOptionsStorage.GetSuggestionsContext);
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
        if (this.SuggestedNames.Count > 0 || _isDisposed)
        {
            // Don't get suggestions again
            return;
        }

        if (_getSuggestionsTask.Status is TaskStatus.RanToCompletion or TaskStatus.Faulted or TaskStatus.Canceled)
        {
            var listener = _listenerProvider.GetListener(FeatureAttribute.SmartRename);
            var listenerToken = listener.BeginAsyncOperation(nameof(_smartRenameSession.GetSuggestionsAsync));
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            _getSuggestionsTask = GetSuggestionsTaskAsync(isAutomaticOnInitialization, _cancellationTokenSource.Token).CompletesAsyncOperation(listenerToken);
        }
    }

    private async Task GetSuggestionsTaskAsync(bool isAutomaticOnInitialization, CancellationToken cancellationToken)
    {
        if (isAutomaticOnInitialization)
        {
            // ConfigureAwait(true) to stay on the UI thread;
            // WPF view is bound to _smartRenameSession properties and so they must be updated on the UI thread.
            await Task.Delay(_smartRenameSession.AutomaticFetchDelay, cancellationToken).ConfigureAwait(true);
        }

        if (cancellationToken.IsCancellationRequested || _isDisposed)
        {
            return;
        }

        if (IsUsingContext)
        {
            var document = this.BaseViewModel.Session.TriggerDocument;
            var smartRenameContext = ImmutableDictionary<string, string[]>.Empty;
            var editorRenameService = document.GetRequiredLanguageService<IEditorInlineRenameService>();
            if (editorRenameService.IsRenameContextSupported)
            {
                var renameLocations = await this.BaseViewModel.Session.AllRenameLocationsTask.JoinAsync(cancellationToken)
                    .ConfigureAwait(true);
                var context = await editorRenameService.GetRenameContextAsync(this.BaseViewModel.Session.RenameInfo, renameLocations, cancellationToken)
                    .ConfigureAwait(true);
                smartRenameContext = ImmutableDictionary.CreateRange<string, string[]>(
                    context
                    .Select(n => new KeyValuePair<string, string[]>(n.Key, n.Value.ToArray())));
            }

            _ = await _smartRenameSession.GetSuggestionsAsync(smartRenameContext, cancellationToken)
                .ConfigureAwait(true);
        }
        else
        {
            _ = await _smartRenameSession.GetSuggestionsAsync(cancellationToken)
                .ConfigureAwait(true);
        }
    }

    private void SessionPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        _threadingContext.ThrowIfNotOnUIThread();
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
        _cancellationTokenSource?.Cancel();
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
        _isDisposed = true;
        _smartRenameSession.PropertyChanged -= SessionPropertyChanged;
        BaseViewModel.PropertyChanged -= BaseViewModelPropertyChanged;
        _smartRenameSession.Dispose();
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }

    /// <summary>
    /// When smart rename operates in explicit mode, this method gets the suggestions.
    /// When smart rename operates in automatic mode, this method toggles the automatic suggestions, 
    /// and gets the suggestions if it was just enabled.
    /// </summary>
    public void ToggleOrTriggerSuggestions()
    {
        if (this.SupportsAutomaticSuggestions)
        {
            this.IsAutomaticSuggestionsEnabled = !this.IsAutomaticSuggestionsEnabled;
            if (this.IsAutomaticSuggestionsEnabled)
            {
                this.FetchSuggestions(isAutomaticOnInitialization: false);
            }

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
        if (e.PropertyName == nameof(BaseViewModel.IdentifierText))
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}
