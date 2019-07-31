// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a pattern with a constant value.
    /// <para>
    /// Current usage:
    ///  (1) C# constant pattern.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IConstantPatternOperation : IPatternOperation
    {
        /// <summary>
        /// Constant value of the pattern operation.
        /// </summary>
        IOperation Value { get; }
    }
}
