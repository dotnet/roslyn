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

        public string InterfaceCantHaveField => ServicesVSResources.Interface_cant_have_field;
            
        public string InterfaceCantHaveAbstractMember => ServicesVSResources.Interface_cant_have_abstract_member;

        private PullMemberUpViewModel ViewModel { get; }

        private bool ProceedToSelectAll { get; set; } = false;

        internal PullMemberUpDialog(PullMemberUpViewModel pullMemberUpViewModel)
        {
            ViewModel = pullMemberUpViewModel;
            DataContext = ViewModel;
            InitializeComponent();
            MemberSelection.SizeChanged += (s, e) =>
            {
                var memberSelectionView = ((GridView)MemberSelection.View);
                memberSelectionView.Columns[0].Width = e.NewSize.Width - memberSelectionView.Columns[1].Width - 50;
            };
        }

        private void TargetMembersContainer_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TargetMembersContainer.SelectedItem is MemberSymbolViewModelGraphNode memberGraphNode)
            {
                ViewModel.SelectedTarget = memberGraphNode;
                if (memberGraphNode.MemberSymbol is INamedTypeSymbol interfaceSymbol &&
                    interfaceSymbol.TypeKind == TypeKind.Interface)
                {
                    DisableAllFieldCheckBox();
                    DisableAllMakeAbstractBox();
                }
                else
                {
                    EnableAllFieldCheckBox();
                    EnableAllMakeAbstractBox();
                }
            }
        }

        private void DisableAllMakeAbstractBox()
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                member.IsMakeAbstractSelectable = false;
            }
        }

        private void EnableAllMakeAbstractBox()
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                if (member.MemberSymbol.Kind != SymbolKind.Field && !member.MemberSymbol.IsAbstract)
                {
                    member.IsMakeAbstractSelectable = true;
                }
            }
        }

        private void DisableAllFieldCheckBox()
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                if (member.MemberSymbol.Kind == SymbolKind.Field)
                {
                    member.IsSelectable = false;
                }
            }
        }

        private void EnableAllFieldCheckBox()
        {
           foreach (var member in ViewModel.SelectedMembersContainer)
            {
                if (member.MemberSymbol.Kind == SymbolKind.Field)
                {
                    member.IsSelectable = true;
                }
            }
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedMembers = ViewModel.SelectedMembersContainer.
                Where(memberSymbolView => memberSymbolView.IsChecked && memberSymbolView.IsSelectable).
                Select(memberSymbolView => memberSymbolView.MemberSymbol);
            if (ViewModel.SelectedTarget != null && selectedMembers.Count() != 0)
            {
                var result = ViewModel.CreateAnaysisResult();
                if (result.IsPullUpOperationCauseError)
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
                var dependents = ViewModel.FindDependents(member.MemberSymbol);

                foreach (var symbol in dependents)
                {
                    var memberView = ViewModel.SymbolToMemberViewMap[symbol];
                    if (memberView.IsSelectable)
                    {
                        memberView.IsChecked = true;
                    }
                }
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
                if (member.IsSelectable)
                {
                    member.IsChecked = false;
                }
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

    internal class BooleanReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }
}
