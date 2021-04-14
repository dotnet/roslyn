// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
{
    /// <summary>
    /// The template selector used by inheritance margin context menu.
    /// By default a context menu would only create MenuItem for each item. Using this selector to
    /// let it create separator if the view model is a separator view model.
    /// </summary>
    internal class MenuItemContainerTemplateSelector : ItemContainerTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, ItemsControl parentItemsControl)
        {
            if (item is SeparatorViewModel separatorViewModel)
            {
                if (separatorViewModel.IsFirstMenuItem)
                {
                    return (DataTemplate)parentItemsControl.FindResource("TargetSeparatorDataTemplate");
                }
                else
                {
                    return (DataTemplate)parentItemsControl.FindResource("TargetSeparatorDataTemplateWithBorder");
                }
            }

            if (item is TargetMenuItemViewModel)
            {
                return (DataTemplate)parentItemsControl.FindResource("TargetDataTemplate");
            }

            if (item is MemberMenuItemViewModel)
            {
                return (DataTemplate)parentItemsControl.FindResource("MemberDataTemplate");
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}
