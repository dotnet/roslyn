// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a local variable declaration.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IVariableDeclaration : IOperation
    {
        /// <summary>
        /// Symbols declared by the declaration. In VB, it's possible to declare multiple variables with the
        /// same initializer. In C#, this will always have a single symbol.
        /// </summary>
        ImmutableArray<ILocalSymbol> Variables { get; }

        /// <summary>
        /// Optional initializer of the variable.
        /// </summary>
        IVariableInitializer Initializer { get; }
    }
}

