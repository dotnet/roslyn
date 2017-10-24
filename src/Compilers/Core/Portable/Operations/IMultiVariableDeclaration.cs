// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents multiple declared variables in a single declarator.
    /// </summary>
    /// <para>
    /// Current Usage:
    ///   (1) VB As New statements
    ///   (2) VB multiple declarations in a single declarator
    /// </para>
    public interface IMultiVariableDeclaration : IVariableDeclaration
    {
        /// <summary>
        /// Individual variable declarations declared by this multiple declaration.
        /// </summary>
        ImmutableArray<ISingleVariableDeclaration> Declarations { get; }
    }
}
