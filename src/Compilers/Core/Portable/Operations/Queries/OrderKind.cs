// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Kind of ordering for an <see cref="IOrderingExpression"/>
    /// </summary>
    public enum OrderKind
    {
        /// <summary>
        /// Represents no ordering for an <see cref="IOrderingExpression"/>.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Represents an ascending ordering for an <see cref="IOrderingExpression"/>.
        /// </summary>
        Ascending = 0x1,

        /// <summary>
        /// Represents an ascending ordering for an <see cref="IOrderingExpression"/>.
        /// </summary>
        Descending = 0x2,
    }
}

