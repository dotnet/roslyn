// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a type parameter in a generic type or generic method.
    /// </summary>
    internal abstract partial class TypeParameterSymbol : TypeSymbol, ITypeParameterSymbolInternal
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

        protected sealed override TypeSymbol OriginalTypeSymbolDefinition
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

        internal virtual UseSiteInfo<AssemblySymbol> GetConstraintsUseSiteErrorInfo()
        {
            return default;
        }

        /// <summary>
        /// The types that were directly specified as constraints on the type parameter.
        /// Duplicates and cycles are removed, although the collection may include
        /// redundant constraints where one constraint is a base type of another.
        /// </summary>
        internal ImmutableArray<TypeWithAnnotations> ConstraintTypesNoUseSiteDiagnostics
        {
            get
            {
                this.EnsureAllConstraintsAreResolved();
                return this.GetConstraintTypes(ConsList<TypeParameterSymbol>.Empty);
            }
        }

        internal ImmutableArray<TypeWithAnnotations> ConstraintTypesWithDefinitionUseSiteDiagnostics(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var result = ConstraintTypesNoUseSiteDiagnostics;

            AppendConstraintsUseSiteErrorInfo(ref useSiteInfo);

            foreach (var constraint in result)
            {
                ((TypeSymbol)constraint.Type.OriginalDefinition).AddUseSiteInfo(ref useSiteInfo);
            }

            return result;
        }

        private void AppendConstraintsUseSiteErrorInfo(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            useSiteInfo.Add(this.GetConstraintsUseSiteErrorInfo());
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
        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        // Type parameters do not have members
        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity)
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

        internal sealed override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => null;

        internal sealed override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved = null)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        protected sealed override ImmutableArray<NamedTypeSymbol> GetAllInterfaces()
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

        internal NamedTypeSymbol EffectiveBaseClass(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            AppendConstraintsUseSiteErrorInfo(ref useSiteInfo);
            var result = EffectiveBaseClassNoUseSiteDiagnostics;

            if ((object)result != null)
            {
                result.OriginalDefinition.AddUseSiteInfo(ref useSiteInfo);
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

        internal ImmutableArray<NamedTypeSymbol> EffectiveInterfacesWithDefinitionUseSiteDiagnostics(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var result = EffectiveInterfacesNoUseSiteDiagnostics;

            foreach (var iface in result)
            {
                iface.OriginalDefinition.AddUseSiteInfo(ref useSiteInfo);
            }

            return result;
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

        internal TypeSymbol DeducedBaseType(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            AppendConstraintsUseSiteErrorInfo(ref useSiteInfo);
            var result = DeducedBaseTypeNoUseSiteDiagnostics;

            if ((object)result != null)
            {
                ((TypeSymbol)result.OriginalDefinition).AddUseSiteInfo(ref useSiteInfo);
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

        internal ImmutableArray<NamedTypeSymbol> AllEffectiveInterfacesWithDefinitionUseSiteDiagnostics(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            var result = AllEffectiveInterfacesNoUseSiteDiagnostics;

            // Since bases affect content of AllInterfaces set, we need to make sure they all are good.
            var current = DeducedBaseType(ref useSiteInfo);

            while ((object)current != null)
            {
                current = current.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteInfo);
            }

            foreach (var iface in result)
            {
                iface.OriginalDefinition.AddUseSiteInfo(ref useSiteInfo);
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

        internal abstract ImmutableArray<TypeWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress);

        internal abstract ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress);

        internal abstract NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress);

        internal abstract TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress);

        private static bool ConstraintImpliesReferenceType(TypeSymbol constraint)
        {
            if (constraint.TypeKind == TypeKind.TypeParameter)
            {
                return ((TypeParameterSymbol)constraint).IsReferenceTypeFromConstraintTypes;
            }

            return NonTypeParameterConstraintImpliesReferenceType(constraint);
        }

        internal static bool NonTypeParameterConstraintImpliesReferenceType(TypeSymbol constraint)
        {
            Debug.Assert(constraint.TypeKind != TypeKind.TypeParameter);

            if (!constraint.IsReferenceType)
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
        internal static bool CalculateIsReferenceTypeFromConstraintTypes(ImmutableArray<TypeWithAnnotations> constraintTypes)
        {
            foreach (var constraintType in constraintTypes)
            {
                if (ConstraintImpliesReferenceType(constraintType.Type))
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool? IsNotNullableFromConstraintTypes(ImmutableArray<TypeWithAnnotations> constraintTypes)
        {
            Debug.Assert(!constraintTypes.IsDefaultOrEmpty);

            bool? result = false;
            foreach (TypeWithAnnotations constraintType in constraintTypes)
            {
                bool? fromType = IsNotNullableFromConstraintType(constraintType, out _);
                if (fromType == true)
                {
                    return true;
                }
                else if (fromType == null)
                {
                    result = null;
                }
            }

            return result;
        }

        internal static bool? IsNotNullableFromConstraintType(TypeWithAnnotations constraintType, out bool isNonNullableValueType)
        {
            if (constraintType.Type.IsNonNullableValueType())
            {
                isNonNullableValueType = true;
                return true;
            }

            isNonNullableValueType = false;

            if (constraintType.NullableAnnotation.IsAnnotated())
            {
                return false;
            }

            if (constraintType.TypeKind == TypeKind.TypeParameter)
            {
                bool? isNotNullable = ((TypeParameterSymbol)constraintType.Type).IsNotNullable;

                if (isNotNullable == false)
                {
                    return false;
                }
                else if (isNotNullable == null)
                {
                    return null;
                }
            }

            if (constraintType.NullableAnnotation.IsOblivious())
            {
                return null;
            }

            return true;
        }

        internal static bool CalculateIsValueTypeFromConstraintTypes(ImmutableArray<TypeWithAnnotations> constraintTypes)
        {
            foreach (var constraintType in constraintTypes)
            {
                if (constraintType.Type.IsValueType)
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
                if (this.HasReferenceTypeConstraint)
                {
                    return true;
                }

                return IsReferenceTypeFromConstraintTypes;
            }
        }

        internal bool? CalculateIsNotNullableFromNonTypeConstraints()
        {
            if (this.HasNotNullConstraint || this.HasValueTypeConstraint)
            {
                return true;
            }

            if (this.HasReferenceTypeConstraint)
            {
                return !this.ReferenceTypeConstraintIsNullable;
            }

            return false;
        }

        protected bool? CalculateIsNotNullable()
        {
            bool? fromNonTypeConstraints = CalculateIsNotNullableFromNonTypeConstraints();

            if (fromNonTypeConstraints == true)
            {
                return fromNonTypeConstraints;
            }

            ImmutableArray<TypeWithAnnotations> constraintTypes = this.ConstraintTypesNoUseSiteDiagnostics;

            if (constraintTypes.IsEmpty)
            {
                return fromNonTypeConstraints;
            }

            bool? fromTypes = IsNotNullableFromConstraintTypes(constraintTypes);

            if (fromTypes == true || fromNonTypeConstraints == false)
            {
                return fromTypes;
            }

            Debug.Assert(fromNonTypeConstraints == null);
            Debug.Assert(fromTypes != true);
            return null;
        }

        /// <returns>
        /// - 'true' if constraints disallow nullable reference types
        /// - 'false' if constraints (or lack of constraints) permit nullable reference types
        /// - 'null' if constrained to oblivious type
        /// </returns>
        internal abstract bool? IsNotNullable { get; }

        public sealed override bool IsValueType
        {
            get
            {
                if (this.HasValueTypeConstraint)
                {
                    return true;
                }

                return IsValueTypeFromConstraintTypes;
            }
        }

        internal sealed override ManagedKind GetManagedKind(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            return HasUnmanagedTypeConstraint ? ManagedKind.Unmanaged : ManagedKind.Managed;
        }

        public sealed override bool IsRefLikeType
        {
            get
            {
                return false;
            }
        }

        public sealed override bool IsReadOnly
        {
            get
            {
                // even if T is indirectly constrained to a struct, 
                // we only can use members via constrained calls, so "true" would have no effect
                return false;
            }
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        public abstract bool HasReferenceTypeConstraint { get; }

        public abstract bool IsReferenceTypeFromConstraintTypes { get; }

        /// <summary>
        /// Returns whether the reference type constraint (the 'class' constraint) should also be treated as nullable ('class?') or non-nullable (class!).
        /// In some cases this aspect is unknown (null value is returned). For example, when 'class' constraint is specified in a NonNullTypes(false) context.  
        /// This API returns false when <see cref="HasReferenceTypeConstraint"/> is false.
        /// </summary>
        internal abstract bool? ReferenceTypeConstraintIsNullable { get; }

        public abstract bool HasNotNullConstraint { get; }

        public abstract bool HasValueTypeConstraint { get; }

        public abstract bool IsValueTypeFromConstraintTypes { get; }

        public abstract bool HasUnmanagedTypeConstraint { get; }

        public abstract VarianceKind Variance { get; }

        internal sealed override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return false;
        }

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison)
        {
            return this.Equals(t2 as TypeParameterSymbol, comparison);
        }

        internal bool Equals(TypeParameterSymbol other)
        {
            return Equals(other, TypeCompareKind.ConsiderEverything);
        }

        private bool Equals(TypeParameterSymbol other, TypeCompareKind comparison)
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
            return other.ContainingSymbol.ContainingType.Equals(this.ContainingSymbol.ContainingType, comparison);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(ContainingSymbol, Ordinal);
        }

        internal override void AddNullableTransforms(ArrayBuilder<byte> transforms)
        {
        }

        internal override bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeSymbol result)
        {
            result = this;
            return true;
        }

        internal override TypeSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform)
        {
            return this;
        }

        internal override TypeSymbol MergeEquivalentTypes(TypeSymbol other, VarianceKind variance)
        {
            Debug.Assert(this.Equals(other, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
            return this;
        }

        protected sealed override ISymbol CreateISymbol()
        {
            return new PublicModel.TypeParameterSymbol(this, DefaultNullableAnnotation);
        }

        protected sealed override ITypeSymbol CreateITypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation)
        {
            Debug.Assert(nullableAnnotation != DefaultNullableAnnotation);
            return new PublicModel.TypeParameterSymbol(this, nullableAnnotation);
        }

        internal override bool IsRecord => false;

        internal override bool IsRecordStruct => false;

        internal sealed override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
        {
            return SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
        }

        internal sealed override bool HasInlineArrayAttribute(out int length)
        {
            length = 0;
            return false;
        }
    }
}
