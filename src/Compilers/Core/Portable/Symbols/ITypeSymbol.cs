﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a type.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ITypeSymbol : INamespaceOrTypeSymbol
    {
        /// <summary>
        /// An enumerated value that identifies whether this type is an array, pointer, enum, and so on.
        /// </summary>
        TypeKind TypeKind { get; }

        /// <summary>
        /// The declared base type of this type, or null. The object type, interface types,
        /// and pointer types do not have a base type. The base type of a type parameter
        /// is its effective base class.
        /// </summary>
        INamedTypeSymbol BaseType { get; }

        /// <summary>
        /// Gets the set of interfaces that this type directly implements. This set does not include
        /// interfaces that are base interfaces of directly implemented interfaces. This does
        /// include the interfaces declared as constraints on type parameters.
        /// </summary>
        ImmutableArray<INamedTypeSymbol> Interfaces { get; }

        /// <summary>
        /// The list of all interfaces of which this type is a declared subtype, excluding this type
        /// itself. This includes all declared base interfaces, all declared base interfaces of base
        /// types, and all declared base interfaces of those results (recursively). This also is the effective
        /// interface set of a type parameter. Each result
        /// appears exactly once in the list. This list is topologically sorted by the inheritance
        /// relationship: if interface type A extends interface type B, then A precedes B in the
        /// list. This is not quite the same as "all interfaces of which this type is a proper
        /// subtype" because it does not take into account variance: AllInterfaces for
        /// <c><![CDATA[IEnumerable<string>]]></c> will not include <c><![CDATA[IEnumerable<object>]]></c>;
        /// </summary>
        ImmutableArray<INamedTypeSymbol> AllInterfaces { get; }

        /// <summary>
        /// True if this type is known to be a reference type. It is never the case that
        /// <see cref="IsReferenceType"/> and <see cref="IsValueType"/> both return true. However, for an unconstrained type
        /// parameter, <see cref="IsReferenceType"/> and <see cref="IsValueType"/> will both return false.
        /// </summary>
        bool IsReferenceType { get; }

        /// <summary>
        /// True if this type is known to be a value type. It is never the case that
        /// <see cref="IsReferenceType"/> and <see cref="IsValueType"/> both return true. However, for an unconstrained type
        /// parameter, <see cref="IsReferenceType"/> and <see cref="IsValueType"/> will both return false.
        /// </summary>
        bool IsValueType { get; }

        /// <summary>
        /// Is this a symbol for an anonymous type (including anonymous VB delegate).
        /// </summary>
        bool IsAnonymousType { get; }

        /// <summary>
        /// Is this a symbol for a tuple .
        /// </summary>
        bool IsTupleType { get; }

        /// <summary>
        /// The original definition of this symbol. If this symbol is constructed from another
        /// symbol by type substitution then <see cref="OriginalDefinition"/> gets the original symbol as it was defined in
        /// source or metadata.
        /// </summary>
        new ITypeSymbol OriginalDefinition { get; }

        /// <summary>
        /// An enumerated value that identifies certain 'special' types such as <see cref="System.Object"/>. 
        /// Returns <see cref="Microsoft.CodeAnalysis.SpecialType.None"/> if the type is not special.
        /// </summary>
        SpecialType SpecialType { get; }

        /// <summary>
        /// Returns the corresponding symbol in this type or a base type that implements 
        /// interfaceMember (either implicitly or explicitly), or null if no such symbol exists
        /// (which might be either because this type doesn't implement the container of
        /// interfaceMember, or this type doesn't supply a member that successfully implements
        /// interfaceMember).
        /// </summary>
        /// <param name="interfaceMember">
        /// Must be a non-null interface property, method, or event.
        /// </param>
        ISymbol FindImplementationForInterfaceMember(ISymbol interfaceMember);
    }
}
