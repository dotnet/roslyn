// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a constituent interpolation part of an interpolated string operation.
    /// <para>
    /// Current usage:
    ///  (1) C# interpolation part.
    ///  (2) VB interpolation part.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInterpolationOperation : IInterpolatedStringContentOperation
    {
        /// <summary>
        /// Expression of the interpolation.
        /// </summary>
        IOperation Expression { get; }
        /// <summary>
        /// Optional alignment of the interpolation.
        /// </summary>
        IOperation Alignment { get; }
        /// <summary>
        /// Optional format string of the interpolation.
        /// </summary>
        IOperation FormatString { get; }
    }
}
