// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
