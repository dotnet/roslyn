// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using System.Windows.Media;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal static class DependencyObjectExtensions
    {
        public static DependencyObject TryGetParent(this DependencyObject obj)
        {
            return (obj is Visual) ? VisualTreeHelper.GetParent(obj) : null;
        }

        public static T GetParentOfType<T>(this DependencyObject element) where T : Visual
        {
            var parent = element.TryGetParent();
            if (parent is T)
            {
                return (T)parent;
            }

            if (parent == null)
            {
                return null;
            }

            return parent.GetParentOfType<T>();
        }
    }
}
