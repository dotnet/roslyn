// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an operation with two operands and a binary operator that produces a result with a non-null type.
    /// <para>
    /// Current usage:
    ///  (1) C# binary operator expression.
    ///  (2) VB binary operator expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IBinaryOperation : IOperation
    {
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        BinaryOperatorKind OperatorKind { get; }
        /// <summary>
        /// Left operand.
        /// </summary>
        IOperation LeftOperand { get; }
        /// <summary>
        /// Right operand.
        /// </summary>
        IOperation RightOperand { get; }
        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol OperatorMethod { get; }
        /// <summary>
        /// <see langword="true" /> if this is a 'lifted' binary operator.  When there is an
        /// operator that is defined to work on a value type, 'lifted' operators are
        /// created to work on the <see cref="System.Nullable{T}" /> versions of those
        /// value types.
        /// </summary>
        bool IsLifted { get; }
        /// <summary>
        /// <see langword="true" /> if this is a 'checked' binary operator.
        /// </summary>
        bool IsChecked { get; }
        /// <summary>
        /// <see langword="true" /> if the comparison is text based for string or object comparison in VB.
        /// </summary>
        bool IsCompareText { get; }
    }
}
