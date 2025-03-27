// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Enumeration for kinds of scoped modifiers.
    /// </summary>
    public enum ScopedKind : byte
    {
        /// <summary>
        /// Not scoped.
        /// </summary>
        None = 0,

        /// <summary>
        /// A ref scoped to the enclosing block or method.
        /// </summary>
        ScopedRef = 1,

        /// <summary>
        /// A value scoped to the enclosing block or method.
        /// </summary>
        ScopedValue = 2,
    }
}
