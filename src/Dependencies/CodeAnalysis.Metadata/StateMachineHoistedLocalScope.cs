// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Debugging
{
    internal struct StateMachineHoistedLocalScope
    {
        public readonly int StartOffset;
        public readonly int EndOffset;

        public StateMachineHoistedLocalScope(int startoffset, int endOffset)
        {
            StartOffset = startoffset;
            EndOffset = endOffset;
        }
    }
}
