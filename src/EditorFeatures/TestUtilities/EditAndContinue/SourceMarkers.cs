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
        @"[<][/]?(AS|ER|N|TS)[:][.0-9,]+[>]",
        RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

    private static readonly Regex s_activeStatementPattern = new(
        @"[<]AS[:]    (?<Id>[0-9,]+) [>]
            (?<ActiveStatement>.*)
          [<][/]AS[:] (\k<Id>)       [>]",
        RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

    public static readonly Regex ExceptionRegionPattern = new(
        @"[<]ER[:]      (?<Id>(?:[0-9]+[.][0-9]+[,]?)+)   [>]
            (?<ExceptionRegion>.*)
          [<][/]ER[:]   (\k<Id>)                          [>]",
        RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

    private static readonly Regex s_trackingStatementPattern = new(
        @"[<]TS[:]    (?<Id>[0-9,]+) [>]
            (?<TrackingStatement>.*)
          [<][/]TS[:] (\k<Id>)       [>]",
        RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

    private static readonly Regex s_nodePattern = new(
        @"[<]N[:]      (?<Id>[0-9]+[.][0-9]+)   [>]
            (?<Node>.*)
          [<][/]N[:]   (\k<Id>)                 [>]",
        RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

    internal static string Clear(string source)
        => s_tags.Replace(source, m => new string(' ', m.Length));

    internal static string[] Clear(string[] sources)
        => sources.Select(Clear).ToArray();

    private static IEnumerable<(int, int)> ParseIds(Match match)
        => from ids in match.Groups["Id"].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
           let parts = ids.Split('.')
           select (int.Parse(parts[0]), (parts.Length > 1) ? int.Parse(parts[1]) : 0);

    private static IEnumerable<(TextSpan Span, ImmutableArray<(int major, int minor)> Ids)> GetSpans(string markedSource, Regex regex, string contentGroupName)
    {
        return Recurse(markedSource, offset: 0);

        IEnumerable<(TextSpan Span, ImmutableArray<(int major, int minor)> Ids)> Recurse(string markedSource, int offset)
        {
            foreach (var match in regex.Matches(markedSource).ToEnumerable())
            {
                var markedSyntax = match.Groups[contentGroupName];
                var ids = ParseIds(match).ToImmutableArray();
                var absoluteOffset = offset + markedSyntax.Index;

                var span = markedSyntax.Length != 0 ? new TextSpan(absoluteOffset, markedSyntax.Length) : new TextSpan();
                yield return (span, ids);

                foreach (var nestedSpan in Recurse(markedSyntax.Value, absoluteOffset))
                {
                    yield return nestedSpan;
                }
            }
        }
    }

    public static IEnumerable<(TextSpan Span, int Id)> GetActiveSpans(string markedSource)
    {
        foreach (var (span, ids) in GetSpans(markedSource, s_activeStatementPattern, "ActiveStatement"))
        {
            foreach (var (major, _) in ids)
            {
                yield return (span, major);
            }
        }
    }

    public static TextSpan[] GetTrackingSpans(string src, int count)
    {
        var matches = s_trackingStatementPattern.Matches(src);
        if (matches.Count == 0)
        {
            return Array.Empty<TextSpan>();
        }

        var result = new TextSpan[count];

        for (var i = 0; i < matches.Count; i++)
        {
            var span = matches[i].Groups["TrackingStatement"];
            foreach (var (id, _) in ParseIds(matches[i]))
            {
                result[id] = new TextSpan(span.Index, span.Length);
            }
        }

        Contract.ThrowIfTrue(result.Any(span => span == default));

        return result;
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
                result[activeStatementId] ??= new List<TextSpan>();
                EnsureSlot(result[activeStatementId], exceptionRegionId);

                var regionText = plainSource.AsSpan().Slice(exceptionRegion.Index, exceptionRegion.Length);
                var start = IndexOfDifferent(regionText, ' ');
                var length = LastIndexOfDifferent(regionText, ' ') - start + 1;

                result[activeStatementId][exceptionRegionId] = new TextSpan(exceptionRegion.Index + start, length);
            }
        }

        return result.Select(r => r.AsImmutableOrEmpty()).ToImmutableArray();
    }

    public static ImmutableArray<ImmutableArray<TextSpan>> GetNodeSpans(string markedSource)
    {
        var result = new List<List<TextSpan>>();

        foreach (var (span, ids) in GetSpans(markedSource, s_nodePattern, "Node"))
        {
            Debug.Assert(ids.Length == 1);

            var (major, minor) = ids[0];

            EnsureSlot(result, major);
            result[major] ??= new List<TextSpan>();
            EnsureSlot(result[major], minor);
            result[major][minor] = span;
        }

        return result.Select(r => r.AsImmutableOrEmpty()).AsImmutableOrEmpty();
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
