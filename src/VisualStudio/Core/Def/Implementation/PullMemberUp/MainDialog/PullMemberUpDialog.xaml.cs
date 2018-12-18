// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
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

        public string SelectMembers => ServicesVSResources.Select_members_colon;

        public string SelectDestination => ServicesVSResources.Select_destination_colon;

        public string Description => ServicesVSResources.Select_destination_and_members_to_pull_up;

        public string SelectPublic => ServicesVSResources.Select_Public;

        public string SelectDependents => ServicesVSResources.Select_Dependents;

        public string Members => ServicesVSResources.Members;

        public string MakeAbstract => ServicesVSResources.Make_abstract;

        public string InterfaceCannotHaveField => ServicesVSResources.Interface_can_not_have_field;

        public string SpinnerToolTip => ServicesVSResources.Calculating_dependents;

        internal PullMemberUpViewModel ViewModel { get; }

        internal PullMemberUpDialog(PullMemberUpViewModel pullMemberUpViewModel)
        {
            ViewModel = pullMemberUpViewModel;
            DataContext = pullMemberUpViewModel;
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

        private void SelecDependentsButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectDependents();
        }

        private void SelectPublic_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectPublicMembers();
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectAllMembers();
        }

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.DeSelectAllMembers();
        }

        private void MemberSelectionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ViewModel.EnableOrDisableOkButton();
            ViewModel.CheckAndSetStateOfSelectAllCheckBox();
        }

        private void MemberSelectionCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.EnableOrDisableOkButton();
            ViewModel.CheckAndSetStateOfSelectAllCheckBox();
        }

        private void Destination_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (Destination.SelectedItem is BaseTypeTreeNodeViewModel memberGraphNode)
            {
                ViewModel.SelectedDestination = memberGraphNode;
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
