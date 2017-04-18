// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an interpolated string expression.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInterpolatedStringExpression : IOperation
    {
        /// <summary>
        /// Constituent parts of interpolated string, each can be a string <see cref="ILiteralExpression"/> or an <see cref="IInterpolation"/>.
        /// </summary>
        ImmutableArray<IOperation> Parts { get; }
    }
}

