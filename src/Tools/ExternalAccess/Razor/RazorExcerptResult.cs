// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal readonly struct RazorExcerptResult
    {
        public readonly SourceText Content;

        public readonly TextSpan MappedSpan;

        public readonly ImmutableArray<ClassifiedSpan> ClassifiedSpans;

        public readonly Document Document;

        public readonly TextSpan Span;

        public RazorExcerptResult(SourceText content, TextSpan mappedSpan, ImmutableArray<ClassifiedSpan> classifiedSpans, Document document, TextSpan span)
        {
            Content = content;
            MappedSpan = mappedSpan;
            ClassifiedSpans = classifiedSpans;

            Document = document;
            Span = span;
        }
    }
}
