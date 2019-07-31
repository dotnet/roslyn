// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an operation that drops the resulting value and the type of the underlying wrapped <see cref="Operation" />.
    /// <para>
    /// Current usage:
    ///  (1) C# expression statement.
    ///  (2) VB expression statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IExpressionStatementOperation : IOperation
    {
        /// <summary>
        /// Underlying operation with a value and type.
        /// </summary>
        IOperation Operation { get; }
    }
}
