// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# try or a VB Try statement.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ITryStatement : IOperation
    {
        /// <summary>
        /// Body of the try, over which the handlers are active.
        /// </summary>
        IBlockStatement Body { get; }
        /// <summary>
        /// Catch clauses of the try.
        /// </summary>
        ImmutableArray<ICatchClause> Catches { get; }
        /// <summary>
        /// Finally handler of the try.
        /// </summary>
        IBlockStatement Finally { get; }
    }
}

