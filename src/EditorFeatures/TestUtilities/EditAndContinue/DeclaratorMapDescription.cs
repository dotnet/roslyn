// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    public sealed class SyntaxMapDescription
    {
        public readonly ImmutableArray<ImmutableArray<TextSpan>> OldSpans;
        public readonly ImmutableArray<ImmutableArray<TextSpan>> NewSpans;

        public SyntaxMapDescription(string oldSource, string newSource)
        {
            OldSpans = GetSpans(oldSource);
            NewSpans = GetSpans(newSource);

            Assert.Equal(OldSpans.Length, NewSpans.Length);
            for (var i = 0; i < OldSpans.Length; i++)
            {
                Assert.Equal(OldSpans[i].Length, NewSpans[i].Length);
            }
        }

        private static readonly Regex s_statementPattern = new Regex(
            @"[<]N[:]      (?<Id>[0-9]+[.][0-9]+)   [>]
              (?<Node>.*)
              [<][/]N[:]   (\k<Id>)                 [>]", RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        internal static ImmutableArray<ImmutableArray<TextSpan>> GetSpans(string src)
        {
            var matches = s_statementPattern.Matches(src);
            var result = new List<List<TextSpan>>();

            for (var i = 0; i < matches.Count; i++)
            {
                var stmt = matches[i].Groups["Node"];
                var id = matches[i].Groups["Id"].Value.Split('.');
                var id0 = int.Parse(id[0]);
                var id1 = int.Parse(id[1]);

                EnsureSlot(result, id0);

                if (result[id0] == null)
                {
                    result[id0] = new List<TextSpan>();
                }

                EnsureSlot(result[id0], id1);
                result[id0][id1] = new TextSpan(stmt.Index, stmt.Length);
            }

            return result.Select(r => r.AsImmutableOrEmpty()).AsImmutableOrEmpty();
        }

        internal IEnumerable<KeyValuePair<TextSpan, TextSpan>> this[int i]
        {
            get
            {
                for (var j = 0; j < OldSpans[i].Length; j++)
                {
                    yield return KeyValuePairUtil.Create(OldSpans[i][j], NewSpans[i][j]);
                }
            }
        }

        private static void EnsureSlot<T>(List<T> list, int i)
        {
            while (i >= list.Count)
            {
                list.Add(default);
            }
        }
    }
}
