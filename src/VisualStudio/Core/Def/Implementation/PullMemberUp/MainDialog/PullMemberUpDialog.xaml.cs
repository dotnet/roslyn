// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.WarningDialog;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog
{
    /// <summary>
    /// Interaction logic for PullhMemberUpDialog.xaml
    /// </summary>
    internal partial class PullMemberUpDialog : DialogWindow
    {
        public string OK => ServicesVSResources.OK;

        public string Cancel => ServicesVSResources.Cancel;

        public string PullMembersUpTitle => ServicesVSResources.Pull_Members_Up;

        public string SelectMembers => ServicesVSResources.Select_members;

        public string SelectDestination => ServicesVSResources.Select_destinations;

        public string Description => ServicesVSResources.Select_destination_and_members_to_pull_up;

        public string SelectPublic => ServicesVSResources.Select_Public;

        public string SelectDependents => ServicesVSResources.Select_Dependents;

        public string Members => ServicesVSResources.Members;

        public string MakeAbstract => ServicesVSResources.Make_abstract;

        public string InterfaceCantHaveField => ServicesVSResources.Interface_cant_have_field;
            
        public string InterfaceCantHaveAbstractMember => ServicesVSResources.Interface_cant_have_abstract_member;

        public string SpinnerToolTip => ServicesVSResources.Calculating_dependents;

        public PullMemberUpViewModel ViewModel { get; }

        internal PullMemberUpDialog(PullMemberUpViewModel pullMemberUpViewModel)
        {
            ViewModel = pullMemberUpViewModel;
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            var result = ViewModel.CreateAnaysisResult();
            if (result.PullUpOperationCausesError)
            {
                if (ShowWarningDialog(result))
                {
                    DialogResult = true;
                }
            }
            else
            {
                DialogResult = true;
            }
        }

        private bool ShowWarningDialog(PullMembersUpAnalysisResult result)
        {
            var warningViewModel = new PullMemberUpWarningViewModel(result);
            var warningDialog = new PullMemberUpWarningDialog(warningViewModel);
            return warningDialog.ShowModal().GetValueOrDefault();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private async void SelecDependentsButton_Click(object sender, RoutedEventArgs e)
        {
            var checkedMembers = ViewModel.Members.
                Where(member => member.IsChecked && member.IsCheckable);
            
            foreach (var member in checkedMembers)
            {
                var dependentsTask = ViewModel.DependentsMap[member.MemberSymbol].GetValueAsync(ViewModel.CancellationTokenSource.Token);
                if (!dependentsTask.IsCompleted)
                {
                    // Finding dependents task may be expensive, so if it is not completed,
                    // Show a spiner with tooltip saying it is being calculated, disable the button, after it is completed, resume the button.
                    member.SpinnerVisibility = Visibility.Visible;
                    member.IsCheckable = false;
                }

                var dependents = await dependentsTask.ConfigureAwait(false);
                // Resume the button and hide spinner
                member.IsCheckable = true;
                member.SpinnerVisibility = Visibility.Hidden;

                foreach (var symbol in dependents)
                {
                    var memberView = ViewModel.SymbolToMemberViewMap[symbol];
                    if (memberView.IsCheckable)
                    {
                        memberView.IsChecked = true;
                    }
                }
            }
        }

        private void SelectPublic_Click(object sender, RoutedEventArgs e)
        {
            foreach (var member in ViewModel.Members)
            {
                if (member.IsCheckable && member.MemberSymbol.DeclaredAccessibility == Accessibility.Public)
                {
                    member.IsChecked = true;
                }
            }
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var member in ViewModel.Members)
            {
                if (member.IsCheckable)
                {
                    member.IsChecked = true;
                }
            }
        }

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var member in ViewModel.Members)
            {
                if (member.IsCheckable)
                {
                    member.IsChecked = false;
                }
            }
        }

        private void MemberSelectionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            EnableOrDisableOkButton();
            CheckAndSetStateOfSelectAllCheckBox();
        }

        private void MemberSelectionCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            EnableOrDisableOkButton();
            CheckAndSetStateOfSelectAllCheckBox();
        }

        private void CheckAndSetStateOfSelectAllCheckBox()
        {
            var checkableMembers = ViewModel.Members.WhereAsArray(member => member.IsCheckable);
            if (checkableMembers.All(member => member.IsChecked))
            {
                ViewModel.SelectAllCheckBoxState = true;
                ViewModel.ThreeStateEnable = false;
            }
            else if (checkableMembers.Any(member => member.IsChecked))
            {
                ViewModel.ThreeStateEnable = true;
                ViewModel.SelectAllCheckBoxState = null;
            }
            else
            {
                ViewModel.SelectAllCheckBoxState = false;
                ViewModel.ThreeStateEnable = false;
            }
        }

        private void EnableOrDisableOkButton()
        {
            var selectedMembers = ViewModel.Members.
                WhereAsArray(memberSymbolView => memberSymbolView.IsChecked && memberSymbolView.IsCheckable).
                SelectAsArray(memberSymbolView => memberSymbolView.MemberSymbol);
            ViewModel.OkButtonEnabled = ViewModel.SelectedDestination != null && selectedMembers.Count() != 0 ? true : false;
        }

        private void Destination_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (Destination.SelectedItem is BaseTypeTreeNodeViewModel memberGraphNode)
            {
                ViewModel.SelectedDestination = memberGraphNode;
                EnableOrDisableOkButton();
                if (memberGraphNode.MemberSymbol is INamedTypeSymbol interfaceSymbol &&
                    interfaceSymbol.TypeKind == TypeKind.Interface)
                {
                    // Disable field check box and make abstract check box
                    foreach (var member in ViewModel.Members)
                    {
                        member.IsMakeAbstractCheckable = false;
                        if (member.MemberSymbol.Kind == SymbolKind.Field)
                        {
                            member.IsCheckable = false;
                        }
                    }
                }
                else
                {
                    // Resume them back
                    foreach (var member in ViewModel.Members)
                    {
                        if (member.MemberSymbol.Kind != SymbolKind.Field && !member.MemberSymbol.IsAbstract)
                        {
                            member.IsMakeAbstractCheckable = true;
                        }

                        if (member.MemberSymbol.Kind == SymbolKind.Field)
                        {
                            member.IsCheckable = true;
                        }
                    }
                }
            }
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
