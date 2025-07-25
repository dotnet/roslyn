// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Windows;
using System.Windows.Media;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

internal static class DependencyObjectExtensions
{
    extension(DependencyObject obj)
    {
        public DependencyObject TryGetParent()
        {
            return (obj is Visual) ? VisualTreeHelper.GetParent(obj) : null;
        }
    }

    extension(DependencyObject element)
    {
        public T GetParentOfType<T>() where T : Visual
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
