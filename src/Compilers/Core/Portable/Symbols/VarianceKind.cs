// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
