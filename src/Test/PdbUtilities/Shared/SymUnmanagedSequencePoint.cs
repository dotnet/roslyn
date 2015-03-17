// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.SymReaderInterop
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal struct SymUnmanagedSequencePoint
    {
        public readonly int Offset;
        public readonly ISymUnmanagedDocument Document;
        public readonly int StartLine;
        public readonly int StartColumn;
        public readonly int EndLine;
        public readonly int EndColumn;

        public bool IsHidden => StartLine == 0xfeefee;

        public SymUnmanagedSequencePoint(
            int offset,
            ISymUnmanagedDocument document,
            int startLine,
            int startColumn,
            int endLine,
            int endColumn)
        {
            this.Offset = offset;
            this.Document = document;
            this.StartLine = startLine;
            this.StartColumn = startColumn;
            this.EndLine = endLine;
            this.EndColumn = endColumn;
        }

        private string GetDebuggerDisplay()
        {
            return string.Format(
                "SequencePoint: Offset = {0:x4}, Range = ({1}, {2})..({3}, {4})",
                Offset, StartLine, StartColumn, EndLine, EndColumn);
        }
    }

    internal static class SequencePointExtensions
    {
        internal static ImmutableArray<SymUnmanagedSequencePoint> GetSequencePoints(this ISymUnmanagedMethod method)
        {
            // NB: method.GetSequencePoints(0, out numAvailable, ...) always returns 0.
            int numAvailable;
            int hr = method.GetSequencePointCount(out numAvailable);
            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);
            if (numAvailable == 0)
            {
                return ImmutableArray<SymUnmanagedSequencePoint>.Empty;
            }

            int[] offsets = new int[numAvailable];
            ISymUnmanagedDocument[] documents = new ISymUnmanagedDocument[numAvailable];
            int[] startLines = new int[numAvailable];
            int[] startColumns = new int[numAvailable];
            int[] endLines = new int[numAvailable];
            int[] endColumns = new int[numAvailable];

            int numRead;
            hr = method.GetSequencePoints(numAvailable, out numRead, offsets, documents, startLines, startColumns, endLines, endColumns);
            SymUnmanagedReaderExtensions.ThrowExceptionForHR(hr);
            if (numRead != numAvailable)
            {
                throw new InvalidOperationException(string.Format("Read only {0} of {1} sequence points.", numRead, numAvailable));
            }

            var builder = ArrayBuilder<SymUnmanagedSequencePoint>.GetInstance(numRead);
            for (int i = 0; i < numRead; i++)
            {
                builder.Add(new SymUnmanagedSequencePoint(
                    offsets[i],
                    documents[i],
                    startLines[i],
                    startColumns[i],
                    endLines[i],
                    endColumns[i]));
            }

            return builder.ToImmutableAndFree();
        }
    }
}
