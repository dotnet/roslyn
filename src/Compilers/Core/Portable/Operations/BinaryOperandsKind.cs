// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    public enum BinaryOperandsKind
    {
        None = 0x0,

        OperatorMethod = 0x100,
        Integer = 0x200,
        Unsigned = 0x300,
        Floating = 0x400,
        Decimal = 0x500,
        Boolean = 0x600,
        Enum = 0x700,
        Dynamic = 0x800,
        Object = 0x900,
        Pointer = 0xa00,
        PointerInteger = 0xb00,
        IntegerPointer = 0xc00,
        String = 0xd00,
        Delegate = 0xe00,
        Nullable = 0xf00,

        Invalid = 0xff00
    }
}

