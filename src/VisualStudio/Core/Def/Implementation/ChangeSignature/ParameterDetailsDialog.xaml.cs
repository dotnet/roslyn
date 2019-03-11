// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    /// <summary>
    /// Interaction logic for ParameterDetailsDialog.xaml
    /// </summary>
    internal partial class ParameterDetailsDialog : DialogWindow
    {
        private readonly ParameterDetailsDialogViewModel _viewModel;

        // Expose localized strings for binding
        public string ParameterDetailsDialogTitle { get { return ServicesVSResources.Parameter_Details; } }
        public string OK { get { return ServicesVSResources.OK; } }
        public string Cancel { get { return ServicesVSResources.Cancel; } }

        // Nested dialog, so don't specify a helpTopic
        internal ParameterDetailsDialog(ParameterDetailsDialogViewModel viewModel)
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
