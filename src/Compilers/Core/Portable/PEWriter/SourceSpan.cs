// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    [SuppressMessage("Performance", "RS0008", Justification = "Equality not actually implemented")]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    internal struct SourceSpan
    {
        public readonly int StartLine;
        public readonly int StartColumn;
        public readonly int EndLine;
        public readonly int EndColumn;
        public readonly Cci.DebugSourceDocument Document;

        public SourceSpan(
            Cci.DebugSourceDocument document,
            int startLine,
            int startColumn,
            int endLine,
            int endColumn)
        {
            Debug.Assert(document != null);

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

        private string GetDebuggerDisplay()
        {
            return $"({StartLine}, {StartColumn}) - ({EndLine}, {EndColumn})";
        }
    }
}
