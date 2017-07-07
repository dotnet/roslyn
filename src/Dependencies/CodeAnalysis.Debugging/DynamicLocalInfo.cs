// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal struct DynamicLocalInfo
    {
        public readonly ImmutableArray<bool> Flags;
        public readonly int SlotId;
        public readonly string LocalName;

        public DynamicLocalInfo(ImmutableArray<bool> flags, int slotId, string localName)
        {
            Flags = flags;
            SlotId = slotId;
            LocalName = localName;
        }
    }
}
