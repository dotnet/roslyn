// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog
{
    internal class PullMemberUpDialogViewModel : AbstractNotifyPropertyChanged
    {
        public ImmutableArray<PullMemberUpSymbolViewModel> Members { get; set; }
        public ImmutableArray<BaseTypeTreeNodeViewModel> Destinations { get; set; }
        public bool OkButtonEnabled { get => _okButtonEnabled; set => SetProperty(ref _okButtonEnabled, value, nameof(OkButtonEnabled)); }
        public bool? SelectAllCheckBoxState { get => _selectAllCheckBoxState; set => SetProperty(ref _selectAllCheckBoxState, value, nameof(SelectAllCheckBoxState)); }
        public bool SelectAllCheckBoxThreeStateEnable { get => _selectAllCheckBoxThreeStateEnable; set => SetProperty(ref _selectAllCheckBoxThreeStateEnable, value, nameof(SelectAllCheckBoxThreeStateEnable)); }
        public string SelectAllCheckBoxAutomationText => ServicesVSResources.Select_All;
        public string DestinationTreeViewAutomationText => ServicesVSResources.Select_destination;
        public string SelectMemberListViewAutomationText => ServicesVSResources.Select_member;
        private bool _selectAllCheckBoxThreeStateEnable;
        private bool? _selectAllCheckBoxState;
        private readonly IWaitIndicator _waitIndicator;
        private BaseTypeTreeNodeViewModel _selectedDestination;
        private readonly ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> _symbolToDependentsMap;
        private readonly ImmutableDictionary<ISymbol, PullMemberUpSymbolViewModel> _symbolToMemberViewMap;
        private bool _okButtonEnabled;

        public PullMemberUpDialogViewModel(
            IWaitIndicator waitIndicator,
            ImmutableArray<PullMemberUpSymbolViewModel> members,
            ImmutableArray<BaseTypeTreeNodeViewModel> destinations,
            ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> dependentsMap)
        {
            _waitIndicator = waitIndicator;
            Members = members;
            Destinations = destinations;
            _symbolToDependentsMap = dependentsMap;
            _symbolToMemberViewMap = members.ToImmutableDictionary(memberViewModel => memberViewModel.Symbol);
            if (destinations != default && !destinations.IsEmpty)
            {
                // Select a destination by default
                destinations[0].IsChecked = true;
            }
        }

        public BaseTypeTreeNodeViewModel SelectedDestination
        {
            get => _selectedDestination;
            set
            {
                if (SetProperty(ref _selectedDestination, value, nameof(SelectedDestination)))
                {
                    var fields = Members.WhereAsArray(memberViewModel => memberViewModel.Symbol.IsKind(SymbolKind.Field));
                    var makeAbstractEnabledCheckboxes = Members.
                        WhereAsArray(memberViewModel => !memberViewModel.Symbol.IsKind(SymbolKind.Field) && !memberViewModel.Symbol.IsAbstract);
                    var isInterface = _selectedDestination.Symbol.TypeKind == TypeKind.Interface;
                    // Disable field check box and make abstract if destination is interface
                    foreach (var member in fields)
                    {
                        member.IsCheckable = !isInterface;
                        member.TooltipText = isInterface ? ServicesVSResources.Interface_cannot_have_field : string.Empty;
                    }

                    foreach (var member in makeAbstractEnabledCheckboxes)
                    {
                        member.IsMakeAbstractCheckable = !isInterface;
                    }

                    SetStatesOfOkButtonAndSelectAllCheckBox();
                }
            }
        }

        public PullMembersUpOptions CreatePullMemberUpOptions()
        {
            var selectedOptionFromDialog = Members.
                Where(memberSymbolView => memberSymbolView.IsChecked && memberSymbolView.IsCheckable).
                SelectAsArray(memberViewModel =>
                    (member: memberViewModel.Symbol,
                    makeAbstract: memberViewModel.IsMakeAbstractCheckable && memberViewModel.MakeAbstract));

            var options = PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(
                SelectedDestination.Symbol,
                selectedOptionFromDialog);
            return options;
        }

        public void SelectAllMembers()
        {
            SelectMembers(Members);
        }

        public void SelectPublicMembers()
        {
            SelectMembers(Members.WhereAsArray(memberViewModel => memberViewModel.Symbol.DeclaredAccessibility == Accessibility.Public));
        }

        public void SetStatesOfOkButtonAndSelectAllCheckBox()
        {
            EnableOrDisableOkButton();
            CheckAndSetStateOfSelectAllCheckBox();
        }

        public void SelectDependents()
        {
            var checkedMembers = Members
                .WhereAsArray(member => member.IsChecked && member.IsCheckable);

            var waitResult = _waitIndicator.Wait(
                    title: ServicesVSResources.Pull_Members_Up,
                    message: ServicesVSResources.Calculating_dependents,
                    allowCancel: true,
                    showProgress: true,
                    context =>
                    {
                        foreach (var member in Members)
                        {
                            _symbolToDependentsMap[member.Symbol].Wait(context.CancellationToken);
                        }
                    });

            if (waitResult == WaitIndicatorResult.Completed)
            {
                foreach (var member in checkedMembers)
                {
                    var membersToSelected = FindDependentsRecursively(member.Symbol).SelectAsArray(symbol => _symbolToMemberViewMap[symbol]);
                    SelectMembers(membersToSelected);
                }
            }
        }

        public void DeSelectAllMembers()
        {
            foreach (var member in Members.Where(viewModel => viewModel.IsCheckable))
            {
                member.IsChecked = false;
            }

            SetStatesOfOkButtonAndSelectAllCheckBox();
        }

        private ImmutableHashSet<ISymbol> FindDependentsRecursively(ISymbol member)
        {
            var queue = new Queue<ISymbol>();
            // Under situation like two methods call each other, this hashset is used to 
            // prevent the infinity loop.
            var visited = new HashSet<ISymbol>();
            var result = new HashSet<ISymbol>();
            queue.Enqueue(member);
            visited.Add(member);
            while (!queue.IsEmpty())
            {
                var currentMember = queue.Dequeue();
                result.Add(currentMember);
                visited.Add(currentMember);
                foreach (var dependent in _symbolToDependentsMap[currentMember].Result)
                {
                    if (!visited.Contains(dependent))
                    {
                        queue.Enqueue(dependent);
                    }
                }
            }

            return result.ToImmutableHashSet();
        }

        private void SelectMembers(ImmutableArray<PullMemberUpSymbolViewModel> memberViewModels)
        {
            foreach (var member in memberViewModels.WhereAsArray(viewModel => viewModel.IsCheckable))
            {
                member.IsChecked = true;
            }

            SetStatesOfOkButtonAndSelectAllCheckBox();
        }

        private void EnableOrDisableOkButton()
        {
            var selectedMembers = Members
                .Where(memberSymbolView => memberSymbolView.IsChecked && memberSymbolView.IsCheckable);
            OkButtonEnabled = SelectedDestination != null && selectedMembers.Count() != 0;
        }

        private void CheckAndSetStateOfSelectAllCheckBox()
        {
            var checkableMembers = Members.Where(member => member.IsCheckable);
            if (checkableMembers.All(member => member.IsChecked))
            {
                SelectAllCheckBoxState = true;
                SelectAllCheckBoxThreeStateEnable = false;
            }
            else if (checkableMembers.Any(member => member.IsChecked))
            {
                SelectAllCheckBoxThreeStateEnable = true;
                SelectAllCheckBoxState = null;
            }
            else
            {
                SelectAllCheckBoxState = false;
                SelectAllCheckBoxThreeStateEnable = false;
            }
        }
    }
}
