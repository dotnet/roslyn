// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    /// <summary>
    /// Interaction logic for PullhMemberUpDialog.xaml
    /// </summary>
    internal partial class PullMemberUpDialog : DialogWindow
    {
        public string OK => ServicesVSResources.OK;

        public string Cancel => ServicesVSResources.Cancel;

        public string PullMembersUpTitle => ServicesVSResources.Pull_Up_Members;

        public string SelectMembers => ServicesVSResources.Select_Members;

        public string SelectDestination => ServicesVSResources.Select_Destination;

        public string PullUpDescription => ServicesVSResources.Pull_Up_Description;

        public string SelectAll => ServicesVSResources.Select_All;

        public string DeselectAll => ServicesVSResources.Deselect_All;

        public string SelectPublic => ServicesVSResources.Select_Public;

        public string SelectDependents => ServicesVSResources.Select_Dependents;

        public string Members => ServicesVSResources.Members;

        public string MakeAbstract => ServicesVSResources.Make_abstract;

        private PullMemberUpViewModel ViewModel { get; }

        private bool ProceedToSelectAll { get; set; } = false;

        internal PullMemberUpDialog(PullMemberUpViewModel pullMemberUpViewModel)
        {
            ViewModel = pullMemberUpViewModel;
            DataContext = ViewModel;
            InitializeComponent();
            MemberSelection.SizeChanged += (s, e) =>
            {
                ((GridView)MemberSelection.View).Columns[0].Width = e.NewSize.Width * 0.6;
                ((GridView)MemberSelection.View).Columns[1].Width = e.NewSize.Width * 0.3;
            };
        }

        private void TargetMembersContainer_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TargetMembersContainer.SelectedItem is MemberSymbolViewModelGraphNode memberGraphNode)
            {
                ViewModel.SelectedTarget = memberGraphNode;
                if (memberGraphNode.MemberSymbolViewModel.MemberSymbol is INamedTypeSymbol interfaceSymbol && interfaceSymbol.TypeKind == TypeKind.Interface)
                {
                    DisableFieldCheckBox();
                    DisableAbstractBox();
                }
                else
                {
                    EnableFieldChekcBox();
                    EnableAbstractBox();
                }
            }
        }

        private void DisableAbstractBox()
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                member.IsAbstractSelectable = false;
                member.IsAbstract = false;
            }
        }

        private void EnableAbstractBox()
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                if (member.MemberSymbol.Kind != SymbolKind.Field && !member.MemberSymbol.IsAbstract)
                {
                    member.IsAbstractSelectable = true;
                }
            }
        }

        private void DisableFieldCheckBox()
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                if (member.MemberSymbol.Kind == SymbolKind.Field)
                {
                    member.IsChecked = false;
                    member.IsSelectable = false;
                }
            }
        }

        private void EnableFieldChekcBox()
        {
           foreach (var member in ViewModel.SelectedMembersContainer)
            {
                if (member.MemberSymbol.Kind == SymbolKind.Field)
                {
                    member.IsChecked = false;
                    member.IsSelectable = true;
                }
            }
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedMembers = ViewModel.SelectedMembersContainer.
                Where(memberSymbolView => memberSymbolView.IsChecked).
                Select(memberSymbolView => memberSymbolView.MemberSymbol);
            if (ViewModel.SelectedTarget != null && selectedMembers.Count() != 0)
            {
                var result = ViewModel.Service.CreateAnaysisResult(ViewModel);
                if (result.IsValid)
                {
                    DialogResult = true;
                }
                else
                {
                    if (ShowWarningDialog(result))
                    {
                        DialogResult = true;
                    }
                }
            }
        }

        private bool ShowWarningDialog(AnalysisResult result)
        {
            var warningViewModel = new PullMemberUpWarningViewModel(result);
            var warningDialog = new PullMemberUpDialogWarning(warningViewModel);
            return warningDialog.ShowModal().GetValueOrDefault();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void SelecDependentsButton_Click(object sender, RoutedEventArgs e)
        {
            var checkedMembers = ViewModel.SelectedMembersContainer.
                Where(member => member.IsChecked &&
                      member.MemberSymbol.Kind != SymbolKind.Field);
            
            foreach (var member in checkedMembers)
            {
                var dependents = ViewModel.LazyDependentsMap[member.MemberSymbol].Value;

                foreach (var symbol in dependents)
                {
                    var memberView = ViewModel.SymbolToMemberView[symbol];
                    if (memberView.IsSelectable)
                    {
                        memberView.IsChecked = true;
                    }
                }
            }
        }

        private void SelectSymbols(IEnumerable<ISymbol> members)
        {
            foreach (var member in members)
            {
                var index = ViewModel.SelectedMembersContainer.Select(symbolView => symbolView.MemberSymbol).ToList().IndexOf(member);
                ViewModel.SelectedMembersContainer[index].IsChecked = true;
            }
        }

        private void SelectAllButton_Click()
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                if (member.IsSelectable)
                {
                    member.IsChecked = true;
                }
            }
        }

        private void SelectPublic_Click(object sender, RoutedEventArgs e)
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                if (member.IsSelectable && member.MemberSymbol.DeclaredAccessibility == Accessibility.Public)
                {
                    member.IsChecked = true;
                }
            }
        }

        private void DeselectedAll_Click()
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                member.IsChecked = false;
            }
        }

        private void SelectAllAndDeselectedAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (ProceedToSelectAll)
            {
                SelectAllButton_Click();
            }

            ProceedToSelectAll = true;
        }

        private void SelectAllAndDeselectCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DeselectedAll_Click();
            ProceedToSelectAll = true;
        }

        private void MemberSelectionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ProceedToSelectAll = false;
            ViewModel.SelectAllAndDeselectAllChecked = true;
        }
    }
}
