// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Represents additional info needed by async method implementation methods 
    /// (MoveNext methods) to properly emit necessary PDB data for async debugging.
    /// </summary>
    internal sealed class AsyncMoveNextBodyDebugInfo : StateMachineMoveNextBodyDebugInfo
    {
        /// <summary> 
        /// IL offset of catch handler or -1 
        /// </summary>
        public readonly int CatchHandlerOffset;

        /// <summary> 
        /// Set of IL offsets where await operators yield control
        ///  </summary>
        public readonly ImmutableArray<int> YieldOffsets;

        /// <summary> 
        /// Set of IL offsets where await operators are to be resumed 
        /// </summary>
        public readonly ImmutableArray<int> ResumeOffsets;

        public AsyncMoveNextBodyDebugInfo(
            Cci.IMethodDefinition kickoffMethod,
            int catchHandlerOffset,
            ImmutableArray<int> yieldOffsets,
            ImmutableArray<int> resumeOffsets)
            : base(kickoffMethod)
        {
            Debug.Assert(!yieldOffsets.IsDefault);
            Debug.Assert(!resumeOffsets.IsDefault);
            Debug.Assert(yieldOffsets.Length == resumeOffsets.Length);

            CatchHandlerOffset = catchHandlerOffset;
            YieldOffsets = yieldOffsets;
            ResumeOffsets = resumeOffsets;
        }
    }
}
