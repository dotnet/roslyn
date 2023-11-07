// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
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
            OldSpans = SourceMarkers.GetNodeSpans(oldSource);
            NewSpans = SourceMarkers.GetNodeSpans(newSource);

            Assert.Equal(OldSpans.Length, NewSpans.Length);
            for (var i = 0; i < OldSpans.Length; i++)
            {
                Assert.Equal(OldSpans[i].Length, NewSpans[i].Length);
            }
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
    }
}
