// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an operation with one operand.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IUnaryOperatorExpression : IHasOperatorMethodExpression
    {
        /// <summary>
        /// Kind of unary operation.
        /// </summary>
        UnaryOperatorKind OperatorKind { get; }
        /// <summary>
        /// Single operand.
        /// </summary>
        IOperation Operand { get; }

        /// <summary>
        /// <code>true</code> if this is a 'lifted' unary operator.  When there is an 
        /// operator that is defined to work on a value type, 'lifted' operators are 
        /// created to work on the <see cref="System.Nullable{T}"/> versions of those
        /// value types.
        /// </summary>
        bool IsLifted { get; }

        /// <summary>
        /// <code>true</code> if this is a 'checked' binary operator.
        /// </summary>
        bool IsChecked { get; }
    }
}
