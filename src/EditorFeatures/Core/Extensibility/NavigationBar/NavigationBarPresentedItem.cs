﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// The items that are displayed in the Navigation Bar when it is not expanded. They are never
    /// indented and cannot be used as the target of navigation.
    /// </summary>
    internal class NavigationBarPresentedItem : NavigationBarItem
    {
        public NavigationBarPresentedItem(
            string text,
            Glyph glyph,
            ImmutableArray<ITrackingSpan> trackingSpans,
            ITrackingSpan? navigationTrackingSpan,
            ImmutableArray<NavigationBarItem> childItems,
            bool bolded,
            bool grayed)
            : base(
                  text,
                  glyph,
                  trackingSpans,
                  navigationTrackingSpan,
                  childItems,
                  indent: 0,
                  bolded: bolded,
                  grayed: grayed)
        {
        }
    }
}
