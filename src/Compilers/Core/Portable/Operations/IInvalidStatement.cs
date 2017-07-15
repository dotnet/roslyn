// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a syntactically or semantically invalid C# or VB statement.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInvalidStatement : IOperation
    {
        /// <summary>
        /// Child operations.
        /// </summary>
        ImmutableArray<IOperation> Children { get; }
    }
}

