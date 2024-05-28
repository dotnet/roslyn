// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking;

/// <summary>
/// Interaction logic for ValueTrackingRoot.xaml
/// </summary>
internal partial class ValueTrackingRoot : UserControl
{
    public string EmptyText => ServicesVSResources.Select_an_appropriate_symbol_to_start_value_tracking;

    public ValueTrackingRoot()
    {
        InitializeComponent();
    }

    public void SetChild(FrameworkElement? child)
    {
        RootGrid.Children.Clear();

        if (child is null)
        {
            RootGrid.Children.Add(EmptyTextMessage);
            EmptyTextMessage.Visibility = Visibility.Visible;
        }
        else
        {
            EmptyTextMessage.Visibility = Visibility.Collapsed;
            RootGrid.Children.Add(child);
        }
    }
}
