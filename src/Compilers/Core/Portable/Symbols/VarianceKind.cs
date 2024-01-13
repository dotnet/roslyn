// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// An enumeration declaring the kinds of variance supported for generic type parameters.
    /// </summary>
    public enum VarianceKind : short
    {
        /// <summary>
        /// Invariant.
        /// </summary>
        None = 0,

        /// <summary>
        /// Covariant (<c>out</c>).
        /// </summary>
        Out = 1,

        /// <summary>
        /// Contravariant (<c>in</c>).
        /// </summary>
        In = 2,
    }
}
