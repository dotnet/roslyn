// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue
{
    /// <summary>
    /// Active statement debug information retrieved from the runtime and the PDB.
    /// </summary>
    /// <remarks>
    /// Creates a ManagedActiveStatementDebugInfo.
    /// </remarks>
    /// <param name="activeInstruction">Instruction of the active statement that is being executed.</param>
    /// <param name="documentName">Document name as found in the PDB, if the active statement location was determined.</param>
    /// <param name="sourceSpan">Location of the closest non-hidden sequence point from the active statement.</param>
    /// <param name="flags">Active statement flags shared across all threads that own the active statement.</param>
    [DataContract]
    internal readonly struct ManagedActiveStatementDebugInfo(
        ManagedInstructionId activeInstruction,
        string? documentName,
        SourceSpan sourceSpan,
        ActiveStatementFlags flags)
    {

        /// <summary>
        /// The instruction of the active statement that is being executed.
        /// </summary>
        [DataMember(Name = "activeInstruction")]
        public ManagedInstructionId ActiveInstruction { get; } = activeInstruction;

        /// <summary>
        /// Document name as found in the PDB, or null if the debugger can't determine the location of the active statement.
        /// </summary>
        [DataMember(Name = "documentName")]
        public string? DocumentName { get; } = documentName;

        /// <summary>
        /// Location of the closest non-hidden sequence point retrieved from the PDB, 
        /// or default(<see cref="SourceSpan"/>) if the debugger can't determine the location of the active statement.
        /// </summary>
        [DataMember(Name = "sourceSpan")]
        public SourceSpan SourceSpan { get; } = sourceSpan;

        /// <summary>
        /// Aggregated across any threads that own the active instruction.
        /// </summary>
        [DataMember(Name = "flags")]
        public ActiveStatementFlags Flags { get; } = flags;

        public bool HasSourceLocation => DocumentName != null;
    }
}
