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
        /// Expression-bodied local functions are normalized here to be a single <see cref="IOperation"/>
        /// inside of a block statement.
        /// </remarks>
        IBlockOperation Body { get; }
    }
}

