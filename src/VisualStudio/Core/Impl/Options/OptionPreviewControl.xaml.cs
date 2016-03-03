// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal partial class OptionPreviewControl : AbstractOptionPageControl
    {
        internal AbstractOptionPreviewViewModel ViewModel;
        private readonly IServiceProvider _serviceProvider;
        private readonly Func<OptionSet, IServiceProvider, AbstractOptionPreviewViewModel> _createViewModel;


        internal OptionPreviewControl(IServiceProvider serviceProvider, Func<OptionSet, IServiceProvider, AbstractOptionPreviewViewModel> createViewModel) : base(serviceProvider)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _createViewModel = createViewModel;
        }

        private void Options_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = (ListView)sender;
            var checkbox = listView.SelectedItem as CheckBoxOptionViewModel;
            if (checkbox != null)
            {
                ViewModel.UpdatePreview(checkbox.GetPreview());
            }

            var radioButton = listView.SelectedItem as AbstractRadioButtonViewModel;
            if (radioButton != null)
            {
                ViewModel.UpdatePreview(radioButton.Preview);
            }

            var checkBoxWithCombo = listView.SelectedItem as CheckBoxWithComboOptionViewModel;
            if (checkBoxWithCombo != null)
            {
                ViewModel.UpdatePreview(checkBoxWithCombo.GetPreview());
            }
        }

        private void Options_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && e.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
                var listView = (ListView)sender;
                var checkBox = listView.SelectedItem as CheckBoxOptionViewModel;
                if (checkBox != null)
                {
                    checkBox.IsChecked = !checkBox.IsChecked;
                    e.Handled = true;
                }

                var radioButton = listView.SelectedItem as AbstractRadioButtonViewModel;
                if (radioButton != null)
                {
                    radioButton.IsChecked = true;
                    e.Handled = true;
                }

                var checkBoxWithCombo = listView.SelectedItem as CheckBoxWithComboOptionViewModel;
                if (checkBoxWithCombo != null)
                {
                    checkBoxWithCombo.IsChecked = !checkBoxWithCombo.IsChecked;
                    e.Handled = true;
                }
            }
        }

        internal override void SaveSettings()
        {
            var optionSet = this.OptionService.GetOptions();
            var changedOptions = this.ViewModel.ApplyChangedOptions(optionSet);

            this.OptionService.SetOptions(changedOptions);
            OptionLogger.Log(optionSet, changedOptions);
        }

        internal override void LoadSettings()
        {
            this.ViewModel = _createViewModel(this.OptionService.GetOptions(), _serviceProvider);

            // Use the first item's preview.
            var firstItem = this.ViewModel.Items.OfType<CheckBoxOptionViewModel>().First();
            this.ViewModel.SetOptionAndUpdatePreview(firstItem.IsChecked, firstItem.Option, firstItem.GetPreview());

            DataContext = ViewModel;
        }

        internal override void Close()
        {
            base.Close();

            if (this.ViewModel != null)
            {
                this.ViewModel.Dispose();
            }
        }
    }
}
