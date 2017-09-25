﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an increment (<see cref="OperationKind.IncrementExpression"/>) or decrement (<see cref="OperationKind.DecrementExpression"/>) expression in C#.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IIncrementOrDecrementExpression : IOperation, IHasOperatorMethodExpression
    {
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        IOperation Target { get; }

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

