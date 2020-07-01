// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Active statement debug information retrieved from the runtime and the PDB.
    /// </summary>
    [DataContract]
    internal readonly struct ActiveStatementDebugInfo
    {
        /// <summary>
        /// The instruction of the active statement that is being executed.
        /// </summary>
        [DataMember(Order = 0)]
        public readonly ActiveInstructionId InstructionId;

        /// <summary>
        /// Document name as found in the PDB, or null if the debugger can't determine the location of the active statement.
        /// </summary>
        [DataMember(Order = 1)]
        public readonly string? DocumentName;

        /// <summary>
        /// Location of the closest non-hidden sequence point retrieved from the PDB, 
        /// or default(<see cref="LinePositionSpan"/>) if the debugger can't determine the location of the active statement.
        /// </summary>
        [DataMember(Order = 2)]
        public readonly LinePositionSpan LinePositionSpan;

        /// <summary>
        /// Threads that share the instruction. May contain duplicates in case a thread is executing a function recursively.
        /// </summary>
        [DataMember(Order = 3)]
        public readonly ImmutableArray<Guid> ThreadIds;

        /// <summary>
        /// Aggregated across <see cref="ThreadIds"/>.
        /// </summary>
        [DataMember(Order = 4)]
        public readonly ActiveStatementFlags Flags;

        public ActiveStatementDebugInfo(
            ActiveInstructionId instructionId,
            string? documentName,
            LinePositionSpan linePositionSpan,
            ImmutableArray<Guid> threadIds,
            ActiveStatementFlags flags)
        {
            Debug.Assert(!threadIds.IsDefaultOrEmpty);

            ThreadIds = threadIds;
            InstructionId = instructionId;
            Flags = flags;
            DocumentName = documentName;
            LinePositionSpan = linePositionSpan;
        }

        public bool HasSourceLocation => DocumentName != null;
    }
}
