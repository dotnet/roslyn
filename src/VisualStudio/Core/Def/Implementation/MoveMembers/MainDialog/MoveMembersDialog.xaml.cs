// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.Controls;
using Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.WarningDialog;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.MainDialog
{
    /// <summary>
    /// Interaction logic for MoveMembersDialog.xaml
    /// </summary>
    internal partial class MoveMembersDialog : DialogWindow
    {
        public string OK => ServicesVSResources.OK;
        public string Cancel => ServicesVSResources.Cancel;
        public string SelectMembers => ServicesVSResources.Select_members_colon;
        public string SelectDestination => ServicesVSResources.Select_destination_colon;
        public string Description { get; }
        public bool HasDescription => !string.IsNullOrEmpty(Description);
        public string SelectPublic => ServicesVSResources.Select_Public;
        public string SelectDependents => ServicesVSResources.Select_Dependents;
        public string MembersHeader => ServicesVSResources.Members;
        public string MakeAbstractHeader => ServicesVSResources.Make_abstract;

        public MoveMembersDialogViewModel ViewModel { get; }

        public MoveMembersDialog(string title, string description, MoveMembersDialogViewModel pullMemberUpViewModel)
        {
            Title = title;
            Description = description;
            ViewModel = pullMemberUpViewModel;
            DataContext = pullMemberUpViewModel;

            // Set focus to first tab control when the window is loaded
            Loaded += (s, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            InitializeComponent();

            this.DestinationSelectionGroupBox.Content = ViewModel.SelectDestinationViewModel.CreateUserControl();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var analyzedMembers = ViewModel.AnalyzeCheckedMembers();
            var needsChanges = analyzedMembers.Any(member => member.MoveMemberNeedsToDoExtraChanges);
            if (needsChanges)
            {
                var warningViewModel = new MoveMembersWarningViewModel(ViewModel.SelectedDestination, analyzedMembers);
                var warningDialog = new MoveMembersWarningDialog(warningViewModel);
                if (warningDialog.ShowModal().GetValueOrDefault())
                {
                    DialogResult = true;
                }
            }
            else
            {
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void SelectDependentsButton_Click(object sender, RoutedEventArgs e)
            => ViewModel.SelectDependents();

        private void SelectPublic_Click(object sender, RoutedEventArgs e)
            => ViewModel.SelectPublicMembers();

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
            => ViewModel.SelectAllMembers();

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
            => ViewModel.DeSelectAllMembers();

        private void MemberSelectionCheckBox_Checked(object sender, RoutedEventArgs e)
            => ViewModel.SetStatesOfOkButtonAndSelectAllCheckBox();

        private void MemberSelectionCheckBox_Unchecked(object sender, RoutedEventArgs e)
            => ViewModel.SetStatesOfOkButtonAndSelectAllCheckBox();

        public TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        public class TestAccessor
        {
            private readonly MoveMembersDialog _dialog;
            public TestAccessor(MoveMembersDialog dialog)
            {
                _dialog = dialog;
            }

            public Button OKButton => _dialog.OKButton;
            public Button CancelButton => _dialog.CancelButton;
            public CheckBox SelectAllCheckBox => _dialog.SelectAllCheckBox;
            public RadioButton DestinationCurrentFileSelectionRadioButton => MoveToNewTypeControl.DestinationCurrentFileSelectionRadioButton;
            public RadioButton DestinationNewFileSelectionRadioButton => MoveToNewTypeControl.DestinationNewFileSelectionRadioButton;
            public MoveToNewTypeControl MoveToNewTypeControl => (MoveToNewTypeControl)_dialog.DestinationSelectionGroupBox.Content;
            public DataGrid MemberSelectionGrid => _dialog.MemberSelection;
        }
    }
}
