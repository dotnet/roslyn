﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

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
            => new PatternMatch(Kind, _punctuationStripped, IsCaseSensitive, matchedSpans);

        public int CompareTo(PatternMatch other)
            => CompareTo(other, ignoreCase: false);

        public int CompareTo(PatternMatch other, bool ignoreCase)
        {
            int diff;
            if ((diff = CompareType(this, other)) != 0 ||
                (diff = CompareCase(this, other, ignoreCase)) != 0 ||
                (diff = ComparePunctuation(this, other)) != 0)
            {
                return diff;
            }

            return 0;
        }

        private static int ComparePunctuation(PatternMatch result1, PatternMatch result2)
        {
            // Consider a match to be better if it was successful without stripping punctuation
            // versus a match that had to strip punctuation to succeed.
            if (result1._punctuationStripped != result2._punctuationStripped)
            {
                return result1._punctuationStripped ? 1 : -1;
            }

            return 0;
        }

        private static int CompareCase(PatternMatch result1, PatternMatch result2, bool ignoreCase)
        {
            if (!ignoreCase)
            {
                if (result1.IsCaseSensitive != result2.IsCaseSensitive)
                {
                    return result1.IsCaseSensitive ? -1 : 1;
                }
            }

            return 0;
        }

        private static int CompareType(PatternMatch result1, PatternMatch result2)
            => result1.Kind - result2.Kind;
    }
}
