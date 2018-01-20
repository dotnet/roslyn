// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct ActiveStatementDebugInfo
    {
        public readonly ActiveInstructionId InstructionId;
        public readonly string DocumentName;
        public readonly LinePositionSpan LinePositionSpan;

        /// <summary>
        /// Aggregated accross <see cref="ThreadIds"/>.
        /// </summary>
        public readonly ActiveStatementFlags Flags;

        /// <summary>
        /// Threads that share the instruction. May contain duplicates in case a thread is executing a function recursively.
        /// </summary>
        public readonly ImmutableArray<Guid> ThreadIds;

        public ActiveStatementDebugInfo(
            ActiveInstructionId instructionId,
            string documentName,
            LinePositionSpan linePositionSpan,
            ImmutableArray<Guid> threadIds,
            ActiveStatementFlags flags)
        {
            ThreadIds = threadIds;
            InstructionId = instructionId;
            Flags = flags;
            DocumentName = documentName;
            LinePositionSpan = linePositionSpan;
        }
    }
}
