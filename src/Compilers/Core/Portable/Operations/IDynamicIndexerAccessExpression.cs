// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a dynamic indexer expression in C#.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDynamicIndexerAccessExpression : IHasDynamicArgumentsExpression
    {
        /// <summary>
        /// Dynamically indexed expression.
        /// </summary>
        IOperation Expression { get; }
    }
}
