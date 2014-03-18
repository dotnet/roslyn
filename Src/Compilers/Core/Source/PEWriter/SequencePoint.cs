// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    internal struct SequencePoint
    {
        public readonly int Offset;
        public readonly int StartLine;
        public readonly int StartColumn;
        public readonly int EndLine;
        public readonly int EndColumn;
        public readonly DebugSourceDocument Document;

        public SequencePoint(
            DebugSourceDocument document,
            int offset,
            int startLine,
            int startColumn,
            int endLine,
            int endColumn)
        {
            Debug.Assert(document != null);

            Offset = offset;
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
            Document = document;
        }

        public override int GetHashCode()
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool Equals(object obj)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}