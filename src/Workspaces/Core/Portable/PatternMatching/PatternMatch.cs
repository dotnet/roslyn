﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PatternMatching
{
    internal struct PatternMatch : IComparable<PatternMatch>
    {
        /// <summary>
        /// True if this was a case sensitive match.
        /// </summary>
        public bool IsCaseSensitive { get; }

        /// <summary>
        /// The type of match that occurred.
        /// </summary>
        public PatternMatchKind Kind { get; }

        /// <summary>
        /// The spans in the original text that were matched.  Only returned if the 
        /// pattern matcher is asked to collect these spans.
        /// </summary>
        public ImmutableArray<TextSpan> MatchedSpans { get; }

        private readonly bool _punctuationStripped;

        internal PatternMatch(
            PatternMatchKind resultType,
            bool punctuationStripped,
            bool isCaseSensitive,
            TextSpan? matchedSpan)
            : this(resultType, punctuationStripped, isCaseSensitive,
                   matchedSpan == null ? ImmutableArray<TextSpan>.Empty : ImmutableArray.Create(matchedSpan.Value))
        {
        }

        internal PatternMatch(
            PatternMatchKind resultType,
            bool punctuationStripped,
            bool isCaseSensitive,
            ImmutableArray<TextSpan> matchedSpans)
            : this()
        {
            this.Kind = resultType;
            this.IsCaseSensitive = isCaseSensitive;
            this.MatchedSpans = matchedSpans;
            _punctuationStripped = punctuationStripped;
        }

        public PatternMatch WithMatchedSpans(ImmutableArray<TextSpan> matchedSpans)
            => new(Kind, _punctuationStripped, IsCaseSensitive, matchedSpans);

        public int CompareTo(PatternMatch other)
            => CompareTo(other, ignoreCase: false);

        public int CompareTo(PatternMatch other, bool ignoreCase)
            => ComparerWithState.CompareTo(this, other, ignoreCase, s_comparers);

        private static readonly ImmutableArray<Func<PatternMatch, bool, IComparable>> s_comparers =
            ImmutableArray.Create<Func<PatternMatch, bool, IComparable>>(
                // Compare types
                (p, b) => p.Kind,
                // Compare cases
                (p, b) => !b && !p.IsCaseSensitive,
                // Consider a match to be better if it was successful without stripping punctuation
                // versus a match that had to strip punctuation to succeed.
                (p, b) => p._punctuationStripped);
    }
}
