// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;

/// <summary>
/// Interaction logic for MemberSelection.xaml
/// </summary>
internal partial class MemberSelection : UserControl
{
    public string SelectDependents => ServicesVSResources.Select_Dependents;
    public string SelectPublic => ServicesVSResources.Select_Public;
    public string MembersHeader => ServicesVSResources.Members;
    public string MakeAbstractHeader => ServicesVSResources.Make_abstract;
    public string SelectAll => ServicesVSResources.Select_All;
    public string DeselectAll => ServicesVSResources.Deselect_All;

    public MemberSelectionViewModel ViewModel { get; }

    public MemberSelection(MemberSelectionViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        InitializeComponent();

        UpdateAbstractColumnVisibility();
    }

    private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MemberSelectionViewModel.ShowMakeAbstract))
        {
            UpdateAbstractColumnVisibility();
        }
    }

    private void UpdateAbstractColumnVisibility()
    {
        AbstractColumn.Visibility = ViewModel.ShowMakeAbstract ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SelectDependentsButton_Click(object sender, RoutedEventArgs e)
        => ViewModel.SelectDependents();

    private void SelectPublic_Click(object sender, RoutedEventArgs e)
        => ViewModel.SelectPublic();

    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        => ViewModel.SelectAll();

    private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        => ViewModel.DeselectAll();
}
