// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The LineDirectiveMap is created to enable translating positions, using the #line directives
    /// in a file. The basic implementation creates an ordered array of line mapping entries, one
    /// for each #line directive in the file (plus one at the beginning). If the file has no
    /// directives, then the array has just one element in it. To map line numbers, a binary search
    /// of the mapping entries is done and nearest line mapping is applied.
    /// </summary>
    internal abstract partial class LineDirectiveMap<TDirective>
        where TDirective : SyntaxNode
    {
        internal readonly ImmutableArray<LineMappingEntry> Entries;

        // Get all active #line directives under trivia into the list, in source code order.
        protected abstract bool ShouldAddDirective(TDirective directive);

        // Given a directive and the previous entry, create a new entry.
        protected abstract LineMappingEntry GetEntry(TDirective directive, SourceText sourceText, LineMappingEntry previous);

        // Creates the first entry with language specific content
        protected abstract LineMappingEntry InitializeFirstEntry();

        protected LineDirectiveMap(SyntaxTree syntaxTree)
        {
            // Accumulate all the directives, in source code order
            var syntaxRoot = (SyntaxNodeOrToken)syntaxTree.GetRoot();
            IList<TDirective> directives = syntaxRoot.GetDirectives<TDirective>(filter: ShouldAddDirective);
            Debug.Assert(directives != null);

            // Create the entry map.
            Entries = CreateEntryMap(syntaxTree, directives);
        }

        // Given a span and a default file name, return a FileLinePositionSpan that is the mapped
        // span, taking into account line directives.
        public FileLinePositionSpan TranslateSpan(SourceText sourceText, string treeFilePath, TextSpan span)
        {
            var unmappedStartPos = sourceText.Lines.GetLinePosition(span.Start);
            var unmappedEndPos = sourceText.Lines.GetLinePosition(span.End);
            var entry = FindEntry(unmappedStartPos.Line);

            return TranslateSpan(entry, treeFilePath, unmappedStartPos, unmappedEndPos);
        }

        protected FileLinePositionSpan TranslateSpan(in LineMappingEntry entry, string treeFilePath, LinePosition unmappedStartPos, LinePosition unmappedEndPos)
        {
            string path = entry.MappedPathOpt ?? treeFilePath;
            var span = entry.State == PositionState.RemappedSpan ?
                TranslateEnhancedLineDirectiveSpan(entry, unmappedStartPos, unmappedEndPos) :
                TranslateLineDirectiveSpan(entry, unmappedStartPos, unmappedEndPos);
            return new FileLinePositionSpan(path, span, hasMappedPath: entry.MappedPathOpt != null);
        }

        private static LinePositionSpan TranslateLineDirectiveSpan(in LineMappingEntry entry, LinePosition unmappedStartPos, LinePosition unmappedEndPos)
        {
            return new LinePositionSpan(translatePosition(entry, unmappedStartPos), translatePosition(entry, unmappedEndPos));

            static LinePosition translatePosition(in LineMappingEntry entry, LinePosition unmapped)
            {
                int mappedLine = unmapped.Line - entry.UnmappedLine + entry.MappedLine;
                return (mappedLine == -1) ? new LinePosition(unmapped.Character) : new LinePosition(mappedLine, unmapped.Character);
            }
        }

        private static LinePositionSpan TranslateEnhancedLineDirectiveSpan(in LineMappingEntry entry, LinePosition unmappedStartPos, LinePosition unmappedEndPos)
        {
            // A span starting on the first line, at or before 'UnmappedCharacterOffset' is
            // mapped to the entire 'MappedSpan', regardless of the size of the unmapped span,
            // even if the unmapped span ends before 'UnmappedCharacterOffset'.
            if (unmappedStartPos.Line == entry.UnmappedLine &&
                unmappedStartPos.Character < entry.UnmappedCharacterOffset.GetValueOrDefault())
            {
                return entry.MappedSpan;
            }

            // A span starting on the first line after 'UnmappedCharacterOffset', or starting on
            // a subsequent line, is mapped to a span of corresponding size.
            return new LinePositionSpan(translatePosition(entry, unmappedStartPos), translatePosition(entry, unmappedEndPos));

            static LinePosition translatePosition(in LineMappingEntry entry, LinePosition unmapped)
            {
                return new LinePosition(
                    unmapped.Line - entry.UnmappedLine + entry.MappedSpan.Start.Line,
                    unmapped.Line == entry.UnmappedLine ?
                        entry.MappedSpan.Start.Character + unmapped.Character - entry.UnmappedCharacterOffset.GetValueOrDefault() :
                        unmapped.Character);
            }
        }

        /// <summary>
        /// Determines whether the position is considered to be hidden from the debugger or not.
        /// </summary>
        public abstract LineVisibility GetLineVisibility(SourceText sourceText, int position);

        /// <summary>
        /// Combines TranslateSpan and IsHiddenPosition to not search the entries twice when emitting sequence points
        /// </summary>
        internal abstract FileLinePositionSpan TranslateSpanAndVisibility(SourceText sourceText, string treeFilePath, TextSpan span, out bool isHiddenPosition);

        /// <summary>
        /// Are there any hidden regions in the map?
        /// </summary>
        /// <returns>True if there's at least one hidden region in the map.</returns>
        public bool HasAnyHiddenRegions()
        {
            return this.Entries.Any(static e => e.State == PositionState.Hidden);
        }

        // Find the line mapped entry with the largest unmapped line number <= lineNumber.
        protected LineMappingEntry FindEntry(int lineNumber)
        {
            int r = FindEntryIndex(lineNumber);

            return this.Entries[r];
        }

        // Find the index of the line mapped entry with the largest unmapped line number <= lineNumber.
        protected int FindEntryIndex(int lineNumber)
        {
            int r = Entries.BinarySearch(new LineMappingEntry(lineNumber));
            return r >= 0 ? r : ((~r) - 1);
        }

        // Given the ordered list of all directives in the file, return the ordered line mapping
        // entry for the file. This always starts with the null mapped that maps line 0 to line 0.
        private ImmutableArray<LineMappingEntry> CreateEntryMap(SyntaxTree tree, IList<TDirective> directives)
        {
            var entries = ArrayBuilder<LineMappingEntry>.GetInstance(directives.Count + 1);

            var current = InitializeFirstEntry();
            entries.Add(current);

            if (directives.Count > 0)
            {
                var sourceText = tree.GetText();
                foreach (var directive in directives)
                {
                    current = GetEntry(directive, sourceText, current);
                    entries.Add(current);
                }
            }

#if DEBUG
            // Make sure the entries array is correctly sorted. 
            for (int i = 0; i < entries.Count - 1; ++i)
            {
                Debug.Assert(entries[i].CompareTo(entries[i + 1]) < 0);
            }
#endif

            return entries.ToImmutableAndFree();
        }

        protected abstract LineVisibility GetUnknownStateVisibility(int index);

        /// <summary>
        /// The caller is expected to not call this if <see cref="Entries"/> is empty.
        /// </summary>
        public IEnumerable<LineMapping> GetLineMappings(TextLineCollection lines)
        {
            Debug.Assert(Entries.Length > 1);

            var current = Entries[0];

            // the first entry is always initialized to unmapped:
            Debug.Assert(
                current.State is PositionState.Unmapped or PositionState.Unknown &&
                current.UnmappedLine == 0 &&
                current.MappedLine == 0 &&
                current.MappedPathOpt == null);

            for (int i = 1; i < Entries.Length; i++)
            {
                var next = Entries[i];

                int unmappedEndLine = next.UnmappedLine - 2;
                Debug.Assert(unmappedEndLine >= current.UnmappedLine - 1);

                // Skip empty spans - two consecutive #line directives or #line on the first line.
                if (unmappedEndLine >= current.UnmappedLine)
                {
                    // C#: Span ends just at the start of the line containing #line directive
                    //
                    // #line Current "file1"
                    // [|....\n
                    // ...........\n|]
                    // #line Next "file2"
                    //
                    // VB: Span starts at the beginning of the line following the #ExternalSource directive and ends at the start of the line preceding #End ExternalSource.
                    // #ExternalSource("file", 1)
                    // [|....\n
                    // ...........\n|]
                    // #End ExternalSource

                    var endLine = lines[unmappedEndLine];
                    int lineLength = endLine.EndIncludingLineBreak - endLine.Start;

                    yield return CreateLineMapping(current, unmappedEndLine, lineLength, currentIndex: i - 1);
                }

                current = next;
            }

            var lastLine = lines[^1];

            // Last span (unless the last #line/#End ExternalSource is on the last line):
            // #line Current "file1"
            // [|....\n
            // ...........\n|]
            //
            // #End ExternalSource
            // [|....\n
            // ...........\n|]
            if (current.UnmappedLine <= lastLine.LineNumber)
            {
                int lineLength = lastLine.EndIncludingLineBreak - lastLine.Start;
                int unmappedEndLine = lastLine.LineNumber;

                yield return CreateLineMapping(current, unmappedEndLine, lineLength, currentIndex: Entries.Length - 1);
            }
        }

        private LineMapping CreateLineMapping(in LineMappingEntry entry, int unmappedEndLine, int lineLength, int currentIndex)
        {
            var unmapped = new LinePositionSpan(
                new LinePosition(entry.UnmappedLine, character: 0),
                new LinePosition(unmappedEndLine, lineLength));

            if (entry.State == PositionState.Hidden ||
                entry.State == PositionState.Unknown && GetUnknownStateVisibility(currentIndex) == LineVisibility.Hidden)
            {
                return new LineMapping(unmapped, characterOffset: null, mappedSpan: default);
            }

            string path = entry.MappedPathOpt ?? string.Empty;
            bool hasMappedPath = entry.MappedPathOpt != null;

            if (entry.State == PositionState.RemappedSpan)
            {
                return new LineMapping(
                    unmapped,
                    characterOffset: entry.UnmappedCharacterOffset,
                    new FileLinePositionSpan(path, entry.MappedSpan, hasMappedPath));
            }

            var mappedSpan = new LinePositionSpan(
                new LinePosition(entry.MappedLine, character: 0),
                new LinePosition(entry.MappedLine + unmappedEndLine - entry.UnmappedLine, lineLength));
            var mapped = new FileLinePositionSpan(path, mappedSpan, hasMappedPath);
            return new LineMapping(unmapped, characterOffset: null, mapped);
        }
    }
}
