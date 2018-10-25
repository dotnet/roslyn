// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    /// <summary>
    /// Interaction logic for PushMemberUpDialogxaml.xaml
    /// </summary>
    internal partial class PullMemberUpDialogWarningxaml : DialogWindow
    {
        public string Back => ServicesVSResources.Back;

        public string Finish => ServicesVSResources.Finish;

        public string PullMembersUpTitle => ServicesVSResources.Pull_Up_Members;

        public string TextTitle => ServicesVSResources.Review_Problems;

        public string FoundProblem => ServicesVSResources.Found_Problems;

        internal PullMemberUpDialogWarningxaml(PullMemberUpWarningViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;

        private void BackButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
