// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
        protected readonly LineMappingEntry[] Entries;

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
            IEnumerable<TDirective> directives = syntaxRoot.GetDirectives<TDirective>(filter: ShouldAddDirective);
            Debug.Assert(directives != null);

            // Create the entry map.
            this.Entries = CreateEntryMap(syntaxTree.GetText(), directives);
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
            int r = Array.BinarySearch(this.Entries, new LineMappingEntry(lineNumber));
            return r >= 0 ? r : ((~r) - 1);
        }

        // Given the ordered list of all directives in the file, return the ordered line mapping
        // entry for the file. This always starts with the null mapped that maps line 0 to line 0.
        private LineMappingEntry[] CreateEntryMap(SourceText sourceText, IEnumerable<TDirective> directives)
        {
            var entries = new LineMappingEntry[directives.Count() + 1];
            var current = InitializeFirstEntry();
            var index = 0;
            entries[index] = current;

            foreach (var directive in directives)
            {
                current = GetEntry(directive, sourceText, current);
                ++index;
                entries[index] = current;
            }

#if DEBUG
            // Make sure the entries array is correctly sorted. 
            for (int i = 0; i < entries.Length - 1; ++i)
            {
                Debug.Assert(entries[i].CompareTo(entries[i + 1]) < 0);
            }
#endif

            return entries;
        }
    }
}
