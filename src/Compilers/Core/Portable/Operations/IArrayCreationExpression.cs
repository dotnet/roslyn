// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents the creation of an array instance.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IArrayCreationExpression : IOperation
    {
        /// <summary>
        /// Sizes of the dimensions of the created array instance.
        /// </summary>
        ImmutableArray<IOperation> DimensionSizes { get; }
        /// <summary>
        /// Values of elements of the created array instance.
        /// </summary>
        IArrayInitializer Initializer { get; }
    }
}

