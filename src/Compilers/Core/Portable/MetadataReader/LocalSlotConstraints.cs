// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    [Flags]
    internal enum LocalSlotConstraints : byte
    {
        None = 0,
        ByRef = 1,
        Pinned = 2,
    }
}
