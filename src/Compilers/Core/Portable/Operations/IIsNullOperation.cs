// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    /// <summary>
    /// Represents result of checking whether the <see cref="Operand" /> is null.
    /// For reference types this checks if the <see cref="Operand" /> is a null reference,
    /// for nullable types this checks if the <see cref="Operand" /> doesn’t have a value.
    /// The node is produced as part of a flow graph during rewrite of <see cref="ICoalesceOperation" />
    /// and <see cref="IConditionalAccessOperation" /> nodes.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IIsNullOperation : IOperation
    {
        /// <summary>
        /// Value to check.
        /// </summary>
        IOperation Operand { get; }
    }
}
