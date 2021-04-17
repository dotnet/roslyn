// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class ActiveStatementsDescription
    {
        internal static readonly ActiveStatementsDescription Empty = new();

        public readonly ImmutableArray<UnmappedActiveStatement> OldStatements;
        public readonly ActiveStatementsMap OldStatementsMap;
        public readonly ImmutableArray<SourceFileSpan> NewMappedSpans;
        public readonly ImmutableArray<ImmutableArray<SourceFileSpan>> NewMappedRegions;
        public readonly ImmutableArray<LinePositionSpan> OldUnmappedTrackingSpans;

        private ActiveStatementsDescription()
        {
            OldStatements = ImmutableArray<UnmappedActiveStatement>.Empty;
            NewMappedSpans = ImmutableArray<SourceFileSpan>.Empty;
            OldStatementsMap = ActiveStatementsMap.Empty;
            NewMappedRegions = ImmutableArray<ImmutableArray<SourceFileSpan>>.Empty;
            OldUnmappedTrackingSpans = ImmutableArray<LinePositionSpan>.Empty;
        }

        public ActiveStatementsDescription(string oldSource, SyntaxTree oldTree, string newSource, SyntaxTree newTree, ActiveStatementFlags[]? flags)
        {
            var oldText = SourceText.From(oldSource);
            var newText = SourceText.From(newSource);

            var oldDocumentMap = new Dictionary<string, List<ActiveStatement>>();
            var oldActiveStatementMarkers = GetActiveSpans(oldSource).ToArray();
            var newActiveStatementMarkers = GetActiveSpans(newSource).ToArray();

            var activeStatementCount = Math.Max(
                (oldActiveStatementMarkers.Length == 0) ? -1 : oldActiveStatementMarkers.Max(m => m.Id),
                (newActiveStatementMarkers.Length == 0) ? -1 : newActiveStatementMarkers.Max(m => m.Id)) + 1;

            var oldExceptionRegionMarkers = GetExceptionRegions(oldSource, activeStatementCount);
            var newExceptionRegionMarkers = GetExceptionRegions(newSource, activeStatementCount);

            OldStatements = oldActiveStatementMarkers.Aggregate(
                new List<UnmappedActiveStatement>(),
                (list, marker) =>
                {
                    var (unmappedSpan, ordinal) = marker;
                    var mappedSpan = oldTree.GetMappedLineSpan(unmappedSpan);
                    var documentActiveStatements = oldDocumentMap.GetOrAdd(mappedSpan.Path, path => new List<ActiveStatement>());
                    var statementFlags = (flags != null) ? flags[ordinal] : (ordinal == 0) ? ActiveStatementFlags.IsLeafFrame : ActiveStatementFlags.IsNonLeafFrame;
                    var exceptionRegions = oldExceptionRegionMarkers[ordinal].SelectAsArray(unmappedRegionSpan => (SourceFileSpan)oldTree.GetMappedLineSpan(unmappedRegionSpan));

                    var unmappedActiveStatement = new UnmappedActiveStatement(
                        unmappedSpan,
                        new ActiveStatement(
                            ordinal,
                            documentOrdinal: documentActiveStatements.Count,
                            statementFlags,
                            mappedSpan,
                            instructionId: default),
                        new ActiveStatementExceptionRegions(exceptionRegions, isActiveStatementCovered: true));

                    documentActiveStatements.Add(unmappedActiveStatement.Statement);
                    return SetListItem(list, ordinal, unmappedActiveStatement);
                }).ToImmutableArray();

            OldStatementsMap = new ActiveStatementsMap(
                documentPathMap: oldDocumentMap.ToImmutableDictionary(e => e.Key, e => e.Value.OrderBy(ActiveStatementsMap.Comparer).ToImmutableArray()),
                instructionMap: ActiveStatementsMap.Empty.InstructionMap);

            var newMappedSpans = new ArrayBuilder<SourceFileSpan>();
            var newMappedRegions = new ArrayBuilder<ImmutableArray<SourceFileSpan>>();

            newMappedSpans.ZeroInit(activeStatementCount);
            newMappedRegions.ZeroInit(activeStatementCount);

            // initialize with deleted spans (they will retain their file path):
            foreach (var oldStatement in OldStatements)
            {
                newMappedSpans[oldStatement.Statement.Ordinal] = new SourceFileSpan(oldStatement.Statement.FilePath, default);
                newMappedRegions[oldStatement.Statement.Ordinal] = ImmutableArray<SourceFileSpan>.Empty;
            }

            // update with spans marked in the new source:
            foreach (var (unmappedSpan, ordinal) in newActiveStatementMarkers)
            {
                newMappedSpans[ordinal] = newTree.GetMappedLineSpan(unmappedSpan);
                newMappedRegions[ordinal] = newExceptionRegionMarkers[ordinal].SelectAsArray(span => (SourceFileSpan)newTree.GetMappedLineSpan(span));
            }

            NewMappedSpans = newMappedSpans.ToImmutable();
            NewMappedRegions = newMappedRegions.ToImmutable();

            // Tracking spans are marked in the new source since the editor moves them around as the user 
            // edits the source and we get their positions when analyzing the new source.
            // The EnC analyzer uses old tracking spans as hints to find matching nodes.
            OldUnmappedTrackingSpans = GetTrackingSpans(newSource, activeStatementCount).
                SelectAsArray(s => newText.Lines.GetLinePositionSpan(s));
        }

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
            => match.Groups["Id"].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse);

        internal static int[] GetIds(string ids)
            => ids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();

        internal static IEnumerable<(int, int)> GetDottedIds(Match match)
            => from ids in match.Groups["Id"].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
               let parts = ids.Split('.')
               select (int.Parse(parts[0]), int.Parse(parts[1]));

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

        internal static TextSpan[] GetTrackingSpans(string src, int count)
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
                foreach (var id in GetIds(matches[i]))
                {
                    result[id] = new TextSpan(span.Index, span.Length);
                }
            }

            Contract.ThrowIfTrue(result.Any(span => span == default));

            return result;
        }

        internal static ImmutableArray<ImmutableArray<TextSpan>> GetExceptionRegions(string src, int activeStatementCount)
        {
            var matches = ExceptionRegionPattern.Matches(src);
            var result = new List<TextSpan>[activeStatementCount];

            for (var i = 0; i < matches.Count; i++)
            {
                var exceptionRegion = matches[i].Groups["ExceptionRegion"];

                foreach (var (activeStatementId, exceptionRegionId) in GetDottedIds(matches[i]))
                {
                    result[activeStatementId] ??= new List<TextSpan>();
                    EnsureSlot(result[activeStatementId], exceptionRegionId);
                    result[activeStatementId][exceptionRegionId] = new TextSpan(exceptionRegion.Index, exceptionRegion.Length);
                }
            }

            return result.Select(r => r.AsImmutableOrEmpty()).ToImmutableArray();
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
}
