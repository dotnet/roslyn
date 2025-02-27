// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal readonly struct DynamicLocalInfo
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
