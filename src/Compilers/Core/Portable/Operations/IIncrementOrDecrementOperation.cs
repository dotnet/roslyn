// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an <see cref="OperationKind.Increment"/> or <see cref="OperationKind.Decrement"/> operation.
    /// Note that this operation is different from an <see cref="IUnaryOperation"/> as it mutates the <see cref="Target"/>,
    /// while unary operator expression does not mutate it's operand.
    /// <para>
    /// Current usage:
    ///  (1) C# increment expression or decrement expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IIncrementOrDecrementOperation : IOperation
    {
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        IOperation Target { get; }

        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol OperatorMethod { get; }

        /// <summary>
        /// <code>true</code> if this is a postfix expression.
        /// <code>false</code> if this is a prefix expression.
        /// </summary>
        bool IsPostfix { get; }

        /// <summary>
        /// <code>true</code> if this is a 'lifted' increment operator.  When there is an 
        /// operator that is defined to work on a value type, 'lifted' operators are 
        /// created to work on the <see cref="System.Nullable{T}"/> versions of those
        /// value types.
        /// </summary>
        bool IsLifted { get; }

        /// <summary>
        /// <code>true</code> if overflow checking is performed for the arithmetic operation.
        /// </summary>
        bool IsChecked { get; }
    }
}

