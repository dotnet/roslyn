// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# ?: or VB If expression.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IConditionalChoiceExpression : IOperation
    {
        /// <summary>
        /// Condition to be tested.
        /// </summary>
        IOperation Condition { get; }
        /// <summary>
        /// Value evaluated if the Condition is true.
        /// </summary>
        IOperation IfTrueValue { get; }
        /// <summary>
        /// Value evaluated if the Condition is false.
        /// </summary>
        IOperation IfFalseValue { get; }
    }
}

