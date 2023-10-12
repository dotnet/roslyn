// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.Editor.SmartRename;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename
{
    internal class SuggestedNamesControlViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ISmartRenameSession _smartRenameSession;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public SuggestedNamesControlViewModel(
            ISmartRenameSession smartRenameSession)
        {
            _smartRenameSession = smartRenameSession;
            _smartRenameSession.PropertyChanged += SessionPropertyChanged;
            _cancellationTokenSource = new();
            smartRenameSession.GetSuggestionsAsync(_cancellationTokenSource.Token);
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

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
            _smartRenameSession.OnCancel();
        }

        public void Commit(string newIdentifierName)
        {
            _smartRenameSession.OnSuccess(newIdentifierName);
        }

        public ObservableCollection<string> SuggestedNames { get; } = new ObservableCollection<string>();

        public bool IsAvailable => _smartRenameSession.IsAvailable;

        public bool InProgress => _smartRenameSession.IsInProgress;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Dispose()
        {
            _smartRenameSession.Dispose();
            _cancellationTokenSource.Dispose();
            _smartRenameSession.PropertyChanged -= SessionPropertyChanged;
        }
    }
}
