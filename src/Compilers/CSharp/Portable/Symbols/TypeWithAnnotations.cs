// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A struct that combines a single type with annotations
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal readonly struct TypeWithAnnotations : IFormattable
    {
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        internal sealed class Boxed
        {
            internal readonly TypeWithAnnotations Value;
            internal Boxed(TypeWithAnnotations value)
            {
                Value = value;
            }
            internal string GetDebuggerDisplay() => Value.GetDebuggerDisplay();
        }

        /// <summary>
        /// The underlying type, unless overridden by _extensions.
        /// </summary>
        internal readonly TypeSymbol DefaultType;

        /// <summary>
        /// Additional data or behavior. Such cases should be
        /// uncommon to minimize allocations.
        /// </summary>
        private readonly Extensions _extensions;

        public readonly NullableAnnotation NullableAnnotation;

        private TypeWithAnnotations(TypeSymbol defaultType, NullableAnnotation nullableAnnotation, Extensions extensions)
        {
            Debug.Assert(defaultType?.IsNullableType() != true || nullableAnnotation == NullableAnnotation.Annotated);
            Debug.Assert(extensions != null);

            DefaultType = defaultType;
            NullableAnnotation = nullableAnnotation;
            _extensions = extensions;
        }

        public override string ToString() => Type.ToString();

        private static readonly SymbolDisplayFormat DebuggerDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        internal static readonly SymbolDisplayFormat TestDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
            compilerInternalOptions: SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier);

        internal static TypeWithAnnotations Create(bool isNullableEnabled, TypeSymbol typeSymbol, bool isAnnotated = false)
        {
            if (typeSymbol is null)
            {
                return default;
            }

            return Create(typeSymbol, nullableAnnotation: isAnnotated ? NullableAnnotation.Annotated : isNullableEnabled ? NullableAnnotation.NotAnnotated : NullableAnnotation.Oblivious);
        }

        internal static TypeWithAnnotations Create(TypeSymbol typeSymbol, NullableAnnotation nullableAnnotation = NullableAnnotation.Oblivious, ImmutableArray<CustomModifier> customModifiers = default)
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

        internal TypeWithAnnotations AsAnnotated()
        {
            if (NullableAnnotation.IsAnnotated() || (Type.IsValueType && Type.IsNullableType()))
            {
                return this;
            }

            return Create(Type, NullableAnnotation.Annotated, CustomModifiers);
        }

        internal TypeWithAnnotations AsNotAnnotated()
        {
            if (NullableAnnotation.IsNotAnnotated() || (Type.IsValueType && !Type.IsNullableType()))
            {
                return this;
            }

            return Create(Type, NullableAnnotation.NotAnnotated, CustomModifiers);
        }

        internal bool IsPossiblyNullableTypeTypeParameter()
        {
            return NullableAnnotation.IsNotAnnotated() &&
                (Type?.IsPossiblyNullableReferenceTypeTypeParameter() == true ||
                 Type?.IsNullableTypeOrTypeParameter() == true);
        }

        internal NullableAnnotation GetValueNullableAnnotation()
        {
            if (IsPossiblyNullableTypeTypeParameter())
            {
                return NullableAnnotation.Annotated;
            }

            // https://github.com/dotnet/roslyn/issues/31675: Is a similar case needed in ValueCanBeNull?
            if (NullableAnnotation != NullableAnnotation.NotAnnotated && Type.IsNullableTypeOrTypeParameter())
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
                        return Type.IsNullableTypeOrTypeParameter();

                    default:
                        throw ExceptionUtilities.UnexpectedValue(NullableAnnotation);
                }
            }
        }

        private static bool IsIndexedTypeParameter(TypeSymbol typeSymbol)
        {
            return typeSymbol is IndexedTypeParameterSymbol;
        }

        private static TypeWithAnnotations CreateNonLazyType(TypeSymbol typeSymbol, NullableAnnotation nullableAnnotation, ImmutableArray<CustomModifier> customModifiers)
        {
            return new TypeWithAnnotations(typeSymbol, nullableAnnotation, Extensions.Create(customModifiers));
        }

        private static TypeWithAnnotations CreateLazyNullableType(CSharpCompilation compilation, TypeWithAnnotations underlying)
        {
            return new TypeWithAnnotations(defaultType: underlying.DefaultType, nullableAnnotation: NullableAnnotation.Annotated, Extensions.CreateLazy(compilation, underlying));
        }

        /// <summary>
        /// True if the fields are unset. Appropriate when detecting if a lazily-initialized variable has been initialized.
        /// </summary>
        internal bool IsDefault => DefaultType is null && this.NullableAnnotation == 0 && (_extensions == null || _extensions == Extensions.Default);

        /// <summary>
        /// True if the type is not null.
        /// </summary>
        internal bool HasType => !(DefaultType is null);

        public TypeWithAnnotations SetIsAnnotated(CSharpCompilation compilation)
        {
            Debug.Assert(CustomModifiers.IsEmpty);

            var typeSymbol = this.Type;

            if (typeSymbol.TypeKind != TypeKind.TypeParameter)
            {
                if (!typeSymbol.IsValueType && !typeSymbol.IsErrorType())
                {
                    return CreateNonLazyType(typeSymbol, NullableAnnotation.Annotated, this.CustomModifiers);
                }
                else
                {
                    return makeNullableT();
                }
            }

            if (((TypeParameterSymbol)typeSymbol).TypeParameterKind == TypeParameterKind.Cref)
            {
                // We always bind annotated type parameters in cref as `Nullable<T>`
                return makeNullableT();
            }

            // It is not safe to check if a type parameter is a reference type right away, this can send us into a cycle.
            // In this case we delay asking this question as long as possible.
            return CreateLazyNullableType(compilation, this);

            TypeWithAnnotations makeNullableT()
                => Create(compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(typeSymbol)));
        }

        /// <summary>
        /// If this is a lazy nullable type pending resolution, forces this to be resolved as a nullable value type.
        /// </summary>
        public void TryForceResolveAsNullableValueType()
        {
            _extensions.TryForceResolveAsNullableValueType();
        }

        /// <summary>
        /// If this is a lazy nullable type pending resolution, forces this to be resolved as a nullable reference type.
        /// </summary>
        public void TryForceResolveAsNullableReferenceType()
        {
            _extensions.TryForceResolveAsNullableReferenceType();
        }

        private TypeWithAnnotations AsNullableReferenceType() => _extensions.AsNullableReferenceType(this);
        public TypeWithAnnotations AsNotNullableReferenceType() => _extensions.AsNotNullableReferenceType(this);

        /// <summary>
        /// Merges top-level and nested nullability from an otherwise identical type.
        /// </summary>
        internal TypeWithAnnotations MergeNullability(TypeWithAnnotations other, VarianceKind variance)
        {
            TypeSymbol typeSymbol = other.Type;
            NullableAnnotation nullableAnnotation = this.NullableAnnotation.MergeNullableAnnotation(other.NullableAnnotation, variance);
            TypeSymbol type = Type.MergeNullability(typeSymbol, variance);
            Debug.Assert((object)type != null);
            return Create(type, nullableAnnotation, CustomModifiers);
        }

        public TypeWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers) =>
            _extensions.WithModifiers(this, customModifiers);

        public bool IsResolved => _extensions?.IsResolved != false;
        public TypeSymbol Type => _extensions?.GetResolvedType(DefaultType);
        public TypeSymbol NullableUnderlyingTypeOrSelf => _extensions.GetNullableUnderlyingTypeOrSelf(DefaultType);

        /// <summary>
        /// Is this System.Nullable`1 type, or its substitution.
        /// 
        /// To check whether a type is System.Nullable`1 or is a type parameter constrained to System.Nullable`1
        /// use <see cref="TypeSymbolExtensions.IsNullableTypeOrTypeParameter" /> instead.
        /// </summary>
        public bool IsNullableType() => Type.IsNullableType();

        /// <summary>
        /// The list of custom modifiers, if any, associated with the <see cref="Type"/>.
        /// </summary>
        public ImmutableArray<CustomModifier> CustomModifiers => _extensions.CustomModifiers;

        public TypeKind TypeKind => Type.TypeKind;
        public SpecialType SpecialType => _extensions.GetSpecialType(DefaultType);
        public Cci.PrimitiveTypeCode PrimitiveTypeCode => Type.PrimitiveTypeCode;

        public bool IsVoidType() =>
            _extensions.IsVoid(DefaultType);
        public bool IsSZArray() =>
            _extensions.IsSZArray(DefaultType);
        public bool IsStatic =>
            _extensions.IsStatic(DefaultType);
        public bool IsRestrictedType(bool ignoreSpanLikeTypes = false) =>
            _extensions.IsRestrictedType(DefaultType, ignoreSpanLikeTypes);

        public string ToDisplayString(SymbolDisplayFormat format = null)
        {
            if (!IsResolved)
            {
                if (!IsSafeToResolve())
                {
                    if (NullableAnnotation.IsAnnotated() &&
                        format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier))
                    {
                        return DefaultType.ToDisplayString(format) + "?";
                    }

                    return DefaultType.ToDisplayString(format);
                }
            }

            var str = !HasType ? "<null>" : Type.ToDisplayString(format);
            if (format != null)
            {
                if (NullableAnnotation.IsAnnotated() &&
                    format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier) &&
                    (!HasType || (!IsNullableType() && !Type.IsValueType)))
                {
                    return str + "?";
                }
                else if (NullableAnnotation.IsNotAnnotated() &&
                    format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier) &&
                    (!HasType || (!Type.IsValueType && !Type.IsTypeParameterDisallowingAnnotation())))
                {
                    return str + "!";
                }
            }

            return str;
        }

        private bool IsSafeToResolve()
        {
            var declaringMethod = (DefaultType as TypeParameterSymbol)?.DeclaringMethod as SourceOrdinaryMethodSymbol;
            return !((object)declaringMethod != null && !declaringMethod.HasComplete(CompletionPart.FinishMethodChecks) &&
                   (declaringMethod.IsOverride || declaringMethod.IsExplicitInterfaceImplementation));
        }

        internal string GetDebuggerDisplay() => !this.HasType ? "<null>" : ToDisplayString(DebuggerDisplayFormat);

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        }

        public bool Equals(TypeWithAnnotations other, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt = null)
        {
            if (this.IsSameAs(other))
            {
                return true;
            }

            if (!HasType)
            {
                if (other.HasType)
                {
                    return false;
                }
            }
            else if (!other.HasType || !TypeSymbolEquals(other, comparison, isValueTypeOverrideOpt))
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

            if ((comparison & TypeCompareKind.IgnoreNullableModifiersForReferenceTypes) == 0)
            {
                if (otherAnnotation != thisAnnotation &&
                    ((comparison & TypeCompareKind.ObliviousNullableModifierMatchesAny) == 0 || (!thisAnnotation.IsOblivious() && !otherAnnotation.IsOblivious())))
                {
                    if (!HasType)
                    {
                        return false;
                    }

                    TypeSymbol type = Type;
                    bool isValueType;

                    if (isValueTypeOverrideOpt == null || !(type is TypeParameterSymbol typeParameter) || !isValueTypeOverrideOpt.TryGetValue(typeParameter, out isValueType))
                    {
                        isValueType = type.IsValueType && !type.IsNullableType();
                    }

                    if (!isValueType)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal sealed class EqualsComparer : EqualityComparer<TypeWithAnnotations>
        {
            internal static readonly EqualsComparer ConsiderEverythingComparer = new EqualsComparer(TypeCompareKind.ConsiderEverything, isValueTypeOverrideOpt: null);

            private readonly TypeCompareKind _compareKind;
            private readonly IReadOnlyDictionary<TypeParameterSymbol, bool> _isValueTypeOverrideOpt;

            public EqualsComparer(TypeCompareKind compareKind, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt)
            {
                _compareKind = compareKind;
                _isValueTypeOverrideOpt = isValueTypeOverrideOpt;
            }

            public override int GetHashCode(TypeWithAnnotations obj)
            {
                if (!obj.HasType)
                {
                    return 0;
                }
                return obj.Type.GetHashCode();
            }

            public override bool Equals(TypeWithAnnotations x, TypeWithAnnotations y)
            {
                if (!x.HasType)
                {
                    return !y.HasType;
                }
                return x.Equals(y, _compareKind, _isValueTypeOverrideOpt);
            }
        }

        internal bool TypeSymbolEquals(TypeWithAnnotations other, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt) =>
            _extensions.TypeSymbolEquals(this, other, comparison, isValueTypeOverrideOpt);

        public bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return Type.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes) ||
                   Symbol.GetUnificationUseSiteDiagnosticRecursive(ref result, this.CustomModifiers, owner, ref checkedTypes);
        }

        public bool IsAtLeastAsVisibleAs(Symbol sym, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // System.Nullable is public, so it is safe to delegate to the underlying.
            return NullableUnderlyingTypeOrSelf.IsAtLeastAsVisibleAs(sym, ref useSiteDiagnostics);
        }

        public TypeWithAnnotations SubstituteType(AbstractTypeMap typeMap) =>
            _extensions.SubstituteType(this, typeMap, withTupleUnification: false);
        public TypeWithAnnotations SubstituteTypeWithTupleUnification(AbstractTypeMap typeMap) =>
            _extensions.SubstituteType(this, typeMap, withTupleUnification: true);

        internal TypeWithAnnotations TransformToTupleIfCompatible() => _extensions.TransformToTupleIfCompatible(this);

        internal TypeWithAnnotations SubstituteTypeCore(AbstractTypeMap typeMap, bool withTupleUnification)
        {
            var newCustomModifiers = typeMap.SubstituteCustomModifiers(this.CustomModifiers);
            TypeSymbol typeSymbol = this.Type;
            var newTypeWithModifiers = typeMap.SubstituteType(typeSymbol, withTupleUnification);

            if (!typeSymbol.IsTypeParameter())
            {
                Debug.Assert(newTypeWithModifiers.NullableAnnotation.IsOblivious() || (typeSymbol.IsNullableType() && newTypeWithModifiers.NullableAnnotation.IsAnnotated()));
                Debug.Assert(newTypeWithModifiers.CustomModifiers.IsEmpty);

                if (typeSymbol.Equals(newTypeWithModifiers.Type, TypeCompareKind.ConsiderEverything) &&
                    newCustomModifiers == CustomModifiers)
                {
                    return this; // substitution had no effect on the type or modifiers
                }
                else if ((NullableAnnotation.IsOblivious() || (typeSymbol.IsNullableType() && NullableAnnotation.IsAnnotated())) &&
                    newCustomModifiers.IsEmpty)
                {
                    return newTypeWithModifiers;
                }

                return Create(newTypeWithModifiers.Type, NullableAnnotation, newCustomModifiers);
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

            Debug.Assert(!IsIndexedTypeParameter(newTypeWithModifiers.Type) || newTypeWithModifiers.NullableAnnotation.IsOblivious());

            if (NullableAnnotation.IsAnnotated() || newTypeWithModifiers.NullableAnnotation.IsAnnotated())
            {
                newAnnotation = NullableAnnotation.Annotated;
            }
            else if (IsIndexedTypeParameter(newTypeWithModifiers.Type))
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
                newTypeWithModifiers.Type,
                newAnnotation,
                newCustomModifiers.Concat(newTypeWithModifiers.CustomModifiers));
        }

        public void ReportDiagnosticsIfObsolete(Binder binder, SyntaxNode syntax, DiagnosticBag diagnostics) =>
            _extensions.ReportDiagnosticsIfObsolete(this, binder, syntax, diagnostics);

        private bool TypeSymbolEqualsCore(TypeWithAnnotations other, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt)
        {
            return Type.Equals(other.Type, comparison, isValueTypeOverrideOpt);
        }

        private void ReportDiagnosticsIfObsoleteCore(Binder binder, SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            binder.ReportDiagnosticsIfObsolete(diagnostics, Type, syntax, hasBaseReceiver: false);
        }

        /// <summary>
        /// Extract type under assumption that there should be no custom modifiers or annotations.
        /// The method asserts otherwise.
        /// </summary>
        public TypeSymbol AsTypeSymbolOnly() => _extensions.AsTypeSymbolOnly(DefaultType);

        /// <summary>
        /// Is this the given type parameter?
        /// </summary>
        public bool Is(TypeParameterSymbol other)
        {
            return NullableAnnotation.IsOblivious() && ((object)DefaultType == other) &&
                   CustomModifiers.IsEmpty;
        }

        public TypeWithAnnotations WithTypeAndModifiers(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers) =>
            _extensions.WithTypeAndModifiers(this, typeSymbol, customModifiers);

        /// <summary>
        /// Used by callers before calling CSharpCompilation.EnsureNullableAttributeExists().
        /// </summary>
        /// <remarks>
        /// This method ignores any [NullableContext]. For example, if there is a [NullableContext(1)]
        /// at the containing type, and this type reference is oblivious, NeedsNullableAttribute()
        /// will return false even though a [Nullable(0)] will be emitted for this type reference.
        /// In practice, this shouldn't be an issue though since EnsuresNullableAttributeExists()
        /// will have returned true for at least some of other type references that required
        /// [Nullable(1)] and were subsequently aggregated to the [NullableContext(1)].
        /// </remarks>
        public bool NeedsNullableAttribute()
        {
            return NeedsNullableAttribute(this, typeOpt: null);
        }

        public static bool NeedsNullableAttribute(
            TypeWithAnnotations typeWithAnnotationsOpt,
            TypeSymbol typeOpt)
        {
            var type = TypeSymbolExtensions.VisitType(
                typeWithAnnotationsOpt,
                typeOpt,
                typeWithAnnotationsPredicateOpt: (t, a, b) => t.NullableAnnotation != NullableAnnotation.Oblivious && !t.Type.IsErrorType() && !t.Type.IsValueType,
                typePredicateOpt: null,
                arg: (object)null);
            return (object)type != null;
        }

        /// <summary>
        /// If the type is a non-generic value type or Nullable&lt;&gt;, and
        /// is not a type parameter, the nullability is not included in the byte[].
        /// </summary>
        private static bool IsNonGenericValueType(TypeSymbol type)
        {
            var namedType = type as NamedTypeSymbol;
            if (namedType is null)
            {
                return false;
            }
            if (namedType.IsGenericType)
            {
                return type.IsNullableType();
            }
            return type.IsValueType && !type.IsTupleType;
        }

        public void AddNullableTransforms(ArrayBuilder<byte> transforms)
        {
            AddNullableTransforms(this, transforms);
        }

        private static void AddNullableTransforms(TypeWithAnnotations typeWithAnnotations, ArrayBuilder<byte> transforms)
        {
            while (true)
            {
                var type = typeWithAnnotations.Type;

                if (!IsNonGenericValueType(type))
                {
                    var annotation = typeWithAnnotations.NullableAnnotation;
                    byte flag;
                    if (annotation.IsOblivious() || type.IsValueType)
                    {
                        flag = NullableAnnotationExtensions.ObliviousAttributeValue;
                    }
                    else if (annotation.IsAnnotated())
                    {
                        flag = NullableAnnotationExtensions.AnnotatedAttributeValue;
                    }
                    else
                    {
                        flag = NullableAnnotationExtensions.NotAnnotatedAttributeValue;
                    }
                    transforms.Add(flag);
                }

                if (type.TypeKind != TypeKind.Array)
                {
                    type.AddNullableTransforms(transforms);
                    return;
                }

                // Avoid recursion to allow for deeply-nested arrays.
                typeWithAnnotations = ((ArrayTypeSymbol)type).ElementTypeWithAnnotations;
            }
        }

        public bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeWithAnnotations result)
        {
            result = this;

            TypeSymbol oldTypeSymbol = Type;
            byte transformFlag;

            // Check IsNonGenericValueType first to avoid
            // applying transforms to simple value types.
            if (IsNonGenericValueType(oldTypeSymbol))
            {
                transformFlag = NullableAnnotationExtensions.ObliviousAttributeValue;
            }
            else if (transforms.IsDefault)
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

            TypeSymbol newTypeSymbol;
            if (!oldTypeSymbol.ApplyNullableTransforms(defaultTransformFlag, transforms, ref position, out newTypeSymbol))
            {
                return false;
            }

            if ((object)oldTypeSymbol != newTypeSymbol)
            {
                result = result.WithTypeAndModifiers(newTypeSymbol, result.CustomModifiers);
            }

            switch (transformFlag)
            {
                case NullableAnnotationExtensions.AnnotatedAttributeValue:
                    result = result.AsNullableReferenceType();
                    break;

                case NullableAnnotationExtensions.NotAnnotatedAttributeValue:
                    result = result.AsNotNullableReferenceType();
                    break;

                case NullableAnnotationExtensions.ObliviousAttributeValue:
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

        public TypeWithAnnotations WithTopLevelNonNullability()
        {
            var typeSymbol = Type;
            if (NullableAnnotation.IsNotAnnotated() || (typeSymbol.IsValueType && !typeSymbol.IsNullableType()))
            {
                return this;
            }

            return CreateNonLazyType(typeSymbol, NullableAnnotation.NotAnnotated, CustomModifiers);
        }

        public TypeWithAnnotations SetUnknownNullabilityForReferenceTypes()
        {
            var typeSymbol = Type;

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
            return Type.GetHashCode();
        }

        /// <summary>
        /// Used by the generated <see cref="BoundTypeExpression.Update"/>.
        /// </summary>
        public static bool operator ==(TypeWithAnnotations? x, TypeWithAnnotations? y)
        {
            return x.HasValue == y.HasValue && (!x.HasValue || x.GetValueOrDefault().IsSameAs(y.GetValueOrDefault()));
        }

        /// <summary>
        /// Used by the generated <see cref="BoundTypeExpression.Update"/>.
        /// </summary>
        public static bool operator !=(TypeWithAnnotations? x, TypeWithAnnotations? y)
        {
            return !(x == y);
        }

        // Field-wise ReferenceEquals.
        internal bool IsSameAs(TypeWithAnnotations other)
        {
            return ReferenceEquals(DefaultType, other.DefaultType) &&
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
            return TypeWithState.Create(
                Type,
                IsPossiblyNullableTypeTypeParameter() || NullableAnnotation.IsAnnotated() ? NullableFlowState.MaybeNull : NullableFlowState.NotNull);
        }

        /// <summary>
        /// Additional data or behavior beyond the core TypeWithAnnotations.
        /// </summary>
        private abstract class Extensions
        {
            internal static readonly Extensions Default = new NonLazyType(customModifiers: ImmutableArray<CustomModifier>.Empty);

            internal static Extensions Create(ImmutableArray<CustomModifier> customModifiers)
            {
                if (customModifiers.IsEmpty)
                {
                    return Default;
                }
                return new NonLazyType(customModifiers);
            }

            internal static Extensions CreateLazy(CSharpCompilation compilation, TypeWithAnnotations underlying)
            {
                return new LazyNullableTypeParameter(compilation, underlying);
            }

            internal abstract bool IsResolved { get; }
            internal abstract TypeSymbol GetResolvedType(TypeSymbol defaultType);
            internal abstract ImmutableArray<CustomModifier> CustomModifiers { get; }

            internal abstract TypeWithAnnotations AsNullableReferenceType(TypeWithAnnotations type);
            internal abstract TypeWithAnnotations AsNotNullableReferenceType(TypeWithAnnotations type);

            internal abstract TypeWithAnnotations WithModifiers(TypeWithAnnotations type, ImmutableArray<CustomModifier> customModifiers);

            internal abstract TypeSymbol GetNullableUnderlyingTypeOrSelf(TypeSymbol typeSymbol);

            internal abstract TypeSymbol AsTypeSymbolOnly(TypeSymbol typeSymbol);

            internal abstract SpecialType GetSpecialType(TypeSymbol typeSymbol);
            internal abstract bool IsRestrictedType(TypeSymbol typeSymbol, bool ignoreSpanLikeTypes);
            internal abstract bool IsStatic(TypeSymbol typeSymbol);
            internal abstract bool IsVoid(TypeSymbol typeSymbol);
            internal abstract bool IsSZArray(TypeSymbol typeSymbol);

            internal abstract TypeWithAnnotations WithTypeAndModifiers(TypeWithAnnotations type, TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers);

            internal abstract bool TypeSymbolEquals(TypeWithAnnotations type, TypeWithAnnotations other, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt);
            internal abstract TypeWithAnnotations SubstituteType(TypeWithAnnotations type, AbstractTypeMap typeMap, bool withTupleUnification);
            internal abstract TypeWithAnnotations TransformToTupleIfCompatible(TypeWithAnnotations type);
            internal abstract void ReportDiagnosticsIfObsolete(TypeWithAnnotations type, Binder binder, SyntaxNode syntax, DiagnosticBag diagnostics);

            internal abstract void TryForceResolveAsNullableValueType();

            internal abstract void TryForceResolveAsNullableReferenceType();
        }

        private sealed class NonLazyType : Extensions
        {
            private readonly ImmutableArray<CustomModifier> _customModifiers;

            public NonLazyType(ImmutableArray<CustomModifier> customModifiers)
            {
                Debug.Assert(!customModifiers.IsDefault);
                _customModifiers = customModifiers;
            }

            internal override bool IsResolved => true;
            internal override TypeSymbol GetResolvedType(TypeSymbol defaultType) => defaultType;
            internal override ImmutableArray<CustomModifier> CustomModifiers => _customModifiers;

            internal override SpecialType GetSpecialType(TypeSymbol typeSymbol) => typeSymbol.SpecialType;
            internal override bool IsRestrictedType(TypeSymbol typeSymbol, bool ignoreSpanLikeTypes) => typeSymbol.IsRestrictedType(ignoreSpanLikeTypes);
            internal override bool IsStatic(TypeSymbol typeSymbol) => typeSymbol.IsStatic;
            internal override bool IsVoid(TypeSymbol typeSymbol) => typeSymbol.IsVoidType();
            internal override bool IsSZArray(TypeSymbol typeSymbol) => typeSymbol.IsSZArray();

            internal override TypeSymbol GetNullableUnderlyingTypeOrSelf(TypeSymbol typeSymbol) => typeSymbol.StrippedType();

            internal override TypeWithAnnotations WithModifiers(TypeWithAnnotations type, ImmutableArray<CustomModifier> customModifiers)
            {
                return CreateNonLazyType(type.DefaultType, type.NullableAnnotation, customModifiers);
            }

            internal override TypeSymbol AsTypeSymbolOnly(TypeSymbol typeSymbol) => typeSymbol;

            internal override TypeWithAnnotations WithTypeAndModifiers(TypeWithAnnotations type, TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                return CreateNonLazyType(typeSymbol, type.NullableAnnotation, customModifiers);
            }

            internal override TypeWithAnnotations AsNullableReferenceType(TypeWithAnnotations type)
            {
                return CreateNonLazyType(type.DefaultType, NullableAnnotation.Annotated, _customModifiers);
            }

            internal override TypeWithAnnotations AsNotNullableReferenceType(TypeWithAnnotations type)
            {
                var defaultType = type.DefaultType;
                return CreateNonLazyType(defaultType, defaultType.IsNullableType() ? type.NullableAnnotation : NullableAnnotation.NotAnnotated, _customModifiers);
            }

            internal override bool TypeSymbolEquals(TypeWithAnnotations type, TypeWithAnnotations other, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt)
            {
                return type.TypeSymbolEqualsCore(other, comparison, isValueTypeOverrideOpt);
            }

            internal override TypeWithAnnotations SubstituteType(TypeWithAnnotations type, AbstractTypeMap typeMap, bool withTupleUnification)
            {
                return type.SubstituteTypeCore(typeMap, withTupleUnification);
            }

            internal override TypeWithAnnotations TransformToTupleIfCompatible(TypeWithAnnotations type)
            {
                var defaultType = type.DefaultType;
                var transformedType = TupleTypeSymbol.TransformToTupleIfCompatible(defaultType);
                if ((object)defaultType != transformedType)
                {
                    return TypeWithAnnotations.Create(transformedType, type.NullableAnnotation, _customModifiers);
                }
                return type;
            }

            internal override void ReportDiagnosticsIfObsolete(TypeWithAnnotations type, Binder binder, SyntaxNode syntax, DiagnosticBag diagnostics)
            {
                type.ReportDiagnosticsIfObsoleteCore(binder, syntax, diagnostics);
            }

            internal override void TryForceResolveAsNullableValueType()
            {
            }

            internal override void TryForceResolveAsNullableReferenceType()
            {
            }
        }

        /// <summary>
        /// Nullable type parameter. The underlying TypeSymbol is resolved
        /// lazily to avoid cycles when binding declarations.
        /// </summary>
        private sealed class LazyNullableTypeParameter : Extensions
        {
            private readonly CSharpCompilation _compilation;
            private readonly TypeWithAnnotations _underlying;
            private TypeSymbol _resolved;

            public LazyNullableTypeParameter(CSharpCompilation compilation, TypeWithAnnotations underlying)
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
                    Debug.Assert(_underlying.IsSafeToResolve());

                    if (!_underlying.Type.IsValueType)
                    {
                        TryForceResolveAsNullableReferenceType();
                    }
                    else
                    {
                        TryForceResolveAsNullableValueType();
                    }
                }

                return _resolved;
            }

            internal override TypeSymbol GetNullableUnderlyingTypeOrSelf(TypeSymbol typeSymbol) => _underlying.Type;

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

            internal override bool IsResolved => (object)_resolved != null;
            internal override TypeSymbol GetResolvedType(TypeSymbol defaultType) => GetResolvedType();
            internal override ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;

            internal override TypeWithAnnotations WithModifiers(TypeWithAnnotations type, ImmutableArray<CustomModifier> customModifiers)
            {
                if (customModifiers.IsEmpty)
                {
                    return type;
                }

                var resolvedType = GetResolvedType();
                if (resolvedType.IsNullableType())
                {
                    return TypeWithAnnotations.Create(resolvedType, type.NullableAnnotation, customModifiers: customModifiers);
                }

                return CreateNonLazyType(resolvedType, type.NullableAnnotation, customModifiers);
            }

            internal override TypeWithAnnotations WithTypeAndModifiers(TypeWithAnnotations type, TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                if (typeSymbol.IsNullableType())
                {
                    return TypeWithAnnotations.Create(typeSymbol, type.NullableAnnotation, customModifiers: customModifiers);
                }

                return CreateNonLazyType(typeSymbol, type.NullableAnnotation, customModifiers);
            }

            internal override TypeWithAnnotations AsNullableReferenceType(TypeWithAnnotations type)
            {
                return type;
            }

            internal override TypeWithAnnotations AsNotNullableReferenceType(TypeWithAnnotations type)
            {
                if (!_underlying.Type.IsValueType)
                {
                    return _underlying;
                }
                return type;
            }

            internal override TypeWithAnnotations SubstituteType(TypeWithAnnotations type, AbstractTypeMap typeMap, bool withTupleUnification)
            {
                if ((object)_resolved != null)
                {
                    return type.SubstituteTypeCore(typeMap, withTupleUnification);
                }

                var newUnderlying = _underlying.SubstituteTypeCore(typeMap, withTupleUnification);
                if (!newUnderlying.IsSameAs(this._underlying))
                {
                    if (newUnderlying.Type.Equals(this._underlying.Type, TypeCompareKind.ConsiderEverything) &&
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

            internal override TypeWithAnnotations TransformToTupleIfCompatible(TypeWithAnnotations type)
            {
                return type;
            }

            internal override void ReportDiagnosticsIfObsolete(TypeWithAnnotations type, Binder binder, SyntaxNode syntax, DiagnosticBag diagnostics)
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

            internal override bool TypeSymbolEquals(TypeWithAnnotations type, TypeWithAnnotations other, TypeCompareKind comparison, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverrideOpt)
            {
                var otherLazy = other._extensions as LazyNullableTypeParameter;

                if ((object)otherLazy != null)
                {
                    return _underlying.TypeSymbolEquals(otherLazy._underlying, comparison, isValueTypeOverrideOpt);
                }

                return type.TypeSymbolEqualsCore(other, comparison, isValueTypeOverrideOpt);
            }

            internal override void TryForceResolveAsNullableValueType()
            {
                Interlocked.CompareExchange(ref _resolved,
                    _compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(_underlying)),
                    null);
            }

            internal override void TryForceResolveAsNullableReferenceType()
            {
                Interlocked.CompareExchange(ref _resolved, _underlying.Type, null);
            }
        }
    }
}
