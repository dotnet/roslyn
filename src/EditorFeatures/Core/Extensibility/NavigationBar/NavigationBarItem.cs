// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    [DataContract]
    internal abstract class NavigationBarItem
    {
        [DataMember(Order = 0)]
        public string Text { get; private set; }
        [DataMember(Order = 1)]
        public Glyph Glyph { get; private set; }
        [DataMember(Order = 2)]
        public bool Bolded { get; private set; }
        [DataMember(Order = 3)]
        public bool Grayed { get; private set; }
        [DataMember(Order = 4)]
        public int Indent { get; private set; }
        [DataMember(Order = 5)]
        public IList<NavigationBarItem> ChildItems { get; private set; }

        [DataMember(Order = 6)]
        public IList<TextSpan> Spans { get; internal set; }
        internal IList<ITrackingSpan> TrackingSpans { get; set; }

        public NavigationBarItem(
            string text,
            Glyph glyph,
            IList<TextSpan> spans,
            IList<NavigationBarItem> childItems = null,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
        {
            this.Text = text;
            this.Glyph = glyph;
            this.Spans = spans;
            this.ChildItems = childItems ?? SpecializedCollections.EmptyList<NavigationBarItem>();
            this.Indent = indent;
            this.Bolded = bolded;
            this.Grayed = grayed;
        }

        internal void InitializeTrackingSpans(ITextSnapshot textSnapshot)
        {
            this.TrackingSpans = this.Spans.Select(s => textSnapshot.CreateTrackingSpan(s.ToSpan(), SpanTrackingMode.EdgeExclusive)).ToList();

            if (this.ChildItems != null)
            {
                this.ChildItems.Do(i => i.InitializeTrackingSpans(textSnapshot));
            }
        }
    }
}
