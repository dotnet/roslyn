// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a conditional goto.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IConditionalGotoStatement : IOperation
    {
        /// <summary>
        /// Condition of the branch.
        /// </summary>
        IOperation Condition { get; }
        /// <summary>
        /// Label that is the target of the branch.
        /// </summary>
        ILabelSymbol Target { get; }
        /// <summary>
        /// Indicates if the jump will be executed when the condition is true.
        /// Otherwise, it will be executed when the condition is false.
        /// </summary>
        bool JumpIfTrue { get; }
    }
}
