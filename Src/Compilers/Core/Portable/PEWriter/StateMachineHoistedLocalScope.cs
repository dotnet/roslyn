// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.Cci
{
    internal struct StateMachineHoistedLocalScope
    {
        public readonly uint Offset;
        public readonly uint Length;

        public StateMachineHoistedLocalScope(uint offset, uint length)
        {
            Offset = offset;
            Length = length;
        }
    }
}
