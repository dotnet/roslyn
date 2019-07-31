// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a constituent string literal part of an interpolated string operation.
    /// <para>
    /// Current usage:
    ///  (1) C# interpolated string text.
    ///  (2) VB interpolated string text.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInterpolatedStringTextOperation : IInterpolatedStringContentOperation
    {
        /// <summary>
        /// Text content.
        /// </summary>
        IOperation Text { get; }
    }
}
