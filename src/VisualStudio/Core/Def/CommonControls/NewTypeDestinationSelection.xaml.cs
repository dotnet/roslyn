// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;

/// <summary>
/// Interaction logic for NewTypeDestinationSelection.xaml
/// </summary>
internal partial class NewTypeDestinationSelection : UserControl
{
    // This allows for binding of the ViewModel as a property in XAML 
    // which can be useful if control is being hosted
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(NewTypeDestinationSelectionViewModel),
        typeof(NewTypeDestinationSelection),
        new PropertyMetadata((s, a) =>
            {
                var control = (NewTypeDestinationSelection)s;
                control.DataContext = a.NewValue;
            })
    );

    public NewTypeDestinationSelectionViewModel ViewModel
    {
        get => (NewTypeDestinationSelectionViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public string GeneratedName => ServicesVSResources.Generated_name_colon;
    public string SelectDestinationFile => ServicesVSResources.Select_destination;
    public string SelectCurrentFileAsDestination => ServicesVSResources.Add_to_current_file;
    public string SelectNewFileAsDestination => ServicesVSResources.New_file_name_colon;
    public string NewTypeName => ServicesVSResources.New_Type_Name_colon;

    public NewTypeDestinationSelection()
    {
        ViewModel = NewTypeDestinationSelectionViewModel.Default;
        DataContext = ViewModel;

        InitializeComponent();
    }

    private void SelectAllInTextBox(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textbox
            && Mouse.LeftButton == MouseButtonState.Released)
        {
            textbox.SelectAll();
        }
    }
}
