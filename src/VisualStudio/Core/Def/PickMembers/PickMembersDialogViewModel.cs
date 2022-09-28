// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PickMembers
{
    internal class PickMembersDialogViewModel : AbstractNotifyPropertyChanged
    {
        private readonly List<MemberSymbolViewModel> _allMembers;

        public List<MemberSymbolViewModel> MemberContainers { get; set; }
        public List<OptionViewModel> Options { get; set; }

        /// <summary>
        /// <see langword="true"/> if 'Select All' was chosen.  <see langword="false"/> if 'Deselect All' was chosen.
        /// </summary>
        public bool SelectedAll { get; set; }

        internal PickMembersDialogViewModel(
            IGlyphService glyphService,
            ImmutableArray<ISymbol> members,
            ImmutableArray<PickMembersOption> options,
            bool selectAll)
        {
            _allMembers = members.Select(m => new MemberSymbolViewModel(m, glyphService)).ToList();
            MemberContainers = _allMembers;
            Options = options.Select(o => new OptionViewModel(o)).ToList();

            if (selectAll)
            {
                SelectAll();
            }
            else
            {
                DeselectAll();
            }
        }

        internal void Filter(string searchText)
        {
            searchText = searchText.Trim();
            MemberContainers = searchText.Length == 0
                ? _allMembers
                : _allMembers.Where(m => m.SymbolAutomationText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            NotifyPropertyChanged(nameof(MemberContainers));
        }

        internal void DeselectAll()
        {
            SelectedAll = false;
            foreach (var memberContainer in MemberContainers)
                memberContainer.IsChecked = false;
        }

        internal void SelectAll()
        {
            SelectedAll = true;
            foreach (var memberContainer in MemberContainers)
                memberContainer.IsChecked = true;
        }

        private int? _selectedIndex;

        public int? SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }

            set
            {
                var newSelectedIndex = value == -1 ? null : value;
                if (newSelectedIndex == _selectedIndex)
                {
                    return;
                }

                _selectedIndex = newSelectedIndex;

                NotifyPropertyChanged(nameof(CanMoveUp));
                NotifyPropertyChanged(nameof(MoveUpAutomationText));
                NotifyPropertyChanged(nameof(CanMoveDown));
                NotifyPropertyChanged(nameof(MoveDownAutomationText));
            }
        }

        public string MoveUpAutomationText
        {
            get
            {
                if (!CanMoveUp)
                {
                    return string.Empty;
                }

                return string.Format(ServicesVSResources.Move_0_above_1, MemberContainers[SelectedIndex.Value].SymbolAutomationText, MemberContainers[SelectedIndex.Value - 1].SymbolAutomationText);
            }
        }

        public string MoveDownAutomationText
        {
            get
            {
                if (!CanMoveDown)
                {
                    return string.Empty;
                }

                return string.Format(ServicesVSResources.Move_0_below_1, MemberContainers[SelectedIndex.Value].SymbolAutomationText, MemberContainers[SelectedIndex.Value + 1].SymbolAutomationText);
            }
        }

        [MemberNotNullWhen(true, nameof(SelectedIndex))]
        public bool CanMoveUp
        {
            get
            {
                if (!SelectedIndex.HasValue)
                {
                    return false;
                }

                var index = SelectedIndex.Value;
                return index > 0;
            }
        }

        [MemberNotNullWhen(true, nameof(SelectedIndex))]
        public bool CanMoveDown
        {
            get
            {
                if (!SelectedIndex.HasValue)
                {
                    return false;
                }

                var index = SelectedIndex.Value;
                return index < MemberContainers.Count - 1;
            }
        }

        internal void MoveUp()
        {
            Contract.ThrowIfFalse(CanMoveUp);

            var index = SelectedIndex.Value;
            Move(MemberContainers, index, delta: -1);
        }

        internal void MoveDown()
        {
            Contract.ThrowIfFalse(CanMoveDown);

            var index = SelectedIndex.Value;
            Move(MemberContainers, index, delta: 1);
        }

        private void Move(List<MemberSymbolViewModel> list, int index, int delta)
        {
            var param = list[index];
            list.RemoveAt(index);
            list.Insert(index + delta, param);

            SelectedIndex += delta;
        }

        internal class MemberSymbolViewModel : SymbolViewModel<ISymbol>
        {
            public MemberSymbolViewModel(ISymbol symbol, IGlyphService glyphService) : base(symbol, glyphService)
            {
            }
        }

        internal class OptionViewModel : AbstractNotifyPropertyChanged
        {
            public PickMembersOption Option { get; }

            public string Title { get; }

            public OptionViewModel(PickMembersOption option)
            {
                Option = option;
                Title = option.Title;
                IsChecked = option.Value;
            }

            private bool _isChecked;
            public bool IsChecked
            {
                get => _isChecked;

                set
                {
                    Option.Value = value;
                    SetProperty(ref _isChecked, value);
                }
            }

            public string MemberAutomationText => Option.Title;
        }
    }
}
