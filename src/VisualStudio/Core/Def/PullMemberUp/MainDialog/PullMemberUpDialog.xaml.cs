// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

#nullable disable

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;
using Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.WarningDialog;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog
{
    /// <summary>
    /// Interaction logic for PullMemberUpDialog.xaml
    /// </summary>
    internal partial class PullMemberUpDialog : DialogWindow
    {
        public string OK => ServicesVSResources.OK;
        public string Cancel => ServicesVSResources.Cancel;
        public string PullMembersUpTitle => ServicesVSResources.Pull_Members_Up;
        public string SelectMembers => ServicesVSResources.Select_members_colon;
        public string SelectDestination => ServicesVSResources.Select_destination_colon;
        public string Description => ServicesVSResources.Select_destination_and_members_to_pull_up;

        public PullMemberUpDialogViewModel ViewModel { get; }

        public MemberSelection MemberSelectionControl { get; }

        public PullMemberUpDialog(PullMemberUpDialogViewModel pullMemberUpViewModel)
        {
            ViewModel = pullMemberUpViewModel;
            DataContext = pullMemberUpViewModel;

            MemberSelectionControl = new MemberSelection(ViewModel.MemberSelectionViewModel);

            // Set focus to first tab control when the window is loaded
            Loaded += (s, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var options = ViewModel.CreatePullMemberUpOptions();
            if (options.PullUpOperationNeedsToDoExtraChanges)
            {
                if (ShowWarningDialog(options))
                {
                    DialogResult = true;
                }
            }
            else
            {
                DialogResult = true;
            }
        }

        private static bool ShowWarningDialog(PullMembersUpOptions result)
        {
            var warningViewModel = new PullMemberUpWarningViewModel(result);
            var warningDialog = new PullMemberUpWarningDialog(warningViewModel);
            return warningDialog.ShowModal().GetValueOrDefault();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Destination_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DestinationTreeView.SelectedItem is BaseTypeTreeNodeViewModel memberGraphNode)
            {
                ViewModel.SelectedDestination = memberGraphNode;
            }
        }
    }
}
