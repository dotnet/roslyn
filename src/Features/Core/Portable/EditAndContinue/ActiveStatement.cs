// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class ActiveStatement
    {
        public readonly int DebugId;
        public readonly DocumentId DocumentId;
        public readonly int Ordinal;
        public readonly ActiveStatementFlags Flags;
        public readonly LinePositionSpan Span;
        public readonly ActiveInstructionId InstructionId;

        public ActiveStatement(int debugId, DocumentId documentId, int ordinal, ActiveStatementFlags flags, LinePositionSpan span, ActiveInstructionId instructionId)
        {
            Debug.Assert(debugId >= 0);
            Debug.Assert(documentId != null);
            Debug.Assert(ordinal >= 0);

            DebugId = debugId;
            DocumentId = documentId;
            Ordinal = ordinal;
            Flags = flags;
            Span = span;
            InstructionId = instructionId;
        }

        public bool IsLeaf => (Flags & ActiveStatementFlags.LeafFrame) != 0;

        public ActiveStatementId Id => new ActiveStatementId(DocumentId, Ordinal);

        internal ActiveStatement WithSpan(LinePositionSpan span)
            => new ActiveStatement(DebugId, DocumentId, Ordinal, Flags, span, InstructionId);

        internal ActiveStatement WithFlags(ActiveStatementFlags flags)
            => new ActiveStatement(DebugId, DocumentId, Ordinal, flags, Span, InstructionId);
    }
}
