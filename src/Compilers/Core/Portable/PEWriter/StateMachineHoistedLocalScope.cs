// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.Cci
{
    internal struct StateMachineHoistedLocalScope
    {
        /// <summary>
        /// Start IL offset of the scope (inclusive).
        /// </summary>
        public readonly uint StartOffset;

        /// <summary>
        /// End IL offset of the scope (exlusive).
        /// </summary>
        public readonly uint EndOffset;

        public StateMachineHoistedLocalScope(uint startOffset, uint endOffset)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
        }
    }
}
