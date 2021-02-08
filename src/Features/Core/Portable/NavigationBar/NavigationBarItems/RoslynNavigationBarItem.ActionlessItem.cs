// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    internal abstract partial class RoslynNavigationBarItem
    {
        /// <summary>
        /// An item that is displayed and can be chosen but which has no action.
        /// </summary>
        public class ActionlessItem : RoslynNavigationBarItem
        {
            public ActionlessItem(
                string text,
                Glyph glyph,
                ImmutableArray<TextSpan> spans,
                ImmutableArray<RoslynNavigationBarItem> childItems = default,
                int indent = 0,
                bool bolded = false,
                bool grayed = false)
                : base(RoslynNavigationBarItemKind.Actionless, text, glyph, bolded, grayed, indent, childItems, spans)
            {
            }

            protected internal override SerializableNavigationBarItem Dehydrate()
                => SerializableNavigationBarItem.ActionlessItem(Text, Glyph, Spans, SerializableNavigationBarItem.Dehydrate(ChildItems), Indent, Bolded, Grayed);
        }
    }
}
