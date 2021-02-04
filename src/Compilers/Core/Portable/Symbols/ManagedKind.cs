// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Enumeration of the possible managed kinds.
    /// </summary>
    [Flags]
    internal enum ManagedKind : byte
    {
        Unknown = 0,
        Unmanaged = 1,
        Managed = 2,
        WithGenerics = 4, // used with Unmanaged; considered "managed" in C# 7.3 and earlier
        WithNullableOfT = 8 // used with Unmanaged | WithGenerics 
    }
}
