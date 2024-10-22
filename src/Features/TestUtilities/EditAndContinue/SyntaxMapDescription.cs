// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal sealed class SyntaxMapDescription
{
    public readonly struct Mapping(ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)> nodeSpans, ImmutableArray<TextSpan> newSpans, Match<SyntaxNode>? match)
    {
        public readonly ImmutableArray<(TextSpan oldSpan, TextSpan newSpan)> Spans = nodeSpans;

        public LinePosition NodePosition(int index)
        {
            Debug.Assert(match != null, "Must be created from edit script");
            Debug.Assert(index < Spans.Length, $"Marker <N:{index}> not present in new source");

            return match.NewRoot.SyntaxTree.GetLineSpan(Spans[index].newSpan).StartLinePosition;
        }

        public LinePosition Position(int index)
        {
            Debug.Assert(match != null, "Must be created from edit script");
            Debug.Assert(index < newSpans.Length, $"Marker <S:{index}> not present in new source");

            return match.NewRoot.SyntaxTree.GetLineSpan(newSpans[index]).StartLinePosition;
        }
    }

    // Spans from <N:major:minor> markers. Indexed by major and then minor.
    // Used for matching spans between old source and new source (syntax map).
    public readonly ImmutableArray<ImmutableArray<TextSpan>> OldNodeSpans;
    public readonly ImmutableArray<ImmutableArray<TextSpan>> NewNodeSpans;

    // Spans from <S:index> markers. Used for general span marking in new source.
    public readonly ImmutableArray<TextSpan> NewSpans;

    public readonly Match<SyntaxNode>? Match;

    public SyntaxMapDescription(string oldSource, string newSource, Match<SyntaxNode>? match = null)
    {
        OldNodeSpans = SourceMarkers.GetNodeSpans(oldSource);
        NewNodeSpans = SourceMarkers.GetNodeSpans(newSource);

        Assert.Equal(OldNodeSpans.Length, NewNodeSpans.Length);
        for (var i = 0; i < OldNodeSpans.Length; i++)
        {
            Assert.Equal(OldNodeSpans[i].Length, NewNodeSpans[i].Length);
        }

        NewSpans = SourceMarkers.GetSpans(newSource, tagName: "S");
        Match = match;
    }

    public Mapping Single()
    {
        Debug.Assert(OldNodeSpans.Length == 1);
        return this[0];
    }

    public Mapping this[int i]
        => new(OldNodeSpans[i].ZipAsArray(NewNodeSpans[i], static (oldSpan, newSpan) => (oldSpan, newSpan)), NewSpans, Match);
}
