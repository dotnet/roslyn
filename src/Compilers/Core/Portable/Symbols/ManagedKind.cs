// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// PROTOTYPE
    /// </summary>
    [Flags]
    internal enum ManagedKind : byte
    {
        Unknown = 0,
        Unmanaged = 1,
        UnmanagedWithGenerics = 2,
        Managed = 3,
    }
}
