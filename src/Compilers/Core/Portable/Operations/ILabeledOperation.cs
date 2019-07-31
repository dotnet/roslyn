// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an operation with a label.
    /// <para>
    /// Current usage:
    ///  (1) C# labeled statement.
    ///  (2) VB label statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ILabeledOperation : IOperation
    {
        /// <summary>
        /// Label that can be the target of branches.
        /// </summary>
        ILabelSymbol Label { get; }
        /// <summary>
        /// Operation that has been labeled. In VB, this is always null.
        /// </summary>
        IOperation Operation { get; }
    }
}
