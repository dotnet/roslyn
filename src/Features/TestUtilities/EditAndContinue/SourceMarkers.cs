// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal static class SourceMarkers
{
    private static readonly Regex s_tags = new(
        "[<]  (?<IsEnd>/?)  (?<Name>(AS|ER|N|S|TS))[:]  (?<Id>[.0-9,]+)  (?<IsStartAndEnd>/?)  [>]", RegexOptions.IgnorePatternWhitespace);

    public static readonly Regex ExceptionRegionPattern = new(
        """
        [<]ER[:]      (?<Id>(?:[0-9]+[.][0-9]+[,]?)+)   [>]
                    (?<ExceptionRegion>.*)
                  [<][/]ER[:]   (\k<Id>)                          [>]
        """,
        RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

    private static readonly Regex s_trackingStatementPattern = new(
        """
        [<]TS[:]    (?<Id>[0-9,]+) [>]
                    (?<TrackingStatement>.*)
                  [<][/]TS[:] (\k<Id>)       [>]
        """,
        RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

    internal static string Clear(string source)
        => s_tags.Replace(source, m => new string(' ', m.Length));

    internal static string[] Clear(string[] sources)
        => [.. sources.Select(Clear)];

    private static IEnumerable<(int, int)> ParseIds(Match match)
        => from ids in match.Groups["Id"].Value.Split([','], StringSplitOptions.RemoveEmptyEntries)
           let parts = ids.Split('.')
           select (int.Parse(parts[0]), (parts.Length > 1) ? int.Parse(parts[1]) : -1);

    private static IEnumerable<((int major, int minor) id, TextSpan span)> ParseSpans(string markedSource, string tagName)
    {
        // id -> content start index
        var tagMap = new Dictionary<(int major, int minor), (int start, int end)>();

        foreach (var match in s_tags.Matches(markedSource).ToEnumerable())
        {
            if (match.Groups["Name"].Value != tagName)
            {
                continue;
            }

            var isStartingTag = match.Groups["IsEnd"].Value == "" || match.Groups["IsStartAndEnd"].Value != "";
            var isEndingTag = match.Groups["IsEnd"].Value != "" || match.Groups["IsStartAndEnd"].Value != "";
            Contract.ThrowIfFalse(isStartingTag || isEndingTag);

            foreach (var id in ParseIds(match))
            {
                if (isStartingTag && isEndingTag)
                {
                    tagMap.Add(id, (match.Index, match.Index));
                }
                else if (isStartingTag)
                {
                    tagMap.Add(id, (match.Index + match.Length, -1));
                }
                else
                {
                    tagMap[id] = (tagMap[id].start, match.Index);
                }
            }
        }

        foreach (var (id, (start, end)) in tagMap.OrderBy(k => k.Key))
        {
            Contract.ThrowIfFalse(end >= 0, $"Missing ending tag for {id}");
            yield return (id, TextSpan.FromBounds(start, end));
        }
    }

    public static IEnumerable<(TextSpan Span, int Id)> GetActiveSpans(string markedSource)
        => ParseSpans(markedSource, tagName: "AS").Select(s => (s.span, s.id.major));

    public static (int id, TextSpan span)[] GetTrackingSpans(string src)
    {
        var matches = s_trackingStatementPattern.Matches(src);
        if (matches.Count == 0)
        {
            return [];
        }

        var result = new List<(int id, TextSpan span)>();

        for (var i = 0; i < matches.Count; i++)
        {
            var span = matches[i].Groups["TrackingStatement"];
            foreach (var (id, _) in ParseIds(matches[i]))
            {
                result.Add((id, new TextSpan(span.Index, span.Length)));
            }
        }

        Contract.ThrowIfTrue(result.Any(span => span == default));

        return [.. result];
    }

    public static ImmutableArray<ImmutableArray<TextSpan>> GetExceptionRegions(string markedSource)
    {
        var matches = ExceptionRegionPattern.Matches(markedSource);
        var plainSource = Clear(markedSource);

        var result = new List<List<TextSpan>>();

        for (var i = 0; i < matches.Count; i++)
        {
            var exceptionRegion = matches[i].Groups["ExceptionRegion"];

            foreach (var (activeStatementId, exceptionRegionId) in ParseIds(matches[i]))
            {
                EnsureSlot(result, activeStatementId);
                result[activeStatementId] ??= [];
                EnsureSlot(result[activeStatementId], exceptionRegionId);

                var regionText = plainSource.AsSpan().Slice(exceptionRegion.Index, exceptionRegion.Length);
                var start = IndexOfDifferent(regionText, ' ');
                var length = LastIndexOfDifferent(regionText, ' ') - start + 1;

                result[activeStatementId][exceptionRegionId] = new TextSpan(exceptionRegion.Index + start, length);
            }
        }

        return [.. result.Select(r => r.AsImmutableOrEmpty())];
    }

    public static ImmutableArray<ImmutableArray<TextSpan>> GetNodeSpans(string markedSource)
    {
        var result = new List<List<TextSpan>>();

        foreach (var ((major, minor), span) in ParseSpans(markedSource, tagName: "N"))
        {
            var (i, j) = (minor >= 0) ? (major, minor) : (0, major);

            EnsureSlot(result, i);
            result[i] ??= [];
            EnsureSlot(result[i], j);
            result[i][j] = span;
        }

        return [.. result.Select(r => r.AsImmutableOrEmpty())];
    }

    public static ImmutableArray<TextSpan> GetSpans(string markedSource, string tagName)
    {
        var result = new List<TextSpan>();

        foreach (var ((major, minor), span) in ParseSpans(markedSource, tagName))
        {
            Debug.Assert(minor == -1);
            EnsureSlot(result, major);
            result[major] = span;
        }

        return [.. result];
    }

    public static int IndexOfDifferent(ReadOnlySpan<char> span, char c)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] != c)
            {
                return i;
            }
        }

        return -1;
    }

    public static int LastIndexOfDifferent(ReadOnlySpan<char> span, char c)
    {
        for (var i = span.Length - 1; i >= 0; i--)
        {
            if (span[i] != c)
            {
                return i;
            }
        }

        return -1;
    }

    public static List<T> SetListItem<T>(List<T> list, int i, T item)
    {
        EnsureSlot(list, i);
        list[i] = item;
        return list;
    }

    public static void EnsureSlot<T>(List<T> list, int i)
    {
        while (i >= list.Count)
        {
            list.Add(default!);
        }
    }
}
