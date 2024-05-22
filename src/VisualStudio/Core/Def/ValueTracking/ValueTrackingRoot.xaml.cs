// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
