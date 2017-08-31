// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an expression that tests if a value is of a specific type.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IIsTypeExpression : IOperation
    {
        /// <summary>
        /// Value to test.
        /// </summary>
        IOperation Operand { get; }

        /// <summary>
        /// Type for which to test.
        /// </summary>
        ITypeSymbol IsType { get; }

        /// <summary>
        /// Flag indicating if this is an "is not" type expression.
        /// True for VB "TypeOf ... IsNot ..." expression.
        /// False, otherwise.
        /// </summary>
        bool IsNotTypeExpression { get; }
    }
}
