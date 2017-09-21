// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a reference to an array element.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    internal interface IArrayElementReferenceExpression : IOperation // https://github.com/dotnet/roslyn/issues/22006
    {
        /// <summary>
        /// Array to be indexed.
        /// </summary>
        IOperation ArrayReference { get; }
        /// <summary>
        /// Indices that specify an individual element.
        /// </summary>
        ImmutableArray<IOperation> Indices { get; }
    }
}

