// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an increment or decrement expression in C#.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IIncrementExpression : IOperation, IHasOperatorMethodExpression
    {
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        IOperation Target { get; }

        /// <summary>
        /// <code>true</code> if this is a decrement expression.
        /// <code>false</code> if this is an increment expression.
        /// </summary>
        bool IsDecrement { get; }

        /// <summary>
        /// <code>true</code> if this is a postfix expression.
        /// <code>false</code> if this is a prefix expression.
        /// </summary>
        bool IsPostfix { get; }
    }
}

