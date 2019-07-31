// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a declarator that declares multiple individual variables.
    /// </summary>
    /// <para>
    /// Current Usage:
    ///   (1) C# VariableDeclaration
    ///   (2) C# fixed declarations
    ///   (3) C# using declarations
    ///   (4) VB Dim statement declaration groups
    ///   (5) VB Using statement variable declarations
    /// </para>
    /// <remarks>
    /// The initializer of this node is applied to all individual declarations in <see cref="Declarators" />. There cannot
    /// be initializers in both locations except in invalid code scenarios.
    /// In C#, this node will never have an initializer.
    /// This corresponds to the VariableDeclarationSyntax in C#, and the VariableDeclaratorSyntax in Visual Basic.
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IVariableDeclarationOperation : IOperation
    {
        /// <summary>
        /// Individual variable declarations declared by this multiple declaration.
        /// </summary>
        /// <remarks>
        /// All <see cref="IVariableDeclarationGroupOperation" /> will have at least 1 <see cref="IVariableDeclarationOperation" />,
        /// even if the declaration group only declares 1 variable.
        /// </remarks>
        ImmutableArray<IVariableDeclaratorOperation> Declarators { get; }
        /// <summary>
        /// Optional initializer of the variable.
        /// </summary>
        /// <remarks>
        /// In C#, this will always be null.
        /// </remarks>
        IVariableInitializerOperation Initializer { get; }
        /// <summary>
        /// Array dimensions supplied to an array declaration in error cases, ignored by the compiler. This is only used for the C# case of
        /// RankSpecifierSyntax nodes on an ArrayTypeSyntax.
        /// </summary>
        ImmutableArray<IOperation> IgnoredDimensions { get; }
    }
}
