// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A simple class that combines a single type symbol with annotations
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal abstract class TypeSymbolWithAnnotations : IFormattable
    {
        public sealed override string ToString() => TypeSymbol.ToString();
        public string Name => TypeSymbol.Name;
        public SymbolKind Kind => TypeSymbol.Kind;

#pragma warning disable CS0809
        [Obsolete("Unsupported", error: true)]
        public sealed override bool Equals(object other)
#pragma warning restore CS0809
        {
            throw ExceptionUtilities.Unreachable;
        }

#pragma warning disable CS0809
        [Obsolete("Unsupported", error: true)]
        public sealed override int GetHashCode()
#pragma warning restore CS0809
        {
            throw ExceptionUtilities.Unreachable;
        }

        [Obsolete("Unsupported", error: true)]
        public static bool operator ==(TypeSymbolWithAnnotations x, TypeSymbolWithAnnotations y)
        {
            throw ExceptionUtilities.Unreachable;
        }

        [Obsolete("Unsupported", error: true)]
        public static bool operator !=(TypeSymbolWithAnnotations x, TypeSymbolWithAnnotations y)
        {
            throw ExceptionUtilities.Unreachable;
        }

        [Obsolete("Unsupported", error: true)]
        public static bool operator ==(Symbol x, TypeSymbolWithAnnotations y)
        {
            throw ExceptionUtilities.Unreachable;
        }

        [Obsolete("Unsupported", error: true)]
        public static bool operator !=(Symbol x, TypeSymbolWithAnnotations y)
        {
            throw ExceptionUtilities.Unreachable;
        }

        [Obsolete("Unsupported", error: true)]
        public static bool operator ==(TypeSymbolWithAnnotations x, Symbol y)
        {
            throw ExceptionUtilities.Unreachable;
        }

        [Obsolete("Unsupported", error: true)]
        public static bool operator !=(TypeSymbolWithAnnotations x, Symbol y)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal static readonly SymbolDisplayFormat DebuggerDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
            compilerInternalOptions: SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier);

        internal static TypeSymbolWithAnnotations CreateNonNull(bool nonNullTypes, TypeSymbol typeSymbol)
        {
            return Create(typeSymbol, isNullableIfReferenceType: nonNullTypes ? (bool?)false : null);
        }

        internal static TypeSymbolWithAnnotations Create(ModuleSymbol module, TypeSymbol typeSymbol)
        {
            return Create(typeSymbol, isNullableIfReferenceType: module.UtilizesNullableReferenceTypes ? (bool?)false : null);
        }

        // PROTOTYPE(NullableReferenceTypes): Check we are not using this method on type references in
        // member signatures visible outside the assembly. Consider overriding, implementing, NoPIA embedding, etc.
        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol)
        {
            return Create(typeSymbol, ImmutableArray<CustomModifier>.Empty);
        }

        // PROTOTYPE(NullableReferenceTypes): Check we are not using this method on type references in
        // member signatures visible outside the assembly. Consider overriding, implementing, NoPIA embedding, etc.
        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
        {
            if (typeSymbol is null)
            {
                return null;
            }

            return new NonLazyType(typeSymbol, isNullable: typeSymbol.IsNullableType(), customModifiers);
        }

        // PROTOTYPE(NullableReferenceTypes): Check we are not using this method on type references in
        // member signatures visible outside the assembly. Consider overriding, implementing, NoPIA embedding, etc.
        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, bool? isNullableIfReferenceType)
        {
            return Create(typeSymbol, isNullableIfReferenceType, ImmutableArray<CustomModifier>.Empty);
        }

        // PROTOTYPE(NullableReferenceTypes): Check we are not using this method on type references in
        // member signatures visible outside the assembly. Consider overriding, implementing, NoPIA embedding, etc.
        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, bool? isNullableIfReferenceType, ImmutableArray<CustomModifier> customModifiers)
        {
            if (typeSymbol is null)
            {
                return null;
            }

            if (isNullableIfReferenceType != true && typeSymbol.TypeKind == TypeKind.TypeParameter)
            {
                Debug.Assert(customModifiers.IsEmpty);
                return new LazyUnannotatedTypeParameter((TypeParameterSymbol)typeSymbol, isNullableIfReferenceType);
            }

            if (isNullableIfReferenceType == false || !typeSymbol.IsReferenceType || typeSymbol.IsNullableType())
            {
                return Create(typeSymbol, customModifiers);
            }

            Debug.Assert(!typeSymbol.IsNullableType());
            return new NonLazyType(typeSymbol, isNullableIfReferenceType, customModifiers);
        }

        public TypeSymbolWithAnnotations AsNullableReferenceOrValueType(CSharpCompilation compilation)
        {
            Debug.Assert(compilation.IsFeatureEnabled(MessageID.IDS_FeatureStaticNullChecking));
            Debug.Assert(CustomModifiers.IsEmpty);

            var typeSymbol = this.TypeSymbol;

            // It is not safe to check if a type parameter is a reference type right away, this can send us into a cycle.
            // In this case we delay asking this question as long as possible.
            if (typeSymbol.TypeKind != TypeKind.TypeParameter)
            {
                if (!typeSymbol.IsValueType)
                {
                    return new NonLazyType(typeSymbol, isNullable: true, this.CustomModifiers);
                }
                else
                {
                    return Create(compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(typeSymbol)));
                }
            }

            return new LazyNullableTypeParameter(compilation.Assembly, this);
        }

        /// <summary>
        /// Adjust types in signatures coming from metadata.
        /// </summary>
        public abstract TypeSymbolWithAnnotations AsNullableReferenceType();
        public abstract TypeSymbolWithAnnotations AsNotNullableReferenceType();
        public abstract TypeSymbolWithAnnotations AsObliviousReferenceType();

        public abstract TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers);

        public abstract TypeSymbol TypeSymbol { get; }
        public virtual TypeSymbol NullableUnderlyingTypeOrSelf => TypeSymbol.StrippedType();

        /// <summary>
        /// Is this a nullable reference or value type.
        /// If it is a nullable value type, <see cref="TypeSymbol"/>
        /// returns symbol for constructed System.Nullable`1 type.
        /// If it is a nullable reference type, <see cref="TypeSymbol"/>
        /// simply returns a symbol for the reference type.
        /// </summary>
        public abstract bool? IsNullable { get; }

        /// <summary>
        /// Is this System.Nullable`1 type, or its substitution.
        /// </summary>
        public bool IsNullableType() => TypeSymbol.IsNullableType();

        /// <summary>
        /// The list of custom modifiers, if any, associated with the <see cref="TypeSymbol"/>.
        /// </summary>
        public abstract ImmutableArray<CustomModifier> CustomModifiers { get; }

        public bool IsReferenceType => TypeSymbol.IsReferenceType;
        public bool IsValueType => TypeSymbol.IsValueType;
        public TypeKind TypeKind => TypeSymbol.TypeKind;
        public virtual SpecialType SpecialType => TypeSymbol.SpecialType;
        public bool IsManagedType => TypeSymbol.IsManagedType;
        public Cci.PrimitiveTypeCode PrimitiveTypeCode => TypeSymbol.PrimitiveTypeCode;
        public bool IsEnumType() => TypeSymbol.IsEnumType();
        public bool IsDynamic() => TypeSymbol.IsDynamic();
        public bool IsObjectType() => TypeSymbol.IsObjectType();
        public bool IsArray() => TypeSymbol.IsArray();
        public virtual bool IsRestrictedType(bool ignoreSpanLikeTypes = false) => TypeSymbol.IsRestrictedType(ignoreSpanLikeTypes);
        public bool IsPointerType() => TypeSymbol.IsPointerType();
        public bool IsErrorType() => TypeSymbol.IsErrorType();
        public bool IsUnsafe() => TypeSymbol.IsUnsafe();
        public virtual bool IsStatic => TypeSymbol.IsStatic;
        public bool IsNullableTypeOrTypeParameter() => TypeSymbol.IsNullableTypeOrTypeParameter();
        public virtual bool IsVoid => TypeSymbol.SpecialType == SpecialType.System_Void;
        public virtual bool IsSZArray() => TypeSymbol.IsSZArray();
        public TypeSymbolWithAnnotations GetNullableUnderlyingType() => TypeSymbol.GetNullableUnderlyingTypeWithAnnotations();

        internal abstract bool GetIsReferenceType(ConsList<TypeParameterSymbol> inProgress);
        internal abstract bool GetIsValueType(ConsList<TypeParameterSymbol> inProgress);

        public string ToDisplayString(SymbolDisplayFormat format = null)
        {
            var str = TypeSymbol.ToDisplayString(format);
            if (format != null && !IsValueType)
            {
                switch (IsNullable)
                {
                    case true:
                        if ((format.MiscellaneousOptions & SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier) != 0)
                        {
                            return str + "?";
                        }
                        break;
                    case false:
                        if ((format.CompilerInternalOptions & SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier) != 0)
                        {
                            return str + "!";
                        }
                        break;
                }
            }
            return str;
        }

        internal string GetDebuggerDisplay() => ToDisplayString(DebuggerDisplayFormat);

        // PROTOTYPE(NullableReferenceTypes): Remove IFormattable implementation
        // if instances should not be used as Diagnostic arguments.
        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        }

        public bool Equals(TypeSymbolWithAnnotations other, TypeCompareKind comparison)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if ((object)other == null || !TypeSymbolEquals(other, comparison))
            {
                return false;
            }

            // Make sure custom modifiers are the same.
            if ((comparison & TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) == 0 && !this.CustomModifiers.SequenceEqual(other.CustomModifiers))
            {
                return false;
            }

            if ((comparison & TypeCompareKind.CompareNullableModifiersForReferenceTypes) != 0 && other.IsNullable != this.IsNullable)
            {
                if ((comparison & TypeCompareKind.UnknownNullableModifierMatchesAny) == 0 || (this.IsNullable.HasValue && other.IsNullable.HasValue))
                {
                    return false;
                }
            }

            return true;
        }

        internal sealed class EqualsComparer : EqualityComparer<TypeSymbolWithAnnotations>
        {
            internal static readonly EqualsComparer Instance = new EqualsComparer();

            private EqualsComparer()
            {
            }

            public override int GetHashCode(TypeSymbolWithAnnotations obj)
            {
                if (obj is null)
                {
                    return 0;
                }
                return obj.TypeSymbol.GetHashCode();
            }

            public override bool Equals(TypeSymbolWithAnnotations x, TypeSymbolWithAnnotations y)
            {
                if (x is null)
                {
                    return y is null;
                }
                return x.Equals(y, TypeCompareKind.CompareNullableModifiersForReferenceTypes);
            }
        }

        protected virtual bool TypeSymbolEquals(TypeSymbolWithAnnotations other, TypeCompareKind comparison)
        {
            return this.TypeSymbol.Equals(other.TypeSymbol, comparison);
        }

        public bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return TypeSymbol.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes) ||
                   Symbol.GetUnificationUseSiteDiagnosticRecursive(ref result, this.CustomModifiers, owner, ref checkedTypes);
        }

        public void CheckAllConstraints(ConversionsBase conversions, Location location, DiagnosticBag diagnostics)
        {
            TypeSymbol.CheckAllConstraints(conversions, location, diagnostics);
        }

        public bool IsAtLeastAsVisibleAs(Symbol sym, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // System.Nullable is public, so it is safe to delegate to the underlying.
            return NullableUnderlyingTypeOrSelf.IsAtLeastAsVisibleAs(sym, ref useSiteDiagnostics);
        }

        public TypeSymbolWithAnnotations SubstituteType(AbstractTypeMap typeMap)
        {
            return SubstituteType(typeMap, withTupleUnification: false);
        }

        public TypeSymbolWithAnnotations SubstituteTypeWithTupleUnification(AbstractTypeMap typeMap)
        {
            return SubstituteType(typeMap, withTupleUnification: true);
        }

        protected virtual TypeSymbolWithAnnotations SubstituteType(AbstractTypeMap typeMap, bool withTupleUnification)
        {
            var newCustomModifiers = typeMap.SubstituteCustomModifiers(this.CustomModifiers);
            var newTypeWithModifiers = typeMap.SubstituteType(this.TypeSymbol, withTupleUnification);
            bool? newIsNullable = (newTypeWithModifiers.IsNullable != null && this.IsNullable != true) ? newTypeWithModifiers.IsNullable : this.IsNullable;

            if (!TypeSymbolEquals(newTypeWithModifiers, TypeCompareKind.CompareNullableModifiersForReferenceTypes) ||
                !newTypeWithModifiers.CustomModifiers.IsEmpty ||
                newIsNullable != this.IsNullable ||
                newCustomModifiers != this.CustomModifiers)
            {
                if (newTypeWithModifiers.TypeSymbol.IsNullableType())
                {
                    Debug.Assert(newIsNullable == true);

                    if (newCustomModifiers.IsEmpty)
                    {
                        return newTypeWithModifiers;
                    }

                    return TypeSymbolWithAnnotations.Create(newTypeWithModifiers.TypeSymbol, newCustomModifiers.Concat(newTypeWithModifiers.CustomModifiers));
                }

                if (newIsNullable == false)
                {
                    Debug.Assert(newTypeWithModifiers.IsNullable != true);
                    if (newCustomModifiers.IsEmpty)
                    {
                        return newTypeWithModifiers;
                    }

                    return TypeSymbolWithAnnotations.Create(newTypeWithModifiers.TypeSymbol, newCustomModifiers.Concat(newTypeWithModifiers.CustomModifiers));
                }

                Debug.Assert(newIsNullable != false);

                if (newCustomModifiers.IsEmpty && newTypeWithModifiers.IsNullable == newIsNullable)
                {
                    return newTypeWithModifiers;
                }

                newCustomModifiers = newCustomModifiers.Concat(newTypeWithModifiers.CustomModifiers);

                Debug.Assert(newIsNullable != false);
                return new NonLazyType(newTypeWithModifiers.TypeSymbol, newIsNullable, newCustomModifiers);
            }

            return this; // substitution had no effect on the type or modifiers
        }

        public virtual void ReportDiagnosticsIfObsolete(Binder binder, SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            binder.ReportDiagnosticsIfObsolete(diagnostics, TypeSymbol, syntax, hasBaseReceiver: false);
        }

        /// <summary>
        /// Extract type under assumption that there should be no custom modifiers or annotations.
        /// The method asserts otherwise.
        /// </summary>
        public abstract TypeSymbol AsTypeSymbolOnly();

        /// <summary>
        /// Is this the given type parameter?
        /// </summary>
        public abstract bool Is(TypeParameterSymbol other);

        public TypeSymbolWithAnnotations Update(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
        {
            if (CustomModifiers != customModifiers || !TypeSymbol.Equals(typeSymbol, TypeCompareKind.AllAspects))
            {
                return DoUpdate(typeSymbol, customModifiers);
            }

            return this;
        }

        protected abstract TypeSymbolWithAnnotations DoUpdate(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers);

        public bool ContainsNullableReferenceTypes()
        {
            var typeSymbol = TypeSymbol;

            if (IsNullable == true && !typeSymbol.IsValueType)
            {
                return true;
            }

            return typeSymbol.ContainsNullableReferenceTypes();
        }

        public void AddNullableTransforms(ArrayBuilder<bool> transforms)
        {
            var typeSymbol = TypeSymbol;
            transforms.Add(IsNullable == true && !typeSymbol.IsNullableType() && !typeSymbol.IsValueType);
            typeSymbol.AddNullableTransforms(transforms);
        }

        public bool ApplyNullableTransforms(ImmutableArray<bool> transforms, bool useNonNullTypes, ref int position, out TypeSymbolWithAnnotations result)
        {
            result = this;

            bool isNullable;
            if (transforms.IsDefault)
            {
                // No explicit transforms. All reference types are non-nullable.
                isNullable = false;
            }
            else if (position < transforms.Length)
            {
                isNullable = transforms[position++];
            }
            else
            {
                return false;
            }

            TypeSymbol oldTypeSymbol = TypeSymbol;
            TypeSymbol newTypeSymbol;

            if (!oldTypeSymbol.ApplyNullableTransforms(transforms, useNonNullTypes, ref position, out newTypeSymbol))
            {
                return false;
            }

            if ((object)oldTypeSymbol != newTypeSymbol)
            {
                result = result.DoUpdate(newTypeSymbol, result.CustomModifiers);
            }

            if (!result.IsValueType)
            {
                if (isNullable)
                {
                    result = result.AsNullableReferenceType();
                }
                else
                {
                    if (useNonNullTypes)
                    {
                        result = result.AsNotNullableReferenceType();
                    }
                    else
                    {
                        result = result.AsObliviousReferenceType();
                    }
                }
            }

            return true;
        }

        public TypeSymbolWithAnnotations SetUnknownNullabilityForReferenceTypesIfNecessary(ModuleSymbol module)
        {
            return module.UtilizesNullableReferenceTypes ?
                this :
                this.SetUnknownNullabilityForReferenceTypes();
        }

        public TypeSymbolWithAnnotations WithTopLevelNonNullabilityForReferenceTypes()
        {
            var typeSymbol = TypeSymbol;
            if (IsNullable == false || typeSymbol.IsValueType)
            {
                return this;
            }

            return new NonLazyType(typeSymbol, isNullable: false, CustomModifiers);
         }

        public TypeSymbolWithAnnotations SetUnknownNullabilityForReferenceTypes()
        {
            var typeSymbol = TypeSymbol;

            if (IsNullable.HasValue)
            {
                if (!typeSymbol.IsValueType)
                {
                    typeSymbol = typeSymbol.SetUnknownNullabilityForReferenceTypes();
                    return new NonLazyType(typeSymbol, isNullable: null, CustomModifiers);
                }
            }

            var newTypeSymbol = typeSymbol.SetUnknownNullabilityForReferenceTypes();

            if ((object)newTypeSymbol != typeSymbol)
            {
                return DoUpdate(newTypeSymbol, CustomModifiers);
            }

            return this;
        }

        private sealed class NonLazyType : TypeSymbolWithAnnotations
        {
            private readonly TypeSymbol _typeSymbol;
            private readonly bool? _isNullable;
            private readonly ImmutableArray<CustomModifier> _customModifiers;

            public NonLazyType(TypeSymbol typeSymbol, bool? isNullable, ImmutableArray<CustomModifier> customModifiers)
            {
                Debug.Assert((object)typeSymbol != null);
                Debug.Assert(!customModifiers.IsDefault);
                Debug.Assert(!typeSymbol.IsNullableType() || isNullable == true);
                _typeSymbol = typeSymbol;
                _isNullable = isNullable;
                _customModifiers = customModifiers;
            }

            public sealed override TypeSymbol TypeSymbol => _typeSymbol;
            public sealed override bool? IsNullable => _isNullable;
            public override ImmutableArray<CustomModifier> CustomModifiers => _customModifiers;

            internal override bool GetIsReferenceType(ConsList<TypeParameterSymbol> inProgress)
            {
                if (_typeSymbol.TypeKind == TypeKind.TypeParameter)
                {
                    return ((TypeParameterSymbol)_typeSymbol).GetIsReferenceType(inProgress);
                }
                return _typeSymbol.IsReferenceType;
            }

            internal override bool GetIsValueType(ConsList<TypeParameterSymbol> inProgress)
            {
                if (_typeSymbol.TypeKind == TypeKind.TypeParameter)
                {
                    return ((TypeParameterSymbol)_typeSymbol).GetIsValueType(inProgress);
                }
                return _typeSymbol.IsValueType;
            }

            public override TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers)
            {
                return new NonLazyType(_typeSymbol, _isNullable, customModifiers);
            }

            public override TypeSymbol AsTypeSymbolOnly() => _typeSymbol;

            // PROTOTYPE(NullableReferenceTypes): Use WithCustomModifiers.Is() => false
            // and set IsNullable=null always for GetTypeParametersAsTypeArguments.
            public override bool Is(TypeParameterSymbol other) => _typeSymbol.Equals(other, TypeCompareKind.CompareNullableModifiersForReferenceTypes) && _customModifiers.IsEmpty;

            protected sealed override TypeSymbolWithAnnotations DoUpdate(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                return new NonLazyType(typeSymbol, _isNullable, customModifiers);
            }

            public override TypeSymbolWithAnnotations AsNullableReferenceType()
            {
                return _isNullable == true ?
                    this :
                    new NonLazyType(_typeSymbol, isNullable: true, _customModifiers);
            }

            public override TypeSymbolWithAnnotations AsNotNullableReferenceType()
            {
                return _isNullable == false || _typeSymbol.IsNullableType() ?
                    this :
                    new NonLazyType(_typeSymbol, isNullable: false, _customModifiers);
            }

            public override TypeSymbolWithAnnotations AsObliviousReferenceType()
            {
                Debug.Assert(_isNullable != true);
                return _isNullable == null ?
                    this :
                    new NonLazyType(_typeSymbol, isNullable: null, _customModifiers);
            }
        }

        /// <summary>
        /// Nullable type parameter. The underlying TypeSymbol is resolved
        /// lazily to avoid cycles when binding declarations.
        /// </summary>
        private sealed class LazyNullableTypeParameter : TypeSymbolWithAnnotations
        {
            private readonly AssemblySymbol _assembly;
            private readonly TypeSymbolWithAnnotations _underlying;
            private TypeSymbol _resolved;

            public LazyNullableTypeParameter(AssemblySymbol assembly, TypeSymbolWithAnnotations underlying)
            {
                Debug.Assert(underlying is LazyUnannotatedTypeParameter || underlying.IsNullable != true);
                Debug.Assert(underlying.TypeKind == TypeKind.TypeParameter);
                Debug.Assert(underlying.CustomModifiers.IsEmpty);
                _assembly = assembly;
                _underlying = underlying;
            }

            public override bool? IsNullable => true;
            public override bool IsVoid => false;
            public override bool IsSZArray() => false;
            public override bool IsStatic => false;

            public override TypeSymbol TypeSymbol
            {
                get
                {
                    if ((object)_resolved == null)
                    {
                        if (!_underlying.IsValueType)
                        {
                            _resolved = _underlying.TypeSymbol;
                        }
                        else
                        {
                            var resolved = _assembly.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(_underlying));
                            Interlocked.CompareExchange(ref _resolved, resolved, null);
                        }
                    }

                    return _resolved;
                }
            }

            internal override bool GetIsReferenceType(ConsList<TypeParameterSymbol> inProgress)
            {
                return _underlying.GetIsReferenceType(inProgress);
            }

            internal override bool GetIsValueType(ConsList<TypeParameterSymbol> inProgress)
            {
                return _underlying.GetIsValueType(inProgress);
            }

            public override TypeSymbol NullableUnderlyingTypeOrSelf => _underlying.TypeSymbol;

            public override SpecialType SpecialType => SpecialType.None;

            public override bool IsRestrictedType(bool ignoreSpanLikeTypes) => false;

            public override TypeSymbol AsTypeSymbolOnly()
            {
                Debug.Assert(TypeSymbol.IsNullableType() && CustomModifiers.IsEmpty);
                return TypeSymbol;
            }

            // PROTOTYPE(NullableReferenceTypes): This implementation looks
            // incorrect since a type parameter cannot be Nullable<T>.
            public override bool Is(TypeParameterSymbol other)
            {
                if (!other.IsNullableType())
                {
                    return false;
                }

                return TypeSymbol.Equals(other, TypeCompareKind.CompareNullableModifiersForReferenceTypes);
            }

            public override ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty; 

            public override TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers)
            {
                if (customModifiers.IsDefaultOrEmpty)
                {
                    return this;
                }

                // It should be safe to force resolution
                var typeSymbol = TypeSymbol;
                if (typeSymbol.IsNullableType())
                {
                    return TypeSymbolWithAnnotations.Create(typeSymbol, customModifiers);
                }

                return new NonLazyType(typeSymbol, isNullable: true, customModifiers);
            }

            protected override TypeSymbolWithAnnotations DoUpdate(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                if (typeSymbol.IsNullableType())
                {
                    return TypeSymbolWithAnnotations.Create(typeSymbol, customModifiers);
                }

                return new NonLazyType(typeSymbol, isNullable: true, customModifiers);
            }

            public override TypeSymbolWithAnnotations AsNullableReferenceType() => this;

            public override TypeSymbolWithAnnotations AsNotNullableReferenceType()
            {
                if (!_underlying.TypeSymbol.IsValueType)
                {
                    Debug.Assert(_underlying.IsNullable == false);
                    return _underlying;
                }

                Debug.Assert(!this.IsNullable == false);
                return this;
            }

            public override TypeSymbolWithAnnotations AsObliviousReferenceType()
            {
                // AsObliviousReferenceType is used to produce a null-oblivious when applying a nullable transform
                // in a context with [NonNullTypes(false)]. But that attribute only affects types that don't have
                // a `?` annotation. Since LazyNullableType always results from a `?` annotation, this method is unreachable.
                throw ExceptionUtilities.Unreachable;
            }

            protected override TypeSymbolWithAnnotations SubstituteType(AbstractTypeMap typeMap, bool withTupleUnification)
            {
                if ((object)_resolved != null)
                {
                    return base.SubstituteType(typeMap, withTupleUnification);
                }

                var newUnderlying = _underlying.SubstituteType(typeMap, withTupleUnification);
                if ((object)newUnderlying != this._underlying)
                {
                    if ((newUnderlying.TypeSymbol.Equals(this._underlying.TypeSymbol, TypeCompareKind.AllAspects) || 
                            newUnderlying.TypeSymbol is IndexedTypeParameterSymbolForOverriding) &&
                        newUnderlying.CustomModifiers.IsEmpty)
                    {
                        return new LazyNullableTypeParameter(_assembly, newUnderlying);
                    }

                    return base.SubstituteType(typeMap, withTupleUnification);
                }
                else
                {
                    return this; // substitution had no effect on the type or modifiers
                }
            }

            public override void ReportDiagnosticsIfObsolete(Binder binder, SyntaxNode syntax, DiagnosticBag diagnostics)
            {
                if ((object)_resolved != null)
                {
                    base.ReportDiagnosticsIfObsolete(binder, syntax, diagnostics);
                }
                else
                {
                    diagnostics.Add(new LazyObsoleteDiagnosticInfo(this, binder.ContainingMemberOrLambda, binder.Flags), syntax.GetLocation());
                }
            }

            protected override bool TypeSymbolEquals(TypeSymbolWithAnnotations other, TypeCompareKind comparison)
            {
                var otherLazy = other as LazyNullableTypeParameter;
                if ((object)otherLazy != null)
                {
                    return _underlying.TypeSymbolEquals(otherLazy._underlying, comparison);
                }
                return base.TypeSymbolEquals(other, comparison);
            }
        }

        /// <summary>
        /// Non-nullable or null-oblivious type parameter. IsNullable is calculated
        /// lazily to avoid cycles when binding declarations.
        /// </summary>
        private sealed class LazyUnannotatedTypeParameter : TypeSymbolWithAnnotations
        {
            private readonly TypeParameterSymbol _typeParameter;
            private readonly bool? _isNullableIfReferenceType;

            internal LazyUnannotatedTypeParameter(TypeParameterSymbol typeParameter, bool? isNullableIfReferenceType)
            {
                Debug.Assert(isNullableIfReferenceType != true); // Nullable type parameters should be represented with LazyNullableTypeParameter.
                _typeParameter = typeParameter;
                _isNullableIfReferenceType = isNullableIfReferenceType;
            }

            public override TypeSymbol TypeSymbol => _typeParameter;
            public override bool? IsNullable => _isNullableIfReferenceType == false || !_typeParameter.IsReferenceType ? (bool?)false : null;

            public override ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;

            public override TypeSymbolWithAnnotations AsNotNullableReferenceType()
            {
                return _isNullableIfReferenceType == false ?
                    this :
                    new LazyUnannotatedTypeParameter(_typeParameter, isNullableIfReferenceType: false);
            }

            public override TypeSymbolWithAnnotations AsNullableReferenceType()
            {
                return new LazyNullableTypeParameter(_typeParameter.ContainingAssembly, this);
            }

            public override TypeSymbolWithAnnotations AsObliviousReferenceType()
            {
                return _isNullableIfReferenceType == null ?
                    this :
                    new LazyUnannotatedTypeParameter(_typeParameter, isNullableIfReferenceType: null);
            }

            public override TypeSymbol AsTypeSymbolOnly()
            {
                throw new NotImplementedException();
            }

            public override bool Is(TypeParameterSymbol other)
            {
                return _typeParameter.Equals(other);
            }

            protected override TypeSymbolWithAnnotations SubstituteType(AbstractTypeMap typeMap, bool withTupleUnification)
            {
                var newUnderlying = typeMap.SubstituteType(_typeParameter, withTupleUnification);
                if ((object)newUnderlying != this)
                {
                    if ((newUnderlying.TypeSymbol.Equals(_typeParameter, TypeCompareKind.AllAspects) ||
                            newUnderlying.TypeSymbol is IndexedTypeParameterSymbolForOverriding) &&
                        newUnderlying.CustomModifiers.IsEmpty)
                    {
                        return newUnderlying;
                    }

                    return base.SubstituteType(typeMap, withTupleUnification);
                }
                else
                {
                    return this; // substitution had no effect on the type or modifiers
                }
            }

            protected override bool TypeSymbolEquals(TypeSymbolWithAnnotations other, TypeCompareKind comparison)
            {
                var otherLazy = other as LazyUnannotatedTypeParameter;
                if ((object)otherLazy != null)
                {
                    return
                        //IsNullable == otherInProgress.IsNullable &&
                        _isNullableIfReferenceType == otherLazy._isNullableIfReferenceType &&
                        _typeParameter == otherLazy._typeParameter;
                }
                return base.TypeSymbolEquals(other, comparison);
            }

            public override TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers)
            {
                Debug.Assert(customModifiers.IsEmpty);
                return this;
            }

            protected override TypeSymbolWithAnnotations DoUpdate(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                throw new NotImplementedException();
            }

            internal override bool GetIsReferenceType(ConsList<TypeParameterSymbol> inProgress)
            {
                return _typeParameter.GetIsReferenceType(inProgress);
            }

            internal override bool GetIsValueType(ConsList<TypeParameterSymbol> inProgress)
            {
                return _typeParameter.GetIsValueType(inProgress);
            }
        }
    }
}
