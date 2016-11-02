// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.PatternMatching
{
    /// <summary>
    /// Pattern matching results returned when calling <see cref="PatternMatcher.GetMatches(string, string)"/>
    /// Specifically, this type individually provides the matches produced when matching against the
    /// 'candidate' text and the 'container' text.
    /// </summary>
    internal struct PatternMatches
    {
        public static readonly PatternMatches Empty = new PatternMatches(
            ImmutableArray<PatternMatch>.Empty, ImmutableArray<PatternMatch>.Empty);

        public readonly ImmutableArray<PatternMatch> CandidateMatches;
        public readonly ImmutableArray<PatternMatch> ContainerMatches;

        public PatternMatches(ImmutableArray<PatternMatch> candidateMatches,
                              ImmutableArray<PatternMatch> containerMatches = default(ImmutableArray<PatternMatch>))
        {
            CandidateMatches = candidateMatches.NullToEmpty();
            ContainerMatches = containerMatches.NullToEmpty();
        }

        public bool IsEmpty => CandidateMatches.IsEmpty && ContainerMatches.IsEmpty;

        internal bool All(Func<PatternMatch, bool> predicate)
        {
            return CandidateMatches.All(predicate) && ContainerMatches.All(predicate);
        }

        internal bool Any(Func<PatternMatch, bool> predicate)
        {
            return CandidateMatches.Any(predicate) || ContainerMatches.Any(predicate);
        }
    }
}