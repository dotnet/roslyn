// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal readonly struct TupleElementNamesInfo
    {
        internal readonly ImmutableArray<string?> ElementNames;
        internal readonly int SlotIndex; // Locals only
        internal readonly string LocalName;
        internal readonly int ScopeStart; // Constants only
        internal readonly int ScopeEnd; // Constants only

        internal TupleElementNamesInfo(ImmutableArray<string?> elementNames, int slotIndex, string localName, int scopeStart, int scopeEnd)
        {
            Debug.Assert(!elementNames.IsDefault);

            ElementNames = elementNames;
            SlotIndex = slotIndex;
            LocalName = localName;
            ScopeStart = scopeStart;
            ScopeEnd = scopeEnd;
        }
    }
}
