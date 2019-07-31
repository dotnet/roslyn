// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an operation that tests if a value matches a specific pattern.
    /// <para>
    /// Current usage:
    ///  (1) C# is pattern expression. For example, "x is int i".
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IIsPatternOperation : IOperation
    {
        /// <summary>
        /// Underlying operation to test.
        /// </summary>
        IOperation Value { get; }
        /// <summary>
        /// Pattern.
        /// </summary>
        IPatternOperation Pattern { get; }
    }
}
