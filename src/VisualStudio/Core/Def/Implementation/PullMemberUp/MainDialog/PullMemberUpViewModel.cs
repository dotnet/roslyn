// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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
    internal class PullMemberUpViewModel : AbstractNotifyPropertyChanged
    {
        public ImmutableArray<PullMemberUpSymbolViewModel> Members { get; set; }

        public ImmutableArray<BaseTypeTreeNodeViewModel> Destinations { get; set; }

        private BaseTypeTreeNodeViewModel _selectedDestination;

        public BaseTypeTreeNodeViewModel SelectedDestination
        {
            get => _selectedDestination;
            set
            {
                if (SetProperty(ref _selectedDestination, value, nameof(SelectedDestination)))
                {
                    var fields = Members.WhereAsArray(memberViewModel => memberViewModel.MemberSymbol.IsKind(SymbolKind.Field));
                    if (_selectedDestination.MemberSymbol is INamedTypeSymbol interfaceSymbol &&
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

        public ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> DependentsMap;

        public ImmutableDictionary<ISymbol, PullMemberUpSymbolViewModel> SymbolToMemberViewMap { get; }

        private bool _okButtonEnabled;

        public bool OkButtonEnabled { get => _okButtonEnabled; set => SetProperty(ref _okButtonEnabled, value, nameof(OkButtonEnabled)); }

        private bool? _selectAllCheckBoxState;

        public bool? SelectAllCheckBoxState { get => _selectAllCheckBoxState; set => SetProperty(ref _selectAllCheckBoxState, value, nameof(SelectAllCheckBoxState)); }

        private bool _threeStateEnable;

        public bool ThreeStateEnable { get => _threeStateEnable; set => SetProperty(ref _threeStateEnable, value, nameof(ThreeStateEnable)); }

        public readonly IWaitIndicator WaitIndicator;

        public readonly CancellationToken CancellationToken;

        internal PullMemberUpViewModel(
            ImmutableArray<BaseTypeTreeNodeViewModel> destinations,
            ImmutableArray<PullMemberUpSymbolViewModel> members,
            ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> dependentsMap,
            IWaitIndicator waitIndicator,
            CancellationToken cancellationToken)
        {
            Destinations = destinations;
            DependentsMap = dependentsMap;
            Members = members;
            SymbolToMemberViewMap = members.ToImmutableDictionary(memberViewModel => memberViewModel.MemberSymbol);
            WaitIndicator = waitIndicator;
            CancellationToken = cancellationToken;
        }

        internal PullMembersUpAnalysisResult CreateAnaysisResult()
        {
            var selectedOptionFromDialog = Members.
                WhereAsArray(memberSymbolView => memberSymbolView.IsChecked && memberSymbolView.IsCheckable).
                SelectAsArray(memberSymbolView =>
                    (member: memberSymbolView.MemberSymbol,
                    makeAbstract: memberSymbolView.MakeAbstract));

            var result = PullMembersUpAnalysisBuilder.BuildAnalysisResult(
                SelectedDestination.MemberSymbol as INamedTypeSymbol,
                selectedOptionFromDialog);
            return result;
        }

        internal void SelectAllMembers()
        {
            SelectMembers(Members);
        }

        internal void SelectPublicMembers()
        {
            SelectMembers(Members.WhereAsArray(memberViewModel => memberViewModel.MemberSymbol.DeclaredAccessibility == Accessibility.Public));
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
                ThreeStateEnable = false;
            }
            else if (checkableMembers.Any(member => member.IsChecked))
            {
                ThreeStateEnable = true;
                SelectAllCheckBoxState = null;
            }
            else
            {
                SelectAllCheckBoxState = false;
                ThreeStateEnable = false;
            }
        }
        
        internal void SelectDependents()
        {
            var checkedMembers = Members.
                Where(member => member.IsChecked && member.IsCheckable);
            foreach (var member in checkedMembers)
            {
                var dependentsTask = DependentsMap[member.MemberSymbol];
                var waitResult = WaitIndicator.Wait(
                        title: ServicesVSResources.Pull_Members_Up, 
                        message: ServicesVSResources.Calculating_dependents, 
                        allowCancel: true,
                        showProgress: false,
                        context =>
                        {
                            DependentsMap[member.MemberSymbol].Wait(context.CancellationToken);
                        });

                if (waitResult == WaitIndicatorResult.Completed)
                {
                    var memberToSelected = dependentsTask.Result.SelectAsArray(symbol => SymbolToMemberViewMap[symbol]);
                    SelectMembers(memberToSelected);
                }
            }
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

        internal void DeSelectAllMembers()
        {
            foreach (var member in Members.WhereAsArray(viewModel => viewModel.IsCheckable))
            {
                member.IsChecked = false;
            }

            CheckAndSetStateOfSelectAllCheckBox();
            EnableOrDisableOkButton();
        }
    }
}
