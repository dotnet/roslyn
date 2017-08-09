// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Kind of unary operator
    /// </summary>
    public enum UnaryOperatorKind
    {
        None = 0x0,

        BitwiseNegation = 0x1,
        LogicalNot = 0x2,
        Plus = 0x3,
        Minus = 0x4,
        True = 0x5,
        False = 0x6,
        BitwiseOrLogicalNot = 0x7,

        Invalid = 0xff
    }
}

