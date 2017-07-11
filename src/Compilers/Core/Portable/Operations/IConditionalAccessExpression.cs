// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an expression that includes a ? or ?. conditional access instance expression.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IConditionalAccessExpression : IOperation
    {
        /// <summary>
        /// Expression to be evaluated if the conditional instance is non null.
        /// </summary>
        IOperation ConditionalValue { get; }
        /// <summary>
        /// Expresson that is conditionally accessed.
        /// </summary>
        IOperation ConditionalInstance { get; }
    }
}

