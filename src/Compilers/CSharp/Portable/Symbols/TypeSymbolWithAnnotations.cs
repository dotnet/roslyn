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
    internal enum NullableAnnotation : byte
    {
        Unknown,     // No information. Think oblivious.
        NotNullable, // For string, int, T
        Nullable,    // For string?, T? where T : class; and for int?, T? where T : struct.
        NotNullableBasedOnAnalysis, // Explicitly set by flow analysis
        NullableBasedOnAnalysis, // Explicitly set by flow analysis
    }

    internal static class NullableAnnotationExtensions
    {
        public static bool IsAnyNullable(this NullableAnnotation annotation)
        {
            return annotation == NullableAnnotation.Nullable || annotation == NullableAnnotation.NullableBasedOnAnalysis;
        }

        public static bool IsAnyNotNullable(this NullableAnnotation annotation)
        {
            return annotation == NullableAnnotation.NotNullable || annotation == NullableAnnotation.NotNullableBasedOnAnalysis;
        }
    }

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
            private NullableAnnotation _nullableAnnotation;
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
                _nullableAnnotation = type.NullableAnnotation;
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
            Debug.Assert((object)defaultType != null);
            Debug.Assert(!defaultType.IsNullableType() || nullableAnnotation.IsAnyNullable());
            Debug.Assert(extensions != null);
            _defaultType = defaultType;

            NullableAnnotation = nullableAnnotation;
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

            return Create(typeSymbol, nullableAnnotation: isAnnotated ? NullableAnnotation.Nullable : nonNullTypesContext.NonNullTypes == true ? NullableAnnotation.NotNullable : NullableAnnotation.Unknown,
                          customModifiers.NullToEmpty());
        }

        internal static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, NullableAnnotation nullableAnnotation = NullableAnnotation.Unknown, ImmutableArray<CustomModifier> customModifiers = default)
        {
            if (typeSymbol is null)
            {
                return default;
            }

            if (!nullableAnnotation.IsAnyNullable() && typeSymbol.IsNullableType())
            {
                // int?, T? where T : struct (add annotation)
                nullableAnnotation = NullableAnnotation.Nullable;
            }

            return CreateNonLazyType(typeSymbol, nullableAnnotation, customModifiers.NullToEmpty());
        }

        internal bool IsPossiblyNullableReferenceTypeTypeParameter()
        {
            return NullableAnnotation == NullableAnnotation.NotNullable && TypeSymbol.IsPossiblyNullableReferenceTypeTypeParameter();
        }

        internal NullableAnnotation GetValueNullableAnnotation()
        {
            if (IsPossiblyNullableReferenceTypeTypeParameter())
            {
                return NullableAnnotation.NullableBasedOnAnalysis;
            }

            return NullableAnnotation;
        }

        internal bool? ValueCanBeNull()
        {
            switch (NullableAnnotation)
            {
                case NullableAnnotation.Unknown:
                    return null;

                case NullableAnnotation.Nullable:
                case NullableAnnotation.NullableBasedOnAnalysis:
                    return true;

                case NullableAnnotation.NotNullableBasedOnAnalysis:
                    return false;

                case NullableAnnotation.NotNullable:
                    return TypeSymbol.IsPossiblyNullableReferenceTypeTypeParameter();

                default:
                    throw ExceptionUtilities.UnexpectedValue(NullableAnnotation);
            }
        }

        // https://github.com/dotnet/roslyn/issues/30050: Check we are not using this method on type references in
        // member signatures visible outside the assembly. Consider overriding, implementing, NoPIA embedding, etc.

        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, bool? isNullableIfReferenceType, ImmutableArray<CustomModifier> customModifiers = default, bool fromDeclaration = false)
        {
            if (typeSymbol is null)
            {
                return default;
            }

            NullableAnnotation nullableAnnotation;

            if (typeSymbol.IsValueType)
            {
                nullableAnnotation = typeSymbol.IsNullableType() ?
                    NullableAnnotation.Nullable :
                    isNullableIfReferenceType == null ? NullableAnnotation.Unknown : NullableAnnotation.NotNullable;
            }
            else
            {
                switch (isNullableIfReferenceType)
                {
                    case true:
                        nullableAnnotation = fromDeclaration ? NullableAnnotation.Nullable : NullableAnnotation.NullableBasedOnAnalysis;
                        break;
                    case false:
                        nullableAnnotation = fromDeclaration ? NullableAnnotation.NotNullable : NullableAnnotation.NotNullableBasedOnAnalysis;
                        break;
                    default:
                        nullableAnnotation = NullableAnnotation.Unknown;
                        break;
                }
            }

            return Create(typeSymbol, nullableAnnotation, customModifiers.NullToEmpty());
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
            return new TypeSymbolWithAnnotations(defaultType: underlying._defaultType, nullableAnnotation: NullableAnnotation.Nullable, Extensions.CreateLazy(compilation, underlying));
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
                    return CreateNonLazyType(typeSymbol, NullableAnnotation.Nullable, this.CustomModifiers);
                }
                else
                {
                    return Create(compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(typeSymbol)));
                }
            }

            return CreateLazyNullableType(compilation, this);
        }

        public TypeSymbolWithAnnotations AsNullableReferenceType(bool fromDeclaration) => _extensions.AsNullableReferenceType(this, fromDeclaration);
        public TypeSymbolWithAnnotations AsNotNullableReferenceType() => _extensions.AsNotNullableReferenceType(this);

        /// <summary>
        /// Merges top-level and nested nullability from an otherwise identical type.
        /// <paramref name="hadNullabilityMismatch"/> is true if there was conflict
        /// merging nullability and warning should be reported by the caller.
        /// </summary>
        internal TypeSymbolWithAnnotations MergeNullability(TypeSymbolWithAnnotations other, VarianceKind variance, out bool hadNullabilityMismatch)
        {
            TypeSymbol typeSymbol = other.TypeSymbol;
            NullableAnnotation nullableAnnotation = MergeNullableAnnotation(typeSymbol, NullableAnnotation, other.NullableAnnotation, variance, out bool hadTopLevelMismatch);
            TypeSymbol type = TypeSymbol.MergeNullability(typeSymbol, variance, out bool hadNestedMismatch);
            Debug.Assert((object)type != null);
            hadNullabilityMismatch = hadTopLevelMismatch | hadNestedMismatch;
            return Create(type, nullableAnnotation, CustomModifiers);
        }

        /// <summary>
        /// Merges nullability.
        /// <paramref name="hadNullabilityMismatch"/> is true if there was conflict.
        /// </summary>
        private static NullableAnnotation MergeNullableAnnotation(TypeSymbol type, NullableAnnotation a, NullableAnnotation b, VarianceKind variance, out bool hadNullabilityMismatch)
        {
            hadNullabilityMismatch = false;
            if (a == b)
            {
                return a;
            }

            if (a.IsAnyNullable() && b.IsAnyNullable())
            {
                return NullableAnnotation.Nullable;
            }

            switch (variance)
            {
                case VarianceKind.In:

                    // If nullability on both sides matches - result is that nullability (trivial cases like these are handled before the switch)
                    // If either candidate is not nullable - result is not nullable
                    // Otherwise - result is "oblivious". 

                    if (a == NullableAnnotation.NotNullableBasedOnAnalysis || b == NullableAnnotation.NotNullableBasedOnAnalysis)
                    {
                        return NullableAnnotation.NotNullableBasedOnAnalysis;
                    }

                    if (a == NullableAnnotation.NotNullable || b == NullableAnnotation.NotNullable)
                    {
                        return NullableAnnotation.NotNullable;
                    }

                    Debug.Assert(a == NullableAnnotation.Unknown || b == NullableAnnotation.Unknown);
                    return NullableAnnotation.Unknown;

                case VarianceKind.Out:

                    // If nullability on both sides matches - result is that nullability (trivial cases like these are handled before the switch)
                    // If either candidate is nullable - result is nullable
                    // Otherwise - result is "oblivious". 

                    if (a.IsAnyNullable())
                    {
                        Debug.Assert(!b.IsAnyNullable());
                        return a;
                    }

                    if (b.IsAnyNullable())
                    {
                        return b;
                    }

                    if (a == NullableAnnotation.Unknown || b == NullableAnnotation.Unknown)
                    {
                        return NullableAnnotation.Unknown;
                    }

                    Debug.Assert((a == NullableAnnotation.NotNullable && b == NullableAnnotation.NotNullableBasedOnAnalysis) ||
                        (b == NullableAnnotation.NotNullable && a == NullableAnnotation.NotNullableBasedOnAnalysis));
                    return NullableAnnotation.NotNullable; // It is reasonable to settle on this value because the difference in annotations is either
                                                           // not significant for the type, or candidate corresponding to this value is possibly a 
                                                           // nullable reference type type parameter and nullable should win. 

                default:

                    // If nullability on both sides matches - result is that nullability (trivial cases like these are handled before the switch)
                    // If either candidate is "oblivious" - result is the nullability of the other candidate
                    // Otherwise - we declare a mismatch and result is not nullable. 

                    if (a == NullableAnnotation.Unknown)
                    {
                        return b;
                    }
                    if (b == NullableAnnotation.Unknown)
                    {
                        return a;
                    }

                    // At this point we know that either nullability of both sides is significantly different NotNullable vs. Nullable,
                    // or we are dealing with different flavors of not nullable for both candidates
                    if ((a == NullableAnnotation.NotNullable && b == NullableAnnotation.NotNullableBasedOnAnalysis) ||
                        (b == NullableAnnotation.NotNullable && a == NullableAnnotation.NotNullableBasedOnAnalysis))
                    {
                        if (!type.IsPossiblyNullableReferenceTypeTypeParameter())
                        {
                            // For this type both not nullable annotations are equivalent and therefore match.
                            return NullableAnnotation.NotNullable;
                        }

                        // We are dealing with different flavors of not nullable for a possibly nullable reference type parameter,
                        // we don't have a reliable way to merge them since one of them can actually represent a nullable type.
                    }
                    else
                    {
                        Debug.Assert(a.IsAnyNullable() != b.IsAnyNullable());
                    }

                    hadNullabilityMismatch = true;
                    return NullableAnnotation.NotNullable;
            }
        }

        public TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers) =>
            _extensions.WithModifiers(this, customModifiers);

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

                switch (NullableAnnotation)
                {
                    case NullableAnnotation.Unknown:
                        Debug.Assert(!TypeSymbol.IsNullableType());
                        if (TypeSymbol.IsValueType)
                        {
                            return false;
                        }

                        return null;

                    case NullableAnnotation.Nullable:
                    case NullableAnnotation.NullableBasedOnAnalysis:
                        return true;

                    case NullableAnnotation.NotNullableBasedOnAnalysis:
                    case NullableAnnotation.NotNullable:
                        return false;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(NullableAnnotation);
                }
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
                    (NullableAnnotation == NullableAnnotation.Nullable ||
                     (NullableAnnotation == NullableAnnotation.NullableBasedOnAnalysis && !TypeSymbol.IsUnconstrainedTypeParameter())))
                {
                    return str + "?";
                }
                else if (format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier) &&
                    !IsValueType &&
                    IsNullable == false && !TypeSymbol.IsUnconstrainedTypeParameter())
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

            if ((comparison & TypeCompareKind.IgnoreNullableModifiersForReferenceTypes) == 0)
            {
                var thisAnnotation = NullableAnnotation;
                var otherAnnotation = other.NullableAnnotation;
                if (otherAnnotation != thisAnnotation)
                {
                    if (thisAnnotation == NullableAnnotation.Unknown || otherAnnotation == NullableAnnotation.Unknown)
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
                    else if (thisAnnotation.IsAnyNullable())
                    {
                        if (!otherAnnotation.IsAnyNullable())
                        {
                            return false;
                        }
                    }
                    else if (!otherAnnotation.IsAnyNullable())
                    {
                        Debug.Assert(thisAnnotation.IsAnyNotNullable());
                        Debug.Assert(otherAnnotation.IsAnyNotNullable());
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
            TypeSymbol typeSymbol = this.TypeSymbol;
            var newTypeWithModifiers = typeMap.SubstituteType(typeSymbol, withTupleUnification);

            if (!typeSymbol.IsTypeParameter())
            {
                Debug.Assert(newTypeWithModifiers.NullableAnnotation == NullableAnnotation.Unknown || (typeSymbol.IsNullableType() && newTypeWithModifiers.NullableAnnotation.IsAnyNullable()));
                Debug.Assert(newTypeWithModifiers.CustomModifiers.IsEmpty);

                if (typeSymbol.Equals(newTypeWithModifiers.TypeSymbol, TypeCompareKind.ConsiderEverything) &&
                    newCustomModifiers == CustomModifiers)
                {
                    return this; // substitution had no effect on the type or modifiers
                }
                else if ((NullableAnnotation == NullableAnnotation.Unknown || (typeSymbol.IsNullableType() && NullableAnnotation.IsAnyNullable())) &&
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
            else if(Is((TypeParameterSymbol)typeSymbol))
            {
                return newTypeWithModifiers;
            }

            NullableAnnotation newAnnotation;

            Debug.Assert(!IsIndexedTypeParameter(newTypeWithModifiers.TypeSymbol) || newTypeWithModifiers.NullableAnnotation == NullableAnnotation.Unknown);

            if (NullableAnnotation.IsAnyNullable() || newTypeWithModifiers.NullableAnnotation.IsAnyNullable())
            {
                newAnnotation = NullableAnnotation == NullableAnnotation.Nullable || newTypeWithModifiers.NullableAnnotation == NullableAnnotation.Nullable ?
                    NullableAnnotation.Nullable : NullableAnnotation.NullableBasedOnAnalysis;
            }
            else if (IsIndexedTypeParameter(newTypeWithModifiers.TypeSymbol))
            {
                newAnnotation = NullableAnnotation;
            }
            else if (NullableAnnotation != NullableAnnotation.Unknown)
            {
                if (!typeSymbol.IsUnconstrainedTypeParameter())
                {
                    newAnnotation = NullableAnnotation;
                }
                else
                {
                    newAnnotation = newTypeWithModifiers.NullableAnnotation;
                }
            }
            else if (newTypeWithModifiers.NullableAnnotation != NullableAnnotation.Unknown)
            {
                newAnnotation = newTypeWithModifiers.NullableAnnotation;
            }
            else
            {
                Debug.Assert(NullableAnnotation == NullableAnnotation.Unknown);
                Debug.Assert(newTypeWithModifiers.NullableAnnotation == NullableAnnotation.Unknown);
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
            return NullableAnnotation == NullableAnnotation.Unknown && ((object)_defaultType == other) &&
                   CustomModifiers.IsEmpty;
        }

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
                typeWithAnnotationsPredicateOpt: (t, a, b) => t.NullableAnnotation.IsAnyNullable() && !t.TypeSymbol.IsErrorType() && !t.TypeSymbol.IsValueType,
                typePredicateOpt: null,
                arg: (object)null);
            return (object)type != null;
        }

        public void AddNullableTransforms(ArrayBuilder<bool> transforms)
        {
            var typeSymbol = TypeSymbol;
            transforms.Add(NullableAnnotation.IsAnyNullable() && !typeSymbol.IsValueType);
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

            if (isAnnotated)
            {
                result = result.AsNullableReferenceType(fromDeclaration: true);
            }
            else if (nonNullTypesContext.NonNullTypes == true)
            {
                result = result.AsNotNullableReferenceType();
            }
            else if (result.NullableAnnotation != NullableAnnotation.Unknown && (!result.NullableAnnotation.IsAnyNullable() || !oldTypeSymbol.IsValueType))
            {
                result = CreateNonLazyType(newTypeSymbol, NullableAnnotation.Unknown, result.CustomModifiers);
            }

            return true;
        }

        public TypeSymbolWithAnnotations WithTopLevelNonNullabilityForReferenceTypes()
        {
            var typeSymbol = TypeSymbol;
            if (NullableAnnotation == NullableAnnotation.NotNullableBasedOnAnalysis || typeSymbol.IsValueType)
            {
                return this;
            }

            return CreateNonLazyType(typeSymbol, NullableAnnotation.NotNullableBasedOnAnalysis, CustomModifiers);
        }

        public TypeSymbolWithAnnotations SetUnknownNullabilityForReferenceTypes()
        {
            var typeSymbol = TypeSymbol;

            if (IsNullable.HasValue)
            {
                if (!typeSymbol.IsValueType)
                {
                    typeSymbol = typeSymbol.SetUnknownNullabilityForReferenceTypes();

                    return CreateNonLazyType(typeSymbol, NullableAnnotation.Unknown, CustomModifiers);
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

            internal abstract TypeSymbolWithAnnotations AsNullableReferenceType(TypeSymbolWithAnnotations type, bool fromDeclaration);
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

            internal override TypeSymbolWithAnnotations AsNullableReferenceType(TypeSymbolWithAnnotations type, bool fromDeclaration)
            {
                return CreateNonLazyType(type._defaultType, fromDeclaration ? NullableAnnotation.Nullable : NullableAnnotation.NullableBasedOnAnalysis, _customModifiers);
            }

            internal override TypeSymbolWithAnnotations AsNotNullableReferenceType(TypeSymbolWithAnnotations type)
            {
                var defaultType = type._defaultType;
                return CreateNonLazyType(defaultType, defaultType.IsNullableType() ? type.NullableAnnotation : NullableAnnotation.NotNullable, _customModifiers);
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
                Debug.Assert(!underlying.NullableAnnotation.IsAnyNullable());
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
                    return TypeSymbolWithAnnotations.Create(resolvedType, customModifiers: customModifiers);
                }

                return CreateNonLazyType(resolvedType, type.NullableAnnotation, customModifiers);
            }

            internal override TypeSymbolWithAnnotations WithTypeAndModifiers(TypeSymbolWithAnnotations type, TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                if (typeSymbol.IsNullableType())
                {
                    return TypeSymbolWithAnnotations.Create(typeSymbol, customModifiers: customModifiers);
                }

                return CreateNonLazyType(typeSymbol, type.NullableAnnotation, customModifiers);
            }

            internal override TypeSymbolWithAnnotations AsNullableReferenceType(TypeSymbolWithAnnotations type, bool fromDeclaration)
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
