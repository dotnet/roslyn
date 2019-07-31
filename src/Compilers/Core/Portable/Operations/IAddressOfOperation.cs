// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an operation that creates a pointer value by taking the address of a reference.
    /// <para>
    /// Current usage:
    ///  (1) C# address of expression
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IAddressOfOperation : IOperation
    {
        /// <summary>
        /// Addressed reference.
        /// </summary>
        IOperation Reference { get; }
    }
}
