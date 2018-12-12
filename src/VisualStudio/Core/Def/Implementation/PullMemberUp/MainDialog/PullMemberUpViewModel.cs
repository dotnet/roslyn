// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog
{
    internal class PullMemberUpViewModel : AbstractNotifyPropertyChanged
    {
        public ImmutableArray<PullUpMemberSymbolViewModel> Members { get; set; }

        public ImmutableArray<BaseTypeTreeNodeViewModel> Destinations { get; set; }

        private BaseTypeTreeNodeViewModel _selectedDestination;

        public BaseTypeTreeNodeViewModel SelectedDestination { get => _selectedDestination; set => SetProperty(ref _selectedDestination, value, nameof(SelectedDestination)); }

        public ImmutableDictionary<ISymbol, AsyncLazy<ImmutableArray<ISymbol>>> DependentsMap;

        public ImmutableDictionary<ISymbol, PullUpMemberSymbolViewModel> SymbolToMemberViewMap { get; }

        public CancellationTokenSource CancellationTokenSource { get; }

        private bool _okButtonEnabled;

        public bool OkButtonEnabled { get => _okButtonEnabled; set => SetProperty(ref _okButtonEnabled, value, nameof(OkButtonEnabled)); }

        private bool? _selectAllCheckBoxState;

        public bool? SelectAllCheckBoxState { get => _selectAllCheckBoxState; set => SetProperty(ref _selectAllCheckBoxState, value, nameof(SelectAllCheckBoxState)); }

        private bool _threeStateEnable;

        public bool ThreeStateEnable { get => _threeStateEnable; set => SetProperty(ref _threeStateEnable, value, nameof(ThreeStateEnable)); }

        internal PullMemberUpViewModel(
            ImmutableArray<BaseTypeTreeNodeViewModel> destinations,
            ImmutableArray<PullUpMemberSymbolViewModel> members,
            ImmutableDictionary<ISymbol, AsyncLazy<ImmutableArray<ISymbol>>> dependentsMap,
            CancellationTokenSource cts)
        {
            Destinations = destinations;
            DependentsMap = dependentsMap;
            Members = members;
            SymbolToMemberViewMap = members.ToImmutableDictionary(memberViewModel => memberViewModel.MemberSymbol);
            CancellationTokenSource = cts;
        }

        internal PullMembersUpAnalysisResult CreateAnaysisResult()
        {
            var selectedOptionFromDialog = Members.
                WhereAsArray(memberSymbolView => memberSymbolView.IsChecked && memberSymbolView.IsCheckable).
                SelectAsArray(memberSymbolView =>
                    (member: memberSymbolView.MemberSymbol,
                    makeAbstract: memberSymbolView.MakeAbstract &&
                    memberSymbolView.IsMakeAbstractCheckable));

            var result = PullMembersUpAnalysisBuilder.BuildAnalysisResult(
                SelectedDestination.MemberSymbol as INamedTypeSymbol,
                selectedOptionFromDialog);
            return result;
        }
    }
}
