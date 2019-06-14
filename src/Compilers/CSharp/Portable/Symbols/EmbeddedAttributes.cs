// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Flags]
    internal enum EmbeddedAttributes
    {
        IsReadOnlyAttribute = 0x01,
        IsByRefLikeAttribute = 0x02,
        IsUnmanagedAttribute = 0x04,
        NullableAttribute = 0x08,
        NullablePublicOnlyAttribute = 0x10,
    }
}
