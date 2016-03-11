// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal partial class GridOptionPreviewControl : AbstractOptionPageControl
    {
        internal AbstractOptionPreviewViewModel ViewModel;
        private readonly IServiceProvider _serviceProvider;
        private readonly Func<OptionSet, IServiceProvider, AbstractOptionPreviewViewModel> _createViewModel;

        internal GridOptionPreviewControl(IServiceProvider serviceProvider, 
            Func<OptionSet, IServiceProvider, 
            AbstractOptionPreviewViewModel> createViewModel) 
            : base(serviceProvider)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _createViewModel = createViewModel;
        }

        private void Options_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            var codeStyleItem = (AbstractCodeStyleOptionViewModel)dataGrid.SelectedItem;

            if (codeStyleItem != null && codeStyleItem.NotificationsAvailable)
            {
                ViewModel.UpdatePreview(codeStyleItem.GetPreview());
            }
        }

        private void Options_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // TODO: make the combo to drop down on space or some key.
            if (e.Key == Key.Space && e.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
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

            var firstItem = this.ViewModel.CodeStyleItems.OfType<AbstractCodeStyleOptionViewModel>().First();
            this.ViewModel.SetOptionAndUpdatePreview(firstItem.SelectedPreference.IsChecked, firstItem.Option, firstItem.GetPreview());

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