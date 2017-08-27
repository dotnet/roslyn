// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            return "{ \"" + Old.Replace("\"", "\\\"") + "\", \"" + New.Replace("\"", "\\\"") + "\" }";
        }
    }

    public class MatchingPairs : IEnumerable<MatchingPair>
    {
        public readonly List<MatchingPair> Pairs;

        public MatchingPairs()
        {
            Pairs = new List<MatchingPair>();
        }

        public MatchingPairs(IEnumerable<MatchingPair> pairs)
        {
            Pairs = pairs.ToList();
        }

        public void Add(string old, string @new)
        {
            Pairs.Add(new MatchingPair { Old = old, New = @new });
        }

        public IEnumerator GetEnumerator()
        {
            return Pairs.GetEnumerator();
        }

        IEnumerator<MatchingPair> IEnumerable<MatchingPair>.GetEnumerator()
        {
            return Pairs.GetEnumerator();
        }

        public void AssertEqual(MatchingPairs actual)
        {
            AssertEx.Equal(this, actual, itemSeparator: ",\r\n");
        }
    }
}
