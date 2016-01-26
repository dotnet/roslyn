// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a type parameter in a generic type or generic method.
    /// </summary>
    internal abstract partial class TypeParameterSymbol : TypeSymbol, ITypeParameterSymbol
    {
        /// <summary>
        /// The original definition of this symbol. If this symbol is constructed from another
        /// symbol by type substitution then OriginalDefinition gets the original symbol as it was defined in
        /// source or metadata.
        /// </summary>
        public new virtual TypeParameterSymbol OriginalDefinition
        {
            get
            {
                return this;
            }
        }

        protected override sealed TypeSymbol OriginalTypeSymbolDefinition
        {
            get
            {
                return this.OriginalDefinition;
            }
        }

        /// <summary>
        /// If this is a type parameter of a reduced extension method, gets the type parameter definition that
        /// this type parameter was reduced from. Otherwise, returns Nothing.
        /// </summary>
        public virtual TypeParameterSymbol ReducedFrom
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// The ordinal position of the type parameter in the parameter list which declares
        /// it. The first type parameter has ordinal zero.
        /// </summary>
        public abstract int Ordinal
        {
            // This is needed to determine hiding in C#: 
            //
            // interface IB { void M<T>(C<T> x); }
            // interface ID : IB { new void M<U>(C<U> x); }
            //
            // ID.M<U> hides IB.M<T> even though their formal parameters have different
            // types. When comparing formal parameter types for hiding purposes we must
            // compare method type parameters by ordinal, not by identity.
            get;
        }

        internal virtual DiagnosticInfo GetConstraintsUseSiteErrorInfo()
        {
            return null;
        }

        /// <summary>
        /// The types that were directly specified as constraints on the type parameter.
        /// Duplicates and cycles are removed, although the collection may include
        /// redundant constraints where one constraint is a base type of another.
        /// </summary>
        public ImmutableArray<TypeSymbolWithAnnotations> ConstraintTypes
        {
            get
            {
                return this.ConstraintTypesNoUseSiteDiagnostics;
            }
        }

        internal ImmutableArray<TypeSymbolWithAnnotations> ConstraintTypesNoUseSiteDiagnostics
        {
            get
            {
                this.EnsureAllConstraintsAreResolved();
                return this.GetConstraintTypes(ConsList<TypeParameterSymbol>.Empty);
            }
        }

        internal ImmutableArray<TypeSymbolWithAnnotations> ConstraintTypesWithDefinitionUseSiteDiagnostics(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var result = ConstraintTypesNoUseSiteDiagnostics;

            AppendConstraintsUseSiteErrorInfo(ref useSiteDiagnostics);

            foreach (var constraint in result)
            {
                ((TypeSymbol)constraint.TypeSymbol.OriginalDefinition).AddUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            return result;
        }

        private void AppendConstraintsUseSiteErrorInfo(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            DiagnosticInfo errorInfo = this.GetConstraintsUseSiteErrorInfo();

            if ((object)errorInfo != null)
            {
                if (useSiteDiagnostics == null)
                {
                    useSiteDiagnostics = new HashSet<DiagnosticInfo>();
                }

                useSiteDiagnostics.Add(errorInfo);
            }
        }

        /// <summary>
        /// True if the parameterless constructor constraint was specified for the type parameter.
        /// </summary>
        public abstract bool HasConstructorConstraint { get; }

        /// <summary>
        /// The type parameter kind of this type parameter.
        /// </summary>
        public abstract TypeParameterKind TypeParameterKind { get; }

        /// <summary>
        /// The method that declared this type parameter, or null.
        /// </summary>
        public MethodSymbol DeclaringMethod
        {
            get
            {
                return this.ContainingSymbol as MethodSymbol;
            }
        }

        /// <summary>
        /// The type that declared this type parameter, or null.
        /// </summary>
        public NamedTypeSymbol DeclaringType
        {
            get
            {
                return this.ContainingSymbol as NamedTypeSymbol;
            }
        }

        // Type parameters do not have members
        public sealed override ImmutableArray<Symbol> GetMembers()
        {
            return ImmutableArray<Symbol>.Empty;
        }

        // Type parameters do not have members
        public sealed override ImmutableArray<Symbol> GetMembers(string name)
        {
            return ImmutableArray<Symbol>.Empty;
        }

        // Type parameters do not have members
        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        // Type parameters do not have members
        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        // Type parameters do not have members
        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitTypeParameter(this, argument);
        }

        public override void Accept(CSharpSymbolVisitor visitor)
        {
            visitor.VisitTypeParameter(this);
        }

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor)
        {
            return visitor.VisitTypeParameter(this);
        }

        public sealed override SymbolKind Kind
        {
            get
            {
                return SymbolKind.TypeParameter;
            }
        }

        public sealed override TypeKind TypeKind
        {
            get
            {
                return TypeKind.TypeParameter;
            }
        }

        // Only the compiler can create TypeParameterSymbols.
        internal TypeParameterSymbol()
        {
        }

        public sealed override Accessibility DeclaredAccessibility
        {
            get
            {
                return Accessibility.NotApplicable;
            }
        }

        public sealed override bool IsStatic
        {
            get
            {
                return false;
            }
        }

        public sealed override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        public sealed override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        internal sealed override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                return null;
            }
        }

        internal sealed override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        protected override ImmutableArray<NamedTypeSymbol> GetAllInterfaces()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        /// <summary>
        /// The effective base class of the type parameter (spec 10.1.5). If the deduced
        /// base type is a reference type, the effective base type will be the same as
        /// the deduced base type. Otherwise if the deduced base type is a value type,
        /// the effective base type will be the most derived reference type from which
        /// deduced base type is derived.
        /// </summary>
        internal NamedTypeSymbol EffectiveBaseClassNoUseSiteDiagnostics
        {
            get
            {
                this.EnsureAllConstraintsAreResolved();
                return this.GetEffectiveBaseClass(ConsList<TypeParameterSymbol>.Empty);
            }
        }

        internal NamedTypeSymbol EffectiveBaseClass(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            AppendConstraintsUseSiteErrorInfo(ref useSiteDiagnostics);
            var result = EffectiveBaseClassNoUseSiteDiagnostics;

            if ((object)result != null)
            {
                result.OriginalDefinition.AddUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            return result;
        }

        /// <summary>
        /// The effective interface set (spec 10.1.5).
        /// </summary>
        internal ImmutableArray<NamedTypeSymbol> EffectiveInterfacesNoUseSiteDiagnostics
        {
            get
            {
                this.EnsureAllConstraintsAreResolved();
                return this.GetInterfaces(ConsList<TypeParameterSymbol>.Empty);
            }
        }

        /// <summary>
        /// The most encompassed type (spec 6.4.2) from the constraints.
        /// </summary>
        internal TypeSymbol DeducedBaseTypeNoUseSiteDiagnostics
        {
            get
            {
                this.EnsureAllConstraintsAreResolved();
                return this.GetDeducedBaseType(ConsList<TypeParameterSymbol>.Empty);
            }
        }

        internal TypeSymbol DeducedBaseType(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            AppendConstraintsUseSiteErrorInfo(ref useSiteDiagnostics);
            var result = DeducedBaseTypeNoUseSiteDiagnostics;

            if ((object)result != null)
            {
                ((TypeSymbol)result.OriginalDefinition).AddUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            return result;
        }

        /// <summary>
        /// The effective interface set and any base interfaces of those
        /// interfaces. This is AllInterfaces excluding interfaces that are
        /// only implemented by the effective base type.
        /// </summary>
        internal ImmutableArray<NamedTypeSymbol> AllEffectiveInterfacesNoUseSiteDiagnostics
        {
            get
            {
                return base.GetAllInterfaces();
            }
        }

        internal ImmutableArray<NamedTypeSymbol> AllEffectiveInterfacesWithDefinitionUseSiteDiagnostics(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var result = AllEffectiveInterfacesNoUseSiteDiagnostics;

            // Since bases affect content of AllInterfaces set, we need to make sure they all are good.
            var current = DeducedBaseType(ref useSiteDiagnostics);

            while ((object)current != null)
            {
                current = current.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            foreach (var iface in result)
            {
                iface.OriginalDefinition.AddUseSiteDiagnostics(ref useSiteDiagnostics);
            }

            return result;
        }

        /// <summary>
        /// Called by <see cref="ConstraintTypesNoUseSiteDiagnostics"/>, <see cref="InterfacesNoUseSiteDiagnostics"/>, <see cref="EffectiveBaseClass"/>, and <see cref="DeducedBaseType"/>.
        /// to allow derived classes to ensure constraints within the containing
        /// type or method are resolved in a consistent order, regardless of the
        /// order the callers query individual type parameters.
        /// </summary>
        internal abstract void EnsureAllConstraintsAreResolved();

        /// <summary>
        /// Helper method to force type parameter constraints to be resolved.
        /// </summary>
        protected static void EnsureAllConstraintsAreResolved(ImmutableArray<TypeParameterSymbol> typeParameters)
        {
            foreach (var typeParameter in typeParameters)
            {
                // Invoke any method that forces constraints to be resolved.
                var unused = typeParameter.GetConstraintTypes(ConsList<TypeParameterSymbol>.Empty);
            }
        }

        internal abstract ImmutableArray<TypeSymbolWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress);

        internal abstract ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress);

        internal abstract NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress);

        internal abstract TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress);

        private static bool ConstraintImpliesReferenceType(TypeSymbol constraint)
        {
            if (constraint.TypeKind == TypeKind.TypeParameter)
            {
                return IsReferenceTypeFromConstraintTypes(((TypeParameterSymbol)constraint).ConstraintTypesNoUseSiteDiagnostics);
            }
            else if (!constraint.IsReferenceType)
            {
                return false;
            }
            else
            {
                switch (constraint.TypeKind)
                {
                    case TypeKind.Interface:
                        return false; // can be satisfied by value types
                    case TypeKind.Error:
                        return false;
                }

                switch (constraint.SpecialType)
                {
                    case SpecialType.System_Object:
                    case SpecialType.System_ValueType:
                    case SpecialType.System_Enum:
                        return false; // can be satisfied by value types
                }

                return true;
            }
        }

        // From typedesc.cpp :
        // > A recursive helper that helps determine whether this variable is constrained as ObjRef.
        // > Please note that we do not check the gpReferenceTypeConstraint special constraint here
        // > because this property does not propagate up the constraining hierarchy.
        // > (e.g. "class A<S, T> where S : T, where T : class" does not guarantee that S is ObjRef)
        internal static bool IsReferenceTypeFromConstraintTypes(ImmutableArray<TypeSymbolWithAnnotations> constraintTypes)
        {
            foreach (var constraintType in constraintTypes)
            {
                if (ConstraintImpliesReferenceType(constraintType.TypeSymbol))
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsValueTypeFromConstraintTypes(ImmutableArray<TypeSymbolWithAnnotations> constraintTypes)
        {
            foreach (var constraintType in constraintTypes)
            {
                if (constraintType.IsValueType)
                {
                    return true;
                }
            }
            return false;
        }

        public sealed override bool IsReferenceType
        {
            get
            {
                return this.HasReferenceTypeConstraint || IsReferenceTypeFromConstraintTypes(this.ConstraintTypesNoUseSiteDiagnostics);
            }
        }

        public sealed override bool IsValueType
        {
            get
            {
                return this.HasValueTypeConstraint || IsValueTypeFromConstraintTypes(this.ConstraintTypesNoUseSiteDiagnostics);
            }
        }

        internal sealed override bool IsManagedType
        {
            get
            {
                return true;
            }
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        public abstract bool HasReferenceTypeConstraint { get; }

        public abstract bool HasValueTypeConstraint { get; }

        public abstract VarianceKind Variance { get; }

        internal sealed override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return false;
        }

        internal override bool Equals(TypeSymbol t2, TypeSymbolEqualityOptions options)
        {
            return this.Equals(t2 as TypeParameterSymbol, options);
        }

        internal bool Equals(TypeParameterSymbol other)
        {
            return Equals(other, TypeSymbolEqualityOptions.None);
        }

        private bool Equals(TypeParameterSymbol other, TypeSymbolEqualityOptions options)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if ((object)other == null || !ReferenceEquals(other.OriginalDefinition, this.OriginalDefinition))
            {
                return false;
            }

            // Type parameters may be equal but not reference equal due to independent alpha renamings.
            return other.ContainingSymbol.ContainingType.Equals(this.ContainingSymbol.ContainingType, options);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(ContainingSymbol, Ordinal);
        }

        internal override bool ContainsNullableReferenceTypes()
        {
            return false;
        }

        internal override void AddNullableTransforms(ArrayBuilder<bool> transforms)
        {
        }

        internal override bool ApplyNullableTransforms(ImmutableArray<bool> transforms, ref int position, out TypeSymbol result)
        {
            result = this;
            return true;
        }

        internal override TypeSymbol SetUnknownNullabilityForRefernceTypes()
        {
            return this;
        }

        /// <summary>
        /// Returns a bag of applied custom attributes and data decoded from well-known attributes. Returns null if there are no attributes applied on the symbol.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal virtual CustomAttributesBag<CSharpAttributeData> GetAttributesBag()
        {
            return null;
        }

        #region ITypeParameterTypeSymbol Members

        TypeParameterKind ITypeParameterSymbol.TypeParameterKind
        {
            get
            {
                return (TypeParameterKind)this.TypeParameterKind;
            }
        }

        IMethodSymbol ITypeParameterSymbol.DeclaringMethod
        {
            get { return this.DeclaringMethod; }
        }

        INamedTypeSymbol ITypeParameterSymbol.DeclaringType
        {
            get { return this.DeclaringType; }
        }

        ImmutableArray<ITypeSymbol> ITypeParameterSymbol.ConstraintTypes
        {
            get
            {
                return this.ConstraintTypesNoUseSiteDiagnostics.SelectAsArray(c => (ITypeSymbol)c.TypeSymbol);
            }
        }

        ITypeParameterSymbol ITypeParameterSymbol.OriginalDefinition
        {
            get { return this.OriginalDefinition; }
        }

        ITypeParameterSymbol ITypeParameterSymbol.ReducedFrom
        {
            get { return this.ReducedFrom; }
        }

        #endregion

        #region ISymbol Members

        public override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitTypeParameter(this);
        }

        public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitTypeParameter(this);
        }

        #endregion
    }
}
