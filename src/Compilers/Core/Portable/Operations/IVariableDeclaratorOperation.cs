// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a single variable declarator and initializer.
    /// </summary>
    /// <para>
    /// Current Usage:
    ///   (1) C# variable declarator
    ///   (2) C# catch variable declaration
    ///   (3) VB single variable declaration
    ///   (4) VB catch variable declaration
    /// </para>
    /// <remarks>
    /// In VB, the initializer for this node is only ever used for explicit array bounds initializers. This node corresponds to
    /// the VariableDeclaratorSyntax in C# and the ModifiedIdentifierSyntax in VB.
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IVariableDeclaratorOperation : IOperation
    {
        /// <summary>
        /// Symbol declared by this variable declaration
        /// </summary>
        ILocalSymbol Symbol { get; }
        /// <summary>
        /// Optional initializer of the variable.
        /// </summary>
        /// <remarks>
        /// If this variable is in an <see cref="IVariableDeclarationOperation" />, the initializer may be located
        /// in the parent operation. Call <see cref="OperationExtensions.GetVariableInitializer(IVariableDeclaratorOperation)" />
        /// to check in all locations. It is only possible to have initializers in both locations in VB invalid code scenarios.
        /// </remarks>
        IVariableInitializerOperation Initializer { get; }
        /// <summary>
        /// Additional arguments supplied to the declarator in error cases, ignored by the compiler. This only used for the C# case of
        /// DeclaredArgumentSyntax nodes on a VariableDeclaratorSyntax.
        /// </summary>
        ImmutableArray<IOperation> IgnoredArguments { get; }
    }
}
