// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue
{
    /// <summary>
    /// Active statement debug information retrieved from the runtime and the PDB.
    /// </summary>
    [DataContract]
    internal readonly struct ManagedActiveStatementDebugInfo
    {
        /// <summary>
        /// The instruction of the active statement that is being executed.
        /// </summary>
        [DataMember(Order = 0)]
        public readonly ManagedInstructionId ActiveInstruction;

        /// <summary>
        /// Document name as found in the PDB, or null if the debugger can't determine the location of the active statement.
        /// </summary>
        [DataMember(Order = 1)]
        public readonly string? DocumentName;

        /// <summary>
        /// Location of the closest non-hidden sequence point retrieved from the PDB, 
        /// or default(<see cref="SourceSpan"/>) if the debugger can't determine the location of the active statement.
        /// </summary>
        [DataMember(Order = 2)]
        public readonly SourceSpan SourceSpan;

        /// <summary>
        /// Aggregated across threads.
        /// </summary>
        [DataMember(Order = 3)]
        public readonly ActiveStatementFlags Flags;

        public ManagedActiveStatementDebugInfo(
            ManagedInstructionId activeInstruction,
            string? documentName,
            SourceSpan sourceSpan,
            ActiveStatementFlags flags)
        {
            ActiveInstruction = activeInstruction;
            Flags = flags;
            DocumentName = documentName;
            SourceSpan = sourceSpan;
        }

        public bool HasSourceLocation => DocumentName != null;
    }
}
