// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a local function defined within a method.
    /// <para>
    /// Current usage:
    ///  (1) C# local function statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ILocalFunctionOperation : IOperation
    {
        /// <summary>
        /// Local function symbol.
        /// </summary>
        IMethodSymbol Symbol { get; }
        /// <summary>
        /// Body of the local function.
        /// </summary>
        /// <remarks>
        /// This will return the <see cref="BlockBody"/> if it exists, and the <see cref="ExpressionBody"/> if the <see cref="BlockBody"/> does not exist.
        /// If both exist, this will return just the <see cref="BlockBody"/>, and you must use <see cref="ExpressionBody"/> to retrieve the expression body.
        /// </remarks>
        IBlockOperation Body { get; }
        /// <summary>
        /// The block body of the local function, if it exists.
        /// </summary>
        IBlockOperation BlockBody { get; }
        /// <summary>
        /// The expression body of the local function, if it exists.
        /// </summary>
        IBlockOperation ExpressionBody { get; }
    }
}

