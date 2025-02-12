// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal readonly struct StateMachineHoistedLocalScope
    {
        /// <summary>
        /// The offset of the first operation in the scope.
        /// </summary>
        public readonly int StartOffset;

        /// <summary>
        /// The offset of the first operation outside of the scope, or the method body length.
        /// If zero then <see cref="StartOffset"/> is also zero and the slot represents a synthesized local.
        /// </summary>
        public readonly int EndOffset;

        public StateMachineHoistedLocalScope(int startOffset, int endOffset)
        {
            Debug.Assert(startOffset >= 0);
            Debug.Assert(endOffset > startOffset || startOffset == 0 && endOffset == 0);

            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        public int Length => EndOffset - StartOffset;
        public bool IsDefault => StartOffset == 0 && EndOffset == 0;
    }
}
