// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    [SuppressMessage("Performance", "RS0008", Justification = "Equality not actually implemented")]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    internal readonly struct SourceSpan
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
            RoslynDebug.Assert(document != null);

            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
            Document = document;
        }

        public override int GetHashCode()
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override bool Equals(object? obj)
        {
            throw ExceptionUtilities.Unreachable();
        }

        private string GetDebuggerDisplay()
        {
            return $"({StartLine}, {StartColumn}) - ({EndLine}, {EndColumn})";
        }
    }
}
