﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    public struct MatchingPair
    {
        public string Old;
        public string New;

        public override string ToString()
            => "{ \"" + Old.Replace("\"", "\\\"") + "\", \"" + New.Replace("\"", "\\\"") + "\" }";
    }

    public class MatchingPairs : IEnumerable<MatchingPair>
    {
        public readonly List<MatchingPair> Pairs;

        public MatchingPairs()
            => Pairs = new List<MatchingPair>();

        public MatchingPairs(IEnumerable<MatchingPair> pairs)
            => Pairs = pairs.ToList();

        public void Add(string old, string @new)
            => Pairs.Add(new MatchingPair { Old = old, New = @new });

        public IEnumerator GetEnumerator()
            => Pairs.GetEnumerator();

        IEnumerator<MatchingPair> IEnumerable<MatchingPair>.GetEnumerator()
            => Pairs.GetEnumerator();

        public void AssertEqual(MatchingPairs actual)
            => AssertEx.Equal(this, actual, itemSeparator: ",\r\n");
    }
}
