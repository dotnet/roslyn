// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a constituent interpolation part of an interpolated string expression.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInterpolation : IOperation
    {
        /// <summary>
        /// Expression
        /// </summary>
        IOperation Expression { get; }

        /// <summary>
        /// Optional alignment
        /// </summary>
        IOperation Alignment { get; }

        /// <summary>
        /// Optional format string.
        /// </summary>
        IOperation FormatString { get; }
    }
}

