// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an operation that gets <see cref="System.Type" /> for the given <see cref="TypeOperand" />.
    /// <para>
    /// Current usage:
    ///  (1) C# typeof expression.
    ///  (2) VB GetType expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ITypeOfOperation : IOperation
    {
        /// <summary>
        /// Type operand.
        /// </summary>
        ITypeSymbol TypeOperand { get; }
    }
}
