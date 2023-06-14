// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal readonly struct DynamicLocalInfo(ImmutableArray<bool> flags, int slotId, string localName)
    {
        public readonly ImmutableArray<bool> Flags = flags;
        public readonly int SlotId = slotId;
        public readonly string LocalName = localName;
    }
}
