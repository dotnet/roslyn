﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an anonymous function expression, such as a lambda or anonymous delegate.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IAnonymousFunctionExpression : IOperation
    {
        /// <summary>
        /// Symbol of the anonymous function.
        /// </summary>
        IMethodSymbol Symbol { get; }
        /// <summary>
        /// Body of the anonymous function.
        /// </summary>
        IBlockStatement Body { get; }
    }
}

