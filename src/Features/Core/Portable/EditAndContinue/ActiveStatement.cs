// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Represents an instruction range in the code that contains an active instruction of at least one thread and that is delimited by consecutive sequence points.
    /// More than one thread can share the same instance of <see cref="ActiveStatement"/>.
    /// </summary>
    internal sealed class ActiveStatement
    {
        /// <summary>
        /// Ordinal of the active statement within the set of all active statements.
        /// </summary>
        public readonly int Ordinal;

        /// <summary>
        /// Ordinal of the active statement within the primary containing document (<see cref="PrimaryDocumentId"/>).
        /// </summary>
        public readonly int PrimaryDocumentOrdinal;

        /// <summary>
        /// The instruction of the active statement that is being executed.
        /// </summary>
        public readonly ActiveInstructionId InstructionId;

        /// <summary>
        /// Span in source file.
        /// </summary>
        public readonly LinePositionSpan Span;

        /// <summary>
        /// Document ids - mutliple if the physical file is linked.
        /// </summary>
        public readonly ImmutableArray<DocumentId> DocumentIds;

        /// <summary>
        /// Threads that share the instruction. May contain duplicates in case a thread is executing a function recursively.
        /// </summary>
        public readonly ImmutableArray<Guid> ThreadIds;

        /// <summary>
        /// Aggregated accross <see cref="ThreadIds"/>.
        /// </summary>
        public readonly ActiveStatementFlags Flags;

        public ActiveStatement(int ordinal, int primaryDocumentOrdinal, ImmutableArray<DocumentId> documentIds, ActiveStatementFlags flags, LinePositionSpan span, ActiveInstructionId instructionId, ImmutableArray<Guid> threadIds)
        {
            Debug.Assert(ordinal >= 0);
            Debug.Assert(primaryDocumentOrdinal >= 0);
            Debug.Assert(!documentIds.IsDefaultOrEmpty);

            Ordinal = ordinal;
            PrimaryDocumentOrdinal = primaryDocumentOrdinal;
            DocumentIds = documentIds;
            Flags = flags;
            Span = span;
            ThreadIds = threadIds;
            InstructionId = instructionId;
        }

        /// <summary>
        /// True if at least one of the threads whom this active statement belongs to is in a leaf frame.
        /// </summary>
        public bool IsLeaf => (Flags & ActiveStatementFlags.IsLeafFrame) != 0;

        /// <summary>
        /// True if at least one of the threads whom this active statement belongs to is in a non-leaf frame.
        /// </summary>
        public bool IsNonLeaf => (Flags & ActiveStatementFlags.IsNonLeafFrame) != 0;

        public DocumentId PrimaryDocumentId => DocumentIds[0];

        internal ActiveStatement WithSpan(LinePositionSpan span)
            => new ActiveStatement(Ordinal, PrimaryDocumentOrdinal, DocumentIds, Flags, span, InstructionId, ThreadIds);

        internal ActiveStatement WithFlags(ActiveStatementFlags flags)
            => new ActiveStatement(Ordinal, PrimaryDocumentOrdinal, DocumentIds, flags, Span, InstructionId, ThreadIds);
    }
}
