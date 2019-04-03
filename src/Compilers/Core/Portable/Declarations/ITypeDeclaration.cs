// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a type declaration.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ITypeDeclaration : INamespaceOrTypeDeclaration
    {
        /// <summary>
        /// Gets an enumerated value that identifies whether this type declaration is an class, interface, enum, and so on.
        /// </summary>
        TypeKind TypeKind { get; }

        /// <summary>
        /// Gets a <see cref="Accessibility"/> indicating the declared accessibility for this declaration.
        /// Returns NotApplicable if no accessibility is declared.
        Accessibility DeclaredAccessibility { get; }

        /// <summary>
        /// Returns the arity of this type declaration, or the number of type parameters it takes.
        /// A non-generic type has zero arity.
        int Arity { get; }

        /// <summary>
        /// Get all the children type declarations of this declaration.
        /// </summary>
        new ImmutableArray<ITypeDeclaration> Children { get; }
    }
}
