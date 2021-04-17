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
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
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

        public static readonly Comparer<(ManagedActiveStatementDebugInfo, SourceFileSpan, int)> s_infoSpanComparer =
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

                if (!updatedSpansByDocumentPath.TryGetValue(documentName, out var documentInfos))
                {
                    updatedSpansByDocumentPath.Add(documentName, documentInfos = ArrayBuilder<(ManagedActiveStatementDebugInfo, SourceFileSpan, int)>.GetInstance());
                }

                documentInfos.Add((debugInfo, new SourceFileSpan(documentName, GetUpToDateSpan(debugInfo, remapping)), ordinal++));
            }

            foreach (var (_, infos) in updatedSpansByDocumentPath)
            {
                infos.Sort(s_infoSpanComparer);
            }

            var byDocumentPath = updatedSpansByDocumentPath.ToImmutableDictionary(
                keySelector: entry => entry.Key,
                elementSelector: entry => entry.Value.SelectAsArrayWithIndex((item, index, _) => new ActiveStatement(
                    ordinal: item.ordinal,
                    documentOrdinal: index,
                    flags: item.info.Flags,
                    span: item.span,
                    instructionId: item.info.ActiveInstruction), 0));

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

            return new ActiveStatementsMap(byDocumentPath, byInstruction.ToImmutableDictionary());
        }

        private static LinePositionSpan GetUpToDateSpan(ManagedActiveStatementDebugInfo activeStatementInfo, ImmutableDictionary<ManagedMethodId, ImmutableArray<NonRemappableRegion>> remapping)
        {
            var activeSpan = activeStatementInfo.SourceSpan.ToLinePositionSpan();

            if ((activeStatementInfo.Flags & ActiveStatementFlags.MethodUpToDate) != 0)
            {
                return activeSpan;
            }

            var instructionId = activeStatementInfo.ActiveInstruction;

            // Map active statement spans in non-remappable regions to the latest source locations.
            if (remapping.TryGetValue(instructionId.Method, out var regionsInMethod))
            {
                foreach (var region in regionsInMethod)
                {
                    if (region.Span.Span.Contains(activeSpan) && activeStatementInfo.DocumentName == region.Span.Path)
                    {
                        return activeSpan.AddLineDelta(region.LineDelta);
                    }
                }
            }

            // The active statement is in a method that's not up-to-date but the active span have not changed.
            // We only add changed spans to non-remappable regions map, so we won't find unchanged span there.
            // Return the original span.
            return activeSpan;
        }

        internal async ValueTask<ImmutableArray<UnmappedActiveStatement>> GetOldActiveStatementsAsync(IEditAndContinueAnalyzer analyzer, Document oldDocument, CancellationToken cancellationToken)
        {
            var oldTree = await oldDocument.DocumentState.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var oldRoot = await oldTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var oldText = await oldTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return GetOldActiveStatements(analyzer, oldTree, oldText, oldRoot, cancellationToken);
        }

        internal ImmutableArray<UnmappedActiveStatement> GetOldActiveStatements(IEditAndContinueAnalyzer analyzer, SyntaxTree oldSyntaxTree, SourceText oldText, SyntaxNode oldRoot, CancellationToken cancellationToken)
        {
            Debug.Assert(oldText == oldSyntaxTree.GetText(cancellationToken));
            Debug.Assert(oldRoot == oldSyntaxTree.GetRoot(cancellationToken));

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
                // (when multiple #line map to the same span that overlaps with the active statement).
                if (TryGetTextSpan(oldText.Lines, unmappedLineSpan, out var unmappedSpan) &&
                    mappedStatements.Add(activeStatement))
                {
                    var exceptionRegions = analyzer.GetExceptionRegions(oldRoot, unmappedSpan, activeStatement.IsNonLeaf, cancellationToken);
                    builder.Add(new UnmappedActiveStatement(unmappedSpan, activeStatement, exceptionRegions));
                }
            }

            var hasAnyLineDirectives = false;
            foreach (var (unmappedSection, mappedSection) in oldTree.GetLineMappings(cancellationToken))
            {
                hasAnyLineDirectives = true;

                var targetPath = mappedSection.HasMappedPath ? mappedSection.Path : oldTree.FilePath;

                if (DocumentPathMap.TryGetValue(targetPath, out var activeStatementsInMappedFile))
                {
                    var range = GetOverlappingSpans(
                        mappedSection.Span,
                        activeStatementsInMappedFile,
                        overlapsWith: (mappedSection, activeStatement) => mappedSection.OverlapsWith(activeStatement.Span));

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

            var unmappedStartColumn = (mappedSpan.Start.Line == mappedSection.Start.Line) ?
                unmappedSection.Start.Character + mappedSpan.Start.Character - mappedSection.Start.Character :
                mappedSpan.Start.Character;

            var unmappedEndColumn = (mappedSpan.End.Line == mappedSection.Start.Line) ?
                unmappedSection.Start.Character + mappedSpan.End.Character - mappedSection.Start.Character :
                mappedSpan.End.Character;

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

        internal static Range GetOverlappingSpans<TElement, TSpan>(
            TSpan declarationSpan,
            ImmutableArray<TElement> statements,
            Func<TSpan, TElement, bool> overlapsWith)
        {
            // Statements are sorted by their start position.
            // Therefore we can find the first and the last ones that overlaps the given span
            // and all statements in between them are those that overlap with given span.

            var i = 0;
            while (i < statements.Length && !overlapsWith(declarationSpan, statements[i]))
            {
                i++;
            }

            if (i == statements.Length)
            {
                return default;
            }

            var start = i;
            i++;

            while (i < statements.Length && overlapsWith(declarationSpan, statements[i]))
            {
                i++;
            }

            var end = i;
            return new Range(start, end);
        }
    }
}
