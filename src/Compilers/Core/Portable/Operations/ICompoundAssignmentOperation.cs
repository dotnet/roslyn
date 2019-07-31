// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a compound assignment that mutates the target with the result of a binary operation.
    /// <para>
    /// Current usage:
    ///  (1) C# compound assignment expression.
    ///  (2) VB compound assignment expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ICompoundAssignmentOperation : IAssignmentOperation
    {
        /// <summary>
        /// Kind of binary operation.
        /// </summary>
        BinaryOperatorKind OperatorKind { get; }
        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol OperatorMethod { get; }
        /// <summary>
        /// <see langword="true" /> if this assignment contains a 'lifted' binary operation.
        /// </summary>
        bool IsLifted { get; }
        /// <summary>
        /// <see langword="true" /> if overflow checking is performed for the arithmetic operation.
        /// </summary>
        bool IsChecked { get; }
        /// <summary>
        /// Conversion applied to <see cref="IAssignmentOperation.Target" /> before the operation occurs.
        /// </summary>
        CommonConversion InConversion { get; }
        /// <summary>
        /// Conversion applied to the result of the binary operation, before it is assigned back to
        /// <see cref="IAssignmentOperation.Target" />.
        /// </summary>
        CommonConversion OutConversion { get; }
    }
}
