// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a declaration expression in C#.
    /// Unlike a regular variable declaration, this operation represents an "expression" declaring a variable.
    /// For example,
    ///   1. "var (x, y)" is a deconstruction declaration expression with variables x and y.
    ///   2. "(var x, var y)" is a tuple expression with two declaration expressions.
    ///   3. "M(out var x);" is an invocation expression with an out "var x" declaration expression.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDeclarationExpression : IOperation
    {
        /// <summary>
        /// Underlying expression.
        /// </summary>
        IOperation Expression { get; }
    }
}

