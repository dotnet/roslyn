// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a local function statement.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ILocalFunctionStatement : IOperation
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
        IBlockStatement Body { get; }
    }
}

