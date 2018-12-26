// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private BaseTypeTreeNodeViewModel _selectedDestination;
        public ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> DependentsMap { get; }
        public ImmutableDictionary<ISymbol, PullMemberUpSymbolViewModel> SymbolToMemberViewMap { get; }
        private bool _okButtonEnabled;
        public bool OkButtonEnabled { get => _okButtonEnabled; set => SetProperty(ref _okButtonEnabled, value, nameof(OkButtonEnabled)); }
        private bool? _selectAllCheckBoxState;
        public bool? SelectAllCheckBoxState { get => _selectAllCheckBoxState; set => SetProperty(ref _selectAllCheckBoxState, value, nameof(SelectAllCheckBoxState)); }
        private bool _selectAllCheckBoxThreeStateEnable;
        public bool SelectAllCheckBoxThreeStateEnable { get => _selectAllCheckBoxThreeStateEnable; set => SetProperty(ref _selectAllCheckBoxThreeStateEnable, value, nameof(SelectAllCheckBoxThreeStateEnable)); }
        private readonly IWaitIndicator _waitIndicator;

        public BaseTypeTreeNodeViewModel SelectedDestination
        {
            get => _selectedDestination;
            set
            {
                if (SetProperty(ref _selectedDestination, value, nameof(SelectedDestination)))
                {
                    var fields = Members.WhereAsArray(memberViewModel => memberViewModel.Symbol.IsKind(SymbolKind.Field));
                    if (_selectedDestination.Symbol is INamedTypeSymbol interfaceSymbol &&
                        interfaceSymbol.TypeKind == TypeKind.Interface)
                    {
                        // Disable field check box if destination is interface
                        foreach (var member in fields)
                        {
                            member.IsCheckable = false;
                        }
                    }
                    else
                    {
                        // Resume field check box back
                        foreach (var member in fields)
                        {
                            member.IsCheckable = true;
                        }
                    }
                    EnableOrDisableOkButton();
                    CheckAndSetStateOfSelectAllCheckBox();
                }
            }
        }

        internal PullMemberUpDialogViewModel(
            IWaitIndicator waitIndicator,
            ImmutableArray<PullMemberUpSymbolViewModel> members,
            ImmutableArray<BaseTypeTreeNodeViewModel> destinations,
            ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> dependentsMap)
        {
            Destinations = destinations;
            DependentsMap = dependentsMap;
            Members = members;
            SymbolToMemberViewMap = members.ToImmutableDictionary(memberViewModel => memberViewModel.Symbol);
            _waitIndicator = waitIndicator;
        }

        internal PullMembersUpOptions CreateAnaysisResult()
        {
            var selectedOptionFromDialog = Members.
                WhereAsArray(memberSymbolView => memberSymbolView.IsChecked && memberSymbolView.IsCheckable).
                SelectAsArray(memberSymbolView =>
                    (member: memberSymbolView.Symbol,
                    makeAbstract: memberSymbolView.MakeAbstract));

            var result = PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(
                SelectedDestination.Symbol,
                selectedOptionFromDialog);
            return result;
        }

        internal void SelectAllMembers()
        {
            SelectMembers(Members);
        }

        internal void SelectPublicMembers()
        {
            SelectMembers(Members.WhereAsArray(memberViewModel => memberViewModel.Symbol.DeclaredAccessibility == Accessibility.Public));
        }

        internal void EnableOrDisableOkButton()
        {
            var selectedMembers = Members.
                WhereAsArray(memberSymbolView => memberSymbolView.IsChecked && memberSymbolView.IsCheckable);
            OkButtonEnabled = SelectedDestination != null && selectedMembers.Count() != 0;
        }

        internal void CheckAndSetStateOfSelectAllCheckBox()
        {
            var checkableMembers = Members.WhereAsArray(member => member.IsCheckable);
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
        
        internal void SelectDependents()
        {
            var checkedMembers = Members.
                Where(member => member.IsChecked && member.IsCheckable);

            var waitResult = _waitIndicator.Wait(
                    title: ServicesVSResources.Pull_members_up, 
                    message: ServicesVSResources.Calculating_dependents, 
                    allowCancel: true,
                    showProgress: true,
                    context =>
                    {
                        foreach (var member in checkedMembers)
                        {
                            DependentsMap[member.Symbol].Wait(context.CancellationToken);
                        }
                    });

            if (waitResult == WaitIndicatorResult.Completed)
            {
                foreach (var member in checkedMembers)
                {
                    var membersToSelected = DependentsMap[member.Symbol].Result.SelectAsArray(symbol => SymbolToMemberViewMap[symbol]);
                    SelectMembers(membersToSelected);
                }
            }
        }

        internal void DeSelectAllMembers()
        {
            foreach (var member in Members.WhereAsArray(viewModel => viewModel.IsCheckable))
            {
                member.IsChecked = false;
            }

            CheckAndSetStateOfSelectAllCheckBox();
            EnableOrDisableOkButton();
        }

        private void SelectMembers(ImmutableArray<PullMemberUpSymbolViewModel> memberViewModels)
        {
            foreach (var member in memberViewModels.WhereAsArray(viewModel => viewModel.IsCheckable))
            {
                member.IsChecked = true;
            }

            CheckAndSetStateOfSelectAllCheckBox();
            EnableOrDisableOkButton();
        }
    }
}
