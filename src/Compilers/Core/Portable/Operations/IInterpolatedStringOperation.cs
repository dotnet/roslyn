// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an interpolated string.
    /// <para>
    /// Current usage:
    ///  (1) C# interpolated string expression.
    ///  (2) VB interpolated string expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInterpolatedStringOperation : IOperation
    {
        /// <summary>
        /// Constituent parts of interpolated string, each of which is an <see cref="IInterpolatedStringContentOperation" />.
        /// </summary>
        ImmutableArray<IInterpolatedStringContentOperation> Parts { get; }
    }
}
