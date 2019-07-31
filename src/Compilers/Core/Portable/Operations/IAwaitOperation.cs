// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an await operation.
    /// <para>
    /// Current usage:
    ///  (1) C# await expression.
    ///  (2) VB await expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IAwaitOperation : IOperation
    {
        /// <summary>
        /// Awaited operation.
        /// </summary>
        IOperation Operation { get; }
    }
}
