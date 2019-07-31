// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a range operation.
    /// <para>
    /// Current usage:
    ///  (1) C# range expressions
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IRangeOperation : IOperation
    {
        /// <summary>
        /// Left operand.
        /// </summary>
        IOperation LeftOperand { get; }
        /// <summary>
        /// Right operand.
        /// </summary>
        IOperation RightOperand { get; }
        /// <summary>
        /// <code>true</code> if this is a 'lifted' range operation.  When there is an
        /// operator that is defined to work on a value type, 'lifted' operators are
        /// created to work on the <see cref="System.Nullable{T}" /> versions of those
        /// value types.
        /// </summary>
        bool IsLifted { get; }
        /// <summary>
        /// Factory method used to create this Range value. Can be null if appropriate
        /// symbol was not found.
        /// </summary>
        IMethodSymbol Method { get; }
    }
}
