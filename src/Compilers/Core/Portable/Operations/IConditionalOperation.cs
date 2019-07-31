// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a conditional operation with:
    /// (1) <see cref="Condition" /> to be tested,
    /// (2) <see cref="WhenTrue" /> operation to be executed when <see cref="Condition" /> is true and
    /// (3) <see cref="WhenFalse" /> operation to be executed when the <see cref="Condition" /> is false.
    /// <para>
    /// Current usage:
    ///  (1) C# ternary expression "a ? b : c" and if statement.
    ///  (2) VB ternary expression "If(a, b, c)" and If Else statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IConditionalOperation : IOperation
    {
        /// <summary>
        /// Condition to be tested.
        /// </summary>
        IOperation Condition { get; }
        /// <summary>
        /// Operation to be executed if the <see cref="Condition" /> is true.
        /// </summary>
        IOperation WhenTrue { get; }
        /// <summary>
        /// Operation to be executed if the <see cref="Condition" /> is false.
        /// </summary>
        IOperation WhenFalse { get; }
        /// <summary>
        /// Is result a managed reference
        /// </summary>
        bool IsRef { get; }
    }
}
