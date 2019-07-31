// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a pattern matching operation.
    /// <para>
    /// Current usage:
    ///  (1) C# pattern.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IPatternOperation : IOperation
    {
        /// <summary>
        /// The input type to the pattern-matching operation.
        /// </summary>
        ITypeSymbol InputType { get; }
    }
}
