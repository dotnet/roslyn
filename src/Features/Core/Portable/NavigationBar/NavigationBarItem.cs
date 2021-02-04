// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NavigationBar
{
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
        public ImmutableArray<TextSpan> Spans { get; internal set; }

        public NavigationBarItem(
            string text,
            Glyph glyph,
            ImmutableArray<TextSpan> spans,
            ImmutableArray<NavigationBarItem> childItems = default,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
        {
            this.Text = text;
            this.Glyph = glyph;
            this.Spans = spans;
            this.ChildItems = childItems.NullToEmpty();
            this.Indent = indent;
            this.Bolded = bolded;
            this.Grayed = grayed;
        }

        //internal void InitializeTrackingSpans(ITextSnapshot textSnapshot)
        //{
        //    this.TrackingSpans = this.Spans.Select(s => textSnapshot.CreateTrackingSpan(s.ToSpan(), SpanTrackingMode.EdgeExclusive)).ToList();

        //    if (this.ChildItems != null)
        //    {
        //        this.ChildItems.Do(i => i.InitializeTrackingSpans(textSnapshot));
        //    }
        //}
    }
}
