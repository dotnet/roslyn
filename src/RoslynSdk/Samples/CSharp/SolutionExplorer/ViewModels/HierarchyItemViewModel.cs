using System;
using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using MSBuildWorkspaceTester.Framework;

namespace MSBuildWorkspaceTester.ViewModels
{
    internal abstract class HierarchyItemViewModel : ViewModel, IComparable<HierarchyItemViewModel>
    {
        private bool _isExpanded;
        protected Workspace Workspace { get; }
        private ObservableCollection<HierarchyItemViewModel> _children;
        public ReadOnlyObservableCollection<HierarchyItemViewModel> Children { get; }

        protected HierarchyItemViewModel(Workspace workspace, bool isExpanded = true)
        {
            Workspace = workspace;
            _isExpanded = isExpanded;
            _children = new ObservableCollection<HierarchyItemViewModel>();
            Children = new ReadOnlyObservableCollection<HierarchyItemViewModel>(_children);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetValue(ref _isExpanded, value);
        }

        public string DisplayName => GetDisplayName();

        protected abstract string GetDisplayName();

        public int BinarySearch(HierarchyItemViewModel value)
        {
            int low = 0;
            int high = _children.Count - 1;

            while (low <= high)
            {
                int mid = low + ((high - low) / 2);
                int comp = _children[mid].CompareTo(value);

                if (comp == 0)
                {
                    return mid;
                }

                if (comp < 0)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return ~low;
        }

        public void AddChild(HierarchyItemViewModel item)
        {
            int index = BinarySearch(item);

            if (index < 0)
            {
                _children.Insert(~index, item);
            }
            else
            {
                _children.Insert(index, item);
            }
        }

        public virtual int CompareTo(HierarchyItemViewModel other)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(DisplayName, other.DisplayName);
        }
    }
}
