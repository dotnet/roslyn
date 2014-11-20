// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.Cci
{
    internal struct StateMachineHoistedLocalScope
    {
        public readonly uint StartOffset;
        public readonly uint EndOffset;

        public StateMachineHoistedLocalScope(uint startOffset, uint endOffset)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
        }
    }
}
