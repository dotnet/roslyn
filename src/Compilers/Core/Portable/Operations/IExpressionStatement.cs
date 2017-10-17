// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# or VB statement that consists solely of an expression.
    /// Note that this node is semantically different from the operation representing the underlying expression 
    /// as it represents the value of the expression being dropped and also has no underlying type.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IExpressionStatement : IOperation
    {
        /// <summary>
        /// Expression of the statement.
        /// </summary>
        IOperation Expression { get; }
    }
}

