// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an operation to throw an exception.
    /// <para>
    /// Current usage:
    ///  (1) C# throw expression in a throw statement or a standalone throw expression.
    ///  (2) VB throw expression in a Throw statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IThrowOperation : IOperation
    {
        /// <summary>
        /// Instance of an exception being thrown.
        /// </summary>
        IOperation Exception { get; }
    }
}

