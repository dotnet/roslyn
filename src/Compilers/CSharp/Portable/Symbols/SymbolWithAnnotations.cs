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
                    new TypeSymbolWithAnnotations(_defaultType, _nonNullTypesContext, _isAnnotated, _extensions);
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
        /// [NonNullTypes] context used for determining whether unannotated types are not nullable.
        /// Allows us to get the information without eagerly pulling on the NonNullTypes property (which causes cycles).
        /// </summary>
        public readonly INonNullTypesContext NonNullTypesContext;

        private TypeSymbolWithAnnotations(TypeSymbol defaultType, INonNullTypesContext nonNullTypesContext, bool isAnnotated, Extensions extensions)
        {
            Debug.Assert((object)defaultType != null);
            Debug.Assert(!defaultType.IsNullableType() || isAnnotated);
            Debug.Assert(nonNullTypesContext != null);
            Debug.Assert(extensions != null);
            _defaultType = defaultType;
            IsAnnotated = isAnnotated;
            NonNullTypesContext = nonNullTypesContext;
            _extensions = extensions;
        }

        public override string ToString() => TypeSymbol.ToString();
        public string Name => TypeSymbol.Name;
        public SymbolKind Kind => TypeSymbol.Kind;

        // PROTOTYPE(NullableReferenceTypes): GetDebuggerDisplay pulls on NonNullTypes while debugging
        internal static readonly SymbolDisplayFormat DebuggerDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            /*compilerInternalOptions: SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier*/);

        internal static readonly SymbolDisplayFormat TestDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
            compilerInternalOptions: SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier);

        // PROTOTYPE(NullableReferenceTypes): consider removing this method and using Create below (which handles nullable value types).
        internal static TypeSymbolWithAnnotations CreateUnannotated(INonNullTypesContext nonNullTypesContext, TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers = default)
        {
            return Create(typeSymbol, nonNullTypesContext, isAnnotated: false, customModifiers.NullToEmpty());
        }

        internal static TypeSymbolWithAnnotations Create(INonNullTypesContext nonNullTypesContext, TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers = default)
        {
            bool isNullableType = typeSymbol.IsNullableType();

            // PROTOTYPE(NullableReferenceTypes): this defaulting logic should be removed. There are many paths that currently don't have an explicit context at the moment.
            nonNullTypesContext = nonNullTypesContext ?? NonNullTypesFalseContext.Instance;
            return Create(typeSymbol, nonNullTypesContext, isAnnotated: isNullableType, customModifiers.NullToEmpty());
        }

        // PROTOTYPE(NullableReferenceTypes): Check we are not using this method on type references in
        // member signatures visible outside the assembly. Consider overriding, implementing, NoPIA embedding, etc.
        // PROTOTYPE(NullableReferenceTypes): [Obsolete("Use explicit NonNullTypes context")]
        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol)
        {
            return Create(typeSymbol, ImmutableArray<CustomModifier>.Empty);
        }

        // PROTOTYPE(NullableReferenceTypes): Check we are not using this method on type references in
        // member signatures visible outside the assembly. Consider overriding, implementing, NoPIA embedding, etc.
        // PROTOTYPE(NullableReferenceTypes): [Obsolete("Use explicit NonNullTypes context")]
        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
        {
            if (typeSymbol is null)
            {
                return default;
            }

            return CreateNonLazyType(typeSymbol, NonNullTypesTrueContext.Instance, isAnnotated: typeSymbol.IsNullableType(), customModifiers);
        }

        internal static TypeSymbolWithAnnotations CreateNonLazyType(TypeSymbol typeSymbol, INonNullTypesContext nonNullTypesContext, bool isAnnotated, ImmutableArray<CustomModifier> customModifiers)
        {
            return new TypeSymbolWithAnnotations(typeSymbol, nonNullTypesContext, isAnnotated, Extensions.Create(customModifiers));
        }

        internal static TypeSymbolWithAnnotations CreateLazyNullableType(CSharpCompilation compilation, TypeSymbolWithAnnotations underlying)
        {
            return new TypeSymbolWithAnnotations(defaultType: underlying._defaultType, nonNullTypesContext: underlying.NonNullTypesContext, isAnnotated: true, Extensions.CreateLazy(compilation, underlying));
        }

        // PROTOTYPE(NullableReferenceTypes): [Obsolete("Use explicit NonNullTypes context")]
        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, bool? isNullableIfReferenceType)
        {
            return Create(typeSymbol, isNullableIfReferenceType, ImmutableArray<CustomModifier>.Empty);
        }

        // PROTOTYPE(NullableReferenceTypes): Check we are not using this method on type references in
        // member signatures visible outside the assembly. Consider overriding, implementing, NoPIA embedding, etc.
        // PROTOTYPE(NullableReferenceTypes): [Obsolete("Use explicit NonNullTypes context")]
        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, bool? isNullableIfReferenceType, ImmutableArray<CustomModifier> customModifiers)
        {
            // PROTOTYPE(NullableReferenceTypes): Should be using fine-grained NonNullTypesContext
            var context = isNullableIfReferenceType == null ? NonNullTypesFalseContext.Instance : NonNullTypesTrueContext.Instance;
            return Create(typeSymbol, context, isAnnotated: isNullableIfReferenceType == true, customModifiers);
        }

        // PROTOTYPE(NullableReferenceTypes): Check we are not using this method on type references in
        // member signatures visible outside the assembly. Consider overriding, implementing, NoPIA embedding, etc.
        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, INonNullTypesContext nonNullTypesContext, bool isAnnotated, ImmutableArray<CustomModifier> customModifiers)
        {
            Debug.Assert(nonNullTypesContext != null);

            if (typeSymbol is null)
            {
                return default;
            }

            // PROTOTYPE(NullableReferenceTypes): See if the if/else can be simplified to:
            //    if (typeSymbol.IsNullableType()) isAnnotated = true;
            // Currently, that results in test failures for nullable values of unconstrained
            // type parameters in UnconstrainedTypeParameter_Return_03.

            if ((!isAnnotated && typeSymbol is TypeParameterSymbol) ||
                (isAnnotated && typeSymbol.IsReferenceType && !typeSymbol.IsNullableType()))
            {
                // T (leave unannotated)
                // string? (leave annotated)
                Debug.Assert(!typeSymbol.IsNullableType());
            }
            else
            {
                // T? where T : class (leave annotated)
                // string, int (leave unannotated)
                // int?, T? where T : struct (add annotation)
                // int? (error type)
                isAnnotated = typeSymbol.IsNullableType();
            }

            return CreateNonLazyType(typeSymbol, nonNullTypesContext, isAnnotated: isAnnotated, customModifiers);
        }

        /// <summary>
        /// True if the fields are unset.
        /// </summary>
        internal bool IsNull => _defaultType is null;

        public TypeSymbolWithAnnotations SetIsAnnotated(CSharpCompilation compilation)
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
                    return CreateNonLazyType(typeSymbol, NonNullTypesContext, isAnnotated: true, this.CustomModifiers);
                }
                else
                {
                    return Create(compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(typeSymbol)));
                }
            }

            return CreateLazyNullableType(compilation, this);
        }

        // PROTOTYPE(NullableReferenceTypes): Consider replacing AsNullableReferenceType()
        // and AsNotNullableReferenceType() with a single WithIsAnnotated(bool).

        /// <summary>
        /// Adjust types in signatures coming from metadata.
        /// </summary>
        public TypeSymbolWithAnnotations AsNullableReferenceType() => _extensions.AsNullableReferenceType(this);
        public TypeSymbolWithAnnotations AsNotNullableReferenceType() => _extensions.AsNotNullableReferenceType(this);

        public TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers) =>
            _extensions.WithModifiers(this, customModifiers);
        public TypeSymbolWithAnnotations WithNonNullTypesContext(INonNullTypesContext nonNullTypesContext) =>
            _extensions.WithNonNullTypesContext(this, nonNullTypesContext);

        public TypeSymbol TypeSymbol => _extensions?.GetResolvedType(_defaultType);
        public TypeSymbol NullableUnderlyingTypeOrSelf => _extensions.GetNullableUnderlyingTypeOrSelf(_defaultType);

        // PROTOTYPE(NullableReferenceTypes): IsNullable depends on IsValueType which
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
                    return false;
                }
                if (TypeSymbol.IsValueType)
                {
                    return false;
                }
                return null;
            }
        }

        /// <summary>
        /// Returns:
        /// true if annotated;
        /// false if unannotated and [NonNullTypes(true)] and
        /// null if unannotated and [NonNullTypes(false)].
        /// </summary>
        /// <remarks>
        /// This property considers IsAnnotated and NonNullTypes only. Compare with
        /// IsNullable that also considers IsValueType. (Specifically, IsNullable==false
        /// for an unannotated value type, regardless of [NonNullTypes].)
        /// </remarks>
        internal bool? IsAnnotatedWithNonNullTypesContext
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
                // A null NonNullTypes (ie. no attribute) means the same as NonNullTypes(false).
                if (NonNullTypesContext.NonNullTypes == true)
                {
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

        internal string GetDebuggerDisplay() => _defaultType is null ? "null" : ToDisplayString(DebuggerDisplayFormat);

        // PROTOTYPE(NullableReferenceTypes): Remove IFormattable implementation
        // if instances should not be used as Diagnostic arguments.
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
                var thisIsAnnotated = IsAnnotatedWithNonNullTypesContext;
                var otherIsAnnotated = other.IsAnnotatedWithNonNullTypesContext;
                if (otherIsAnnotated != thisIsAnnotated)
                {
                    if ((comparison & TypeCompareKind.UnknownNullableModifierMatchesAny) == 0 ||
                        (thisIsAnnotated != null && otherIsAnnotated != null))
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

        internal TypeSymbolWithAnnotations SubstituteTypeCore(AbstractTypeMap typeMap, bool withTupleUnification)
        {
            var newCustomModifiers = typeMap.SubstituteCustomModifiers(this.CustomModifiers);
            var newTypeWithModifiers = typeMap.SubstituteType(this.TypeSymbol, withTupleUnification);
            bool newIsAnnotated = this.IsAnnotated || newTypeWithModifiers.IsAnnotated;

            // PROTOTYPE(NullableReferenceTypes): Can we use Equals instead?
            if (this.TypeSymbolEquals(newTypeWithModifiers, TypeCompareKind.CompareNullableModifiersForReferenceTypes) &&
                newTypeWithModifiers.CustomModifiers.IsEmpty &&
                newIsAnnotated == this.IsAnnotated &&
                newCustomModifiers == this.CustomModifiers)
            {
                // PROTOTYPE(NullableReferenceTypes): We're dropping newTypeWithModifiers.NonNullTypes!
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
            }
            else if (newCustomModifiers.IsEmpty && newTypeWithModifiers.IsAnnotated == newIsAnnotated)
            {
                return newTypeWithModifiers;
            }
            return TypeSymbolWithAnnotations.CreateNonLazyType(
                newTypeWithModifiers.TypeSymbol,
                newTypeWithModifiers.NonNullTypesContext,
                isAnnotated: newIsAnnotated,
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
                typeWithAnnotationsPredicateOpt: (t, a, b) => t.IsAnnotated && !t.TypeSymbol.IsValueType,
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

        public void ReportAnnotatedUnconstrainedTypeParameterIfAny(Location location, DiagnosticBag diagnostics)
        {
            if (ContainsAnnotatedUnconstrainedTypeParameter())
            {
                ReportAnnotatedUnconstrainedTypeParameter(location, diagnostics);
            }
        }

        public static void ReportAnnotatedUnconstrainedTypeParameter(Location location, DiagnosticBag diagnostics)
        {
            diagnostics.Add(ErrorCode.ERR_NullableUnconstrainedTypeParameter, location ?? NoLocation.Singleton);
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

            return CreateNonLazyType(typeSymbol, NonNullTypesTrueContext.Instance, isAnnotated: false, CustomModifiers);
        }

        public TypeSymbolWithAnnotations SetUnknownNullabilityForReferenceTypes()
        {
            var typeSymbol = TypeSymbol;

            if (IsNullable.HasValue)
            {
                if (!typeSymbol.IsValueType)
                {
                    typeSymbol = typeSymbol.SetUnknownNullabilityForReferenceTypes();
                    return CreateNonLazyType(typeSymbol, NonNullTypesFalseContext.Instance, isAnnotated: false, CustomModifiers);
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
                return TypeSymbolWithAnnotations.CreateNonLazyType(type._defaultType, type.NonNullTypesContext, type.IsAnnotated, customModifiers);
            }

            internal override TypeSymbolWithAnnotations WithNonNullTypesContext(TypeSymbolWithAnnotations type, INonNullTypesContext nonNullTypesContext)
            {
                Debug.Assert(nonNullTypesContext != null);
                return TypeSymbolWithAnnotations.CreateNonLazyType(type._defaultType, nonNullTypesContext, type.IsAnnotated, _customModifiers);
            }

            internal override TypeSymbol AsTypeSymbolOnly(TypeSymbol typeSymbol) => typeSymbol;

            // PROTOTYPE(NullableReferenceTypes): Use WithCustomModifiers.Is() => false
            // and set IsNullable=null always for GetTypeParametersAsTypeArguments.
            internal override bool Is(TypeSymbol typeSymbol, TypeParameterSymbol other) =>
                typeSymbol.Equals(other, TypeCompareKind.CompareNullableModifiersForReferenceTypes) && _customModifiers.IsEmpty;

            internal override TypeSymbolWithAnnotations WithTypeAndModifiers(TypeSymbolWithAnnotations type, TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                return TypeSymbolWithAnnotations.CreateNonLazyType(typeSymbol, type.NonNullTypesContext, type.IsAnnotated, customModifiers);
            }

            internal override TypeSymbolWithAnnotations AsNullableReferenceType(TypeSymbolWithAnnotations type)
            {
                return TypeSymbolWithAnnotations.CreateNonLazyType(type._defaultType, type.NonNullTypesContext, isAnnotated: true, _customModifiers);
            }

            internal override TypeSymbolWithAnnotations AsNotNullableReferenceType(TypeSymbolWithAnnotations type)
            {
                var defaultType = type._defaultType;
                return TypeSymbolWithAnnotations.CreateNonLazyType(defaultType, type.NonNullTypesContext, isAnnotated: defaultType.IsNullableType(), _customModifiers);
            }

            internal override bool TypeSymbolEquals(TypeSymbolWithAnnotations type, TypeSymbolWithAnnotations other, TypeCompareKind comparison)
            {
                return type.TypeSymbolEqualsCore(other, comparison);
            }

            internal override TypeSymbolWithAnnotations SubstituteType(TypeSymbolWithAnnotations type, AbstractTypeMap typeMap, bool withTupleUnification)
            {
                return type.SubstituteTypeCore(typeMap, withTupleUnification);
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
                Debug.Assert(compilation.IsFeatureEnabled(MessageID.IDS_FeatureStaticNullChecking));
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

            // PROTOTYPE(NullableReferenceTypes): This implementation looks
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
                    return TypeSymbolWithAnnotations.Create(resolvedType, customModifiers);
                }

                return TypeSymbolWithAnnotations.CreateNonLazyType(resolvedType, type.NonNullTypesContext, isAnnotated: true, customModifiers);
            }

            internal override TypeSymbolWithAnnotations WithNonNullTypesContext(TypeSymbolWithAnnotations type, INonNullTypesContext nonNullTypesContext)
            {
                return TypeSymbolWithAnnotations.CreateLazyNullableType(_compilation, _underlying.WithNonNullTypesContext(nonNullTypesContext));
            }

            internal override TypeSymbolWithAnnotations WithTypeAndModifiers(TypeSymbolWithAnnotations type, TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                if (typeSymbol.IsNullableType())
                {
                    return TypeSymbolWithAnnotations.Create(typeSymbol, customModifiers);
                }

                return TypeSymbolWithAnnotations.CreateNonLazyType(typeSymbol, type.NonNullTypesContext, isAnnotated: true, customModifiers);
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
                    if ((newUnderlying.TypeSymbol.Equals(this._underlying.TypeSymbol, TypeCompareKind.AllAspects) ||
                            newUnderlying.TypeSymbol is IndexedTypeParameterSymbolForOverriding) &&
                        newUnderlying.CustomModifiers.IsEmpty)
                    {
                        return TypeSymbolWithAnnotations.CreateLazyNullableType(_compilation, newUnderlying);
                    }

                    return type.SubstituteTypeCore(typeMap, withTupleUnification);
                }
                else
                {
                    return type; // substitution had no effect on the type or modifiers
                }
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
