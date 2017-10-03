// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# 'while' or a VB 'While' loop statement.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IWhileLoopStatement : ILoopStatement
    {
        /// <summary>
        /// Condition of the loop.
        /// </summary>
        IOperation Condition { get; }
    }
}

