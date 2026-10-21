// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using Microsoft.VisualStudio.LanguageServices.InheritanceMargin;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph;

/// <summary>
/// Interaction logic for InheritanceMarginContextMenu.xaml
/// </summary>
internal partial class InheritanceMarginContextMenu : Shell.Controls.ContextMenu
{
    public InheritanceMarginContextMenu()
    {
        InitializeComponent();
    }

    protected override DependencyObject GetContainerForItemOverride()
        => new InheritanceMarginMenuItem();

    private void TargetsSubmenu_OnOpen(object sender, RoutedEventArgs e)
    {
        InheritanceMarginLogger.LogInheritanceTargetsMenuOpen();
    }
}
