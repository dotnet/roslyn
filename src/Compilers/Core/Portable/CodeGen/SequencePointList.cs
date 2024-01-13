// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Maintains a list of sequence points in a space efficient way. Most of the time sequence points
    /// occur in the same syntax tree, so optimize for that case. Store a sequence point as an offset, and 
    /// position in a syntax tree, then translate to CCI format only on demand.
    /// 
    /// Use a ArrayBuilder{RawSequencePoint} to create.
    /// </summary>
    internal class SequencePointList
    {
        private readonly SyntaxTree _tree;
        private readonly OffsetAndSpan[] _points;
        private SequencePointList _next;  // Linked list of all points.

        // No sequence points.
        private static readonly SequencePointList s_empty = new SequencePointList();

        // Construct a list with no sequence points.
        private SequencePointList()
        {
            _points = Array.Empty<OffsetAndSpan>();
        }

        // Construct a list with sequence points from exactly one syntax tree.
        private SequencePointList(SyntaxTree tree, OffsetAndSpan[] points)
        {
            _tree = tree;
            _points = points;
        }

        /// <summary>
        /// Create a SequencePointList with the raw sequence points from an ArrayBuilder.
        /// A linked list of instances for each syntax tree is created (almost always of length one).
        /// </summary>
        public static SequencePointList Create(ArrayBuilder<RawSequencePoint> seqPointBuilder, ILBuilder builder)
        {
            if (seqPointBuilder.Count == 0)
            {
                return SequencePointList.s_empty;
            }

            SequencePointList first = null, current = null;
            int totalPoints = seqPointBuilder.Count;
            int last = 0;

            for (int i = 1; i <= totalPoints; ++i)
            {
                if (i == totalPoints || seqPointBuilder[i].SyntaxTree != seqPointBuilder[i - 1].SyntaxTree)
                {
                    // Create a new list
                    SequencePointList next = new SequencePointList(seqPointBuilder[i - 1].SyntaxTree, GetSubArray(seqPointBuilder, last, i - last, builder));
                    last = i;

                    // Link together with any additional.
                    if (current == null)
                    {
                        first = current = next;
                    }
                    else
                    {
                        current._next = next;
                        current = next;
                    }
                }
            }

            return first;
        }

        public bool IsEmpty
        {
            get
            {
                return _next == null && _points.Length == 0;
            }
        }

        private static OffsetAndSpan[] GetSubArray(ArrayBuilder<RawSequencePoint> seqPointBuilder, int start, int length, ILBuilder builder)
        {
            OffsetAndSpan[] result = new OffsetAndSpan[length];
            for (int i = 0; i < result.Length; i++)
            {
                RawSequencePoint point = seqPointBuilder[i + start];
                int ilOffset = builder.GetILOffsetFromMarker(point.ILMarker);
                Debug.Assert(ilOffset >= 0);
                result[i] = new OffsetAndSpan(ilOffset, point.Span);
            }

            return result;
        }

        /// <summary>
        /// Get all the sequence points, possibly mapping them using #line/ExternalSource directives, and mapping
        /// file names to debug documents with the given mapping function.
        /// </summary>
        /// <param name="documentProvider">Function that maps file paths to CCI debug documents</param>
        /// <param name="builder">where sequence points should be deposited</param>
        public void GetSequencePoints(
            DebugDocumentProvider documentProvider,
            ArrayBuilder<Cci.SequencePoint> builder)
        {
            bool lastPathIsMapped = false;
            string lastPath = null;
            Cci.DebugSourceDocument lastDebugDocument = null;

            FileLinePositionSpan? firstReal = FindFirstRealSequencePoint();
            if (!firstReal.HasValue)
            {
                return;
            }
            lastPath = firstReal.Value.Path;
            lastPathIsMapped = firstReal.Value.HasMappedPath;
            lastDebugDocument = documentProvider(lastPath, basePath: lastPathIsMapped ? this._tree.FilePath : null);

            SequencePointList current = this;
            while (current != null)
            {
                SyntaxTree currentTree = current._tree;

                foreach (var offsetAndSpan in current._points)
                {
                    TextSpan span = offsetAndSpan.Span;

                    // if it's a hidden sequence point, or a sequence point with syntax that points to a position that is inside 
                    // of a hidden region (can be defined with #line hidden (C#) or implicitly by #ExternalSource (VB), make it 
                    // a hidden sequence point.

                    bool isHidden = span == RawSequencePoint.HiddenSequencePointSpan;
                    FileLinePositionSpan fileLinePositionSpan = default;
                    if (!isHidden)
                    {
                        fileLinePositionSpan = currentTree.GetMappedLineSpanAndVisibility(span, out isHidden);
                    }

                    if (isHidden)
                    {
                        if (lastPath == null)
                        {
                            lastPath = currentTree.FilePath;
                            lastDebugDocument = documentProvider(lastPath, basePath: null);
                        }

                        if (lastDebugDocument != null)
                        {
                            builder.Add(new Cci.SequencePoint(
                                lastDebugDocument,
                                offset: offsetAndSpan.Offset,
                                startLine: Cci.SequencePoint.HiddenLine,
                                startColumn: 0,
                                endLine: Cci.SequencePoint.HiddenLine,
                                endColumn: 0));
                        }
                    }
                    else
                    {
                        if (lastPath != fileLinePositionSpan.Path || lastPathIsMapped != fileLinePositionSpan.HasMappedPath)
                        {
                            lastPath = fileLinePositionSpan.Path;
                            lastPathIsMapped = fileLinePositionSpan.HasMappedPath;
                            lastDebugDocument = documentProvider(lastPath, basePath: lastPathIsMapped ? currentTree.FilePath : null);
                        }

                        if (lastDebugDocument != null)
                        {
                            int startLine = (fileLinePositionSpan.StartLinePosition.Line == -1) ? 0 : fileLinePositionSpan.StartLinePosition.Line + 1;
                            int endLine = (fileLinePositionSpan.EndLinePosition.Line == -1) ? 0 : fileLinePositionSpan.EndLinePosition.Line + 1;
                            int startColumn = fileLinePositionSpan.StartLinePosition.Character + 1;
                            int endColumn = fileLinePositionSpan.EndLinePosition.Character + 1;

                            // Trim column number if necessary.
                            // Column must be in range [0, 0xffff) and end column must be greater than start column if on the same line.
                            // The Portable PDB specifies 0x10000, but System.Reflection.Metadata reader has an off-by-one error.
                            // Windows PDBs allow the same range.
                            const int MaxColumn = ushort.MaxValue - 1;

                            if (startColumn > MaxColumn)
                            {
                                startColumn = (startLine == endLine) ? MaxColumn - 1 : MaxColumn;
                            }

                            if (endColumn > MaxColumn)
                            {
                                endColumn = MaxColumn;
                            }

                            builder.Add(new Cci.SequencePoint(
                                lastDebugDocument,
                                offset: offsetAndSpan.Offset,
                                startLine: startLine,
                                startColumn: (ushort)startColumn,
                                endLine: endLine,
                                endColumn: (ushort)endColumn
                            ));
                        }
                    }
                }

                current = current._next;
            }
        }

        // Find the document for the first non-hidden sequence point (issue #4370)
        // Returns null if a real sequence point was not found.
        private FileLinePositionSpan? FindFirstRealSequencePoint()
        {
            SequencePointList current = this;

            while (current != null)
            {
                foreach (var offsetAndSpan in current._points)
                {
                    TextSpan span = offsetAndSpan.Span;
                    bool isHidden = span == RawSequencePoint.HiddenSequencePointSpan;
                    if (!isHidden)
                    {
                        FileLinePositionSpan fileLinePositionSpan = current._tree.GetMappedLineSpanAndVisibility(span, out isHidden);
                        if (!isHidden)
                        {
                            return fileLinePositionSpan;
                        }
                    }
                }
                current = current._next;
            }

            return null;
        }

        /// <summary>
        /// Represents the combination of an IL offset and a source text span.
        /// </summary>
        private readonly struct OffsetAndSpan
        {
            public readonly int Offset;
            public readonly TextSpan Span;

            public OffsetAndSpan(int offset, TextSpan span)
            {
                this.Offset = offset;
                this.Span = span;
            }
        }
    }
}
