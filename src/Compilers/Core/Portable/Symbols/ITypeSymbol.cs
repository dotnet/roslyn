// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

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
        /// IEnumerable&lt;string&gt; will not include IEnumerable&lt;object&gt;.
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

        /// <summary>
        /// True if the type is ref-like, meaning it follows rules similar to CLR by-ref variables. False if the type
        /// is not ref-like or if the language has no concept of ref-like types.
        /// </summary>
        /// <remarks>
        /// <see cref="Span{T}" /> is a commonly used ref-like type.
        /// </remarks>
        bool IsRefLikeType { get; }

        /// <summary>
        /// True if the type is unmanaged according to language rules. False if managed or if the language
        /// has no concept of unmanaged types.
        /// </summary>
        bool IsUnmanagedType { get; }

        /// <summary>
        /// True if the type is readonly.
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// Converts an <c>ITypeSymbol</c> and a nullable flow state to a string representation.
        /// </summary>
        /// <param name="topLevelNullability">The top-level nullability to use for formatting.</param>
        /// <param name="format">Format or null for the default.</param>
        /// <returns>A formatted string representation of the symbol with the given nullability.</returns>
        string ToDisplayString(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null);

        /// <summary>
        /// Converts a symbol to an array of string parts, each of which has a kind. Useful
        /// for colorizing the display string.
        /// </summary>
        /// <param name="topLevelNullability">The top-level nullability to use for formatting.</param>
        /// <param name="format">Format or null for the default.</param>
        /// <returns>A read-only array of string parts.</returns>
        ImmutableArray<SymbolDisplayPart> ToDisplayParts(NullableFlowState topLevelNullability, SymbolDisplayFormat format = null);

        /// <summary>
        /// Converts a symbol to a string that can be displayed to the user. May be tailored to a
        /// specific location in the source code.
        /// </summary>
        /// <param name="semanticModel">Binding information (for determining names appropriate to
        /// the context).</param>
        /// <param name="topLevelNullability">The top-level nullability to use for formatting.</param>
        /// <param name="position">A position in the source code (context).</param>
        /// <param name="format">Formatting rules - null implies <see cref="SymbolDisplayFormat.MinimallyQualifiedFormat"/></param>
        /// <returns>A formatted string that can be displayed to the user.</returns>
        string ToMinimalDisplayString(
            SemanticModel semanticModel,
            NullableFlowState topLevelNullability,
            int position,
            SymbolDisplayFormat format = null);

        /// <summary>
        /// Convert a symbol to an array of string parts, each of which has a kind. May be tailored
        /// to a specific location in the source code. Useful for colorizing the display string.
        /// </summary>
        /// <param name="semanticModel">Binding information (for determining names appropriate to
        /// the context).</param>
        /// <param name="topLevelNullability">The top-level nullability to use for formatting.</param>
        /// <param name="position">A position in the source code (context).</param>
        /// <param name="format">Formatting rules - null implies <see cref="SymbolDisplayFormat.MinimallyQualifiedFormat"/></param>
        /// <returns>A read-only array of string parts.</returns>
        ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(
            SemanticModel semanticModel,
            NullableFlowState topLevelNullability,
            int position,
            SymbolDisplayFormat format = null);
    }

    // Intentionally not extension methods. We don't want them ever be called for symbol classes
    // Once Default Interface Implementations are supported, we can move these methods into the interface. 
    static internal class ITypeSymbolHelpers
    {
        internal static bool IsNullableType(ITypeSymbol typeOpt)
        {
            return typeOpt?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }

        internal static bool IsNullableOfBoolean(ITypeSymbol type)
        {
            return IsNullableType(type) && IsBooleanType(GetNullableUnderlyingType(type));
        }

        internal static ITypeSymbol GetNullableUnderlyingType(ITypeSymbol type)
        {
            Debug.Assert(IsNullableType(type));
            return ((INamedTypeSymbol)type).TypeArguments[0];
        }

        internal static bool IsBooleanType(ITypeSymbol type)
        {
            return type?.SpecialType == SpecialType.System_Boolean;
        }

        internal static bool IsObjectType(ITypeSymbol type)
        {
            return type?.SpecialType == SpecialType.System_Object;
        }

        internal static bool IsSignedIntegralType(ITypeSymbol type)
        {
            return type?.SpecialType.IsSignedIntegralType() == true;
        }

        internal static bool IsUnsignedIntegralType(ITypeSymbol type)
        {
            return type?.SpecialType.IsUnsignedIntegralType() == true;
        }

        internal static bool IsNumericType(ITypeSymbol type)
        {
            return type?.SpecialType.IsNumericType() == true;
        }

        internal static ITypeSymbol GetEnumUnderlyingType(ITypeSymbol type)
        {
            return (type as INamedTypeSymbol)?.EnumUnderlyingType;
        }

        internal static ITypeSymbol GetEnumUnderlyingTypeOrSelf(ITypeSymbol type)
        {
            return GetEnumUnderlyingType(type) ?? type;
        }

        internal static bool IsDynamicType(ITypeSymbol type)
        {
            return type?.Kind == SymbolKind.DynamicType;
        }
    }
}
