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
    internal readonly struct TypeSymbolWithAnnotations : IFormattable
    {
        /// <summary>
        /// A builder for lazy instances of TypeSymbolWithAnnotations.
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        internal struct Builder
        {
            private TypeSymbol _defaultType;
            private INonNullTypesContext _nonNullTypesContext;
            private bool _isAnnotated;
            private bool _treatPossiblyNullableReferenceTypeTypeParameterAsNullable;
            private Extensions _extensions;

            /// <summary>
            /// The underlying type, unless overridden by _extensions.
            /// </summary>
            internal TypeSymbol DefaultType => _defaultType;

            /// <summary>
            /// True if the fields of the builder are unset.
            /// </summary>
            internal bool IsNull => _defaultType is null;

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
                _isAnnotated = type.IsAnnotated;
                _treatPossiblyNullableReferenceTypeTypeParameterAsNullable = type._treatPossiblyNullableReferenceTypeTypeParameterAsNullable;
                Interlocked.CompareExchange(ref _nonNullTypesContext, type.NonNullTypesContext, null);
                Interlocked.CompareExchange(ref _extensions, type._extensions, null);
                return (object)Interlocked.CompareExchange(ref _defaultType, type._defaultType, null) == null;
            }

            /// <summary>
            /// Create immutable TypeSymbolWithAnnotations instance.
            /// </summary>
            internal TypeSymbolWithAnnotations ToType()
            {
                return (object)_defaultType == null ?
                    default :
                    new TypeSymbolWithAnnotations(_defaultType, _nonNullTypesContext, _isAnnotated, _treatPossiblyNullableReferenceTypeTypeParameterAsNullable, _extensions);
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

        /// <summary>
        /// Returns:
        /// false for string, int, T;
        /// true for string?, T? where T : class; and
        /// true for int?, T? where T : struct.
        /// </summary>
        public readonly bool IsAnnotated;

        /// <summary>
        /// True if IsNullable should be true when the type is an unconstrained type parameter.
        /// The field is necessary to allow representing distinct instances for a type parameter
        /// type (IsNullable=true and IsNullable=false) in flow analysis.
        /// </summary>
        private readonly bool _treatPossiblyNullableReferenceTypeTypeParameterAsNullable;

        /// <summary>
        /// [NonNullTypes] context used for determining whether unannotated types are not nullable.
        /// Allows us to get the information without eagerly pulling on the NonNullTypes property (which causes cycles).
        /// </summary>
        public readonly INonNullTypesContext NonNullTypesContext;

        private TypeSymbolWithAnnotations(TypeSymbol defaultType, INonNullTypesContext nonNullTypesContext, bool isAnnotated, bool treatPossiblyNullableReferenceTypeTypeParameterAsNullable, Extensions extensions)
        {
            Debug.Assert((object)defaultType != null);
            Debug.Assert(!defaultType.IsNullableType() || isAnnotated);
            Debug.Assert(nonNullTypesContext != null);
            Debug.Assert(extensions != null);
            _defaultType = defaultType;
            IsAnnotated = isAnnotated;
            _treatPossiblyNullableReferenceTypeTypeParameterAsNullable = treatPossiblyNullableReferenceTypeTypeParameterAsNullable;
            NonNullTypesContext = nonNullTypesContext;
            _extensions = extensions;
        }

        public override string ToString() => TypeSymbol.ToString();
        public string Name => TypeSymbol.Name;
        public SymbolKind Kind => TypeSymbol.Kind;

        // Note: We cannot pull on NonNullTypes while debugging, as that causes cycles, so we only display annotated vs. un-annotated.
        internal static readonly SymbolDisplayFormat DebuggerDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        internal static readonly SymbolDisplayFormat TestDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
            compilerInternalOptions: SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier);

        /// <summary>
        /// Create an instance setting <see cref="IsNullable"/> lazily from
        /// <see cref="NonNullTypesContext"/> and <see cref="IsAnnotated"/>.
        /// </summary>
        /// <remarks>
        /// Should be used for scenarios where calculating <see cref="IsNullable"/> eagerly
        /// may result in cycles (when binding member declarations for instance).
        /// </remarks>
        internal static TypeSymbolWithAnnotations Create(INonNullTypesContext nonNullTypesContext, TypeSymbol typeSymbol, bool isAnnotated = false, ImmutableArray<CustomModifier> customModifiers = default)
        {
            Debug.Assert(nonNullTypesContext != null);
            Debug.Assert((nonNullTypesContext as Symbol)?.IsDefinition != false);
#if DEBUG
            _ = nonNullTypesContext.NonNullTypes; // Should be able to ask this question right away.
#endif
            if (typeSymbol is null)
            {
                return default;
            }
            if (!isAnnotated && typeSymbol.IsNullableType())
            {
                isAnnotated = true;
            }
            return CreateNonLazyType(typeSymbol, nonNullTypesContext, isAnnotated: isAnnotated,
                                     treatPossiblyNullableReferenceTypeTypeParameterAsNullable: !IsIndexedTypeParameter(typeSymbol),
                                     customModifiers.NullToEmpty());
        }

        // https://github.com/dotnet/roslyn/issues/30050: Check we are not using this method on type references in
        // member signatures visible outside the assembly. Consider overriding, implementing, NoPIA embedding, etc.

        /// <summary>
        /// Create an instance setting <see cref="IsNullable"/> directly.
        /// </summary>
        /// <remarks>
        /// Can be used for scenarios where calculating <see cref="IsNullable"/> eagerly will not
        /// result in cycles (when binding executable code for instance), or scenarios where
        /// <see cref="IsNullable"/> is determined by state other than <see cref="IsAnnotated"/>
        /// (in flow analysis for instance).
        /// </remarks>
        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, bool? isNullableIfReferenceType = null, ImmutableArray<CustomModifier> customModifiers = default)
        {
            if (typeSymbol is null)
            {
                return default;
            }

            var context = isNullableIfReferenceType == null ? NonNullTypesFalseContext.Instance : NonNullTypesTrueContext.Instance;
            bool isAnnotated = isNullableIfReferenceType == true;
            bool treatPossiblyNullableReferenceTypeTypeParameterAsNullable = false;

            if (isAnnotated && !typeSymbol.IsValueType)
            {
                // string? (leave annotated)
                // T? where T : class (leave annotated)
                if (!IsIndexedTypeParameter(typeSymbol) && typeSymbol.IsPossiblyNullableReferenceTypeTypeParameter())
                {
                    // T? (leave unannotated)
                    isAnnotated = false;
                    treatPossiblyNullableReferenceTypeTypeParameterAsNullable = true;
                }
            }
            else
            {
                // string, int (leave unannotated)
                // int?, T? where T : struct (add annotation)
                // int? (error type)
                isAnnotated = typeSymbol.IsNullableType();
            }

            return CreateNonLazyType(typeSymbol, context, isAnnotated: isAnnotated,
                treatPossiblyNullableReferenceTypeTypeParameterAsNullable, customModifiers.NullToEmpty());
        }

        private static bool IsIndexedTypeParameter(TypeSymbol typeSymbol)
        {
            return typeSymbol is IndexedTypeParameterSymbol ||
                   typeSymbol is IndexedTypeParameterSymbolForOverriding;
        }

        private static TypeSymbolWithAnnotations CreateNonLazyType(TypeSymbol typeSymbol, INonNullTypesContext nonNullTypesContext,
            bool isAnnotated, bool treatPossiblyNullableReferenceTypeTypeParameterAsNullable, ImmutableArray<CustomModifier> customModifiers)
        {
            return new TypeSymbolWithAnnotations(typeSymbol, nonNullTypesContext, isAnnotated: isAnnotated,
                treatPossiblyNullableReferenceTypeTypeParameterAsNullable, Extensions.Create(customModifiers));
        }

        private static TypeSymbolWithAnnotations CreateLazyNullableType(CSharpCompilation compilation, TypeSymbolWithAnnotations underlying)
        {
            return new TypeSymbolWithAnnotations(defaultType: underlying._defaultType, nonNullTypesContext: underlying.NonNullTypesContext,
                isAnnotated: true, treatPossiblyNullableReferenceTypeTypeParameterAsNullable: false, Extensions.CreateLazy(compilation, underlying));
        }

        /// <summary>
        /// True if the fields are unset.
        /// </summary>
        internal bool IsNull => _defaultType is null;

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
                    return CreateNonLazyType(typeSymbol, NonNullTypesContext, isAnnotated: true, 
                        treatPossiblyNullableReferenceTypeTypeParameterAsNullable: false, this.CustomModifiers);
                }
                else
                {
                    return Create(compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(typeSymbol), NonNullTypesContext));
                }
            }

            return CreateLazyNullableType(compilation, this);
        }

        public TypeSymbolWithAnnotations AsNullableReferenceType() => _extensions.AsNullableReferenceType(this);
        public TypeSymbolWithAnnotations AsNotNullableReferenceType() => _extensions.AsNotNullableReferenceType(this);

        /// <summary>
        /// Merges top-level and nested nullability from an otherwise identical type.
        /// <paramref name="hadNullabilityMismatch"/> is true if there was conflict
        /// merging nullability and warning should be reported by the caller.
        /// </summary>
        internal TypeSymbolWithAnnotations MergeNullability(TypeSymbolWithAnnotations other, VarianceKind variance, out bool hadNullabilityMismatch)
        {
            bool? isNullable = MergeIsNullable(IsNullable, other.IsNullable, variance, out bool hadTopLevelMismatch);
            TypeSymbol type = TypeSymbol.MergeNullability(other.TypeSymbol, variance, out bool hadNestedMismatch);
            Debug.Assert((object)type != null);
            hadNullabilityMismatch = hadTopLevelMismatch | hadNestedMismatch;
            return Create(type, isNullable, CustomModifiers);
        }

        /// <summary>
        /// Merges nullability.
        /// <paramref name="hadNullabilityMismatch"/> is true if there was conflict.
        /// </summary>
        private static bool? MergeIsNullable(bool? a, bool? b, VarianceKind variance, out bool hadNullabilityMismatch)
        {
            hadNullabilityMismatch = false;
            if (a == b)
            {
                return a;
            }
            switch (variance)
            {
                case VarianceKind.In:
                    return (a == false || b == false) ? (bool?)false : null;
                case VarianceKind.Out:
                    return (a == true || b == true) ? (bool?)true : null;
                default:
                    if (a == null)
                    {
                        return b;
                    }
                    if (b == null)
                    {
                        return a;
                    }
                    hadNullabilityMismatch = true;
                    return null;
            }
        }

        public TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers) =>
            _extensions.WithModifiers(this, customModifiers);
        public TypeSymbolWithAnnotations WithNonNullTypesContext(INonNullTypesContext nonNullTypesContext) =>
            _extensions.WithNonNullTypesContext(this, nonNullTypesContext);

        public TypeSymbol TypeSymbol => _extensions?.GetResolvedType(_defaultType);
        public TypeSymbol NullableUnderlyingTypeOrSelf => _extensions.GetNullableUnderlyingTypeOrSelf(_defaultType);

        // https://github.com/dotnet/roslyn/issues/30051: IsNullable depends on IsValueType which
        // can lead to cycles when IsNullable is queried early. Replace this property with
        // the Annotation property that depends on IsAnnotated and NonNullTypes only.
        // Should review all the usages of IsNullable outside of NullableWalker.

        /// <summary>
        /// Returns:
        /// true if this is a nullable reference or value type;
        /// false if this is an unannotated reference type and [NonNullTypes(true)],
        /// or a value type regardless of [NonNullTypes]; and
        /// null if an unannotated reference type and [NonNullTypes(false)].
        /// If this is a nullable value type, <see cref="TypeSymbol"/>
        /// returns symbol for constructed System.Nullable`1 type.
        /// If this is a nullable reference type, <see cref="TypeSymbol"/>
        /// simply returns a symbol for the reference type.
        /// </summary>
        public bool? IsNullable
        {
            get
            {
                if (_defaultType is null)
                {
                    return null;
                }
                if (IsAnnotated)
                {
                    return true;
                }
                if (NonNullTypesContext.NonNullTypes == true)
                {
                    return _treatPossiblyNullableReferenceTypeTypeParameterAsNullable &&
                        TypeSymbol.IsPossiblyNullableReferenceTypeTypeParameter();
                }
                if (TypeSymbol.IsValueType)
                {
                    Debug.Assert(!TypeSymbol.IsNullableType());
                    return false;
                }
                return null;
            }
        }

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
            var str = TypeSymbol.ToDisplayString(format);
            if (format != null)
            {
                if (format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier) &&
                    !IsNullableType() &&
                    IsAnnotated)
                {
                    return str + "?";
                }
                else if (format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier) &&
                    !IsValueType &&
                    IsNullable == false)
                {
                    return str + "!";
                }
            }
            return str;
        }

        internal string GetDebuggerDisplay() => _defaultType is null ? "null" : ToDisplayString(DebuggerDisplayFormat);

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

            if (other.IsNull || !TypeSymbolEquals(other, comparison))
            {
                return false;
            }

            // Make sure custom modifiers are the same.
            if ((comparison & TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) == 0 &&
                !this.CustomModifiers.SequenceEqual(other.CustomModifiers))
            {
                return false;
            }

            if ((comparison & TypeCompareKind.CompareNullableModifiersForReferenceTypes) != 0)
            {
                var thisIsNullable = IsNullable;
                var otherIsNullable = other.IsNullable;
                if (otherIsNullable != thisIsNullable)
                {
                    if ((comparison & TypeCompareKind.UnknownNullableModifierMatchesAny) == 0 ||
                        (thisIsNullable != null && otherIsNullable != null))
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
                if (obj.IsNull)
                {
                    return 0;
                }
                return obj.TypeSymbol.GetHashCode();
            }

            public override bool Equals(TypeSymbolWithAnnotations x, TypeSymbolWithAnnotations y)
            {
                if (x.IsNull)
                {
                    return y.IsNull;
                }
                return x.Equals(y, TypeCompareKind.CompareNullableModifiersForReferenceTypes);
            }
        }

        internal bool TypeSymbolEquals(TypeSymbolWithAnnotations other, TypeCompareKind comparison) =>
            _extensions.TypeSymbolEquals(this, other, comparison);

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

        public TypeSymbolWithAnnotations SubstituteType(AbstractTypeMap typeMap) =>
            _extensions.SubstituteType(this, typeMap, withTupleUnification: false);
        public TypeSymbolWithAnnotations SubstituteTypeWithTupleUnification(AbstractTypeMap typeMap) =>
            _extensions.SubstituteType(this, typeMap, withTupleUnification: true);

        internal TypeSymbolWithAnnotations TransformToTupleIfCompatible() => _extensions.TransformToTupleIfCompatible(this);

        internal TypeSymbolWithAnnotations SubstituteTypeCore(AbstractTypeMap typeMap, bool withTupleUnification)
        {
            var newCustomModifiers = typeMap.SubstituteCustomModifiers(this.CustomModifiers);
            var newTypeWithModifiers = typeMap.SubstituteType(this.TypeSymbol, withTupleUnification);
            bool newIsAnnotated = this.IsAnnotated || newTypeWithModifiers.IsAnnotated;

            // https://github.com/dotnet/roslyn/issues/30052: Can we use Equals instead?
            if (this.TypeSymbolEquals(newTypeWithModifiers, TypeCompareKind.CompareNullableModifiersForReferenceTypes) &&
                newTypeWithModifiers.CustomModifiers.IsEmpty &&
                newIsAnnotated == this.IsAnnotated &&
                newCustomModifiers == this.CustomModifiers)
            {
                // https://github.com/dotnet/roslyn/issues/30052: We're dropping newTypeWithModifiers.NonNullTypes!
                return this; // substitution had no effect on the type or modifiers
            }

            bool newIsNullableType = newTypeWithModifiers.TypeSymbol.IsNullableType();
            if (newIsNullableType)
            {
                if (newCustomModifiers.IsEmpty)
                {
                    return newTypeWithModifiers;
                }
                newIsAnnotated = newTypeWithModifiers.IsAnnotated;
                Debug.Assert(newIsAnnotated);
            }
            else if (newCustomModifiers.IsEmpty && newTypeWithModifiers.IsAnnotated == newIsAnnotated)
            {
                return newTypeWithModifiers;
            }

            return CreateNonLazyType(
                newTypeWithModifiers.TypeSymbol,
                newTypeWithModifiers.NonNullTypesContext,
                isAnnotated: newIsAnnotated,
                newTypeWithModifiers._treatPossiblyNullableReferenceTypeTypeParameterAsNullable,
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
        public bool Is(TypeParameterSymbol other) => _extensions.Is(_defaultType, other);

        public TypeSymbolWithAnnotations WithTypeAndModifiers(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers) =>
            _extensions.WithTypeAndModifiers(this, typeSymbol, customModifiers);

        public bool ContainsNullableReferenceTypes()
        {
            return ContainsNullableReferenceTypes(this, typeOpt: null);
        }

        public static bool ContainsNullableReferenceTypes(
            TypeSymbolWithAnnotations typeWithAnnotationsOpt,
            TypeSymbol typeOpt)
        {
            var type = TypeSymbolExtensions.VisitType(
                typeWithAnnotationsOpt,
                typeOpt,
                typeWithAnnotationsPredicateOpt: (t, a, b) => t.IsAnnotated && !t.TypeSymbol.IsErrorType() && !t.TypeSymbol.IsValueType,
                typePredicateOpt: null,
                arg: (object)null);
            return (object)type != null;
        }

        /// <summary>
        /// Returns true if the type contains an annotated unconstrained type parameter.
        /// </summary>
        public bool ContainsAnnotatedUnconstrainedTypeParameter()
        {
            return ContainsAnnotatedUnconstrainedTypeParameter(this, typeOpt: null);
        }

        public static bool ContainsAnnotatedUnconstrainedTypeParameter(
            TypeSymbolWithAnnotations typeWithAnnotationsOpt,
            TypeSymbol typeOpt)
        {
            var typeParameter = TypeSymbolExtensions.VisitType(
                typeWithAnnotationsOpt,
                typeOpt,
                typeWithAnnotationsPredicateOpt: (t, a, b) => t.IsAnnotated && t.TypeSymbol.IsUnconstrainedTypeParameter(),
                typePredicateOpt: null,
                arg: (object)null);
            return (object)typeParameter != null;
        }

        public void AddNullableTransforms(ArrayBuilder<bool> transforms)
        {
            var typeSymbol = TypeSymbol;
            transforms.Add(IsAnnotated && !typeSymbol.IsValueType);
            typeSymbol.AddNullableTransforms(transforms);
        }

        public bool ApplyNullableTransforms(ImmutableArray<bool> transforms, INonNullTypesContext nonNullTypesContext, ref int position, out TypeSymbolWithAnnotations result)
        {
            Debug.Assert(nonNullTypesContext != null);

            result = this;

            bool isAnnotated;
            if (transforms.IsDefault)
            {
                // No explicit transforms. All reference types are unannotated.
                isAnnotated = false;
            }
            else if (position < transforms.Length)
            {
                isAnnotated = transforms[position++];
            }
            else
            {
                return false;
            }

            TypeSymbol oldTypeSymbol = TypeSymbol;
            TypeSymbol newTypeSymbol;

            if (!oldTypeSymbol.ApplyNullableTransforms(transforms, nonNullTypesContext, ref position, out newTypeSymbol))
            {
                return false;
            }

            if ((object)oldTypeSymbol != newTypeSymbol)
            {
                result = result.WithTypeAndModifiers(newTypeSymbol, result.CustomModifiers);
            }

            result = isAnnotated ?
                result.AsNullableReferenceType() :
                result.AsNotNullableReferenceType();

            result = result.WithNonNullTypesContext(nonNullTypesContext);
            return true;
        }

        public TypeSymbolWithAnnotations WithTopLevelNonNullabilityForReferenceTypes()
        {
            var typeSymbol = TypeSymbol;
            if (IsNullable == false || typeSymbol.IsValueType)
            {
                return this;
            }

            return CreateNonLazyType(typeSymbol, NonNullTypesTrueContext.Instance, isAnnotated: false, 
                treatPossiblyNullableReferenceTypeTypeParameterAsNullable: false, CustomModifiers);
        }

        public TypeSymbolWithAnnotations SetUnknownNullabilityForReferenceTypes()
        {
            var typeSymbol = TypeSymbol;

            if (IsNullable.HasValue)
            {
                if (!typeSymbol.IsValueType)
                {
                    typeSymbol = typeSymbol.SetUnknownNullabilityForReferenceTypes();

                    return CreateNonLazyType(typeSymbol, NonNullTypesFalseContext.Instance, isAnnotated: false, 
                        treatPossiblyNullableReferenceTypeTypeParameterAsNullable: false, CustomModifiers);
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
            if (IsNull)
            {
                return 0;
            }
            return Hash.Combine(TypeSymbol.GetHashCode(), IsAnnotated.GetHashCode());
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
                ReferenceEquals(NonNullTypesContext, other.NonNullTypesContext) &&
                IsAnnotated == other.IsAnnotated &&
                ReferenceEquals(_extensions, other._extensions);
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
            internal abstract TypeSymbolWithAnnotations WithNonNullTypesContext(TypeSymbolWithAnnotations type, INonNullTypesContext nonNullTypesContext);

            internal abstract TypeSymbol GetNullableUnderlyingTypeOrSelf(TypeSymbol typeSymbol);

            internal abstract bool GetIsReferenceType(TypeSymbol typeSymbol, ConsList<TypeParameterSymbol> inProgress);
            internal abstract bool GetIsValueType(TypeSymbol typeSymbol, ConsList<TypeParameterSymbol> inProgress);

            internal abstract TypeSymbol AsTypeSymbolOnly(TypeSymbol typeSymbol);

            internal abstract SpecialType GetSpecialType(TypeSymbol typeSymbol);
            internal abstract bool IsRestrictedType(TypeSymbol typeSymbol, bool ignoreSpanLikeTypes);
            internal abstract bool IsStatic(TypeSymbol typeSymbol);
            internal abstract bool IsVoid(TypeSymbol typeSymbol);
            internal abstract bool IsSZArray(TypeSymbol typeSymbol);

            internal abstract bool Is(TypeSymbol typeSymbol, TypeParameterSymbol other);

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
                return CreateNonLazyType(type._defaultType, type.NonNullTypesContext, type.IsAnnotated,
                    type._treatPossiblyNullableReferenceTypeTypeParameterAsNullable, customModifiers);
            }

            internal override TypeSymbolWithAnnotations WithNonNullTypesContext(TypeSymbolWithAnnotations type, INonNullTypesContext nonNullTypesContext)
            {
                Debug.Assert(nonNullTypesContext != null);
                return CreateNonLazyType(type._defaultType, nonNullTypesContext, type.IsAnnotated,
                    type._treatPossiblyNullableReferenceTypeTypeParameterAsNullable, _customModifiers);
            }

            internal override TypeSymbol AsTypeSymbolOnly(TypeSymbol typeSymbol) => typeSymbol;

            // https://github.com/dotnet/roslyn/issues/30054: Use WithCustomModifiers.Is() => false
            // and set IsNullable=null always for GetTypeParametersAsTypeArguments.
            internal override bool Is(TypeSymbol typeSymbol, TypeParameterSymbol other) =>
                typeSymbol.Equals(other, TypeCompareKind.CompareNullableModifiersForReferenceTypes) && _customModifiers.IsEmpty;

            internal override TypeSymbolWithAnnotations WithTypeAndModifiers(TypeSymbolWithAnnotations type, TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                return CreateNonLazyType(typeSymbol, type.NonNullTypesContext, isAnnotated: type.IsAnnotated,
                    type._treatPossiblyNullableReferenceTypeTypeParameterAsNullable, customModifiers);
            }

            internal override TypeSymbolWithAnnotations AsNullableReferenceType(TypeSymbolWithAnnotations type)
            {
                return CreateNonLazyType(type._defaultType, type.NonNullTypesContext, isAnnotated: true,
                    type._treatPossiblyNullableReferenceTypeTypeParameterAsNullable, _customModifiers);
            }

            internal override TypeSymbolWithAnnotations AsNotNullableReferenceType(TypeSymbolWithAnnotations type)
            {
                var defaultType = type._defaultType;
                return CreateNonLazyType(defaultType, type.NonNullTypesContext, isAnnotated: defaultType.IsNullableType(),
                    treatPossiblyNullableReferenceTypeTypeParameterAsNullable: false, _customModifiers);
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
                    return TypeSymbolWithAnnotations.Create(transformedType, type.IsNullable, _customModifiers);
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
                Debug.Assert(!underlying.IsAnnotated);
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

            // https://github.com/dotnet/roslyn/issues/30054: This implementation looks
            // incorrect since a type parameter cannot be Nullable<T>.
            internal override bool Is(TypeSymbol typeSymbol, TypeParameterSymbol other)
            {
                if (!other.IsNullableType())
                {
                    return false;
                }

                var resolvedType = GetResolvedType();
                return resolvedType.Equals(other, TypeCompareKind.CompareNullableModifiersForReferenceTypes);
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
                    return TypeSymbolWithAnnotations.Create(resolvedType, customModifiers: customModifiers);
                }

                return CreateNonLazyType(resolvedType, type.NonNullTypesContext, isAnnotated: true,
                    treatPossiblyNullableReferenceTypeTypeParameterAsNullable: false, customModifiers);
            }

            internal override TypeSymbolWithAnnotations WithNonNullTypesContext(TypeSymbolWithAnnotations type, INonNullTypesContext nonNullTypesContext)
            {
                return CreateLazyNullableType(_compilation, _underlying.WithNonNullTypesContext(nonNullTypesContext));
            }

            internal override TypeSymbolWithAnnotations WithTypeAndModifiers(TypeSymbolWithAnnotations type, TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                if (typeSymbol.IsNullableType())
                {
                    return TypeSymbolWithAnnotations.Create(typeSymbol, customModifiers: customModifiers);
                }

                return CreateNonLazyType(typeSymbol, type.NonNullTypesContext, isAnnotated: true,
                    type._treatPossiblyNullableReferenceTypeTypeParameterAsNullable, customModifiers);
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
                    if ((newUnderlying.TypeSymbol.Equals(this._underlying.TypeSymbol, TypeCompareKind.CompareNullableModifiersForReferenceTypes) ||
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
