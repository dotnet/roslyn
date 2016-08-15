// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    public partial class NamingRuleTreeItemViewModel : ObservableObject
    {
        internal NamingRuleTreeItemViewModel(string name)
        {
            // TODO: remove this constructor if possible
            _title = name;
            _children = new ChildRuleCollection(this);
            _children.CollectionChanged += OnChildrenCollectionChanged;
        }

        internal NamingRuleTreeItemViewModel(
            string name,
            SymbolSpecificationViewModel symbolSpec,
            NamingStyleViewModel namingStyle,
            EnforcementLevel enforcementLevel,
            NamingStylesOptionPageControlViewModel vm)
        {
            this.EnforcementLevel = enforcementLevel;
            _title = name;
            this.symbolSpec = symbolSpec;
            this.namingStyle = namingStyle;
            _namingStylesViewModel = vm;

            _children = new ChildRuleCollection(this);
            _children.CollectionChanged += OnChildrenCollectionChanged;
        }

        private NamingRuleTreeItemViewModel _parent;
        private readonly ChildRuleCollection _children;
        private bool _hasChildren;

        internal SymbolSpecificationViewModel symbolSpec;
        internal NamingStyleViewModel namingStyle;

        private string _title;
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                this.SetProperty(ref _title, value, () => NotifyPropertyChanged(nameof(ITreeDisplayItem.Text)));
            }
        }

        public NamingRuleTreeItemViewModel Parent
        {
            get
            {
                return _parent;
            }
            private set
            {
                this.SetProperty(ref _parent, value);
            }
        }

        public IList<NamingRuleTreeItemViewModel> Children
        {
            get { return _children; }
        }

        public bool HasChildren
        {
            get
            {
                return _children.Count > 0;
            }
            private set
            {
                this.SetProperty(ref _hasChildren, value);
            }
        }

        internal bool TrySubmit()
        {
            return true;
        }

        private NamingStylesOptionPageControlViewModel _namingStylesViewModel;
        internal NamingStylesOptionPageControlViewModel NamingStylesViewModel
        {
            get
            {
                return _namingStylesViewModel;
            }
        }

        private EnforcementLevel _enforcementLevel;
        internal EnforcementLevel EnforcementLevel
        {
            get
            {
                return _enforcementLevel;
            }

            set
            {
                if (SetProperty(ref _enforcementLevel, value))
                {
                    NotifyPropertyChanged(nameof(IconMoniker));
                    NotifyPropertyChanged(nameof(ExpandedIconMoniker));
                }
            }
        }

        private void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.HasChildren = _children.Count > 0;
        }

        public bool IsAncestorOfMe(NamingRuleTreeItemViewModel rule)
        {
            NamingRuleTreeItemViewModel potentialAncestor = rule.Parent;
            while (potentialAncestor != null)
            {
                if (potentialAncestor == this)
                {
                    return true;
                }

                potentialAncestor = potentialAncestor.Parent;
            }

            return false;
        }

        private class ChildRuleCollection : ObservableCollection<NamingRuleTreeItemViewModel>
        {
            private readonly NamingRuleTreeItemViewModel _parent;

            public ChildRuleCollection() : this(null)
            {
            }

            public ChildRuleCollection(NamingRuleTreeItemViewModel parent)
            {
                _parent = parent;
            }

            protected override void InsertItem(int index, NamingRuleTreeItemViewModel child)
            {
                base.InsertItem(index, child);
                TakeOwnership(child);
            }

            protected override void RemoveItem(int index)
            {
                NamingRuleTreeItemViewModel child = this[index];
                base.RemoveItem(index);
                LoseOwnership(child);
            }

            protected override void SetItem(int index, NamingRuleTreeItemViewModel newChild)
            {
                NamingRuleTreeItemViewModel oldChild = this[index];
                base.SetItem(index, newChild);
                LoseOwnership(oldChild);
                TakeOwnership(newChild);
            }

            protected override void ClearItems()
            {
                List<NamingRuleTreeItemViewModel> children = new List<NamingRuleTreeItemViewModel>(this);
                base.ClearItems();
                foreach (NamingRuleTreeItemViewModel child in children)
                {
                    LoseOwnership(child);
                }
            }

            private void TakeOwnership(NamingRuleTreeItemViewModel child)
            {
                child.Parent = _parent;
            }

            private void LoseOwnership(NamingRuleTreeItemViewModel child)
            {
                child.Parent = null;
            }
        }
    }
}
