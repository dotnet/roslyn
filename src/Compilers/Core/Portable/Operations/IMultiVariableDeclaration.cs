// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents multiple declared variables in a single declarator.
    /// </summary>
    /// <para>
    /// Current Usage:
    ///   (1) VB Dim statement declaration groups
    ///   (2) VB Using statement variable declarations
    /// </para>
    /// <remarks>
    /// The initializer of this node is applied to all individual declarations in <see cref="Declarations"/>. There cannot
    /// be initializers in both locations except in invalid code scenarios.
    ///
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IMultiVariableDeclaration : IVariableDeclaration
    {
        /// <summary>
        /// Individual variable declarations declared by this multiple declaration.
        /// </summary>
        ImmutableArray<ISingleVariableDeclaration> Declarations { get; }
    }
}
