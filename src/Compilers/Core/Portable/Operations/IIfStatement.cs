// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an if statement in C# or an If statement in VB.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IIfStatement : IOperation
    {
        /// <summary>
        /// Condition of the if statement. For C# there is naturally one clause per if, but for VB If statements with multiple clauses are rewritten to have only one.
        /// </summary>
        IOperation Condition { get; }
        /// <summary>
        /// Statement executed if the condition is true.
        /// </summary>
        IOperation IfTrueStatement { get; }
        /// <summary>
        /// Statement executed if the condition is false.
        /// </summary>
        IOperation IfFalseStatement { get; }
    }
}

