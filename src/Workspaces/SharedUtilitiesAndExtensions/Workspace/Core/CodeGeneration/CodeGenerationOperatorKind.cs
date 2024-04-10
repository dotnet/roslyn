// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeGeneration;

internal enum CodeGenerationOperatorKind
{
    Addition = 0,
    BitwiseAnd = 1,
    BitwiseOr = 2,
    Concatenate = 3,
    Decrement = 4,
    Division = 5,
    Equality = 6,
    ExclusiveOr = 7,
    Exponent = 8,
    False = 9,
    GreaterThan = 10,
    GreaterThanOrEqual = 11,
    Increment = 12,
    Inequality = 13,
    IntegerDivision = 14,
    LeftShift = 15,
    LessThan = 16,
    LessThanOrEqual = 17,
    Like = 18,
    LogicalNot = 19,
    Modulus = 20,
    Multiplication = 21,
    OnesComplement = 22,
    RightShift = 23,
    Subtraction = 24,
    True = 25,
    UnaryPlus = 26,
    UnaryNegation = 27,
    UnsignedRightShift = 28,
}
