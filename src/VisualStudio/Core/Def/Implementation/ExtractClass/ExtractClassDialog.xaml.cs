﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ExtractClass
{
    /// <summary>
    /// Interaction logic for ExtractClassDialog.xaml
    /// </summary>
    internal partial class ExtractClassDialog : DialogWindow
    {
        public string OK => ServicesVSResources.OK;
        public string Cancel => ServicesVSResources.Cancel;
        public string SelectMembers => ServicesVSResources.Select_members_colon;
        public string ExtractClassTitle => ServicesVSResources.Extract_Base_Class;
        public ExtractClassViewModel ViewModel { get; }
        public MemberSelection MemberSelectionControl { get; }
        public NewTypeDestinationSelection DestinationSelectionControl { get; }

        public ExtractClassDialog(ExtractClassViewModel viewModel)
        {
            ViewModel = viewModel;

            DataContext = ViewModel;

            MemberSelectionControl = new MemberSelection(ViewModel.MemberSelectionViewModel);
            DestinationSelectionControl = new NewTypeDestinationSelection(ViewModel.DestinationViewModel);

            InitializeComponent();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.TrySubmit())
            {
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;
    }
}
