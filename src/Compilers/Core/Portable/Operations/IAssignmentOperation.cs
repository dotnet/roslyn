// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a base interface for assignments.
    /// <para>
    /// Current usage:
    ///  (1) C# simple, compound and deconstruction assignment expressions.
    ///  (2) VB simple and compound assignment expressions.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IAssignmentOperation : IOperation
    {
        /// <summary>
        /// Target of the assignment.
        /// </summary>
        IOperation Target { get; }
        /// <summary>
        /// Value to be assigned to the target of the assignment.
        /// </summary>
        IOperation Value { get; }
    }
}
