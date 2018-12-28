﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Windows;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.WarningDialog
{
    /// <summary>
    /// Interaction logic for PushMemberUpWarningDialog.xaml
    /// </summary>
    internal partial class PullMemberUpWarningDialog : DialogWindow
    {
        public string Back => ServicesVSResources.Back;
        public string Finish => ServicesVSResources.Finish;
        public string WarningDialogTitle => ServicesVSResources.Review_Fixes;
        public string Description => ServicesVSResources.Additional_fixes_are_needed_to_complete_the_refactoring_Review_fixes_below;

        internal PullMemberUpWarningDialog(PullMemberUpWarningViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log(FunctionId.PullMembersUpWarning_UserProceedToFinish);
            DialogResult = true;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log(FunctionId.PullMembersUpWarning_UserGoBack);
            DialogResult = false;
        }
    }
}
