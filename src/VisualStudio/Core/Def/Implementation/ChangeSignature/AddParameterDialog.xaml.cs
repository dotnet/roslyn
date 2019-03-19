// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    /// <summary>
    /// Interaction logic for AddParameterDialog.xaml
    /// </summary>
    internal partial class AddParameterDialog : DialogWindow
    {
        private readonly AddParameterDialogViewModel _viewModel;

        public string OK { get { return ServicesVSResources.OK; } }
        public string Cancel { get { return ServicesVSResources.Cancel; } }


        public AddParameterDialog(AddParameterDialogViewModel viewModel)
        {
            _viewModel = viewModel;

            InitializeComponent();

            DataContext = viewModel;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.TrySubmit())
            {
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
