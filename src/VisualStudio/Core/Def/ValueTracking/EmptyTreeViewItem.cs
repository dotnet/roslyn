// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    internal class EmptyTreeViewItem : TreeViewItemBase
    {
        public static EmptyTreeViewItem Instance { get; } = new();

        private EmptyTreeViewItem()
        {
        }
    }
}
