// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Editing
{
    public enum OperatorKind
    {
        /// <summary>
        /// The name assigned to an implicit (widening) conversion.
        /// </summary>
        ImplicitConversion,

        /// <summary>
        /// The name assigned to an explicit (narrowing) conversion.
        /// </summary>
        ExplicitConversion,

        /// <summary>
        /// The name assigned to the Addition operator.
        /// </summary>
        Addition,

        /// <summary>
        /// The name assigned to the BitwiseAnd operator.
        /// </summary>
        BitwiseAnd,

        /// <summary>
        /// The name assigned to the BitwiseOr operator.
        /// </summary>
        BitwiseOr,

        /// <summary>
        /// The name assigned to the Decrement operator.
        /// </summary>
        Decrement,

        /// <summary>
        /// The name assigned to the Division operator.
        /// </summary>
        Division,

        /// <summary>
        /// The name assigned to the Equality operator.
        /// </summary>
        Equality,

        /// <summary>
        /// The name assigned to the ExclusiveOr operator.
        /// </summary>
        ExclusiveOr,

        /// <summary>
        /// The name assigned to the False operator.
        /// </summary>
        False,

        /// <summary>
        /// The name assigned to the GreaterThan operator.
        /// </summary>
        GreaterThan,

        /// <summary>
        /// The name assigned to the GreaterThanOrEqual operator.
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// The name assigned to the Increment operator.
        /// </summary>
        Increment,

        /// <summary>
        /// The name assigned to the Inequality operator.
        /// </summary>
        Inequality,

        /// <summary>
        /// The name assigned to the LeftShift operator.
        /// </summary>
        LeftShift,

        /// <summary>
        /// The name assigned to the LessThan operator.
        /// </summary>
        LessThan,

        /// <summary>
        /// The name assigned to the LessThanOrEqual operator.
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// The name assigned to the LogicalNot operator.
        /// </summary>
        LogicalNot,

        /// <summary>
        /// The name assigned to the Modulus operator.
        /// </summary>
        Modulus,

        /// <summary>
        /// The name assigned to the Multiply operator.
        /// </summary>
        Multiply,

        /// <summary>
        /// The name assigned to the OnesComplement operator.
        /// </summary>
        OnesComplement,

        /// <summary>
        /// The name assigned to the RightShift operator.
        /// </summary>
        RightShift,

        /// <summary>
        /// The name assigned to the Subtraction operator.
        /// </summary>
        Subtraction,

        /// <summary>
        /// The name assigned to the True operator.
        /// </summary>
        True,

        /// <summary>
        /// The name assigned to the UnaryNegation operator.
        /// </summary>
        UnaryNegation,

        /// <summary>
        /// The name assigned to the UnaryPlus operator.
        /// </summary>
        UnaryPlus,

        /// <summary>
        /// The name assigned to the UnsignedRightShift operator.
        /// </summary>
        UnsignedRightShift,
    }
}
