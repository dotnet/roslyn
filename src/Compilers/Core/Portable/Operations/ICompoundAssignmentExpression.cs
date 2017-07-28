// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an assignment expression that includes a binary operation.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ICompoundAssignmentExpression : IAssignmentExpression, IHasOperatorMethodExpression
    {
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        BinaryOperationKind BinaryOperationKind { get; }
        /// <summary>
        /// <code>true</code> if this is a 'Lifted' compound assignment.  When there is an 
        /// operator that is defined to work on a value type, 'lifted' operators are 
        /// created to work on the <see cref="System.Nullable{T}"/> versions of those
        /// value types.
        /// </summary>
        bool IsLifted { get; }
    }
}
