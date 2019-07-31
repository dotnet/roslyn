// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an operation that tests if a value is of a specific type.
    /// <para>
    /// Current usage:
    ///  (1) C# "is" operator expression.
    ///  (2) VB "TypeOf" and "TypeOf IsNot" expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IIsTypeOperation : IOperation
    {
        /// <summary>
        /// Value to test.
        /// </summary>
        IOperation ValueOperand { get; }
        /// <summary>
        /// Type for which to test.
        /// </summary>
        ITypeSymbol TypeOperand { get; }
        /// <summary>
        /// Flag indicating if this is an "is not" type expression.
        /// True for VB "TypeOf ... IsNot ..." expression.
        /// False, otherwise.
        /// </summary>
        bool IsNegated { get; }
    }
}
