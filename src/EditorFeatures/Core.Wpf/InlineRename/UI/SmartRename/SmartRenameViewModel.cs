// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.Editor.InlineRename;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.EditorFeatures.Lightup;
using Microsoft.CodeAnalysis.Options;
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

    public bool SupportsAutomaticSuggestions { get; }

    public bool IsAutomaticSuggestionsEnabled { get; private set; }

    public void ToggleAutomaticSuggestions()
    {
        if (!SupportsAutomaticSuggestions)
        {
            return;
        }
        IsAutomaticSuggestionsEnabled = !IsAutomaticSuggestionsEnabled;
        // Use existing option (true if user does not wish to get suggestions automatically) to honor user's choice from before the refactoring.
        _globalOptionService.SetGlobalOption(InlineRenameUIOptionsStorage.CollapseSuggestionsPanel, !IsAutomaticSuggestionsEnabled);
        NotifyPropertyChanged(nameof(IsAutomaticSuggestionsEnabled));
    }

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

    public bool IsSuggestionsPanelCollapsed => !HasSuggestions;

    public bool IsSuggestionsPanelExpanded => HasSuggestions;

    public bool IsButtonHighlighted => IsInProgress || HasSuggestions || IsAutomaticSuggestionsEnabled;

    public string GetSuggestionsTooltip
        => SupportsAutomaticSuggestions
        ? EditorFeaturesWpfResources.Get_AI_suggestions
        : EditorFeaturesWpfResources.Toggle_AI_suggestions;

    public string SubmitTextOverride
        => SupportsAutomaticSuggestions
        ? EditorFeaturesWpfResources.Enter_to_rename_shift_enter_to_preview_ctrl_space_for_ai_suggestion
        : EditorFeaturesWpfResources.Enter_to_rename_shift_enter_to_preview;

    public static string GeneratingSuggestionsLabel => EditorFeaturesWpfResources.Generating_suggestions;

    public ICommand GetSuggestionsCommand { get; }

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
        BaseViewModel.PropertyChanged += IdentifierTextPropertyChanged;
        this.BaseViewModel.IdentifierText = baseViewModel.IdentifierText;

        GetSuggestionsCommand = new DelegateCommand(OnGetSuggestionsCommandExecute, null, threadingContext.JoinableTaskFactory);

        SetupTelemetry();

        this.SupportsAutomaticSuggestions = _globalOptionService.GetOption(InlineRenameUIOptionsStorage.GetSuggestionsAutomatically);
        this.IsAutomaticSuggestionsEnabled = this.SupportsAutomaticSuggestions && !_globalOptionService.GetOption(InlineRenameUIOptionsStorage.CollapseSuggestionsPanel);
        if (this.SupportsAutomaticSuggestions && this.IsAutomaticSuggestionsEnabled)
        {
            OnGetSuggestionsCommandExecute(true);
        }
    }

    private void OnGetSuggestionsCommandExecute(object? parameter)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        if (IsAutomaticSuggestionsEnabled && SuggestedNames.Count > 0)
        {
            // Don't get suggestions again in the automatic scenario
            return;
        }
        if (_getSuggestionsTask.Status is TaskStatus.RanToCompletion or TaskStatus.Faulted or TaskStatus.Canceled)
        {
            var listener = _listenerProvider.GetListener(FeatureAttribute.SmartRename);
            var listenerToken = listener.BeginAsyncOperation(nameof(_smartRenameSession.GetSuggestionsAsync));
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            var isAutomatic = parameter is bool b && b;
            _getSuggestionsTask = GetSuggestionsTaskAsync(isAutomatic, _cancellationTokenSource.Token).CompletesAsyncOperation(listenerToken);
        }
    }

    private async Task GetSuggestionsTaskAsync(bool isAutomatic, CancellationToken cancellationToken)
    {
        if (isAutomatic)
        {
            await Task.Delay(_smartRenameSession.AutomaticFetchDelay, cancellationToken).ConfigureAwait(true);
        }
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        _ = await _smartRenameSession.GetSuggestionsAsync(_cancellationTokenSource.Token);
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

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSuggestionsPanelCollapsed)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSuggestionsPanelExpanded)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsButtonHighlighted)));
            return;
        }
        else if (e.PropertyName == nameof(_smartRenameSession.IsInProgress))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsButtonHighlighted)));
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
        _smartRenameSession.PropertyChanged -= SessionPropertyChanged;
        BaseViewModel.PropertyChanged -= IdentifierTextPropertyChanged;
        _smartRenameSession.Dispose();
        _cancellationTokenSource?.Dispose();
    }

    private void NotifyPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void IdentifierTextPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BaseViewModel.IdentifierText))
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}
