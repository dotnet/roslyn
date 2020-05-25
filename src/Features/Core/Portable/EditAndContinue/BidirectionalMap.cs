// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Differencing;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct BidirectionalMap<T>
    {
        public readonly IReadOnlyDictionary<T, T> Forward;
        public readonly IReadOnlyDictionary<T, T> Reverse;

        public BidirectionalMap(IReadOnlyDictionary<T, T> forward, IReadOnlyDictionary<T, T> reverse)
        {
            Forward = forward;
            Reverse = reverse;
        }

        public BidirectionalMap(IEnumerable<KeyValuePair<T, T>> entries)
        {
            var map = new Dictionary<T, T>();
            var reverseMap = new Dictionary<T, T>();

            foreach (var entry in entries)
            {
                map.Add(entry.Key, entry.Value);
                reverseMap.Add(entry.Value, entry.Key);
            }

            Forward = map;
            Reverse = reverseMap;
        }

        public static BidirectionalMap<T> FromMatch(Match<T> match)
            => new BidirectionalMap<T>(match.Matches, match.ReverseMatches);

        public bool IsDefaultOrEmpty => Forward == null || Forward.Count == 0;
    }
}
