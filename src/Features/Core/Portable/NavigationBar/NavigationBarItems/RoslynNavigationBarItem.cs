// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    /// <summary>
    /// Base type of all C#/VB navigation bar items.  Only for use internally to roslyn.
    /// </summary>
    [DataContract]
    internal abstract partial class RoslynNavigationBarItem : NavigationBarItem
    {
        [DataMember(Order = 7)]
        public readonly RoslynNavigationBarItemKind Kind;

        protected RoslynNavigationBarItem(
            string text,
            Glyph glyph,
            bool bolded,
            bool grayed,
            int indent,
            ImmutableArray<TextSpan> spans,
            ImmutableArray<NavigationBarItem> childItems,
            RoslynNavigationBarItemKind kind)
            : base(text, glyph, bolded, grayed, indent, childItems, spans)
        {
            Kind = kind;
        }
    }
}
