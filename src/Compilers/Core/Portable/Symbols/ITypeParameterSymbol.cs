// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a type parameter in a generic type or generic method.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface ITypeParameterSymbol : ITypeSymbol
    {
        /// <summary>
        /// The ordinal position of the type parameter in the parameter list which declares
        /// it. The first type parameter has ordinal zero.
        /// </summary>
        int Ordinal { get; }

        /// <summary>
        /// The variance annotation, if any, of the type parameter declaration. Type parameters may be 
        /// declared as covariant (<c>out</c>), contravariant (<c>in</c>), or neither.
        /// </summary>
        VarianceKind Variance { get; }

        /// <summary>
        /// The type parameter kind of this type parameter.
        /// </summary>
        TypeParameterKind TypeParameterKind { get; }

        /// <summary>
        /// The method that declares the type parameter, or null.
        /// </summary>
        IMethodSymbol DeclaringMethod { get; }

        /// <summary>
        /// The type that declares the type parameter, or null.
        /// </summary>
        INamedTypeSymbol DeclaringType { get; }

        /// <summary>
        /// True if the reference type constraint (<c>class</c>) was specified for the type parameter.
        /// </summary>
        bool HasReferenceTypeConstraint { get; }

        /// <summary>
        /// If <see cref="HasReferenceTypeConstraint"/> is true, returns the top-level nullability of the
        /// <c>class</c> constraint that was specified for the type parameter. If there was no <c>class</c>
        /// constraint, this returns <see cref="NullableAnnotation.None"/>.
        /// </summary>
        NullableAnnotation ReferenceTypeConstraintNullableAnnotation { get; }

        /// <summary>
        /// True if the value type constraint (<c>struct</c>) was specified for the type parameter.
        /// </summary>
        bool HasValueTypeConstraint { get; }

        /// <summary>
        /// True if the value type constraint (<c>unmanaged</c>) was specified for the type parameter.
        /// </summary>
        bool HasUnmanagedTypeConstraint { get; }

        /// <summary>
        /// True if the notnull constraint (<c>notnull</c>) was specified for the type parameter.
        /// </summary>
        bool HasNotNullConstraint { get; }

        /// <summary>
        /// True if the parameterless constructor constraint (<c>new()</c>) was specified for the type parameter.
        /// </summary>
        bool HasConstructorConstraint { get; }

        /// <summary>
        /// The types that were directly specified as constraints on the type parameter.
        /// </summary>
        ImmutableArray<ITypeSymbol> ConstraintTypes { get; }

        /// <summary>
        /// The top-level nullabilities that were directly specified as constraints on the
        /// constraint types.
        /// </summary>
        ImmutableArray<NullableAnnotation> ConstraintNullableAnnotations { get; }

        /// <summary>
        /// Get the original definition of this type symbol. If this symbol is derived from another
        /// symbol by (say) type substitution, this gets the original symbol, as it was defined in
        /// source or metadata.
        /// </summary>
        new ITypeParameterSymbol OriginalDefinition { get; }

        /// <summary>
        /// If this is a type parameter of a reduced extension method, gets the type parameter definition that
        /// this type parameter was reduced from. Otherwise, returns Nothing.
        /// </summary>
        ITypeParameterSymbol ReducedFrom { get; }
    }
}
