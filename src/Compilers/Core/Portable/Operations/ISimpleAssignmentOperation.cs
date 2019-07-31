// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a simple assignment operation.
    /// <para>
    /// Current usage:
    ///  (1) C# simple assignment expression.
    ///  (2) VB simple assignment expression.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ISimpleAssignmentOperation : IAssignmentOperation
    {
        /// <summary>
        /// Is this a ref assignment
        /// </summary>
        bool IsRef { get; }
    }
}
