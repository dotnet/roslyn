// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# while or do statement, or a VB While or Do statement.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IWhileUntilLoopStatement : IForWhileUntilLoopStatement
    {
        /// <summary>
        /// True if the loop test executes at the top of the loop; false if the loop test executes at the bottom of the loop.
        /// </summary>
        bool IsTopTest { get; }
        /// <summary>
        /// True if the loop is a while loop; false if the loop is an until loop.
        /// </summary>
        bool IsWhile { get; }
    }
}

