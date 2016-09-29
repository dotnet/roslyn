﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Debugging
{
    internal struct DynamicLocalInfo
    {
        public readonly int FlagCount;
        public readonly ulong Flags;
        public readonly int SlotId;
        public readonly string LocalName;

        public DynamicLocalInfo(int flagCount, ulong flags, int slotId, string localName)
        {
            FlagCount = flagCount;
            Flags = flags;
            SlotId = slotId;
            LocalName = localName;
        }
    }
}
