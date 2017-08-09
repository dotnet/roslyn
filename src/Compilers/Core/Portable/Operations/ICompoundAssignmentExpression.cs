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
        BinaryOperatorKind OperatorKind { get; }
        /// <summary>
        /// <code>true</code> if this assignment contains a 'lifted' binary operation.
        /// </summary>
        bool IsLifted { get; }
    }
}
