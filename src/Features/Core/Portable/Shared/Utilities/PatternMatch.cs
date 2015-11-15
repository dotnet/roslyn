// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal struct PatternMatch : IComparable<PatternMatch>
    {
        /// <summary>
        /// The weight of a CamelCase match. A higher number indicates a more accurate match.
        /// </summary>
        public int? CamelCaseWeight { get; }

        /// <summary>
        /// True if this was a case sensitive match.
        /// </summary>
        public bool IsCaseSensitive { get; }

        /// <summary>
        /// The type of match that occurred.
        /// </summary>
        public PatternMatchKind Kind { get; }

        private readonly bool _punctuationStripped;

        internal PatternMatch(PatternMatchKind resultType, bool punctuationStripped, bool isCaseSensitive, int? camelCaseWeight = null)
            : this()
        {
            this.Kind = resultType;
            this.IsCaseSensitive = isCaseSensitive;
            this.CamelCaseWeight = camelCaseWeight;
            _punctuationStripped = punctuationStripped;

            if ((resultType == PatternMatchKind.CamelCase) != camelCaseWeight.HasValue)
            {
                throw new ArgumentException("A CamelCase weight must be specified if and only if the resultType is CamelCase.");
            }
        }

        public int CompareTo(PatternMatch other)
        {
            int diff;
            if ((diff = CompareType(this, other)) != 0 ||
                (diff = CompareCamelCase(this, other)) != 0 ||
                (diff = CompareCase(this, other)) != 0 ||
                (diff = ComparePunctuation(this, other)) != 0)
            {
                return diff;
            }

            return 0;
        }

        internal static int ComparePunctuation(PatternMatch result1, PatternMatch result2)
        {
            // Consider a match to be better if it was successful without stripping punctuation
            // versus a match that had to strip punctuation to succeed.
            if (result1._punctuationStripped != result2._punctuationStripped)
            {
                return result1._punctuationStripped ? 1 : -1;
            }

            return 0;
        }

        internal static int CompareCase(PatternMatch result1, PatternMatch result2)
        {
            if (result1.IsCaseSensitive != result2.IsCaseSensitive)
            {
                return result1.IsCaseSensitive ? -1 : 1;
            }

            return 0;
        }

        internal static int CompareType(PatternMatch result1, PatternMatch result2)
        {
            return result1.Kind - result2.Kind;
        }

        internal static int CompareCamelCase(PatternMatch result1, PatternMatch result2)
        {
            if (result1.Kind == PatternMatchKind.CamelCase && result2.Kind == PatternMatchKind.CamelCase)
            {
                // Swap the values here.  If result1 has a higher weight, then we want it to come
                // first.
                return result2.CamelCaseWeight.Value - result1.CamelCaseWeight.Value;
            }

            return 0;
        }
    }
}
