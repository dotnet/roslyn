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
        private readonly ISmartRenameSession _smartRenameSession;

        private readonly CancellationTokenSource _cancellationTokenSource;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<SuggestedNameViewModel> SuggestedNames { get; } = new ObservableCollection<SuggestedNameViewModel>();

        public bool IsAvailable => _smartRenameSession?.IsAvailable ?? false;

        public bool HasSuggestion => _smartRenameSession?.HasSuggestions ?? false;

        public bool InProgress => _smartRenameSession?.IsInProgress ?? false;

        private string? _currentSelectedName;

        public string? CurrentSelectedName
        {
            get => _currentSelectedName;
            set
            {
                if (_currentSelectedName != value)
                {
                    _currentSelectedName = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSelectedName)));
                }
            }
        }

        public SmartRenameViewModel(ISmartRenameSession smartRenameSession)
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
                    SuggestedNames.Add(new SuggestedNameViewModel(name, this));
                }

                return;
            }

            PropertyChanged?.Invoke(this, e);
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
