// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.VisualStudio.Text.Editor.SmartRename;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename
{
    internal class SuggestedNamesControlViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ISmartRenameSession _smartRenameSession;

        public SuggestedNamesControlViewModel(ISmartRenameSession smartRenameSession)
        {
            _smartRenameSession = smartRenameSession;
            _smartRenameSession.PropertyChanged += SessionPropertyChanged;
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

        public ObservableCollection<string> SuggestedNames { get; } = new ObservableCollection<string>();

        public bool IsAvailable => _smartRenameSession.IsAvailable;

        public bool InProgress => _smartRenameSession.IsInProgress;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Dispose()
        {
            _smartRenameSession.Dispose();
            _smartRenameSession.PropertyChanged -= SessionPropertyChanged;
        }
    }
}
