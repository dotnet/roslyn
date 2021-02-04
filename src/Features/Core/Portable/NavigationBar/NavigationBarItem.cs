// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NavigationBar
{
    /// <summary>
    /// Base type of all navigation bar items, regardless of language.
    /// </summary>
    [DataContract]
    internal abstract class NavigationBarItem
    {
        [DataMember(Order = 0)]
        public readonly string Text;
        [DataMember(Order = 1)]
        public readonly Glyph Glyph;
        [DataMember(Order = 2)]
        public readonly bool Bolded;
        [DataMember(Order = 3)]
        public readonly bool Grayed;
        [DataMember(Order = 4)]
        public readonly int Indent;
        [DataMember(Order = 5)]
        public readonly ImmutableArray<NavigationBarItem> ChildItems;
        [DataMember(Order = 6)]
        public readonly ImmutableArray<TextSpan> Spans;

        protected NavigationBarItem(
            string text,
            Glyph glyph,
            bool bolded,
            bool grayed,
            int indent,
            ImmutableArray<NavigationBarItem> childItems,
            ImmutableArray<TextSpan> spans)
        {
            this.Text = text;
            this.Glyph = glyph;
            this.Spans = spans;
            this.ChildItems = childItems.NullToEmpty();
            this.Indent = indent;
            this.Bolded = bolded;
            this.Grayed = grayed;
        }
    }
}
