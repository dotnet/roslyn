// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Differencing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct BidirectionalMap<T>
        where T : notnull
    {
        public readonly IReadOnlyDictionary<T, T> Forward;
        public readonly IReadOnlyDictionary<T, T> Reverse;

        public BidirectionalMap(IReadOnlyDictionary<T, T> forward, IReadOnlyDictionary<T, T> reverse)
        {
            Contract.ThrowIfFalse(forward.Count == reverse.Count);
            Forward = forward;
            Reverse = reverse;
        }

        public BidirectionalMap<T> With(BidirectionalMap<T> map)
        {
            if (map.Forward.Count == 0)
            {
                return this;
            }

            var count = Forward.Count + map.Forward.Count;
            var forward = new Dictionary<T, T>(count);
            var reverse = new Dictionary<T, T>(count);

            foreach (var entry in Forward)
            {
                forward.Add(entry.Key, entry.Value);
                reverse.Add(entry.Value, entry.Key);
            }

            foreach (var entry in map.Forward)
            {
                forward.Add(entry.Key, entry.Value);
                reverse.Add(entry.Value, entry.Key);
            }

            return new BidirectionalMap<T>(forward, reverse);
        }

        public BidirectionalMap<T> WithMatch(Match<T> match)
            => With(BidirectionalMap<T>.FromMatch(match));

        public static BidirectionalMap<T> FromMatch(Match<T> match)
            => new(match.Matches, match.ReverseMatches);
    }
}
