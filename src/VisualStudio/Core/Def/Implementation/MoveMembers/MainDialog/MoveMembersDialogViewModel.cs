// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.MoveMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.Controls;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.MainDialog
{
    internal class MoveMembersDialogViewModel : AbstractNotifyPropertyChanged
    {
        public ImmutableArray<MoveMembersSymbolViewModel> Members { get; set; }
        public INamedTypeSymbol OriginalTypeSymbol { get; }
        public bool OkButtonEnabled { get => _okButtonEnabled; set => SetProperty(ref _okButtonEnabled, value, nameof(OkButtonEnabled)); }
        public bool? SelectAllCheckBoxState { get => _selectAllCheckBoxState; set => SetProperty(ref _selectAllCheckBoxState, value, nameof(SelectAllCheckBoxState)); }
        public bool SelectAllCheckBoxThreeStateEnable { get => _selectAllCheckBoxThreeStateEnable; set => SetProperty(ref _selectAllCheckBoxThreeStateEnable, value, nameof(SelectAllCheckBoxThreeStateEnable)); }
        public string SelectAllCheckBoxAutomationText => ServicesVSResources.Select_All;
        public string DestinationTreeViewAutomationText => ServicesVSResources.Select_destination;
        public string SelectMemberListViewAutomationText => ServicesVSResources.Select_member;
        private bool _selectAllCheckBoxThreeStateEnable;
        private bool? _selectAllCheckBoxState;
        private readonly IWaitIndicator _waitIndicator;
        private readonly ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> _symbolToDependentsMap;

        private readonly ImmutableDictionary<ISymbol, MoveMembersSymbolViewModel> _symbolToMemberViewMap;
        private bool _okButtonEnabled;

        public MoveMembersDialogViewModel(
            IWaitIndicator waitIndicator,
            INamedTypeSymbol targetType,
            ImmutableArray<MoveMembersSymbolViewModel> members,
            ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> dependentsMap,
            string fileExtension,
            bool suggestInterface = true,
            ImmutableArray<SymbolViewModel<INamedTypeSymbol>> destinations = default)
        {
            OriginalTypeSymbol = targetType;
            _waitIndicator = waitIndicator;
            Members = members.OrderBy(m => m.SymbolName).ToImmutableArray();
            _symbolToDependentsMap = dependentsMap;
            _symbolToMemberViewMap = members.ToImmutableDictionary(memberViewModel => memberViewModel.Symbol);
            if (destinations != default && !destinations.IsEmpty)
            {
                MovingToExistingType = true;
                destinations[0].IsChecked = true;
                Destinations = destinations;
                SelectDestinationViewModel = new MoveToAncestorTypeControlViewModel(Destinations);
            }
            else
            {
                SelectDestinationViewModel = new MoveToNewTypeControlViewModel(suggestInterface, targetType, fileExtension);
            }

            SetStatesOfOkButtonAndSelectAllCheckBox();

            SelectDestinationViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ISelectDestinationViewModel.SelectedDestination))
                {
                    var fields = Members.WhereAsArray(memberViewModel => memberViewModel.Symbol.IsKind(SymbolKind.Field));
                    var makeAbstractEnabledCheckboxes = Members.
                        WhereAsArray(memberViewModel => !memberViewModel.Symbol.IsKind(SymbolKind.Field) && !memberViewModel.Symbol.IsAbstract);
                    var isInterface = SelectedDestination.TypeKind == TypeKind.Interface;
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
                }

                SetStatesOfOkButtonAndSelectAllCheckBox();
            };
        }

        public ISelectDestinationViewModel SelectDestinationViewModel { get; }
        public bool MovingToExistingType { get; }
        public ImmutableArray<SymbolViewModel<INamedTypeSymbol>> Destinations { get; }
        public INamedTypeSymbol SelectedDestination => SelectDestinationViewModel.SelectedDestination;
        public bool HasMembersThatCanBeAbstract => Members.Any(m => m.IsMakeAbstractCheckable);

        public ImmutableArray<(ISymbol member, bool makeAbstract)> GetCheckedMembers()
        => Members.
                Where(memberSymbolView => memberSymbolView.IsChecked && memberSymbolView.IsCheckable).
                SelectAsArray(memberViewModel =>
                    (member: memberViewModel.Symbol,
                    makeAbstract: memberViewModel.IsMakeAbstractCheckable && memberViewModel.MakeAbstract));

        public ImmutableArray<MemberAnalysisResult> AnalyzeCheckedMembers()
        {
            var members = GetCheckedMembers();
            return members.SelectAsArray(memberAndMakeAbstract =>
            {
                if (MovingToExistingType)
                {
                    if (SelectedDestination.TypeKind == TypeKind.Interface)
                    {
                        var changeOriginalToPublic = memberAndMakeAbstract.member.DeclaredAccessibility != Accessibility.Public;
                        var changeOriginalToNonStatic = memberAndMakeAbstract.member.IsStatic;
                        return new MemberAnalysisResult(
                            memberAndMakeAbstract.member,
                            changeOriginalToPublic,
                            changeOriginalToNonStatic,
                            makeMemberDeclarationAbstract: false,
                            changeDestinationTypeToAbstract: false);
                    }
                    else
                    {
                        var changeDestinationToAbstract = !SelectedDestination.IsAbstract && (memberAndMakeAbstract.makeAbstract || memberAndMakeAbstract.member.IsAbstract);
                        return new MemberAnalysisResult(memberAndMakeAbstract.member,
                            changeOriginalToPublic: false,
                            changeOriginalToNonStatic: false,
                            memberAndMakeAbstract.makeAbstract,
                            changeDestinationTypeToAbstract: changeDestinationToAbstract);
                    }
                }
                else
                {
                    return new MemberAnalysisResult(memberAndMakeAbstract.member);
                }
            });
        }

        public void SelectAllMembers()
            => SelectMembers(Members);

        public void SelectPublicMembers()
            => SelectMembers(Members.WhereAsArray(memberViewModel => memberViewModel.Symbol.DeclaredAccessibility == Accessibility.Public));

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

        private void SelectMembers(ImmutableArray<MoveMembersSymbolViewModel> memberViewModels)
        {
            foreach (var member in memberViewModels.WhereAsArray(viewModel => viewModel.IsCheckable))
            {
                member.IsChecked = true;
            }

            SetStatesOfOkButtonAndSelectAllCheckBox();
        }

        private void EnableOrDisableOkButton()
        {
            if (!SelectDestinationViewModel.IsValid)
            {
                OkButtonEnabled = false;
                return;
            }

            var selectedMembers = Members
                .Where(memberSymbolView => memberSymbolView.IsChecked && memberSymbolView.IsCheckable);

            if (MovingToExistingType)
            {
                OkButtonEnabled = SelectedDestination != null && selectedMembers.Any();
            }
            else
            {
                OkButtonEnabled = selectedMembers.Any();
            }
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
