// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences;

/// <summary>
/// Interaction logic for NamingStyleDialog.xaml
/// </summary>
internal partial class NamingStyleDialog : DialogWindow
{
    private readonly NamingStyleViewModel _viewModel;

    public string DialogTitle => ServicesVSResources.Naming_Style;
    public string NamingStyleTitleLabelText => ServicesVSResources.Naming_Style_Title_colon;
    public string RequiredPrefixLabelText => ServicesVSResources.Required_Prefix_colon;
    public string RequiredSuffixLabelText => ServicesVSResources.Required_Suffix_colon;
    public string WordSeparatorLabelText => ServicesVSResources.Word_Separator_colon;
    public string CapitalizationLabelText => ServicesVSResources.Capitalization_colon;
    public string SampleIdentifierLabelText => ServicesVSResources.Sample_Identifier_colon;
    public string OK => ServicesVSResources.OK;
    public string Cancel => EditorFeaturesResources.Cancel;

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
        => DialogResult = false;
}
