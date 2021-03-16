// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ValueTracking;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    internal class TreeViewItemBase : INotifyPropertyChanged
    {
        private bool _isExpanded = false;
        public bool IsNodeExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        private bool _isSelected = false;
        public bool IsNodeSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            NotifyPropertyChanged(name);
        }

        protected void NotifyPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    internal class EmptyTreeViewItem : TreeViewItemBase
    {
        public static EmptyTreeViewItem Instance { get; } = new();

        private EmptyTreeViewItem()
        {
        }
    }

    internal class ValueTrackingTreeItemViewModel : TreeViewItemBase
    {
        private bool _childrenCalculated;
        private readonly Solution _solution;
        private readonly IValueTrackingService _valueTrackingService;

        public ObservableCollection<TreeViewItemBase> ChildItems { get; } = new();

        public Location Location => TrackedItem.Location;
        public ISymbol Symbol => TrackedItem.Symbol;

        public string FileName => Path.GetFileName(Location.SourceTree!.FilePath);
        public string LineDisplay
        {
            get
            {
                var linePosition = Location.GetLineSpan().StartLinePosition;
                return linePosition.Line.ToString();
            }
        }

        public string SymbolName => Symbol.ToDisplayString();
        public string ContainerName => Symbol.ContainingSymbol.ToDisplayString();

        public ValueTrackedItem TrackedItem { get; }

        public ValueTrackingTreeItemViewModel(
            ValueTrackedItem trackedItem,
            Solution solution,
            IValueTrackingService valueTrackingService)
        {
            Contract.ThrowIfFalse(trackedItem.Location.IsInSource);

            ChildItems.Add(EmptyTreeViewItem.Instance);

            TrackedItem = trackedItem;
            _solution = solution;
            _valueTrackingService = valueTrackingService;

            ChildItems.CollectionChanged += (s, a) =>
            {
                NotifyPropertyChanged(nameof(ChildItems));
            };

            PropertyChanged += (s, a) =>
            {
                if (a.PropertyName == nameof(IsNodeExpanded))
                {
                    if (_childrenCalculated)
                    {
                        return;
                    }

                    _childrenCalculated = true;

                    // TODO: What cancellationtoken to use here?
                    CalculateChildrenAsync(CancellationToken.None)
                        .ContinueWith(task =>
                        {
                            if (task.Exception is not null)
                            {
                                _childrenCalculated = false;
                                return;
                            }

                            ChildItems.Clear();

                            foreach (var item in task.Result)
                            {
                                ChildItems.Add(item);
                            }
                        },

                        // Use the UI thread synchronization context for calling back to the UI
                        // thread to add the tiems
                        TaskScheduler.FromCurrentSynchronizationContext());

                }
            };
        }

        private async Task<ImmutableArray<ValueTrackingTreeItemViewModel>> CalculateChildrenAsync(CancellationToken cancellationToken)
        {
            var valueTrackedItems = await _valueTrackingService.TrackValueSourceAsync(
                _solution,
                TrackedItem,
                cancellationToken).ConfigureAwait(false);

            // TODO: Use pooled item here? 
            var builder = new List<ValueTrackingTreeItemViewModel>();
            foreach (var valueTrackedItem in valueTrackedItems)
            {
                builder.Add(new ValueTrackingTreeItemViewModel(
                    valueTrackedItem,
                    _solution,
                    _valueTrackingService));
            }

            return builder.ToImmutableArray();
        }
    }
}
