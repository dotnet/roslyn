﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Flags]
    internal enum EmbeddableAttributes
    {
        IsReadOnlyAttribute = 0x01,
        IsByRefLikeAttribute = 0x02,
        IsUnmanagedAttribute = 0x04,
        NullableAttribute = 0x08,
        NullableContextAttribute = 0x10,
        NullablePublicOnlyAttribute = 0x20,
        NativeIntegerAttribute = 0x40,
    }
}
