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
    /// A struct that combines a single type symbol with annotations
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal readonly struct TypeSymbolWithAnnotations : IFormattable
    {
        /// <summary>
        /// A builder for lazy instances of TypeSymbolWithAnnotations.
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        internal struct Builder
        {
            private TypeSymbol _defaultType;
            private NullableAnnotation _nullableAnnotation;
            private Extensions _extensions;

            /// <summary>
            /// The underlying type, unless overridden by _extensions.
            /// </summary>
            internal TypeSymbol DefaultType => _defaultType;

            /// <summary>
            /// True if the fields of the builder are unset.
            /// </summary>
            internal bool IsDefault => _defaultType is null && _nullableAnnotation == 0 && (_extensions == null || _extensions == Extensions.Default);

            /// <summary>
            /// Set the fields of the builder.
            /// </summary>
            /// <remarks>
            /// This method guarantees: fields will be set once; exactly one caller is
            /// returned true; and IsNull will return true until all fields are initialized.
            /// This method does not guarantee that all fields will be set by the same
            /// caller. Instead, the expectation is that all callers will attempt to initialize
            /// the builder with equivalent TypeSymbolWithAnnotations instances where
            /// different fields of the builder may be assigned from different instances.
            /// </remarks>
            internal bool InterlockedInitialize(TypeSymbolWithAnnotations type)
            {
                if ((object)_defaultType != null)
                {
                    return false;
                }
                _nullableAnnotation = type.NullableAnnotation;
                Interlocked.CompareExchange(ref _extensions, type._extensions, null);
                return (object)Interlocked.CompareExchange(ref _defaultType, type._defaultType, null) == null;
            }

            /// <summary>
            /// Create immutable TypeSymbolWithAnnotations instance.
            /// </summary>
            internal TypeSymbolWithAnnotations ToType()
            {
                return IsDefault ?
                    default :
                    new TypeSymbolWithAnnotations(_defaultType, _nullableAnnotation, _extensions);
            }

            internal string GetDebuggerDisplay() => ToType().GetDebuggerDisplay();
        }

        /// <summary>
        /// The underlying type, unless overridden by _extensions.
        /// </summary>
        private readonly TypeSymbol _defaultType;

        /// <summary>
        /// Additional data or behavior. Such cases should be
        /// uncommon to minimize allocations.
        /// </summary>
        private readonly Extensions _extensions;

        public readonly NullableAnnotation NullableAnnotation;

        private TypeSymbolWithAnnotations(TypeSymbol defaultType, NullableAnnotation nullableAnnotation, Extensions extensions)
        {
            Debug.Assert(defaultType?.IsNullableType() != true || (nullableAnnotation != NullableAnnotation.Oblivious && nullableAnnotation != NullableAnnotation.NotAnnotated));
            Debug.Assert(extensions != null);

            _defaultType = defaultType;
            NullableAnnotation = nullableAnnotation;
            _extensions = extensions;
        }

        public override string ToString() => TypeSymbol.ToString();
        public string Name => TypeSymbol.Name;
        public SymbolKind Kind => TypeSymbol.Kind;

        internal static readonly SymbolDisplayFormat DebuggerDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        internal static readonly SymbolDisplayFormat TestDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
            compilerInternalOptions: SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier);

        internal static TypeSymbolWithAnnotations Create(bool isNullableEnabled, TypeSymbol typeSymbol, bool isAnnotated = false, ImmutableArray<CustomModifier> customModifiers = default)
        {
            if (typeSymbol is null)
            {
                return default;
            }

            return Create(typeSymbol, nullableAnnotation: isAnnotated ? NullableAnnotation.Annotated : isNullableEnabled ? NullableAnnotation.NotAnnotated : NullableAnnotation.Oblivious,
                          customModifiers.NullToEmpty());
        }

        internal static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, NullableAnnotation nullableAnnotation = NullableAnnotation.Oblivious, ImmutableArray<CustomModifier> customModifiers = default)
        {
            if (typeSymbol is null && nullableAnnotation == 0)
            {
                return default;
            }

            switch (nullableAnnotation)
            {
                case NullableAnnotation.Oblivious:
                case NullableAnnotation.NotAnnotated:
                    if (typeSymbol?.IsNullableType() == true)
                    {
                        // int?, T? where T : struct (add annotation)
                        nullableAnnotation = NullableAnnotation.Annotated;
                    }
                    break;
            }

            return CreateNonLazyType(typeSymbol, nullableAnnotation, customModifiers.NullToEmpty());
        }

        internal bool IsPossiblyNullableTypeTypeParameter()
        {
            return NullableAnnotation.IsNotAnnotated() &&
                (TypeSymbol?.IsPossiblyNullableReferenceTypeTypeParameter() == true ||
                 TypeSymbol?.IsNullableTypeOrTypeParameter() == true);
        }

        internal NullableAnnotation GetValueNullableAnnotation()
        {
            if (IsPossiblyNullableTypeTypeParameter())
            {
                return NullableAnnotation.Annotated;
            }

            // https://github.com/dotnet/roslyn/issues/31675: Is a similar case needed in ValueCanBeNull?
            if (NullableAnnotation != NullableAnnotation.NotAnnotated && IsNullableTypeOrTypeParameter())
            {
                return NullableAnnotation.Annotated;
            }

            return NullableAnnotation;
        }

        internal bool CanBeAssignedNull
        {
            get
            {
                switch (NullableAnnotation)
                {
                    case NullableAnnotation.Oblivious:
                    case NullableAnnotation.Annotated:
                        return true;

                    case NullableAnnotation.NotAnnotated:
                        return TypeSymbol.IsNullableTypeOrTypeParameter();

                    default:
                        throw ExceptionUtilities.UnexpectedValue(NullableAnnotation);
                }
            }
        }

        private static bool IsIndexedTypeParameter(TypeSymbol typeSymbol)
        {
            return typeSymbol is IndexedTypeParameterSymbol ||
                   typeSymbol is IndexedTypeParameterSymbolForOverriding;
        }

        private static TypeSymbolWithAnnotations CreateNonLazyType(TypeSymbol typeSymbol, NullableAnnotation nullableAnnotation, ImmutableArray<CustomModifier> customModifiers)
        {
            return new TypeSymbolWithAnnotations(typeSymbol, nullableAnnotation, Extensions.Create(customModifiers));
        }

        private static TypeSymbolWithAnnotations CreateLazyNullableType(CSharpCompilation compilation, TypeSymbolWithAnnotations underlying)
        {
            return new TypeSymbolWithAnnotations(defaultType: underlying._defaultType, nullableAnnotation: NullableAnnotation.Annotated, Extensions.CreateLazy(compilation, underlying));
        }

        /// <summary>
        /// True if the fields are unset. Appropriate when detecting if a lazily-initialized variable has been initialized.
        /// </summary>
        internal bool IsDefault => _defaultType is null && this.NullableAnnotation == 0 && (_extensions == null || _extensions == Extensions.Default);

        /// <summary>
        /// True if the type is not null.
        /// </summary>
        internal bool HasType => !(_defaultType is null);

        public TypeSymbolWithAnnotations SetIsAnnotated(CSharpCompilation compilation)
        {
            Debug.Assert(CustomModifiers.IsEmpty);

            var typeSymbol = this.TypeSymbol;

            // It is not safe to check if a type parameter is a reference type right away, this can send us into a cycle.
            // In this case we delay asking this question as long as possible.
            if (typeSymbol.TypeKind != TypeKind.TypeParameter)
            {
                if (!typeSymbol.IsValueType && !typeSymbol.IsErrorType())
                {
                    return CreateNonLazyType(typeSymbol, NullableAnnotation.Annotated, this.CustomModifiers);
                }
                else
                {
                    return Create(compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(typeSymbol)));
                }
            }

            return CreateLazyNullableType(compilation, this);
        }

        private TypeSymbolWithAnnotations AsNullableReferenceType() => _extensions.AsNullableReferenceType(this);
        public TypeSymbolWithAnnotations AsNotNullableReferenceType() => _extensions.AsNotNullableReferenceType(this);

        /// <summary>
        /// Merges top-level and nested nullability from an otherwise identical type.
        /// </summary>
        internal TypeSymbolWithAnnotations MergeNullability(TypeSymbolWithAnnotations other, VarianceKind variance)
        {
            TypeSymbol typeSymbol = other.TypeSymbol;
            NullableAnnotation nullableAnnotation = MergeNullableAnnotation(this.NullableAnnotation, other.NullableAnnotation, variance);
            TypeSymbol type = TypeSymbol.MergeNullability(typeSymbol, variance);
            Debug.Assert((object)type != null);
            return Create(type, nullableAnnotation, CustomModifiers);
        }

        /// <summary>
        /// Merges nullability.
        /// </summary>
        private static NullableAnnotation MergeNullableAnnotation(NullableAnnotation a, NullableAnnotation b, VarianceKind variance)
        {
            switch (variance)
            {
                case VarianceKind.In:
                    return a.Meet(b);
                case VarianceKind.Out:
                    return a.Join(b);
                case VarianceKind.None:
                    return a.EnsureCompatible(b);
                default:
                    throw ExceptionUtilities.UnexpectedValue(variance);
            }
        }

        public TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers) =>
            _extensions.WithModifiers(this, customModifiers);

        public TypeSymbol TypeSymbol => _extensions?.GetResolvedType(_defaultType);
        public TypeSymbol NullableUnderlyingTypeOrSelf => _extensions.GetNullableUnderlyingTypeOrSelf(_defaultType);

        /// <summary>
        /// Is this System.Nullable`1 type, or its substitution.
        /// </summary>
        public bool IsNullableType() => TypeSymbol.IsNullableType();

        /// <summary>
        /// The list of custom modifiers, if any, associated with the <see cref="TypeSymbol"/>.
        /// </summary>
        public ImmutableArray<CustomModifier> CustomModifiers => _extensions.CustomModifiers;

        public bool IsReferenceType => TypeSymbol.IsReferenceType;
        public bool IsValueType => TypeSymbol.IsValueType;
        public TypeKind TypeKind => TypeSymbol.TypeKind;
        public SpecialType SpecialType => _extensions.GetSpecialType(_defaultType);
        public bool IsManagedType => TypeSymbol.IsManagedType;
        public Cci.PrimitiveTypeCode PrimitiveTypeCode => TypeSymbol.PrimitiveTypeCode;
        public bool IsEnumType() => TypeSymbol.IsEnumType();
        public bool IsDynamic() => TypeSymbol.IsDynamic();
        public bool IsObjectType() => TypeSymbol.IsObjectType();
        public bool IsArray() => TypeSymbol.IsArray();
        public bool IsRestrictedType(bool ignoreSpanLikeTypes = false) =>
            _extensions.IsRestrictedType(_defaultType, ignoreSpanLikeTypes);
        public bool IsPointerType() => TypeSymbol.IsPointerType();
        public bool IsErrorType() => TypeSymbol.IsErrorType();
        public bool IsUnsafe() => TypeSymbol.IsUnsafe();
        public bool IsStatic => _extensions.IsStatic(_defaultType);
        public bool IsNullableTypeOrTypeParameter() => TypeSymbol.IsNullableTypeOrTypeParameter();
        public bool IsVoid => _extensions.IsVoid(_defaultType);
        public bool IsSZArray() => _extensions.IsSZArray(_defaultType);
        public TypeSymbolWithAnnotations GetNullableUnderlyingType() =>
            TypeSymbol.GetNullableUnderlyingTypeWithAnnotations();

        internal bool GetIsReferenceType(ConsList<TypeParameterSymbol> inProgress) =>
            _extensions.GetIsReferenceType(_defaultType, inProgress);
        internal bool GetIsValueType(ConsList<TypeParameterSymbol> inProgress) =>
            _extensions.GetIsValueType(_defaultType, inProgress);

        public string ToDisplayString(SymbolDisplayFormat format = null)
        {
            var str = !HasType ? "<null>" : TypeSymbol.ToDisplayString(format);
            if (format != null)
            {
                if (format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier) &&
                    !IsNullableType() && !IsValueType &&
                    NullableAnnotation.IsAnnotated())
                {
                    return str + "?";
                }
                else if (format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier) &&
                    !IsValueType &&
                    NullableAnnotation.IsNotAnnotated() &&
                    !TypeSymbol.IsTypeParameterDisallowingAnnotation())
                {
                    return str + "!";
                }
            }

            return str;
        }

        internal string GetDebuggerDisplay() => !this.HasType ? "<null>" : ToDisplayString(DebuggerDisplayFormat);

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        }

        public bool Equals(TypeSymbolWithAnnotations other, TypeCompareKind comparison)
        {
            if (this.IsSameAs(other))
            {
                return true;
            }

            if (!HasType)
            {
                if (other.HasType || NullableAnnotation != other.NullableAnnotation)
                    return false;
            }
            else if (!other.HasType || !TypeSymbolEquals(other, comparison))
            {
                return false;
            }

            // Make sure custom modifiers are the same.
            if ((comparison & TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) == 0 &&
                !this.CustomModifiers.SequenceEqual(other.CustomModifiers))
            {
                return false;
            }

            var thisAnnotation = NullableAnnotation;
            var otherAnnotation = other.NullableAnnotation;
            if (!HasType)
            {
                return thisAnnotation == otherAnnotation;
            }
            else if ((comparison & TypeCompareKind.IgnoreNullableModifiersForReferenceTypes) == 0)
            {
                if (otherAnnotation != thisAnnotation && (!TypeSymbol.IsValueType || TypeSymbol.IsNullableType()))
                {
                    if (thisAnnotation.IsOblivious() || otherAnnotation.IsOblivious())
                    {
                        if ((comparison & TypeCompareKind.UnknownNullableModifierMatchesAny) == 0)
                        {
                            return false;
                        }
                    }
                    else if ((comparison & TypeCompareKind.IgnoreInsignificantNullableModifiersDifference) == 0)
                    {
                        return false;
                    }
                    else if (thisAnnotation.IsAnnotated())
                    {
                        if (!otherAnnotation.IsAnnotated())
                        {
                            return false;
                        }
                    }
                    else if (!otherAnnotation.IsAnnotated())
                    {
                        Debug.Assert(thisAnnotation.IsNotAnnotated());
                        Debug.Assert(otherAnnotation.IsNotAnnotated());
                        if (TypeSymbol.IsPossiblyNullableReferenceTypeTypeParameter())
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
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
                if (!obj.HasType)
                {
                    return 0;
                }
                return obj.TypeSymbol.GetHashCode();
            }

            public override bool Equals(TypeSymbolWithAnnotations x, TypeSymbolWithAnnotations y)
            {
                if (!x.HasType)
                {
                    return !y.HasType;
                }
                return x.Equals(y, TypeCompareKind.ConsiderEverything);
            }
        }

        internal bool TypeSymbolEquals(TypeSymbolWithAnnotations other, TypeCompareKind comparison) =>
            _extensions.TypeSymbolEquals(this, other, comparison);

        public bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return TypeSymbol.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes) ||
                   Symbol.GetUnificationUseSiteDiagnosticRecursive(ref result, this.CustomModifiers, owner, ref checkedTypes);
        }

        public void CheckAllConstraints(CSharpCompilation compilation, ConversionsBase conversions, Location location, DiagnosticBag diagnostics)
        {
            TypeSymbol.CheckAllConstraints(compilation, conversions, location, diagnostics);
        }

        public bool IsAtLeastAsVisibleAs(Symbol sym, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // System.Nullable is public, so it is safe to delegate to the underlying.
            return NullableUnderlyingTypeOrSelf.IsAtLeastAsVisibleAs(sym, ref useSiteDiagnostics);
        }

        public TypeSymbolWithAnnotations SubstituteType(AbstractTypeMap typeMap) =>
            _extensions.SubstituteType(this, typeMap, withTupleUnification: false);
        public TypeSymbolWithAnnotations SubstituteTypeWithTupleUnification(AbstractTypeMap typeMap) =>
            _extensions.SubstituteType(this, typeMap, withTupleUnification: true);

        internal TypeSymbolWithAnnotations TransformToTupleIfCompatible() => _extensions.TransformToTupleIfCompatible(this);

        internal TypeSymbolWithAnnotations SubstituteTypeCore(AbstractTypeMap typeMap, bool withTupleUnification)
        {
            var newCustomModifiers = typeMap.SubstituteCustomModifiers(this.CustomModifiers);
            TypeSymbol typeSymbol = this.TypeSymbol;
            var newTypeWithModifiers = typeMap.SubstituteType(typeSymbol, withTupleUnification);

            if (!typeSymbol.IsTypeParameter())
            {
                Debug.Assert(newTypeWithModifiers.NullableAnnotation.IsOblivious() || (typeSymbol.IsNullableType() && newTypeWithModifiers.NullableAnnotation.IsAnnotated()));
                Debug.Assert(newTypeWithModifiers.CustomModifiers.IsEmpty);

                if (typeSymbol.Equals(newTypeWithModifiers.TypeSymbol, TypeCompareKind.ConsiderEverything) &&
                    newCustomModifiers == CustomModifiers)
                {
                    return this; // substitution had no effect on the type or modifiers
                }
                else if ((NullableAnnotation.IsOblivious() || (typeSymbol.IsNullableType() && NullableAnnotation.IsAnnotated())) &&
                    newCustomModifiers.IsEmpty)
                {
                    return newTypeWithModifiers;
                }

                return Create(newTypeWithModifiers.TypeSymbol, NullableAnnotation, newCustomModifiers);
            }

            if (newTypeWithModifiers.Is((TypeParameterSymbol)typeSymbol) &&
                newCustomModifiers == CustomModifiers)
            {
                return this; // substitution had no effect on the type or modifiers
            }
            else if (Is((TypeParameterSymbol)typeSymbol))
            {
                return newTypeWithModifiers;
            }

            NullableAnnotation newAnnotation;

            Debug.Assert(!IsIndexedTypeParameter(newTypeWithModifiers.TypeSymbol) || newTypeWithModifiers.NullableAnnotation.IsOblivious());

            if (NullableAnnotation.IsAnnotated() || newTypeWithModifiers.NullableAnnotation.IsAnnotated())
            {
                newAnnotation = NullableAnnotation.IsAnnotated() || newTypeWithModifiers.NullableAnnotation.IsAnnotated() ?
                    NullableAnnotation.Annotated : NullableAnnotation.Annotated;
            }
            else if (IsIndexedTypeParameter(newTypeWithModifiers.TypeSymbol))
            {
                newAnnotation = NullableAnnotation;
            }
            else if (NullableAnnotation != NullableAnnotation.Oblivious)
            {
                if (!typeSymbol.IsTypeParameterDisallowingAnnotation())
                {
                    newAnnotation = NullableAnnotation;
                }
                else
                {
                    newAnnotation = newTypeWithModifiers.NullableAnnotation;
                }
            }
            else if (newTypeWithModifiers.NullableAnnotation != NullableAnnotation.Oblivious)
            {
                newAnnotation = newTypeWithModifiers.NullableAnnotation;
            }
            else
            {
                Debug.Assert(NullableAnnotation.IsOblivious());
                Debug.Assert(newTypeWithModifiers.NullableAnnotation.IsOblivious());
                newAnnotation = NullableAnnotation;
            }

            return CreateNonLazyType(
                newTypeWithModifiers.TypeSymbol,
                newAnnotation,
                newCustomModifiers.Concat(newTypeWithModifiers.CustomModifiers));
        }

        public void ReportDiagnosticsIfObsolete(Binder binder, SyntaxNode syntax, DiagnosticBag diagnostics) =>
            _extensions.ReportDiagnosticsIfObsolete(this, binder, syntax, diagnostics);

        internal bool TypeSymbolEqualsCore(TypeSymbolWithAnnotations other, TypeCompareKind comparison)
        {
            return TypeSymbol.Equals(other.TypeSymbol, comparison);
        }

        internal void ReportDiagnosticsIfObsoleteCore(Binder binder, SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            binder.ReportDiagnosticsIfObsolete(diagnostics, TypeSymbol, syntax, hasBaseReceiver: false);
        }

        /// <summary>
        /// Extract type under assumption that there should be no custom modifiers or annotations.
        /// The method asserts otherwise.
        /// </summary>
        public TypeSymbol AsTypeSymbolOnly() => _extensions.AsTypeSymbolOnly(_defaultType);

        /// <summary>
        /// Is this the given type parameter?
        /// </summary>
        public bool Is(TypeParameterSymbol other)
        {
            return NullableAnnotation.IsOblivious() && ((object)_defaultType == other) &&
                   CustomModifiers.IsEmpty;
        }

        public TypeSymbolWithAnnotations WithTypeAndModifiers(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers) =>
            _extensions.WithTypeAndModifiers(this, typeSymbol, customModifiers);

        public bool NeedsNullableAttribute()
        {
            return NeedsNullableAttribute(this, typeOpt: null);
        }

        public static bool NeedsNullableAttribute(
            TypeSymbolWithAnnotations typeWithAnnotationsOpt,
            TypeSymbol typeOpt)
        {
            var type = TypeSymbolExtensions.VisitType(
                typeWithAnnotationsOpt,
                typeOpt,
                typeWithAnnotationsPredicateOpt: (t, a, b) => t.NullableAnnotation != NullableAnnotation.Oblivious && !t.TypeSymbol.IsErrorType() && !t.TypeSymbol.IsValueType,
                typePredicateOpt: null,
                arg: (object)null);
            return (object)type != null;
        }

        public void AddNullableTransforms(ArrayBuilder<byte> transforms)
        {
            var typeSymbol = TypeSymbol;
            byte flag;

            if (NullableAnnotation.IsOblivious() || typeSymbol.IsValueType)
            {
                flag = (byte)NullableAnnotation.Oblivious;
            }
            else if (NullableAnnotation.IsAnnotated())
            {
                flag = (byte)NullableAnnotation.Annotated;
            }
            else
            {
                flag = (byte)NullableAnnotation.NotAnnotated;
            }

            transforms.Add(flag);
            typeSymbol.AddNullableTransforms(transforms);
        }

        public bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeSymbolWithAnnotations result)
        {
            result = this;

            byte transformFlag;
            if (transforms.IsDefault)
            {
                transformFlag = defaultTransformFlag;
            }
            else if (position < transforms.Length)
            {
                transformFlag = transforms[position++];
            }
            else
            {
                return false;
            }

            TypeSymbol oldTypeSymbol = TypeSymbol;
            TypeSymbol newTypeSymbol;

            if (!oldTypeSymbol.ApplyNullableTransforms(defaultTransformFlag, transforms, ref position, out newTypeSymbol))
            {
                return false;
            }

            if ((object)oldTypeSymbol != newTypeSymbol)
            {
                result = result.WithTypeAndModifiers(newTypeSymbol, result.CustomModifiers);
            }

            switch ((NullableAnnotation)transformFlag)
            {
                case NullableAnnotation.Annotated:
                    result = result.AsNullableReferenceType();
                    break;

                case NullableAnnotation.NotAnnotated:
                    result = result.AsNotNullableReferenceType();
                    break;

                case NullableAnnotation.Oblivious:
                    if (result.NullableAnnotation != NullableAnnotation.Oblivious &&
                        !(result.NullableAnnotation.IsAnnotated() && oldTypeSymbol.IsNullableType())) // Preserve nullable annotation on Nullable<T>.
                    {
                        result = CreateNonLazyType(newTypeSymbol, NullableAnnotation.Oblivious, result.CustomModifiers);
                    }
                    break;

                default:
                    result = this;
                    return false;
            }

            return true;
        }

        public TypeSymbolWithAnnotations WithTopLevelNonNullability()
        {
            var typeSymbol = TypeSymbol;
            if (NullableAnnotation.IsNotAnnotated() || (typeSymbol.IsValueType && !typeSymbol.IsNullableType()))
            {
                return this;
            }

            return CreateNonLazyType(typeSymbol, NullableAnnotation.NotAnnotated, CustomModifiers);
        }

        public TypeSymbolWithAnnotations SetUnknownNullabilityForReferenceTypes()
        {
            var typeSymbol = TypeSymbol;

            if (NullableAnnotation != NullableAnnotation.Oblivious)
            {
                if (!typeSymbol.IsValueType)
                {
                    typeSymbol = typeSymbol.SetUnknownNullabilityForReferenceTypes();

                    return CreateNonLazyType(typeSymbol, NullableAnnotation.Oblivious, CustomModifiers);
                }
            }

            var newTypeSymbol = typeSymbol.SetUnknownNullabilityForReferenceTypes();

            if ((object)newTypeSymbol != typeSymbol)
            {
                return WithTypeAndModifiers(newTypeSymbol, CustomModifiers);
            }

            return this;
        }

