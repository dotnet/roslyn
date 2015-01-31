// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.DiaSymReader
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public struct SymUnmanagedSequencePoint
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
            return $"SequencePoint: Offset = {Offset:x4}, Range = ({StartLine}, {StartColumn})..({EndLine}, {EndColumn})";
        }
    }
}
