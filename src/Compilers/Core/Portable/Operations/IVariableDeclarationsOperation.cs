// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an operation with one or more <see cref="IVariableDeclarationOperation"/>.
    /// <para>
    /// Current usage:
    ///  (1) C# local declaration statement.
    ///  (2) VB Dim statement.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IVariableDeclarationsOperation : IOperation
    {
        /// <summary>
        /// Variables declared.
        /// </summary>
        ImmutableArray<IVariableDeclarationOperation> Declarations { get; }
    }
}

