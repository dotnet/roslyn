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
        public readonly ActiveStatement[] OldStatements;
        public readonly TextSpan[] NewSpans;
        public readonly ImmutableArray<TextSpan>[] OldRegions;
        public readonly ImmutableArray<TextSpan>[] NewRegions;
        public readonly TextSpan?[] OldTrackingSpans;

        private ActiveStatementsDescription()
        {
            OldStatements = Array.Empty<ActiveStatement>();
            NewSpans = Array.Empty<TextSpan>();
            OldRegions = Array.Empty<ImmutableArray<TextSpan>>();
            NewRegions = Array.Empty<ImmutableArray<TextSpan>>();
            OldTrackingSpans = null;
        }

        private static readonly DocumentId s_dummyDocumentId = DocumentId.CreateNewId(ProjectId.CreateNewId());

        public ActiveStatementsDescription(string oldSource, string newSource)
        {
            var oldText = SourceText.From(oldSource);

            OldStatements = GetActiveSpans(oldSource).Aggregate(
                new List<ActiveStatement>(),
                (list, s) => SetListItem(list, s.Id, CreateActiveStatement(s.Span, s.Id, oldText, s_dummyDocumentId))).ToArray();

            NewSpans = GetActiveSpans(newSource).Aggregate(
                new List<TextSpan>(),
                (list, s) => SetListItem(list, s.Id, s.Span)).ToArray();

            OldRegions = GetExceptionRegions(oldSource, OldStatements.Length);
            NewRegions = GetExceptionRegions(newSource, NewSpans.Length);

            // Tracking spans are marked in the new source since the editor moves them around as the user 
            // edits the source and we get their positions when analyzing the new source.
            // The EnC analyzer uses old trackign spans as hints to find matching nodes.
            // After an edit the tracking spans are updated to match new active statements.
            OldTrackingSpans = GetTrackingSpans(newSource, OldStatements.Length);
        }

        internal static readonly ActiveStatementsDescription Empty = new ActiveStatementsDescription();

        internal static string ClearTags(string source)
            => s_tags.Replace(source, m => new string(' ', m.Length));

        internal static string[] ClearTags(string[] sources)
            => sources.Select(ClearTags).ToArray();

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

        private static IEnumerable<(TextSpan Span, int[] Ids)> GetSpansRecursive(Regex regex, string contentGroupName, string markedSource, int offset)
        {
            foreach (var match in regex.Matches(markedSource).ToEnumerable())
            {
                var markedSyntax = match.Groups[contentGroupName];
                var ids = GetIds(match.Groups["Id"].Value);
                var absoluteOffset = offset + markedSyntax.Index;

                var span = markedSyntax.Length != 0 ? new TextSpan(absoluteOffset, markedSyntax.Length) : new TextSpan();
                yield return (span, ids);

                foreach (var nestedSpan in GetSpansRecursive(regex, contentGroupName, markedSyntax.Value, absoluteOffset))
                {
                    yield return nestedSpan;
                }
            }
        }

        internal static IEnumerable<(TextSpan Span, int Id)> GetActiveSpans(string markedSource)
        {
            foreach (var (span, ids) in GetSpansRecursive(s_activeStatementPattern, "ActiveStatement", markedSource, offset: 0))
            {
                foreach (var id in ids)
                {
                    yield return (span, id);
                }
            }
        }

        private static readonly ImmutableArray<Guid> s_dummyThreadIds = ImmutableArray.Create(default(Guid));

        internal static ActiveStatement CreateActiveStatement(ActiveStatementFlags flags, LinePositionSpan span, DocumentId documentId)
            => new ActiveStatement(
                ordinal: 0,
                primaryDocumentOrdinal: 0,
                ImmutableArray.Create(documentId),
                flags,
                span,
                instructionId: default,
                s_dummyThreadIds);

        internal static ActiveStatement CreateActiveStatement(TextSpan span, int id, SourceText text, DocumentId documentId)
            => CreateActiveStatement(
                (id == 0) ? ActiveStatementFlags.IsLeafFrame : ActiveStatementFlags.IsNonLeafFrame,
                text.Lines.GetLinePositionSpan(span),
                documentId);

        internal static TextSpan?[] GetTrackingSpans(string src, int count)
        {
            var matches = s_trackingStatementPattern.Matches(src);
            if (matches.Count == 0)
            {
                return null;
            }

            var result = new TextSpan?[count];

            for (var i = 0; i < matches.Count; i++)
            {
                var span = matches[i].Groups["TrackingStatement"];
                foreach (var id in GetIds(matches[i]))
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

            for (var i = 0; i < matches.Count; i++)
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
                list.Add(default);
            }
        }
    }
}
