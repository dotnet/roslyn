// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal struct TupleElementNamesInfo
    {
        internal readonly ImmutableArray<string> ElementNames;
        internal readonly int SlotIndex; // Locals only
        internal readonly string LocalName;
        internal readonly int ScopeStart; // Constants only
        internal readonly int ScopeEnd; // Constants only

        internal TupleElementNamesInfo(ImmutableArray<string> elementNames, int slotIndex, string localName, int scopeStart, int scopeEnd)
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
