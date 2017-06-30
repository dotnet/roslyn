// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# while, for, or do statement, or a VB While, For, or Do statement.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IForWhileUntilLoopStatement : ILoopStatement
    {
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        IOperation Condition { get; }
    }
}

