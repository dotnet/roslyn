﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.EditorFeatures.Lightup;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename;

internal sealed class SmartRenameViewModel : INotifyPropertyChanged, IDisposable
{
    public static string GeneratingSuggestions => EditorFeaturesWpfResources.Generating_suggestions;

#pragma warning disable CS0618 // Editor team use Obsolete attribute to mark potential changing API
    private readonly ISmartRenameSessionWrapper _smartRenameSession;
#pragma warning restore CS0618 

    private readonly IThreadingContext _threadingContext;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<string?>? OnSelectedSuggestedNameChanged;

    public ObservableCollection<string> SuggestedNames { get; } = new ObservableCollection<string>();

    public bool IsAvailable => _smartRenameSession.IsAvailable;

    public bool HasSuggestions => _smartRenameSession.HasSuggestions;

    public bool IsInProgress => _smartRenameSession.IsInProgress;

    public string StatusMessage => _smartRenameSession.StatusMessage;

    public bool StatusMessageVisibility => _smartRenameSession.StatusMessageVisibility;

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
                OnSelectedSuggestedNameChanged?.Invoke(this, value);
            }
        }
    }

    public SmartRenameViewModel(
        IThreadingContext threadingContext,
        IAsynchronousOperationListenerProvider listenerProvider,
#pragma warning disable CS0618 // Editor team use Obsolete attribute to mark potential changing API
        ISmartRenameSessionWrapper smartRenameSession)
#pragma warning restore CS0618
    {
        _threadingContext = threadingContext;
        _smartRenameSession = smartRenameSession;
        _smartRenameSession.PropertyChanged += SessionPropertyChanged;
        var listener = listenerProvider.GetListener(FeatureAttribute.SmartRename);

        var listenerToken = listener.BeginAsyncOperation(nameof(_smartRenameSession.GetSuggestionsAsync));
        _smartRenameSession.GetSuggestionsAsync(_cancellationTokenSource.Token).CompletesAsyncOperation(listenerToken);
    }

    private void SessionPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        // _smartRenameSession.SuggestedNames is a normal list. We need to convert it to ObservableCollection to bind to UI Element.
        if (e.PropertyName == nameof(_smartRenameSession.SuggestedNames))
        {
            SuggestedNames.Clear();
            foreach (var name in _smartRenameSession.SuggestedNames)
            {
                SuggestedNames.Add(name);
            }

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
    }

    public void Commit(string finalIdentifierName)
    {
        // It's needed by editor-side telemetry.
        _smartRenameSession.OnSuccess(finalIdentifierName);
    }

    public void Dispose()
    {
        _smartRenameSession.PropertyChanged -= SessionPropertyChanged;
        _smartRenameSession.Dispose();
        _cancellationTokenSource.Dispose();
    }
}
