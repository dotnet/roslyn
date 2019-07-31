// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents one arm of a switch expression.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ISwitchExpressionArmOperation : IOperation
    {
        /// <summary>
        /// The pattern to match.
        /// </summary>
        IPatternOperation Pattern { get; }
        /// <summary>
        /// Guard (when clause expression) associated with the switch arm, if any.
        /// </summary>
        IOperation Guard { get; }
        /// <summary>
        /// Result value of the enclosing switch expression when this arm matches.
        /// </summary>
        IOperation Value { get; }
        /// <summary>
        /// Locals declared within the switch arm (e.g. pattern locals and locals declared in the guard) scoped to the arm.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
    }
}
