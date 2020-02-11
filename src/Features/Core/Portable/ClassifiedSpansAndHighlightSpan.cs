// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal readonly struct ClassifiedSpansAndHighlightSpan
    {
        public const string Key = nameof(ClassifiedSpansAndHighlightSpan);

        public readonly ImmutableArray<ClassifiedSpan> ClassifiedSpans;
        public readonly TextSpan HighlightSpan;

        public ClassifiedSpansAndHighlightSpan(
            ImmutableArray<ClassifiedSpan> classifiedSpans,
            TextSpan highlightSpan)
        {
            ClassifiedSpans = classifiedSpans;
            HighlightSpan = highlightSpan;
        }
    }
}
