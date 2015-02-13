// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.Cci
{
    internal struct StateMachineHoistedLocalScope
    {
        /// <summary>
        /// The offset of the first operation in the scope.
        /// </summary>
        public readonly int StartOffset;

        /// <summary>
        /// The offset of the first operation outside of the scope, or the method body length.
        /// </summary>
        public readonly int EndOffset;

        public StateMachineHoistedLocalScope(int startOffset, int endOffset)
        {
            Debug.Assert(startOffset >= 0);
            Debug.Assert(endOffset > startOffset);

            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        public int Length => EndOffset - StartOffset;
        public bool IsDefault => StartOffset == 0 && EndOffset == 0;
    }
}
