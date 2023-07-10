// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class ActiveStatementsMap
    {
        public static readonly ActiveStatementsMap Empty =
            new(ImmutableDictionary<string, ImmutableArray<ActiveStatement>>.Empty,
                ImmutableDictionary<ManagedInstructionId, ActiveStatement>.Empty);

        public static readonly Comparer<ActiveStatement> Comparer =
            Comparer<ActiveStatement>.Create((x, y) => x.FileSpan.Start.CompareTo(y.FileSpan.Start));

        private static readonly Comparer<(ManagedActiveStatementDebugInfo, SourceFileSpan, int)> s_infoSpanComparer =
            Comparer<(ManagedActiveStatementDebugInfo, SourceFileSpan span, int)>.Create((x, y) => x.span.Start.CompareTo(y.span.Start));

        /// <summary>
        /// Groups active statements by document path as listed in the PDB.
        /// Within each group the statements are ordered by their start position.
        /// </summary>
        public readonly IReadOnlyDictionary<string, ImmutableArray<ActiveStatement>> DocumentPathMap;

        /// <summary>
        /// Active statements by instruction id.
        /// </summary>
        public readonly IReadOnlyDictionary<ManagedInstructionId, ActiveStatement> InstructionMap;

        /// <summary>
        /// Maps syntax tree to active statements with calculated unmapped spans.
        /// </summary>
        private ImmutableDictionary<SyntaxTree, ImmutableArray<UnmappedActiveStatement>> _lazyOldDocumentActiveStatements;

        public ActiveStatementsMap(
            IReadOnlyDictionary<string, ImmutableArray<ActiveStatement>> documentPathMap,
            IReadOnlyDictionary<ManagedInstructionId, ActiveStatement> instructionMap)
        {
            Debug.Assert(documentPathMap.All(entry => entry.Value.IsSorted(Comparer)));

            DocumentPathMap = documentPathMap;
            InstructionMap = instructionMap;

            _lazyOldDocumentActiveStatements = ImmutableDictionary<SyntaxTree, ImmutableArray<UnmappedActiveStatement>>.Empty;
        }

        public static ActiveStatementsMap Create(
            ImmutableArray<ManagedActiveStatementDebugInfo> debugInfos,
            ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> remapping)
        {
            using var _1 = PooledDictionary<string, ArrayBuilder<(ManagedActiveStatementDebugInfo info, SourceFileSpan span, int ordinal)>>.GetInstance(out var updatedSpansByDocumentPath);

            var ordinal = 0;
            foreach (var debugInfo in debugInfos)
            {
                var documentName = debugInfo.DocumentName;
                if (documentName == null)
                {
                    // Ignore active statements that do not have a source location.
                    continue;
                }

                if (!TryGetUpToDateSpan(debugInfo, remapping, out var baseSpan))
                {
                    continue;
                }

                if (!updatedSpansByDocumentPath.TryGetValue(documentName, out var documentInfos))
                {
                    updatedSpansByDocumentPath.Add(documentName, documentInfos = ArrayBuilder<(ManagedActiveStatementDebugInfo, SourceFileSpan, int)>.GetInstance());
                }

                documentInfos.Add((debugInfo, new SourceFileSpan(documentName, baseSpan), ordinal++));
            }

            foreach (var (_, infos) in updatedSpansByDocumentPath)
            {
                infos.Sort(s_infoSpanComparer);
            }

            var byDocumentPath = updatedSpansByDocumentPath.ToImmutableDictionary(
                keySelector: entry => entry.Key,
                elementSelector: entry => entry.Value.SelectAsArray(item => new ActiveStatement(
                    ordinal: item.ordinal,
                    flags: item.info.Flags,
                    span: item.span,
                    instructionId: item.info.ActiveInstruction)));

            using var _2 = PooledDictionary<ManagedInstructionId, ActiveStatement>.GetInstance(out var byInstruction);

            foreach (var (_, statements) in byDocumentPath)
            {
                foreach (var statement in statements)
                {
                    try
                    {
                        byInstruction.Add(statement.InstructionId, statement);
                    }
                    catch (ArgumentException)
                    {
                        throw new InvalidOperationException($"Multiple active statements with the same instruction id returned by Active Statement Provider");
                    }
                }
            }

            // TODO: Remove. Workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1830914.
            if (EditAndContinueMethodDebugInfoReader.IgnoreCaseWhenComparingDocumentNames)
            {
                byDocumentPath = byDocumentPath.WithComparers(keyComparer: StringComparer.OrdinalIgnoreCase);
            }

            return new ActiveStatementsMap(byDocumentPath, byInstruction.ToImmutableDictionary());
        }

        private static bool TryGetUpToDateSpan(ManagedActiveStatementDebugInfo activeStatementInfo, ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> remapping, out LinePositionSpan newSpan)
        {
            // Drop stale active statements - their location in the current snapshot is unknown.
            if (activeStatementInfo.Flags.HasFlag(ActiveStatementFlags.Stale))
            {
                newSpan = default;
                return false;
            }

            var activeSpan = activeStatementInfo.SourceSpan.ToLinePositionSpan();
            if (activeStatementInfo.Flags.HasFlag(ActiveStatementFlags.MethodUpToDate))
            {
                newSpan = activeSpan;
                return true;
            }

            var instructionId = activeStatementInfo.ActiveInstruction;

            // Map active statement spans in non-remappable regions to the latest source locations.
            if (remapping.TryGetValue(instructionId.Method, out var regionsInMethod))
            {
                // Note that active statement spans can be nested. For example,
                // [|var x = y switch { 1 => 0, _ => [|1|] };|]

                foreach (var region in regionsInMethod)
                {
                    if (!region.IsExceptionRegion &&
                        region.OldSpan.Span == activeSpan &&
                        activeStatementInfo.DocumentName == region.OldSpan.Path)
                    {
                        newSpan = region.NewSpan.Span;
                        return true;
                    }
                }
            }

            // The active statement is in a method instance that was updated during Hot Reload session,
            // at which point the location of the span was not known. 
            newSpan = default;
            return false;
        }

        public bool IsEmpty
            => InstructionMap.IsEmpty();

        internal async ValueTask<ImmutableArray<UnmappedActiveStatement>> GetOldActiveStatementsAsync(IEditAndContinueAnalyzer analyzer, Document oldDocument, CancellationToken cancellationToken)
        {
            var oldTree = await oldDocument.DocumentState.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var oldRoot = await oldTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var oldText = await oldTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return GetOldActiveStatements(analyzer, oldTree, oldText, oldRoot, cancellationToken);
        }

        internal ImmutableArray<UnmappedActiveStatement> GetOldActiveStatements(IEditAndContinueAnalyzer analyzer, SyntaxTree oldSyntaxTree, SourceText oldText, SyntaxNode oldRoot, CancellationToken cancellationToken)
        {
            Debug.Assert(oldRoot.SyntaxTree == oldSyntaxTree);

            return ImmutableInterlocked.GetOrAdd(
                ref _lazyOldDocumentActiveStatements,
                oldSyntaxTree,
                oldSyntaxTree => CalculateOldActiveStatementsAndExceptionRegions(analyzer, oldSyntaxTree, oldText, oldRoot, cancellationToken));
        }

        private ImmutableArray<UnmappedActiveStatement> CalculateOldActiveStatementsAndExceptionRegions(IEditAndContinueAnalyzer analyzer, SyntaxTree oldTree, SourceText oldText, SyntaxNode oldRoot, CancellationToken cancellationToken)
        {
            using var _1 = ArrayBuilder<UnmappedActiveStatement>.GetInstance(out var builder);
            using var _2 = PooledHashSet<ActiveStatement>.GetInstance(out var mappedStatements);

            void AddStatement(LinePositionSpan unmappedLineSpan, ActiveStatement activeStatement)
            {
                // Protect against stale/invalid active statement spans read from the PDB.
                // Also guard against active statements unmapped to multiple locations in the unmapped file
                // (when multiple #line directives map to the same span that overlaps with the active statement).
                if (TryGetTextSpan(oldText.Lines, unmappedLineSpan, out var unmappedSpan) &&
                    oldRoot.FullSpan.Contains(unmappedSpan.Start) &&
                    mappedStatements.Add(activeStatement))
                {
                    var exceptionRegions = analyzer.GetExceptionRegions(oldRoot, unmappedSpan, activeStatement.IsNonLeaf, cancellationToken);
                    builder.Add(new UnmappedActiveStatement(unmappedSpan, activeStatement, exceptionRegions));
                }
            }

            var hasAnyLineDirectives = false;
            foreach (var lineMapping in oldTree.GetLineMappings(cancellationToken))
            {
                var unmappedSection = lineMapping.Span;
                var mappedSection = lineMapping.MappedSpan;

                hasAnyLineDirectives = true;

                var targetPath = mappedSection.HasMappedPath ? mappedSection.Path : oldTree.FilePath;

                if (DocumentPathMap.TryGetValue(targetPath, out var activeStatementsInMappedFile))
                {
                    var range = GetSpansStartingInSpan(
                        mappedSection.Span.Start,
                        mappedSection.Span.End,
                        activeStatementsInMappedFile,
                        startPositionComparer: (x, y) => x.Span.Start.CompareTo(y));

                    for (var i = range.Start.Value; i < range.End.Value; i++)
                    {
                        var activeStatement = activeStatementsInMappedFile[i];
                        var unmappedLineSpan = ReverseMapLinePositionSpan(unmappedSection, mappedSection.Span, activeStatement.Span);

                        AddStatement(unmappedLineSpan, activeStatement);
                    }
                }
            }

            if (!hasAnyLineDirectives)
            {
                Debug.Assert(builder.IsEmpty());

                if (DocumentPathMap.TryGetValue(oldTree.FilePath, out var activeStatements))
                {
                    foreach (var activeStatement in activeStatements)
                    {
                        AddStatement(activeStatement.Span, activeStatement);
                    }
                }
            }

            Debug.Assert(builder.IsSorted(Comparer<UnmappedActiveStatement>.Create((x, y) => x.UnmappedSpan.Start.CompareTo(y.UnmappedSpan.End))));

            return builder.ToImmutable();
        }

        private static LinePositionSpan ReverseMapLinePositionSpan(LinePositionSpan unmappedSection, LinePositionSpan mappedSection, LinePositionSpan mappedSpan)
        {
            var lineDifference = unmappedSection.Start.Line - mappedSection.Start.Line;
            var unmappedStartLine = mappedSpan.Start.Line + lineDifference;
            var unmappedEndLine = mappedSpan.End.Line + lineDifference;

            var unmappedStartColumn = (mappedSpan.Start.Line == mappedSection.Start.Line)
                ? unmappedSection.Start.Character + mappedSpan.Start.Character - mappedSection.Start.Character
                : mappedSpan.Start.Character;

            var unmappedEndColumn = (mappedSpan.End.Line == mappedSection.Start.Line)
                ? unmappedSection.Start.Character + mappedSpan.End.Character - mappedSection.Start.Character
                : mappedSpan.End.Character;

            return new(new(unmappedStartLine, unmappedStartColumn), new(unmappedEndLine, unmappedEndColumn));
        }

        private static bool TryGetTextSpan(TextLineCollection lines, LinePositionSpan lineSpan, out TextSpan span)
        {
            if (lineSpan.Start.Line >= lines.Count || lineSpan.End.Line >= lines.Count)
            {
                span = default;
                return false;
            }

            var start = lines[lineSpan.Start.Line].Start + lineSpan.Start.Character;
            var end = lines[lineSpan.End.Line].Start + lineSpan.End.Character;
            span = TextSpan.FromBounds(start, end);
            return true;
        }

        /// <summary>
        /// Since an active statement represents a range between two sequence points and its span is associated with the first of these sequence points,
        /// we decide whether the active statement is relevant within given span by checking whether its start location is within that span.
        /// An active statement may overlap a span even if its starting location is not in the span, but such active statement is not relevant 
        /// for analysis of code within the given span.
        /// 
        /// Assumes that <paramref name="spans"/> are sorted by their start position.
        /// </summary>
        internal static Range GetSpansStartingInSpan<TElement, TPosition>(
            TPosition spanStart,
            TPosition spanEnd,
            ImmutableArray<TElement> spans,
            Func<TElement, TPosition, int> startPositionComparer)
        {
            var start = spans.BinarySearch(spanStart, startPositionComparer);
            if (start < 0)
            {
                // ~start points to the next span whose start position is greater than span start position:
                start = ~start;
            }

            if (start == spans.Length)
            {
                return default;
            }

            var length = spans.AsSpan()[start..].BinarySearch(spanEnd, startPositionComparer);
            if (length < 0)
            {
                // ~length points to the next span whose start position is greater than span start position:
                length = ~length;
            }

            return new Range(start, start + length);
        }
    }
}