#pragma warning disable CS0809
        [Obsolete("Unsupported", error: true)]
        public override bool Equals(object other)
#pragma warning restore CS0809
        {
            throw ExceptionUtilities.Unreachable;
        }

#pragma warning disable CS0809
        [Obsolete("Unsupported", error: true)]
        public override int GetHashCode()
#pragma warning restore CS0809
        {
            if (!HasType)
            {
                return 0;
            }
            return TypeSymbol.GetHashCode();
        }

#pragma warning disable CS0809
        [Obsolete("Unsupported", error: true)]
        public static bool operator ==(TypeSymbolWithAnnotations? x, TypeSymbolWithAnnotations? y)
#pragma warning restore CS0809
        {
            throw ExceptionUtilities.Unreachable;
        }

#pragma warning disable CS0809
        [Obsolete("Unsupported", error: true)]
        public static bool operator !=(TypeSymbolWithAnnotations? x, TypeSymbolWithAnnotations? y)
#pragma warning restore CS0809
        {
            throw ExceptionUtilities.Unreachable;
        }

        // Field-wise ReferenceEquals.
        internal bool IsSameAs(TypeSymbolWithAnnotations other)
        {
            return ReferenceEquals(_defaultType, other._defaultType) &&
                NullableAnnotation == other.NullableAnnotation &&
                ReferenceEquals(_extensions, other._extensions);
        }

        /// <summary>
        /// Compute the flow state resulting from reading from an lvalue.
        /// </summary>
        internal TypeWithState ToTypeWithState()
        {
            // This operation reflects reading from an lvalue, which produces an rvalue.
            // Reading from a variable of a type parameter (that could be substituted with a nullable type), but which
            // cannot itself be annotated (because it isn't known to be a reference type), may yield a null value
            // even though the type parameter isn't annotated.
            return new TypeWithState(
                TypeSymbol,
                IsPossiblyNullableTypeTypeParameter() || NullableAnnotation.IsAnnotated() ? NullableFlowState.MaybeNull : NullableFlowState.NotNull);
        }

        /// <summary>
        /// Additional data or behavior beyond the core TypeSymbolWithAnnotations.
        /// </summary>
        private abstract class Extensions
        {
            internal static readonly Extensions Default = new NonLazyType(ImmutableArray<CustomModifier>.Empty);

            internal static Extensions Create(ImmutableArray<CustomModifier> customModifiers)
            {
                if (customModifiers.IsEmpty)
                {
                    return Default;
                }
                return new NonLazyType(customModifiers);
            }

            internal static Extensions CreateLazy(CSharpCompilation compilation, TypeSymbolWithAnnotations underlying)
            {
                return new LazyNullableTypeParameter(compilation, underlying);
            }

            internal abstract TypeSymbol GetResolvedType(TypeSymbol defaultType);
            internal abstract ImmutableArray<CustomModifier> CustomModifiers { get; }

            internal abstract TypeSymbolWithAnnotations AsNullableReferenceType(TypeSymbolWithAnnotations type);
            internal abstract TypeSymbolWithAnnotations AsNotNullableReferenceType(TypeSymbolWithAnnotations type);

            internal abstract TypeSymbolWithAnnotations WithModifiers(TypeSymbolWithAnnotations type, ImmutableArray<CustomModifier> customModifiers);

            internal abstract TypeSymbol GetNullableUnderlyingTypeOrSelf(TypeSymbol typeSymbol);

            internal abstract bool GetIsReferenceType(TypeSymbol typeSymbol, ConsList<TypeParameterSymbol> inProgress);
            internal abstract bool GetIsValueType(TypeSymbol typeSymbol, ConsList<TypeParameterSymbol> inProgress);

            internal abstract TypeSymbol AsTypeSymbolOnly(TypeSymbol typeSymbol);

            internal abstract SpecialType GetSpecialType(TypeSymbol typeSymbol);
            internal abstract bool IsRestrictedType(TypeSymbol typeSymbol, bool ignoreSpanLikeTypes);
            internal abstract bool IsStatic(TypeSymbol typeSymbol);
            internal abstract bool IsVoid(TypeSymbol typeSymbol);
            internal abstract bool IsSZArray(TypeSymbol typeSymbol);

            internal abstract TypeSymbolWithAnnotations WithTypeAndModifiers(TypeSymbolWithAnnotations type, TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers);

            internal abstract bool TypeSymbolEquals(TypeSymbolWithAnnotations type, TypeSymbolWithAnnotations other, TypeCompareKind comparison);
            internal abstract TypeSymbolWithAnnotations SubstituteType(TypeSymbolWithAnnotations type, AbstractTypeMap typeMap, bool withTupleUnification);
            internal abstract TypeSymbolWithAnnotations TransformToTupleIfCompatible(TypeSymbolWithAnnotations type);
            internal abstract void ReportDiagnosticsIfObsolete(TypeSymbolWithAnnotations type, Binder binder, SyntaxNode syntax, DiagnosticBag diagnostics);
        }

        private sealed class NonLazyType : Extensions
        {
            private readonly ImmutableArray<CustomModifier> _customModifiers;

            public NonLazyType(ImmutableArray<CustomModifier> customModifiers)
            {
                Debug.Assert(!customModifiers.IsDefault);
                _customModifiers = customModifiers;
            }

            internal override TypeSymbol GetResolvedType(TypeSymbol defaultType) => defaultType;
            internal override ImmutableArray<CustomModifier> CustomModifiers => _customModifiers;

            internal override SpecialType GetSpecialType(TypeSymbol typeSymbol) => typeSymbol.SpecialType;
            internal override bool IsRestrictedType(TypeSymbol typeSymbol, bool ignoreSpanLikeTypes) => typeSymbol.IsRestrictedType(ignoreSpanLikeTypes);
            internal override bool IsStatic(TypeSymbol typeSymbol) => typeSymbol.IsStatic;
            internal override bool IsVoid(TypeSymbol typeSymbol) => typeSymbol.SpecialType == SpecialType.System_Void;
            internal override bool IsSZArray(TypeSymbol typeSymbol) => typeSymbol.IsSZArray();

            internal override TypeSymbol GetNullableUnderlyingTypeOrSelf(TypeSymbol typeSymbol) => typeSymbol.StrippedType();

            internal override bool GetIsReferenceType(TypeSymbol typeSymbol, ConsList<TypeParameterSymbol> inProgress)
            {
                if (typeSymbol.TypeKind == TypeKind.TypeParameter)
                {
                    return ((TypeParameterSymbol)typeSymbol).GetIsReferenceType(inProgress);
                }
                return typeSymbol.IsReferenceType;
            }

            internal override bool GetIsValueType(TypeSymbol typeSymbol, ConsList<TypeParameterSymbol> inProgress)
            {
                if (typeSymbol.TypeKind == TypeKind.TypeParameter)
                {
                    return ((TypeParameterSymbol)typeSymbol).GetIsValueType(inProgress);
                }
                return typeSymbol.IsValueType;
            }

            internal override TypeSymbolWithAnnotations WithModifiers(TypeSymbolWithAnnotations type, ImmutableArray<CustomModifier> customModifiers)
            {
                return CreateNonLazyType(type._defaultType, type.NullableAnnotation, customModifiers);
            }

            internal override TypeSymbol AsTypeSymbolOnly(TypeSymbol typeSymbol) => typeSymbol;

            internal override TypeSymbolWithAnnotations WithTypeAndModifiers(TypeSymbolWithAnnotations type, TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                return CreateNonLazyType(typeSymbol, type.NullableAnnotation, customModifiers);
            }

            internal override TypeSymbolWithAnnotations AsNullableReferenceType(TypeSymbolWithAnnotations type)
            {
                return CreateNonLazyType(type._defaultType, NullableAnnotation.Annotated, _customModifiers);
            }

            internal override TypeSymbolWithAnnotations AsNotNullableReferenceType(TypeSymbolWithAnnotations type)
            {
                var defaultType = type._defaultType;
                return CreateNonLazyType(defaultType, defaultType.IsNullableType() ? type.NullableAnnotation : NullableAnnotation.NotAnnotated, _customModifiers);
            }

            internal override bool TypeSymbolEquals(TypeSymbolWithAnnotations type, TypeSymbolWithAnnotations other, TypeCompareKind comparison)
            {
                return type.TypeSymbolEqualsCore(other, comparison);
            }

            internal override TypeSymbolWithAnnotations SubstituteType(TypeSymbolWithAnnotations type, AbstractTypeMap typeMap, bool withTupleUnification)
            {
                return type.SubstituteTypeCore(typeMap, withTupleUnification);
            }

            internal override TypeSymbolWithAnnotations TransformToTupleIfCompatible(TypeSymbolWithAnnotations type)
            {
                var defaultType = type._defaultType;
                var transformedType = TupleTypeSymbol.TransformToTupleIfCompatible(defaultType);
                if ((object)defaultType != transformedType)
                {
                    return TypeSymbolWithAnnotations.Create(transformedType, type.NullableAnnotation, _customModifiers);
                }
                return type;
            }

            internal override void ReportDiagnosticsIfObsolete(TypeSymbolWithAnnotations type, Binder binder, SyntaxNode syntax, DiagnosticBag diagnostics)
            {
                type.ReportDiagnosticsIfObsoleteCore(binder, syntax, diagnostics);
            }
        }

        /// <summary>
        /// Nullable type parameter. The underlying TypeSymbol is resolved
        /// lazily to avoid cycles when binding declarations.
        /// </summary>
        private sealed class LazyNullableTypeParameter : Extensions
        {
            private readonly CSharpCompilation _compilation;
            private readonly TypeSymbolWithAnnotations _underlying;
            private TypeSymbol _resolved;

            public LazyNullableTypeParameter(CSharpCompilation compilation, TypeSymbolWithAnnotations underlying)
            {
                Debug.Assert(!underlying.NullableAnnotation.IsAnnotated());
                Debug.Assert(underlying.TypeKind == TypeKind.TypeParameter);
                Debug.Assert(underlying.CustomModifiers.IsEmpty);
                _compilation = compilation;
                _underlying = underlying;
            }

            internal override bool IsVoid(TypeSymbol typeSymbol) => false;
            internal override bool IsSZArray(TypeSymbol typeSymbol) => false;
            internal override bool IsStatic(TypeSymbol typeSymbol) => false;

            private TypeSymbol GetResolvedType()
            {
                if ((object)_resolved == null)
                {
                    if (!_underlying.IsValueType)
                    {
                        _resolved = _underlying.TypeSymbol;
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref _resolved,
                            _compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(_underlying)),
                            null);
                    }
                }

                return _resolved;
            }

            internal override bool GetIsReferenceType(TypeSymbol typeSymbol, ConsList<TypeParameterSymbol> inProgress)
            {
                return _underlying.GetIsReferenceType(inProgress);
            }

            internal override bool GetIsValueType(TypeSymbol typeSymbol, ConsList<TypeParameterSymbol> inProgress)
            {
                return _underlying.GetIsValueType(inProgress);
            }

            internal override TypeSymbol GetNullableUnderlyingTypeOrSelf(TypeSymbol typeSymbol) => _underlying.TypeSymbol;

            internal override SpecialType GetSpecialType(TypeSymbol typeSymbol)
            {
                var specialType = _underlying.SpecialType;
                return specialType.IsValueType() ? SpecialType.None : specialType;
            }

            internal override bool IsRestrictedType(TypeSymbol typeSymbol, bool ignoreSpanLikeTypes) => _underlying.IsRestrictedType(ignoreSpanLikeTypes);

            internal override TypeSymbol AsTypeSymbolOnly(TypeSymbol typeSymbol)
            {
                var resolvedType = GetResolvedType();
                Debug.Assert(resolvedType.IsNullableType() && CustomModifiers.IsEmpty);
                return resolvedType;
            }

            internal override TypeSymbol GetResolvedType(TypeSymbol defaultType) => GetResolvedType();
            internal override ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;

            internal override TypeSymbolWithAnnotations WithModifiers(TypeSymbolWithAnnotations type, ImmutableArray<CustomModifier> customModifiers)
            {
                if (customModifiers.IsEmpty)
                {
                    return type;
                }

                var resolvedType = GetResolvedType();
                if (resolvedType.IsNullableType())
                {
                    return TypeSymbolWithAnnotations.Create(resolvedType, type.NullableAnnotation, customModifiers: customModifiers);
                }

                return CreateNonLazyType(resolvedType, type.NullableAnnotation, customModifiers);
            }

            internal override TypeSymbolWithAnnotations WithTypeAndModifiers(TypeSymbolWithAnnotations type, TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                if (typeSymbol.IsNullableType())
                {
                    return TypeSymbolWithAnnotations.Create(typeSymbol, type.NullableAnnotation, customModifiers: customModifiers);
                }

                return CreateNonLazyType(typeSymbol, type.NullableAnnotation, customModifiers);
            }

            internal override TypeSymbolWithAnnotations AsNullableReferenceType(TypeSymbolWithAnnotations type)
            {
                return type;
            }

            internal override TypeSymbolWithAnnotations AsNotNullableReferenceType(TypeSymbolWithAnnotations type)
            {
                if (!_underlying.TypeSymbol.IsValueType)
                {
                    return _underlying;
                }
                return type;
            }

            internal override TypeSymbolWithAnnotations SubstituteType(TypeSymbolWithAnnotations type, AbstractTypeMap typeMap, bool withTupleUnification)
            {
                if ((object)_resolved != null)
                {
                    return type.SubstituteTypeCore(typeMap, withTupleUnification);
                }

                var newUnderlying = _underlying.SubstituteTypeCore(typeMap, withTupleUnification);
                if (!newUnderlying.IsSameAs(this._underlying))
                {
                    if ((newUnderlying.TypeSymbol.Equals(this._underlying.TypeSymbol, TypeCompareKind.ConsiderEverything) ||
                            newUnderlying.TypeSymbol is IndexedTypeParameterSymbolForOverriding) &&
                        newUnderlying.CustomModifiers.IsEmpty)
                    {
                        return CreateLazyNullableType(_compilation, newUnderlying);
                    }

                    return type.SubstituteTypeCore(typeMap, withTupleUnification);
                }
                else
                {
                    return type; // substitution had no effect on the type or modifiers
                }
            }

            internal override TypeSymbolWithAnnotations TransformToTupleIfCompatible(TypeSymbolWithAnnotations type)
            {
                return type;
            }

            internal override void ReportDiagnosticsIfObsolete(TypeSymbolWithAnnotations type, Binder binder, SyntaxNode syntax, DiagnosticBag diagnostics)
            {
                if ((object)_resolved != null)
                {
                    type.ReportDiagnosticsIfObsoleteCore(binder, syntax, diagnostics);
                }
                else
                {
                    diagnostics.Add(new LazyObsoleteDiagnosticInfo(type, binder.ContainingMemberOrLambda, binder.Flags), syntax.GetLocation());
                }
            }

            internal override bool TypeSymbolEquals(TypeSymbolWithAnnotations type, TypeSymbolWithAnnotations other, TypeCompareKind comparison)
            {
                var otherLazy = other._extensions as LazyNullableTypeParameter;

                if ((object)otherLazy != null)
                {
                    return _underlying.TypeSymbolEquals(otherLazy._underlying, comparison);
                }

                return type.TypeSymbolEqualsCore(other, comparison);
            }
        }
    }
}
