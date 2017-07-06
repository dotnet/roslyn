// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal struct StateMachineHoistedLocalScope
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
