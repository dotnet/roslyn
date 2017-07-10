// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    public enum SimpleUnaryOperationKind
    {
        None = 0x0,

        BitwiseNegation = 0x1,
        LogicalNot = 0x2,
        PostfixIncrement = 0x3,
        PostfixDecrement = 0x4,
        PrefixIncrement = 0x5,
        PrefixDecrement = 0x6,
        Plus = 0x7,
        Minus = 0x8,
        True = 0x9,
        False = 0xa,
        BitwiseOrLogicalNot = 0xb,

        Invalid = 0xff
    }
}

