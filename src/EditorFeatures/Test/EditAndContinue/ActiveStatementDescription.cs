// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class ActiveStatementsDescription
    {
        public readonly ActiveStatementSpan[] OldSpans;
        public readonly TextSpan[] NewSpans;
        public readonly ImmutableArray<TextSpan>[] OldRegions;
        public readonly ImmutableArray<TextSpan>[] NewRegions;
        public readonly TextSpan?[] TrackingSpans;

        private ActiveStatementsDescription()
        {
            OldSpans = Array.Empty<ActiveStatementSpan>();
            NewSpans = Array.Empty<TextSpan>();
            OldRegions = Array.Empty<ImmutableArray<TextSpan>>();
            NewRegions = Array.Empty<ImmutableArray<TextSpan>>();
            TrackingSpans = null;
        }

        public ActiveStatementsDescription(string oldSource, string newSource)
        {
            OldSpans = GetActiveStatements(oldSource);
            NewSpans = GetActiveSpans(newSource);
            OldRegions = GetExceptionRegions(oldSource, OldSpans.Length);
            NewRegions = GetExceptionRegions(newSource, NewSpans.Length);

            // Tracking spans are marked in the new source since the editor moves them around as the user 
            // edits the source and we get their positions when analyzing the new source.
            TrackingSpans = GetTrackingSpans(newSource, OldSpans.Length);
        }

        internal static readonly ActiveStatementsDescription Empty = new ActiveStatementsDescription();

        internal static string ClearTags(string source)
        {
            return s_tags.Replace(source, m => new string(' ', m.Length));
        }

        private static readonly Regex s_tags = new Regex(
            @"[<][/]?(AS|ER|N|TS)[:][.0-9,]+[>]",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        private static readonly Regex s_activeStatementPattern = new Regex(
            @"[<]AS[:]    (?<Id>[0-9,]+) [>]
              (?<ActiveStatement>.*)
              [<][/]AS[:] (\k<Id>)      [>]",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        public static readonly Regex ExceptionRegionPattern = new Regex(
            @"[<]ER[:]      (?<Id>(?:[0-9]+[.][0-9]+[,]?)+)   [>]
              (?<ExceptionRegion>.*)
              [<][/]ER[:]   (\k<Id>)                 [>]",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        private static readonly Regex s_trackingStatementPattern = new Regex(
            @"[<]TS[:]    (?<Id>[0-9,]+) [>]
              (?<TrackingStatement>.*)
              [<][/]TS[:] (\k<Id>)      [>]",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        internal static ActiveStatementSpan[] GetActiveStatements(string src)
        {
            var text = SourceText.From(src);
            var result = new List<ActiveStatementSpan>();

            int i = 0;
            foreach (var span in GetActiveSpans(src))
            {
                result.Add(new ActiveStatementSpan(
                    (i == 0) ? ActiveStatementFlags.LeafFrame : ActiveStatementFlags.None,
                    text.Lines.GetLinePositionSpan(span)));

                i++;
            }

            return result.ToArray();
        }

        internal static IEnumerable<int> GetIds(Match match)
        {
            return match.Groups["Id"].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse);
        }

        internal static int[] GetIds(string ids)
        {
            return ids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
        }

        internal static IEnumerable<ValueTuple<int, int>> GetDottedIds(Match match)
        {
            return from ids in match.Groups["Id"].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                   let parts = ids.Split('.')
                   select ValueTuple.Create(int.Parse(parts[0]), int.Parse(parts[1]));
        }

        private static IEnumerable<ValueTuple<TextSpan, int[]>> GetSpansRecursive(Regex regex, string contentGroupName, string markedSource, int offset)
        {
            foreach (var match in regex.Matches(markedSource).ToEnumerable())
            {
                var markedSyntax = match.Groups[contentGroupName];
                var ids = GetIds(match.Groups["Id"].Value);
                int absoluteOffset = offset + markedSyntax.Index;

                var span = markedSyntax.Length != 0 ? new TextSpan(absoluteOffset, markedSyntax.Length) : new TextSpan();
                yield return ValueTuple.Create(span, ids);

                foreach (var nestedSpan in GetSpansRecursive(regex, contentGroupName, markedSyntax.Value, absoluteOffset))
                {
                    yield return nestedSpan;
                }
            }
        }

        internal static TextSpan[] GetActiveSpans(string src)
        {
            List<TextSpan> result = new List<TextSpan>();

            foreach (var spanAndIds in GetSpansRecursive(s_activeStatementPattern, "ActiveStatement", src, 0))
            {
                foreach (int id in spanAndIds.Item2)
                {
                    EnsureSlot(result, id);
                    result[id] = spanAndIds.Item1;
                }
            }

            return result.ToArray();
        }

        internal static TextSpan?[] GetTrackingSpans(string src, int count)
        {
            var matches = s_trackingStatementPattern.Matches(src);
            if (matches.Count == 0)
            {
                return null;
            }

            var result = new TextSpan?[count];

            for (int i = 0; i < matches.Count; i++)
            {
                var span = matches[i].Groups["TrackingStatement"];
                foreach (int id in GetIds(matches[i]))
                {
                    result[id] = new TextSpan(span.Index, span.Length);
                }
            }

            return result;
        }

        internal static ImmutableArray<TextSpan>[] GetExceptionRegions(string src, int activeStatementCount)
        {
            var matches = ExceptionRegionPattern.Matches(src);
            var result = new List<TextSpan>[activeStatementCount];

            for (int i = 0; i < matches.Count; i++)
            {
                var exceptionRegion = matches[i].Groups["ExceptionRegion"];

                foreach (var id in GetDottedIds(matches[i]))
                {
                    var activeStatementId = id.Item1;
                    var exceptionRegionId = id.Item2;

                    if (result[activeStatementId] == null)
                    {
                        result[activeStatementId] = new List<TextSpan>();
                    }

                    EnsureSlot(result[activeStatementId], exceptionRegionId);
                    result[activeStatementId][exceptionRegionId] = new TextSpan(exceptionRegion.Index, exceptionRegion.Length);
                }
            }

            return result.Select(r => r.AsImmutableOrEmpty()).ToArray();
        }

        private static void EnsureSlot<T>(List<T> list, int i)
        {
            while (i >= list.Count)
            {
                list.Add(default(T));
            }
        }
    }
}
