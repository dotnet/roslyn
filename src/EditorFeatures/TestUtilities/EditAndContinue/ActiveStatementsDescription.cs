﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
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

        public ActiveStatementsDescription(string oldMarkedSource, string newMarkedSource, Func<string, SyntaxTree> syntaxTreeFactory, ActiveStatementFlags[]? flags)
        {
            var oldSource = ClearTags(oldMarkedSource);
            var newSource = ClearTags(newMarkedSource);

            var oldTree = syntaxTreeFactory(oldSource);
            var newTree = syntaxTreeFactory(newSource);

            var oldDocumentMap = new Dictionary<string, List<ActiveStatement>>();
            OldStatements = CreateActiveStatementMapFromMarkers(oldMarkedSource, oldTree, flags, oldDocumentMap);

            OldStatementsMap = new ActiveStatementsMap(
                documentPathMap: oldDocumentMap.ToImmutableDictionary(e => e.Key, e => e.Value.OrderBy(ActiveStatementsMap.Comparer).ToImmutableArray()),
                instructionMap: ActiveStatementsMap.Empty.InstructionMap);

            var newActiveStatementMarkers = GetActiveSpans(newMarkedSource).ToArray();

            var activeStatementCount = Math.Max(OldStatements.Length, (newActiveStatementMarkers.Length == 0) ? -1 : newActiveStatementMarkers.Max(m => m.Id));

            var newMappedSpans = new ArrayBuilder<SourceFileSpan>();
            var newMappedRegions = new ArrayBuilder<ImmutableArray<SourceFileSpan>>();
            var newExceptionRegionMarkers = GetExceptionRegions(newMarkedSource);

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
                newMappedRegions[ordinal] = (ordinal < newExceptionRegionMarkers.Length) ?
                    newExceptionRegionMarkers[ordinal].SelectAsArray(span => (SourceFileSpan)newTree.GetMappedLineSpan(span)) :
                    ImmutableArray<SourceFileSpan>.Empty;
            }

            NewMappedSpans = newMappedSpans.ToImmutable();
            NewMappedRegions = newMappedRegions.ToImmutable();

            // Tracking spans are marked in the new source since the editor moves them around as the user 
            // edits the source and we get their positions when analyzing the new source.
            // The EnC analyzer uses old tracking spans as hints to find matching nodes.
            var newText = newTree.GetText();
            OldUnmappedTrackingSpans = GetTrackingSpans(newMarkedSource, activeStatementCount).
                SelectAsArray(s => newText.Lines.GetLinePositionSpan(s));
        }

        internal static ImmutableArray<UnmappedActiveStatement> CreateActiveStatementMapFromMarkers(
            string markedSource,
            SyntaxTree tree,
            ActiveStatementFlags[]? flags,
            Dictionary<string, List<ActiveStatement>> documentMap)
        {
            var activeStatementMarkers = GetActiveSpans(markedSource).ToArray();
            var exceptionRegionMarkers = GetExceptionRegions(markedSource);

            return activeStatementMarkers.Aggregate(
                new List<UnmappedActiveStatement>(),
                (list, marker) =>
                {
                    var (unmappedSpan, ordinal) = marker;
                    var mappedSpan = tree.GetMappedLineSpan(unmappedSpan);
                    var documentActiveStatements = documentMap.GetOrAdd(mappedSpan.Path, path => new List<ActiveStatement>());

                    var statementFlags = (flags != null) ? flags[ordinal] :
                        ((ordinal == 0) ? ActiveStatementFlags.IsLeafFrame : ActiveStatementFlags.IsNonLeafFrame) | ActiveStatementFlags.MethodUpToDate;

                    var exceptionRegions = (ordinal < exceptionRegionMarkers.Length) ?
                        exceptionRegionMarkers[ordinal].SelectAsArray(unmappedRegionSpan => (SourceFileSpan)tree.GetMappedLineSpan(unmappedRegionSpan)) :
                        ImmutableArray<SourceFileSpan>.Empty;

                    var unmappedActiveStatement = new UnmappedActiveStatement(
                        unmappedSpan,
                        new ActiveStatement(
                            ordinal,
                            statementFlags,
                            mappedSpan,
                            instructionId: default),
                        new ActiveStatementExceptionRegions(exceptionRegions, isActiveStatementCovered: true));

                    documentActiveStatements.Add(unmappedActiveStatement.Statement);
                    return SetListItem(list, ordinal, unmappedActiveStatement);
                }).ToImmutableArray();
        }

        internal static ImmutableArray<ManagedActiveStatementDebugInfo> GetActiveStatementDebugInfos(
           Func<string, string, SyntaxTree> syntaxTreeFactory,
           string[] markedSources,
           string[]? filePaths = null,
           string? extension = null,
           int[]? methodRowIds = null,
           Guid[]? modules = null,
           int[]? methodVersions = null,
           int[]? ilOffsets = null,
           ActiveStatementFlags[]? flags = null)
        {
            var moduleId = new Guid("00000000-0000-0000-0000-000000000001");
            var map = new Dictionary<string, List<ActiveStatement>>();

            var activeStatements = new ArrayBuilder<ActiveStatement>();

            var sourceIndex = 0;
            foreach (var markedSource in markedSources)
            {
                var documentName = filePaths?[sourceIndex] ?? Path.Combine(TempRoot.Root, TestWorkspace.GetDefaultTestSourceDocumentName(sourceIndex, extension));
                var tree = syntaxTreeFactory(ClearTags(markedSource), documentName);
                var statements = CreateActiveStatementMapFromMarkers(markedSource, tree, flags, map);

                activeStatements.AddRange(statements.Where(s => s.Statement != null).Select(s => s.Statement));
                sourceIndex++;
            }

            activeStatements.Sort((x, y) => x.Ordinal.CompareTo(y.Ordinal));

            return activeStatements.SelectAsArray(statement =>
                new ManagedActiveStatementDebugInfo(
                    new ManagedInstructionId(
                        new ManagedMethodId(
                            (modules != null) ? modules[statement.Ordinal] : moduleId,
                            new ManagedModuleMethodId(
                                token: 0x06000000 | (methodRowIds != null ? methodRowIds[statement.Ordinal] : statement.Ordinal + 1),
                                version: (methodVersions != null) ? methodVersions[statement.Ordinal] : 1)),
                        ilOffset: (ilOffsets != null) ? ilOffsets[statement.Ordinal] : 0),
                    documentName: statement.FilePath,
                    sourceSpan: statement.Span.ToSourceSpan(),
                    flags: statement.Flags));
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

        internal static ImmutableArray<ImmutableArray<TextSpan>> GetExceptionRegions(string markedSource)
        {
            var matches = ExceptionRegionPattern.Matches(markedSource);
            var plainSource = ClearTags(markedSource);

            var result = new List<List<TextSpan>>();

            for (var i = 0; i < matches.Count; i++)
            {
                var exceptionRegion = matches[i].Groups["ExceptionRegion"];

                foreach (var (activeStatementId, exceptionRegionId) in GetDottedIds(matches[i]))
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
}
