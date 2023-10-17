// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using Microsoft.VisualStudio.Text.Editor.SmartRename;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename
{
    internal class SmartRenameViewModel : INotifyPropertyChanged, IDisposable
    {
#pragma warning disable CS0618 // Editor team use Obsolete attribute to mark potential changing API
        private readonly ISmartRenameSession _smartRenameSession;
#pragma warning restore CS0618 

        private readonly CancellationTokenSource _cancellationTokenSource;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<string> SuggestedNames { get; } = new ObservableCollection<string>();

        public bool IsAvailable => _smartRenameSession?.IsAvailable ?? false;

        public bool HasSuggestions => _smartRenameSession?.HasSuggestions ?? false;

        public bool IsInProgress => _smartRenameSession?.IsInProgress ?? false;

        public string StatusMessage => _smartRenameSession?.StatusMessage ?? string.Empty;

        public bool StatusMessageVisibility => _smartRenameSession?.StatusMessageVisibility ?? false;

        public static string GeneratingSuggestions => EditorFeaturesWpfResources.Generating_suggestions;

        private string? _selectedSuggestedName;

        public string? SelectedSuggestedName
        {
            get => _selectedSuggestedName;
            set
            {
                if (_selectedSuggestedName != value)
                {
                    _selectedSuggestedName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedSuggestedName)));
                }
            }
        }

#pragma warning disable CS0618 // Editor team use Obsolete attribute to mark potential changing API
        public SmartRenameViewModel(ISmartRenameSession smartRenameSession)
#pragma warning restore CS0618
        {
            _smartRenameSession = smartRenameSession;
            _smartRenameSession.PropertyChanged += SessionPropertyChanged;
            _cancellationTokenSource = new();
            _smartRenameSession.GetSuggestionsAsync(_cancellationTokenSource.Token);
        }

        private void SessionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_smartRenameSession.SuggestedNames))
            {
                SuggestedNames.Clear();
                foreach (var name in _smartRenameSession.SuggestedNames)
                {
                    SuggestedNames.Add(name);
                }

                return;
            }

            PropertyChanged?.Invoke(this, e);
        }

        public string? ScrollSuggestions(string currentIdentifier, bool down)
        {
            if (!HasSuggestions)
            {
                return null;
            }

            var currentIndex = SuggestedNames.IndexOf(currentIdentifier);
            currentIndex += down ? 1 : -1;
            currentIndex = Math.Max(0, Math.Min(this.SuggestedNames.Count - 1, currentIndex));
            return SuggestedNames[currentIndex];
        }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
            _smartRenameSession.OnCancel();
        }

        public void Commit(string finalIdentifierName)
        {
            _smartRenameSession.OnSuccess(finalIdentifierName);
        }

        public void Dispose()
        {
            _smartRenameSession.Dispose();
            _cancellationTokenSource.Dispose();
            _smartRenameSession.PropertyChanged -= SessionPropertyChanged;
        }
    }
}
