// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
{
    internal class MenuItemContainerTemplateSelector : ItemContainerTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, ItemsControl parentItemsControl)
        {
            if (item is HeaderMenuItemViewModel)
            {
                return (DataTemplate)parentItemsControl.FindResource("HeaderMenuItemTemplate");
            }

            if (item is TargetMenuItemViewModel)
            {
                return (DataTemplate)parentItemsControl.FindResource("TargetMenuItemTemplate");
            }

            if (item is MemberMenuItemViewModel)
            {
                return (DataTemplate)parentItemsControl.FindResource("MemberMenuItemTemplate");
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}
