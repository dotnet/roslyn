// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a query continuation in C#.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IQueryContinuation : IOperation
    {
        /// <summary>
        /// Declared symbol.
        /// </summary>
        IRangeVariableSymbol DeclaredSymbol { get; }

        /// <summary>
        /// Query body of the continuation.
        /// </summary>
        IOperation QueryBody { get; }
    }
}
