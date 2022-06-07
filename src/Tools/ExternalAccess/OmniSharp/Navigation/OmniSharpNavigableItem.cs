// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Navigation
{
    internal readonly struct OmniSharpNavigableItem
    {
        public OmniSharpNavigableItem(ImmutableArray<TaggedText> displayTaggedParts, Document document, TextSpan sourceSpan)
        {
            DisplayTaggedParts = displayTaggedParts;
            Document = document;
            SourceSpan = sourceSpan;
        }

        public ImmutableArray<TaggedText> DisplayTaggedParts { get; }

        public Document Document { get; }

        public TextSpan SourceSpan { get; }
    }
}
