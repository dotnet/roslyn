// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PickMembers
{
    internal class PickMembersDialogViewModel : AbstractNotifyPropertyChanged
    {
        public List<MemberSymbolViewModel> MemberContainers { get; set; }
        public List<OptionViewModel> Options { get; set; }

        internal PickMembersDialogViewModel(
            IGlyphService glyphService,
            ImmutableArray<ISymbol> members,
            ImmutableArray<PickMembersOption> options)
        {
            MemberContainers = members.Select(m => new MemberSymbolViewModel(m, glyphService)).ToList();
            Options = options.Select(o => new OptionViewModel(o)).ToList();
        }

        internal void DeselectAll()
        {
            foreach (var memberContainer in MemberContainers)
            {
                memberContainer.IsChecked = false;
            }
        }

        internal void SelectAll()
        {
            foreach (var memberContainer in MemberContainers)
            {
                memberContainer.IsChecked = true;
            }
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
            Debug.Assert(CanMoveUp);

            var index = SelectedIndex.Value;
            Move(MemberContainers, index, delta: -1);
        }

        internal void MoveDown()
        {
            Debug.Assert(CanMoveDown);

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
