// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a try operation for exception handling code with a body, catch clauses and a finally handler.
    /// <para>
    /// Current usage:
    ///  (1) C# try statement.
    ///  (2) VB Try statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ITryOperation : IOperation
    {
        /// <summary>
        /// Body of the try, over which the handlers are active.
        /// </summary>
        IBlockOperation Body { get; }
        /// <summary>
        /// Catch clauses of the try.
        /// </summary>
        ImmutableArray<ICatchClauseOperation> Catches { get; }
        /// <summary>
        /// Finally handler of the try.
        /// </summary>
        IBlockOperation Finally { get; }
        /// <summary>
        /// Exit label for the try. This will always be null for C#.
        /// </summary>
        ILabelSymbol ExitLabel { get; }
    }
}

