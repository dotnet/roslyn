// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    public enum SimpleBinaryOperationKind
    {
        None = 0x0,

        Add = 0x1,
        Subtract = 0x2,
        Multiply = 0x3,
        Divide = 0x4,
        IntegerDivide = 0x5,
        Remainder = 0x6,
        Power = 0x7,
        LeftShift = 0x8,
        RightShift = 0x9,
        And = 0xa,
        Or = 0xb,
        ExclusiveOr = 0xc,
        ConditionalAnd = 0xd,
        ConditionalOr = 0xe,
        Concatenate = 0xf,

        // Relational operations.

        Equals = 0x10,
        ObjectValueEquals = 0x11,
        NotEquals = 0x12,
        ObjectValueNotEquals = 0x13,
        LessThan = 0x14,
        LessThanOrEqual = 0x15,
        GreaterThanOrEqual = 0x16,
        GreaterThan = 0x17,

        Like = 0x18,

        Invalid = 0xff
    }
}

