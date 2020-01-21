// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Kind of binary operator.
    /// </summary>
    public enum BinaryOperatorKind
    {
        /// <summary>
        /// Represents unknown or error operator kind.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Represents the '+' operator.
        /// </summary>
        Add = 0x1,

        /// <summary>
        /// Represents the '-' operator.
        /// </summary>
        Subtract = 0x2,

        /// <summary>
        /// Represents the '*' operator.
        /// </summary>
        Multiply = 0x3,

        /// <summary>
        /// Represents the '/' operator.
        /// </summary>
        Divide = 0x4,

        /// <summary>
        /// Represents the VB '\' integer divide operator.
        /// </summary>
        IntegerDivide = 0x5,

        /// <summary>
        /// Represents the C# '%' operator and VB 'Mod' operator.
        /// </summary>
        Remainder = 0x6,

        /// <summary>
        /// Represents the VB '^' exponentiation operator.
        /// </summary>
        Power = 0x7,

        /// <summary>
        /// Represents the <![CDATA['<<']]> operator.
        /// </summary>
        LeftShift = 0x8,

        /// <summary>
        /// Represents the <![CDATA['>>']]> operator.
        /// </summary>
        RightShift = 0x9,

        /// <summary>
        /// Represents the C# <![CDATA['&']]> operator and VB 'And' operator.
        /// </summary>
        And = 0xa,

        /// <summary>
        /// Represents the C# <![CDATA['|']]> operator and VB 'Or' operator.
        /// </summary>
        Or = 0xb,

        /// <summary>
        /// Represents the C# '^' operator and VB 'Xor' operator.
        /// </summary>
        ExclusiveOr = 0xc,

        /// <summary>
        /// Represents the C# <![CDATA['&&']]> operator and VB 'AndAlso' operator.
        /// </summary>
        ConditionalAnd = 0xd,

        /// <summary>
        /// Represents the C# <![CDATA['||']]> operator and VB 'OrElse' operator.
        /// </summary>
        ConditionalOr = 0xe,

        /// <summary>
        /// Represents the VB <![CDATA['&']]> operator for string concatenation.
        /// </summary>
        Concatenate = 0xf,

        // Relational operations.

        /// <summary>
        /// Represents the C# '==' operator and VB 'Is' operator and '=' operator for non-object typed operands.
        /// </summary>
        Equals = 0x10,

        /// <summary>
        /// Represents the VB '=' operator for object typed operands.
        /// </summary>
        ObjectValueEquals = 0x11,

        /// <summary>
        /// Represents the C# '!=' operator and VB 'IsNot' operator and <![CDATA['<>']]> operator for non-object typed operands.
        /// </summary>
        NotEquals = 0x12,

        /// <summary>
        /// Represents the VB <![CDATA['<>']]> operator for object typed operands.
        /// </summary>
        ObjectValueNotEquals = 0x13,

        /// <summary>
        /// Represents the <![CDATA['<']]> operator.
        /// </summary>
        LessThan = 0x14,

        /// <summary>
        /// Represents the <![CDATA['<=']]> operator.
        /// </summary>
        LessThanOrEqual = 0x15,

        /// <summary>
        /// Represents the <![CDATA['>=']]> operator.
        /// </summary>
        GreaterThanOrEqual = 0x16,

        /// <summary>
        /// Represents the <![CDATA['>']]> operator.
        /// </summary>
        GreaterThan = 0x17,

        /// <summary>
        /// Represents the VB 'Like' operator.
        /// </summary>
        Like = 0x18
    }
}
