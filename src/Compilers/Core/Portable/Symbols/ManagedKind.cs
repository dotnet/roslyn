// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Enumeration of the possible "degrees of managed" for a type.
    /// </summary>
    [Flags]
    internal enum ManagedKind : byte
    {
        Unknown = 0,
        Unmanaged = 1,
        UnmanagedWithGenerics = 2, // considered "managed" in C# 7.3 and earlier
        Managed = 3,
    }
}
