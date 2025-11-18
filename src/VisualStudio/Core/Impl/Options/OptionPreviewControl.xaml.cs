// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options;

internal partial class OptionPreviewControl : AbstractOptionPageControl
{
    internal AbstractOptionPreviewViewModel ViewModel;
    private readonly IServiceProvider _serviceProvider;
    private readonly Func<OptionStore, IServiceProvider, AbstractOptionPreviewViewModel> _createViewModel;

    internal OptionPreviewControl(IServiceProvider serviceProvider, OptionStore optionStore, Func<OptionStore, IServiceProvider, AbstractOptionPreviewViewModel> createViewModel) : base(optionStore)
    {
        InitializeComponent();

        // AutomationDelegatingListView is defined in ServicesVisualStudio, which has
        // InternalsVisibleTo this project. But, the markup compiler doesn't consider the IVT 
        // relationship, so declaring the AutomationDelegatingListView in XAML would require 
        // duplicating that type in this project. Declaring and setting it here avoids the 
        // markup compiler completely, allowing us to reference the internal 
        // AutomationDelegatingListView without issue.
        var listview = new AutomationDelegatingListView();
        listview.Name = "Options";
        listview.SelectionMode = SelectionMode.Single;
        listview.PreviewKeyDown += Options_PreviewKeyDown;
        listview.SelectionChanged += Options_SelectionChanged;
        listview.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath(nameof(ViewModel.Items)) });
        AutomationProperties.SetName(listview, ServicesVSResources.Options);

        listViewContentControl.Content = listview;

        _serviceProvider = serviceProvider;
        _createViewModel = createViewModel;
    }

    private void Options_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var listView = (AutomationDelegatingListView)sender;
        if (listView.SelectedItem is AbstractCheckBoxViewModel checkbox)
        {
            ViewModel.UpdatePreview(checkbox.GetPreview());
        }

        if (listView.SelectedItem is AbstractRadioButtonViewModel radioButton)
        {
            ViewModel.UpdatePreview(radioButton.Preview);
        }
    }

    private void Options_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && e.KeyboardDevice.Modifiers == ModifierKeys.None)
        {
            var listView = (AutomationDelegatingListView)sender;
            if (listView.SelectedItem is AbstractCheckBoxViewModel checkBox)
            {
                checkBox.IsChecked = !checkBox.IsChecked;
                e.Handled = true;
            }

            if (listView.SelectedItem is AbstractRadioButtonViewModel radioButton)
            {
                radioButton.IsChecked = true;
                e.Handled = true;
            }
        }
    }

    internal override void OnLoad()
    {
        this.ViewModel = _createViewModel(this.OptionStore, _serviceProvider);

        // Use the first item's preview.
        var firstItem = this.ViewModel.Items.OfType<CheckBoxOptionViewModel>().First();
        this.ViewModel.SetOptionAndUpdatePreview(firstItem.IsChecked, firstItem.Option, firstItem.GetPreview());

        DataContext = ViewModel;
    }

    internal override void Close()
    {
        base.Close();

        this.ViewModel?.Dispose();
    }
}
