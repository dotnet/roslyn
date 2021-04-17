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

        protected FileLinePositionSpan TranslateSpan(LineMappingEntry entry, string treeFilePath, LinePosition unmappedStartPos, LinePosition unmappedEndPos)
        {
            string path = entry.MappedPathOpt ?? treeFilePath;
            int mappedStartLine = unmappedStartPos.Line - entry.UnmappedLine + entry.MappedLine;
            int mappedEndLine = unmappedEndPos.Line - entry.UnmappedLine + entry.MappedLine;

            return new FileLinePositionSpan(
                path,
                new LinePositionSpan(
                    (mappedStartLine == -1) ? new LinePosition(unmappedStartPos.Character) : new LinePosition(mappedStartLine, unmappedStartPos.Character),
                    (mappedEndLine == -1) ? new LinePosition(unmappedEndPos.Character) : new LinePosition(mappedEndLine, unmappedEndPos.Character)),
                hasMappedPath: entry.MappedPathOpt != null);
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
            return this.Entries.Any(e => e.State == PositionState.Hidden);
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
            var entries = new ArrayBuilder<LineMappingEntry>(directives.Count + 1);

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
    }
}
