// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an operation that gets a string value for the <see cref="Argument" /> name.
    /// <para>
    /// Current usage:
    ///  (1) C# nameof expression.
    ///  (2) VB NameOf expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface INameOfOperation : IOperation
    {
        /// <summary>
        /// Argument to the name of operation.
        /// </summary>
        IOperation Argument { get; }
    }
}
