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
        /// The executing version of the method might be several generations old.
        /// E.g. when the thread is executing an exception handling region and hasn't been remapped yet.
        /// </summary>
        public readonly ActiveInstructionId InstructionId;

        /// <summary>
        /// The current source span.
        /// </summary>
        public readonly LinePositionSpan Span;

        /// <summary>
        /// Document ids - multiple if the physical file is linked.
        /// TODO: currently we associate all linked documents to the <see cref="ActiveStatement"/> regardless of whether they belong to a project that matches the AS module.
        /// https://github.com/dotnet/roslyn/issues/24320
        /// </summary>
        public readonly ImmutableArray<DocumentId> DocumentIds;

        /// <summary>
        /// Threads that share the instruction. May contain duplicates in case a thread is executing a function recursively.
        /// </summary>
        public readonly ImmutableArray<Guid> ThreadIds;

        /// <summary>
        /// Aggregated across <see cref="ThreadIds"/>.
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

        public bool IsMethodUpToDate => (Flags & ActiveStatementFlags.MethodUpToDate) != 0;

        public DocumentId PrimaryDocumentId => DocumentIds[0];

        internal ActiveStatement WithSpan(LinePositionSpan span)
            => new ActiveStatement(Ordinal, PrimaryDocumentOrdinal, DocumentIds, Flags, span, InstructionId, ThreadIds);

        internal ActiveStatement WithFlags(ActiveStatementFlags flags)
            => new ActiveStatement(Ordinal, PrimaryDocumentOrdinal, DocumentIds, flags, Span, InstructionId, ThreadIds);
    }
}
