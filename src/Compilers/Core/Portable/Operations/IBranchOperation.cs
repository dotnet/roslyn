// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a branch operation.
    /// <para>
    /// Current usage:
    ///  (1) C# goto, break, or continue statement.
    ///  (2) VB GoTo, Exit ***, or Continue *** statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IBranchOperation : IOperation
    {
        /// <summary>
        /// Label that is the target of the branch.
        /// </summary>
        ILabelSymbol Target { get; }
        /// <summary>
        /// Kind of the branch.
        /// </summary>
        BranchKind BranchKind { get; }
    }
}
