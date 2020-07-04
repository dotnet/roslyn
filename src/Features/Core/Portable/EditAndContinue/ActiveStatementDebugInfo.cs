// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Active statement debug information retrieved from the runtime and the PDB.
    /// </summary>
    internal readonly struct ActiveStatementDebugInfo
    {
        /// <summary>
        /// Serialization - JSON serialization does not work with nested readonly structs.
        /// </summary>
        internal sealed class Data
        {
            public Guid ModuleId;
            public int MethodToken;
            public int MethodVersion;
            public int ILOffset;
            public string? DocumentName;
            public LinePositionSpan LinePositionSpan;
            public ActiveStatementFlags Flags;
            public ImmutableArray<Guid> ThreadIds;

            public ActiveStatementDebugInfo Deserialize()
                => new ActiveStatementDebugInfo(
                    new ActiveInstructionId(ModuleId, MethodToken, MethodVersion, ILOffset),
                    DocumentName,
                    LinePositionSpan,
                    ThreadIds,
                    Flags);
        }

        /// <summary>
        /// The instruction of the active statement that is being executed.
        /// </summary>
        public readonly ActiveInstructionId InstructionId;

        /// <summary>
        /// Document name as found in the PDB, or null if the debugger can't determine the location of the active statement.
        /// </summary>
        public readonly string? DocumentName;

        /// <summary>
        /// Location of the closest non-hidden sequence point retrieved from the PDB, 
        /// or default(<see cref="LinePositionSpan"/>) if the debugger can't determine the location of the active statement.
        /// </summary>
        public readonly LinePositionSpan LinePositionSpan;

        /// <summary>
        /// Aggregated across <see cref="ThreadIds"/>.
        /// </summary>
        public readonly ActiveStatementFlags Flags;

        /// <summary>
        /// Threads that share the instruction. May contain duplicates in case a thread is executing a function recursively.
        /// </summary>
        public readonly ImmutableArray<Guid> ThreadIds;

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

        internal Data Serialize()
            => new Data()
            {
                ModuleId = InstructionId.MethodId.ModuleId,
                MethodToken = InstructionId.MethodId.Token,
                MethodVersion = InstructionId.MethodId.Version,
                ILOffset = InstructionId.ILOffset,
                DocumentName = DocumentName,
                LinePositionSpan = LinePositionSpan,
                Flags = Flags,
                ThreadIds = ThreadIds,
            };
    }
}
