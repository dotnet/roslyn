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
        /// Expression that will be evaulated and accessed if non null.
        /// </summary>
        IOperation Expression { get; }
        /// <summary>
        /// Expression to be evaluated if <see cref="Expression"/> is non null.
        /// </summary>
        IOperation WhenNotNull { get; }
    }
}

