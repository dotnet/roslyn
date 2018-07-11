// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a loop operation.
    /// <para>
    /// Current usage:
    ///  (1) C# 'while', 'for', 'foreach' and 'do' loop statements
    ///  (2) VB 'While', 'ForTo', 'ForEach', 'Do While' and 'Do Until' loop statements
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ILoopOperation : IOperation
    {
        /// <summary>
        /// Kind of the loop.
        /// </summary>
        LoopKind LoopKind { get; }
        /// <summary>
        /// Body of the loop.
        /// </summary>
        IOperation Body { get; }
        /// <summary>
        /// Declared locals.
        /// </summary>
        ImmutableArray<ILocalSymbol> Locals { get; }
        /// <summary>
        /// Loop continue label.
        /// </summary>
        ILabelSymbol ContinueLabel { get; }
        /// <summary>
        /// Loop exit/break label.
        /// </summary>
        ILabelSymbol ExitLabel { get; }
    }
}

