// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a block containing a sequence of operations and local declarations.
    /// <para>
    /// Current usage:
    ///  (1) C# "{ ... }" block statement.
    ///  (2) VB implicit block statement for method bodies and other block scoped statements.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IBlockOperation : IOperation
    {
        /// <summary>
        /// Operations contained within the block.
        /// </summary>
        ImmutableArray<IOperation> Operations { get; }
        /// <summary>
        /// Local declarations contained within the block.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
    }
}
