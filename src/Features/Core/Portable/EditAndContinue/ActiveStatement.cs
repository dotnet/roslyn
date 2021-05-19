// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Represents an instruction range in the code that contains an active instruction of at least one thread and that is delimited by consecutive sequence points.
    /// More than one thread can share the same instance of <see cref="ActiveStatement"/>.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal sealed class ActiveStatement
    {
        /// <summary>
        /// Ordinal of the active statement within the set of all active statements.
        /// </summary>
        public readonly int Ordinal;

        /// <summary>
        /// The instruction of the active statement that is being executed.
        /// The executing version of the method might be several generations old.
        /// E.g. when the thread is executing an exception handling region and hasn't been remapped yet.
        /// </summary>
        public readonly ManagedInstructionId InstructionId;

        /// <summary>
        /// The current source span.
        /// </summary>
        public readonly SourceFileSpan FileSpan;

        /// <summary>
        /// Aggregated across all threads.
        /// </summary>
        public readonly ActiveStatementFlags Flags;

        public ActiveStatement(int ordinal, ActiveStatementFlags flags, SourceFileSpan span, ManagedInstructionId instructionId)
        {
            Debug.Assert(ordinal >= 0);

            Ordinal = ordinal;
            Flags = flags;
            FileSpan = span;
            InstructionId = instructionId;
        }

        public ActiveStatement WithSpan(LinePositionSpan span)
            => WithFileSpan(FileSpan.WithSpan(span));

        public ActiveStatement WithFileSpan(SourceFileSpan span)
            => new(Ordinal, Flags, span, InstructionId);

        public ActiveStatement WithFlags(ActiveStatementFlags flags)
            => new(Ordinal, flags, FileSpan, InstructionId);

        public LinePositionSpan Span
            => FileSpan.Span;

        public string FilePath
            => FileSpan.Path;

        /// <summary>
        /// True if at least one of the threads whom this active statement belongs to is in a leaf frame.
        /// </summary>
        public bool IsLeaf
            => (Flags & ActiveStatementFlags.IsLeafFrame) != 0;

        /// <summary>
        /// True if at least one of the threads whom this active statement belongs to is in a non-leaf frame.
        /// </summary>
        public bool IsNonLeaf
            => (Flags & ActiveStatementFlags.IsNonLeafFrame) != 0;

        public bool IsMethodUpToDate
            => (Flags & ActiveStatementFlags.MethodUpToDate) != 0;

        private string GetDebuggerDisplay()
            => $"{Ordinal}: {Span}";
    }
}
