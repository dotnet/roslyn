// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageService
{
    internal enum PredefinedOperator
    {
        None = 0,
        Addition = 1,
        BitwiseAnd = 1 << 1,
        BitwiseOr = 1 << 2,
        Complement = 1 << 3,  // ~ or ! in C#, 'Not' in VB.
        Concatenate = 1 << 4,
        Decrement = 1 << 5,
        Division = 1 << 6,
        Equality = 1 << 7,
        ExclusiveOr = 1 << 8,
        Exponent = 1 << 9,
        GreaterThan = 1 << 10,
        GreaterThanOrEqual = 1 << 11,
        Increment = 1 << 12,
        Inequality = 1 << 13,
        IntegerDivision = 1 << 14,
        LeftShift = 1 << 15,
        LessThan = 1 << 16,
        LessThanOrEqual = 1 << 17,
        Like = 1 << 18,
        Modulus = 1 << 19,
        Multiplication = 1 << 20,
        RightShift = 1 << 21,
        Subtraction = 1 << 22,
        UnsignedRightShift = 1 << 23,
    }
}
