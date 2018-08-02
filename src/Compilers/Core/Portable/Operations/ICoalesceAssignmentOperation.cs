// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a coalesce assignment operation with a target and a conditionally-evaluated value:
    ///  (1) <see cref="Target"/> is evaluated for null. If it is null, <see cref="WhenNull"/> is evaluated and assigned to target.
    ///  (2) <see cref="WhenNull"/> is conditionally evaluated if <see cref="Target"/> is null, and the result is assigned into <see cref="Target"/>.
    /// The result of the entire expression is <see cref="Target"/>, which is only evaluated once.
    /// <para>
    /// Current Usage:
    ///  (1) C# null-coalescing assignment operation <code>Target ??= WhenNull</code>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ICoalesceAssignmentOperation : IOperation
    {
        /// <summary>
        /// Operation to be unconditionally evaluated and assigned if null.
        /// </summary>
        IOperation Target { get; }
        /// <summary>
        /// Operation to be conditionally evaluated and assigned to <see cref="Target"/> if <see cref="Target"/> is null.
        /// </summary>
        IOperation WhenNull { get; }
        /// <summary>
        /// Whether the assignment is evaluated as a checked operation. This only has meaning in dynamic contexts.
        /// </summary>
        bool IsChecked { get; }
    }
}
