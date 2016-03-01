// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    /// <summary>
    /// Interaction logic for NamingStyleDialog.xaml
    /// </summary>
    internal partial class NamingStyleDialog : DialogWindow
    {
        private readonly NamingStyleViewModel _viewModel;

        public string DialogTitle => ServicesVSResources.NamingStyleDialogTitle;
        public string NamingStyleTitleLabelText => ServicesVSResources.NamingStyleTitleLabel;
        public string RequiredPrefixLabelText => ServicesVSResources.RequiredPrefixLabel;
        public string RequiredSuffixLabelText => ServicesVSResources.RequiredSuffixLabel;
        public string WordSeparatorLabelText => ServicesVSResources.WordSeparatorLabel;
        public string CapitalizationLabelText => ServicesVSResources.CapitalizationLabel;
        public string SampleIdentifierLabelText => ServicesVSResources.SampleIdentifierLabel;
        public string OK => ServicesVSResources.OK;
        public string Cancel => ServicesVSResources.Cancel;

        internal NamingStyleDialog(NamingStyleViewModel viewModel)
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
