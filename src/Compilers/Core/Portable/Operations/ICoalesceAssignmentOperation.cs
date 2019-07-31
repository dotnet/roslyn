// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a coalesce assignment operation with a target and a conditionally-evaluated value:
    /// (1) <see cref="IAssignmentOperation.Target" /> is evaluated for null. If it is null, <see cref="IAssignmentOperation.Value" /> is evaluated and assigned to target.
    /// (2) <see cref="IAssignmentOperation.Value" /> is conditionally evaluated if <see cref="IAssignmentOperation.Target" /> is null, and the result is assigned into <see cref="IAssignmentOperation.Target" />.
    /// The result of the entire expression is<see cref="IAssignmentOperation.Target" />, which is only evaluated once.
    /// <para>
    /// Current usage:
    ///  (1) C# null-coalescing assignment operation <code>Target ??= Value</code>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ICoalesceAssignmentOperation : IAssignmentOperation
    {
    }
}
