// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
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
            var oldSource = SourceMarkers.Clear(oldMarkedSource);
            var newSource = SourceMarkers.Clear(newMarkedSource);

            var oldTree = syntaxTreeFactory(oldSource);
            var newTree = syntaxTreeFactory(newSource);

            var oldDocumentMap = new Dictionary<string, List<ActiveStatement>>();
            OldStatements = CreateActiveStatementMapFromMarkers(oldMarkedSource, oldTree, flags, oldDocumentMap);

            OldStatementsMap = new ActiveStatementsMap(
                documentPathMap: oldDocumentMap.ToImmutableDictionary(e => e.Key, e => e.Value.OrderBy(ActiveStatementsMap.Comparer).ToImmutableArray()),
                instructionMap: OldStatements.ToDictionary(s => new ManagedInstructionId(new ManagedMethodId(Guid.NewGuid(), 0x060000001, version: 1), ilOffset: 0), s => s.Statement));

            var newActiveStatementMarkers = SourceMarkers.GetActiveSpans(newMarkedSource).ToArray();

            var activeStatementCount = Math.Max(OldStatements.Length, (newActiveStatementMarkers.Length == 0) ? -1 : newActiveStatementMarkers.Max(m => m.Id));

            var newMappedSpans = new ArrayBuilder<SourceFileSpan>();
            var newMappedRegions = new ArrayBuilder<ImmutableArray<SourceFileSpan>>();
            var newExceptionRegionMarkers = SourceMarkers.GetExceptionRegions(newMarkedSource);

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
                newMappedRegions[ordinal] = (ordinal < newExceptionRegionMarkers.Length)
                    ? newExceptionRegionMarkers[ordinal].SelectAsArray(span => (SourceFileSpan)newTree.GetMappedLineSpan(span))
                    : ImmutableArray<SourceFileSpan>.Empty;
            }

            NewMappedSpans = newMappedSpans.ToImmutable();
            NewMappedRegions = newMappedRegions.ToImmutable();

            // Tracking spans are marked in the new source since the editor moves them around as the user 
            // edits the source and we get their positions when analyzing the new source.
            // The EnC analyzer uses old tracking spans as hints to find matching nodes.
            var newText = newTree.GetText();
            OldUnmappedTrackingSpans = SourceMarkers.GetTrackingSpans(newMarkedSource, activeStatementCount).
                SelectAsArray(s => newText.Lines.GetLinePositionSpan(s));
        }

        internal static ImmutableArray<UnmappedActiveStatement> CreateActiveStatementMapFromMarkers(
            string markedSource,
            SyntaxTree tree,
            ActiveStatementFlags[]? flags,
            Dictionary<string, List<ActiveStatement>> documentMap)
        {
            var activeStatementMarkers = SourceMarkers.GetActiveSpans(markedSource).ToArray();
            var exceptionRegionMarkers = SourceMarkers.GetExceptionRegions(markedSource);

            return activeStatementMarkers.Aggregate(
                new List<UnmappedActiveStatement>(),
                (list, marker) =>
                {
                    var (unmappedSpan, ordinal) = marker;
                    var mappedSpan = tree.GetMappedLineSpan(unmappedSpan);
                    var documentActiveStatements = documentMap.GetOrAdd(mappedSpan.Path, path => new List<ActiveStatement>());

                    var statementFlags = (flags != null) ? flags[ordinal] :
                        ((ordinal == 0) ? ActiveStatementFlags.LeafFrame : ActiveStatementFlags.NonLeafFrame) | ActiveStatementFlags.MethodUpToDate;

                    var exceptionRegions = (ordinal < exceptionRegionMarkers.Length)
                        ? exceptionRegionMarkers[ordinal].SelectAsArray(unmappedRegionSpan => (SourceFileSpan)tree.GetMappedLineSpan(unmappedRegionSpan))
                        : ImmutableArray<SourceFileSpan>.Empty;

                    var unmappedActiveStatement = new UnmappedActiveStatement(
                        unmappedSpan,
                        new ActiveStatement(
                            ordinal,
                            statementFlags,
                            mappedSpan,
                            instructionId: default),
                        new ActiveStatementExceptionRegions(exceptionRegions, isActiveStatementCovered: true));

                    documentActiveStatements.Add(unmappedActiveStatement.Statement);
                    return SourceMarkers.SetListItem(list, ordinal, unmappedActiveStatement);
                }).ToImmutableArray();
        }

        internal static ImmutableArray<UnmappedActiveStatement> GetUnmappedActiveStatements(
           Func<string, string, SyntaxTree> syntaxTreeFactory,
           string[] markedSources,
           string[]? filePaths = null,
           string? extension = null,
           ActiveStatementFlags[]? flags = null)
        {
            var map = new Dictionary<string, List<ActiveStatement>>();

            var activeStatements = new ArrayBuilder<UnmappedActiveStatement>();

            var sourceIndex = 0;
            foreach (var markedSource in markedSources)
            {
                var documentName = filePaths?[sourceIndex] ?? Path.Combine(TempRoot.Root, TestWorkspace.GetDefaultTestSourceDocumentName(sourceIndex, extension));
                var tree = syntaxTreeFactory(SourceMarkers.Clear(markedSource), documentName);
                var statements = CreateActiveStatementMapFromMarkers(markedSource, tree, flags, map);

                activeStatements.AddRange(statements.Where(s => s.Statement != null));
                sourceIndex++;
            }

            activeStatements.Sort((x, y) => x.Statement.Ordinal.CompareTo(y.Statement.Ordinal));
            return activeStatements.ToImmutable();
        }

        internal static ImmutableArray<ManagedActiveStatementDebugInfo> GetActiveStatementDebugInfos(
           ImmutableArray<UnmappedActiveStatement> activeStatements,
           int[]? methodRowIds = null,
           Guid[]? modules = null,
           int[]? methodVersions = null,
           int[]? ilOffsets = null)
        {
            var moduleId = new Guid("00000000-0000-0000-0000-000000000001");

            return activeStatements.Select(s => s.Statement).SelectAsArray(statement =>
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
    }
}
