// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        None,

        /// <summary>
        /// Covariant (<c>out</c>).
        /// </summary>
        Out,

        /// <summary>
        /// Contravariant (<c>in</c>).
        /// </summary>
        In,
    }
}