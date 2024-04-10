// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    [SuppressMessage("Performance", "CA1067", Justification = "Equality not actually implemented")]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    internal readonly struct SequencePoint
    {
        public const int HiddenLine = 0xfeefee;

        public readonly int Offset;
        public readonly int StartLine;
        public readonly int EndLine;
        public readonly ushort StartColumn;
        public readonly ushort EndColumn;
        public readonly DebugSourceDocument Document;

        public SequencePoint(
            DebugSourceDocument document,
            int offset,
            int startLine,
            ushort startColumn,
            int endLine,
            ushort endColumn)
        {
            RoslynDebug.Assert(document != null);

            Offset = offset;
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
            Document = document;
        }

        public bool IsHidden => StartLine == HiddenLine;

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
            return IsHidden ? "<hidden>" : $"{Offset}: ({StartLine}, {StartColumn}) - ({EndLine}, {EndColumn})";
        }
    }
}
