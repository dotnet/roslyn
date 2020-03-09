// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
