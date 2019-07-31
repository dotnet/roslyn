// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a variable declaration statement.
    /// </summary>
    /// <para>
    /// Current Usage:
    ///   (1) C# local declaration statement
    ///   (2) C# fixed statement
    ///   (3) C# using statement
    ///   (4) VB Dim statement
    ///   (5) VB Using statement
    /// </para>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IVariableDeclarationGroupOperation : IOperation
    {
        /// <summary>
        /// Variable declaration in the statement.
        /// </summary>
        /// <remarks>
        /// In C#, this will always be a single declaration, with all variables in <see cref="IVariableDeclarationOperation.Declarators" />.
        /// </remarks>
        ImmutableArray<IVariableDeclarationOperation> Declarations { get; }
    }
}
