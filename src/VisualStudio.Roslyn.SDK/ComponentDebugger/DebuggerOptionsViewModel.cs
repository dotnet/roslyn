// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Roslyn.ComponentDebugger
{
    internal sealed class DebuggerOptionsViewModel : INotifyPropertyChanged
    {
        private readonly Action<int> _indexChanged;

        private IEnumerable<string> _projectNames = ImmutableArray<string>.Empty;

        private int _selectedProjectIndex = -1;

        public event PropertyChangedEventHandler? PropertyChanged;

        public DebuggerOptionsViewModel(Action<int> indexChanged)
        {
            _indexChanged = indexChanged;
        }

        public IEnumerable<string> ProjectNames
        {
            get => _projectNames;
            set
            {
                if (!_projectNames.SequenceEqual(value))
                {
                    _projectNames = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int SelectedProjectIndex
        {
            get => _selectedProjectIndex;
            set
            {
                if (_selectedProjectIndex != value)
                {
                    _selectedProjectIndex = value;
                    NotifyPropertyChanged();
                    _indexChanged?.Invoke(value);
                }
            }
        }

        private void NotifyPropertyChanged([CallerMemberName]string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
