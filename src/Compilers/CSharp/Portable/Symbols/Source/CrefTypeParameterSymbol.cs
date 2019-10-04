// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Type parameters in documentation comments are complicated since they sort of act as declarations,
    /// rather than references.  Consider the following example:
    /// 
    /// <![CDATA[
    /// /// <summary>See <see cref="B{U}.M(U)" />.</summary>
    /// class B<T> { void M(T t) { } }
    /// ]]>
    /// 
    /// We make some key observations:
    ///   1) The type parameter name in the cref is not tied to the type parameter name in the type declaration.
    ///   2) A relationship exists between the two occurrences of "U" in the cref: they both refer to (or define)
    ///        the same symbol.
    /// 
    /// In Roslyn, we've decided on the following representation: within the (entire) scope of a cref, the names
    /// of all type parameters "declared" in the cref are in scope and bind to the corresponding type parameters.
    /// This representation has one major advantage: as long as the appropriate binder (i.e. the one that knows
    /// about the implicitly-declared type parameters) is used, TypeSyntaxes within the cref can be bound by
    /// calling BindType.  In addition to eliminating the necessity for custom binding code in the batch case,
    /// this reduces the problem of exposing such nodes in the SemanticModel to one of ensuring that the right
    /// enclosing binder is chosen.  That is, new code will have to be written to handle CrefSyntaxes, but the
    /// existing code for TypeSyntaxes should just work!
    /// 
    /// In the example above, this means that, between the cref quotation marks, the name "U" binds to an
    /// implicitly declared type parameter, whether it is in "B{U}", "M{U}", or "M{List{U[]}}".
    /// 
    /// Of course, it's not all gravy.  One thing we're giving up by using this representation is the ability to
    /// distinguish between "declared" type parameters with the same name.  Consider the following example:
    /// 
    /// <![CDATA[
    /// <summary>See <see cref=""A{T, T}.M(T)""/>.</summary>
    /// class A<T, U>
    /// {
    ///     void M(T t) { }
    ///     void M(U u) { }
    /// }
    /// ]]>
    /// </summary>
    /// 
    /// The native compiler interprets this in the same way as it would interpret A{T1, T2}.M(T2) and unambiguously
    /// (i.e. without a warning) binds to A{T, U}.M(U).  Since Roslyn does not distinguish between the T's, Roslyn
    /// reports an ambiguity warning and picks the first method.  Furthermore, renaming one 'T' will rename all of
    /// them.
    /// 
    /// This class represents such an implicitly declared type parameter.  The declaring syntax is expected to be
    /// an IdentifierNameSyntax in the type argument list of a QualifiedNameSyntax.
    internal sealed class CrefTypeParameterSymbol : TypeParameterSymbol
    {
        private readonly string _name;
        private readonly int _ordinal;
        private readonly SyntaxReference _declaringSyntax;

        public CrefTypeParameterSymbol(string name, int ordinal, IdentifierNameSyntax declaringSyntax)
        {
            _name = name;
            _ordinal = ordinal;
            _declaringSyntax = declaringSyntax.GetReference();
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return TypeParameterKind.Cref;
            }
        }

        public override string Name
        {
            get { return _name; }
        }

        public override int Ordinal
        {
            get { return _ordinal; }
        }

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool>? isValueTypeOverrideOpt = null)
        {
            Debug.Assert(isValueTypeOverrideOpt == null);

            if (ReferenceEquals(this, t2))
            {
                return true;
            }

            if ((object)t2 == null)
            {
                return false;
            }

            CrefTypeParameterSymbol? other = t2 as CrefTypeParameterSymbol;
            return (object?)other != null &&
                other._name == _name &&
                other._ordinal == _ordinal &&
                other._declaringSyntax.GetSyntax() == _declaringSyntax.GetSyntax();
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_name, _ordinal);
        }

        public override VarianceKind Variance
        {
            get { return VarianceKind.None; }
        }

        public override bool HasValueTypeConstraint
        {
            get { return false; }
        }

        public override bool HasReferenceTypeConstraint
        {
            get { return false; }
        }

        internal override bool? ReferenceTypeConstraintIsNullable
        {
            get { return false; }
        }

        public override bool HasNotNullConstraint => false;

        internal override bool? IsNotNullable => null;

        public override bool HasUnmanagedTypeConstraint
        {
            get { return false; }
        }

        public override bool HasConstructorConstraint
        {
            get { return false; }
        }

        public override Symbol? ContainingSymbol
        {
            get
            {
                return null;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create<Location>(_declaringSyntax.GetLocation());
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray.Create<SyntaxReference>(_declaringSyntax);
            }
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
        }

        internal override ImmutableArray<TypeWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            return ImmutableArray<TypeWithAnnotations>.Empty;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            // Constraints are not checked in crefs, so this should never be examined.
            throw ExceptionUtilities.Unreachable;
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            // Constraints are not checked in crefs, so this should never be examined.
            throw ExceptionUtilities.Unreachable;
        }

        public override bool IsImplicitlyDeclared
        {
            get { return false; }
        }
    }
}
