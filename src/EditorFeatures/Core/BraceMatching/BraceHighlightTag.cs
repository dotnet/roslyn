// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.BraceMatching
{
    internal sealed class BraceHighlightTag : TextMarkerTag, IEquatable<BraceHighlightTag>
    {
        public static readonly BraceHighlightTag StartTag = new(navigateToStart: true);
        public static readonly BraceHighlightTag EndTag = new(navigateToStart: false);

        public bool NavigateToStart { get; }

        private BraceHighlightTag(bool navigateToStart)
            : base(ClassificationTypeDefinitions.BraceMatchingName)
        {
            this.NavigateToStart = navigateToStart;
        }

        public override bool Equals(object obj)
            => Equals(obj as BraceHighlightTag);

        public bool Equals(BraceHighlightTag other)
            => other != null && NavigateToStart == other.NavigateToStart;

        public override int GetHashCode()
            => NavigateToStart.GetHashCode();
    }
}
