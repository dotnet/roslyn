// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a declaration expression operation. Unlike a regular variable declaration <see cref="IVariableDeclaratorOperation" /> and <see cref="IVariableDeclarationOperation" />, this operation represents an "expression" declaring a variable.
    /// <para>
    /// Current usage:
    ///  (1) C# declaration expression. For example,
    ///  (a) "var (x, y)" is a deconstruction declaration expression with variables x and y.
    ///  (b) "(var x, var y)" is a tuple expression with two declaration expressions.
    ///  (c) "M(out var x);" is an invocation expression with an out "var x" declaration expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDeclarationExpressionOperation : IOperation
    {
        /// <summary>
        /// Underlying expression.
        /// </summary>
        IOperation Expression { get; }
    }
}
