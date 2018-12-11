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
        Unknown,      // No information. Think oblivious.
        NotAnnotated, // Type is not annotated - string, int, T (including the case when T is unconstrained).
        Annotated,    // Type is annotated - string?, T? where T : class; and for int?, T? where T : struct.
        NotNullable,  // Explicitly set by flow analysis
        Nullable,     // Explicitly set by flow analysis
    }

    internal static class NullableAnnotationExtensions
    {
        public static bool IsAnyNullable(this NullableAnnotation annotation)
        {
            return annotation == NullableAnnotation.Annotated || annotation == NullableAnnotation.Nullable;
        }

        public static bool IsAnyNotNullable(this NullableAnnotation annotation)
        {
            return annotation == NullableAnnotation.NotAnnotated || annotation == NullableAnnotation.NotNullable;
        }

        /// <summary>
        /// Join nullable annotations from the set of lower bounds for fixing a type parameter.
        /// </summary>
        public static NullableAnnotation JoinForFixingLowerBounds(this NullableAnnotation a, NullableAnnotation b)
        {
            if (a == b)
            {
                return a;
            }

            if (a.IsAnyNullable() && b.IsAnyNullable())
            {
                return NullableAnnotation.Annotated;
            }

            // If nullability on both sides matches - result is that nullability (trivial cases like these are handled above)
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

            Debug.Assert((a == NullableAnnotation.NotAnnotated && b == NullableAnnotation.NotNullable) ||
                (b == NullableAnnotation.NotAnnotated && a == NullableAnnotation.NotNullable));
            return NullableAnnotation.NotAnnotated; // It is reasonable to settle on this value because the difference in annotations is either
                                                    // not significant for the type, or candidate corresponding to this value is possibly a 
                                                    // nullable reference type type parameter and nullable should win. 
        }

        /// <summary>
        /// Join nullable annotations from distinct branches during flow analysis.
        /// </summary>
        public static NullableAnnotation JoinForFlowAnalysisBranches<T>(this NullableAnnotation selfAnnotation, NullableAnnotation otherAnnotation, T type, Func<T, bool> isPossiblyNullableReferenceTypeTypeParameter)
        {
            if (selfAnnotation == otherAnnotation)
            {
                return selfAnnotation;
            }

            if (selfAnnotation.IsAnyNullable() || otherAnnotation.IsAnyNullable())
            {
                return selfAnnotation == NullableAnnotation.Annotated || otherAnnotation == NullableAnnotation.Annotated ?
                            NullableAnnotation.Annotated : NullableAnnotation.Nullable;
            }
            else if (selfAnnotation == NullableAnnotation.Unknown)
            {
                if (otherAnnotation == NullableAnnotation.Unknown || otherAnnotation == NullableAnnotation.NotNullable)
                {
                    return NullableAnnotation.Unknown;
                }
                else
                {
                    Debug.Assert(otherAnnotation == NullableAnnotation.NotAnnotated);
                    if (isPossiblyNullableReferenceTypeTypeParameter(type))
                    {
                        return otherAnnotation;
                    }
                    else
                    {
                        return NullableAnnotation.Unknown;
                    }
                }
            }
            else if (otherAnnotation == NullableAnnotation.Unknown)
            {
                if (selfAnnotation == NullableAnnotation.NotNullable)
                {
                    return NullableAnnotation.Unknown;
                }
                else
                {
                    Debug.Assert(selfAnnotation == NullableAnnotation.NotAnnotated);
                    if (isPossiblyNullableReferenceTypeTypeParameter(type))
                    {
                        return selfAnnotation;
                    }
                    else
                    {
                        return NullableAnnotation.Unknown;
                    }
                }
            }
            else
            {
                return selfAnnotation == NullableAnnotation.NotAnnotated || otherAnnotation == NullableAnnotation.NotAnnotated ?
                            NullableAnnotation.NotAnnotated : NullableAnnotation.NotNullable;
            }
        }

        /// <summary>
        /// Meet two nullable annotations for computing the nullable annotation of a type parameter from upper bounds.
        /// </summary>
        public static NullableAnnotation MeetForFixingUpperBounds(this NullableAnnotation a, NullableAnnotation b)
        {
            if (a == b)
            {
                return a;
            }

            if (a.IsAnyNullable() && b.IsAnyNullable())
            {
                return NullableAnnotation.Annotated;
            }

            // If nullability on both sides matches - result is that nullability (trivial cases like these are handled above)
            // If either candidate is not nullable - result is not nullable
            // Otherwise - result is "oblivious". 

            if (a == NullableAnnotation.NotNullable || b == NullableAnnotation.NotNullable)
            {
                return NullableAnnotation.NotNullable;
            }

            if (a == NullableAnnotation.NotAnnotated || b == NullableAnnotation.NotAnnotated)
            {
                return NullableAnnotation.NotAnnotated;
            }

            Debug.Assert(a == NullableAnnotation.Unknown || b == NullableAnnotation.Unknown);
            return NullableAnnotation.Unknown;
        }

        /// <summary>
        /// Meet two nullable annotations from distinct states for the meet (union) operation in flow analysis.
        /// </summary>
        public static NullableAnnotation MeetForFlowAnalysisFinally(this NullableAnnotation selfAnnotation, NullableAnnotation otherAnnotation)
        {
            if (selfAnnotation == otherAnnotation)
            {
                return selfAnnotation;
            }

            if (selfAnnotation.IsAnyNotNullable() || otherAnnotation.IsAnyNotNullable())
            {
                return selfAnnotation == NullableAnnotation.NotNullable || otherAnnotation == NullableAnnotation.NotNullable ?
                            NullableAnnotation.NotNullable : NullableAnnotation.NotAnnotated;
            }
            else if (selfAnnotation == NullableAnnotation.Unknown || otherAnnotation == NullableAnnotation.Unknown)
            {
                return NullableAnnotation.Unknown;
            }
            else
            {
                return selfAnnotation == NullableAnnotation.Nullable || otherAnnotation == NullableAnnotation.Nullable ?
                            NullableAnnotation.Nullable : NullableAnnotation.Annotated;
            }
        }

        /// <summary>
        /// Check that two nullable annotations are "compatible", which means they could be the same. Return the
        /// nullable annotation to be used as a result. Also returns through <paramref name="hadNullabilityMismatch"/>
        /// whether the caller should report a warning because there was an actual mismatch (e.g. nullable vs non-nullable).
        /// </summary>
        public static NullableAnnotation EnsureCompatible<T>(this NullableAnnotation a, NullableAnnotation b, T type, Func<T, bool> isPossiblyNullableReferenceTypeTypeParameter, out bool hadNullabilityMismatch)
        {
            hadNullabilityMismatch = false;
            if (a == b)
            {
                return a;
            }

            if (a.IsAnyNullable() && b.IsAnyNullable())
            {
                return NullableAnnotation.Annotated;
            }

            // If nullability on both sides matches - result is that nullability (trivial cases like these are handled above)
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
            if ((a == NullableAnnotation.NotAnnotated && b == NullableAnnotation.NotNullable) ||
                (b == NullableAnnotation.NotAnnotated && a == NullableAnnotation.NotNullable))
            {
                if (!isPossiblyNullableReferenceTypeTypeParameter(type))
                {
                    // For this type both not nullable annotations are equivalent and therefore match.
                    return NullableAnnotation.NotAnnotated;
                }

                // We are dealing with different flavors of not nullable for a possibly nullable reference type parameter,
                // we don't have a reliable way to merge them since one of them can actually represent a nullable type.
            }
            else
            {
                Debug.Assert(a.IsAnyNullable() != b.IsAnyNullable());
            }

            hadNullabilityMismatch = true;
            return NullableAnnotation.NotAnnotated;
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
            Debug.Assert(!defaultType.IsNullableType() || (nullableAnnotation != NullableAnnotation.Unknown && nullableAnnotation != NullableAnnotation.NotAnnotated));
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

            return Create(typeSymbol, nullableAnnotation: isAnnotated ? NullableAnnotation.Annotated : isNullableEnabled ? NullableAnnotation.NotAnnotated : NullableAnnotation.Unknown,
                          customModifiers.NullToEmpty());
        }

        internal static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, NullableAnnotation nullableAnnotation = NullableAnnotation.Unknown, ImmutableArray<CustomModifier> customModifiers = default)
        {
            if (typeSymbol is null)
            {
                return default;
            }

            switch (nullableAnnotation)
            {
                case NullableAnnotation.Unknown:
                case NullableAnnotation.NotAnnotated:
                    if (typeSymbol.IsNullableType())
                    {
                        // int?, T? where T : struct (add annotation)
                        nullableAnnotation = NullableAnnotation.Annotated;
                    }
                    break;
            }

            return CreateNonLazyType(typeSymbol, nullableAnnotation, customModifiers.NullToEmpty());
        }

        internal bool IsPossiblyNullableReferenceTypeTypeParameter()
        {
            return NullableAnnotation == NullableAnnotation.NotAnnotated && TypeSymbol.IsPossiblyNullableReferenceTypeTypeParameter();
        }

        internal NullableAnnotation GetValueNullableAnnotation()
        {
            if (IsPossiblyNullableReferenceTypeTypeParameter())
            {
                return NullableAnnotation.Nullable;
            }

            // https://github.com/dotnet/roslyn/issues/31675: Is a similar case needed in ValueCanBeNull?
            if (NullableAnnotation != NullableAnnotation.NotNullable && IsNullableTypeOrTypeParameter())
            {
                return NullableAnnotation.Nullable;
            }

            return NullableAnnotation;
        }

        internal bool? ValueCanBeNull()
        {
            switch (NullableAnnotation)
            {
                case NullableAnnotation.Unknown:
                    return null;

                case NullableAnnotation.Annotated:
                case NullableAnnotation.Nullable:
                    return true;

                case NullableAnnotation.NotNullable:
                    return false;

                case NullableAnnotation.NotAnnotated:
                    return TypeSymbol.IsPossiblyNullableReferenceTypeTypeParameter();

                default:
                    throw ExceptionUtilities.UnexpectedValue(NullableAnnotation);
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
            switch (variance)
            {
                case VarianceKind.In:
                    return a.MeetForFixingUpperBounds(b);
                case VarianceKind.Out:
                    return a.JoinForFixingLowerBounds(b);
                case VarianceKind.None:
                    return a.EnsureCompatible(b, type, _IsPossiblyNullableReferenceTypeTypeParameterDelegate, out hadNullabilityMismatch);
                default:
                    throw ExceptionUtilities.UnexpectedValue(variance);
            }
        }

        private readonly static Func<TypeSymbol, bool> _IsPossiblyNullableReferenceTypeTypeParameterDelegate = type => type.IsPossiblyNullableReferenceTypeTypeParameter();

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
            var str = TypeSymbol.ToDisplayString(format);
            if (format != null)
            {
                if (format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier) &&
                    !IsNullableType() && !IsValueType &&
                    (NullableAnnotation == NullableAnnotation.Annotated ||
                     (NullableAnnotation == NullableAnnotation.Nullable && !TypeSymbol.IsUnconstrainedTypeParameter())))
                {
                    return str + "?";
                }
                else if (format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier) &&
                    !IsValueType &&
                    NullableAnnotation.IsAnyNotNullable() && !TypeSymbol.IsUnconstrainedTypeParameter())
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
            else if (Is((TypeParameterSymbol)typeSymbol))
            {
                return newTypeWithModifiers;
            }

            NullableAnnotation newAnnotation;

            Debug.Assert(!IsIndexedTypeParameter(newTypeWithModifiers.TypeSymbol) || newTypeWithModifiers.NullableAnnotation == NullableAnnotation.Unknown);

            if (NullableAnnotation.IsAnyNullable() || newTypeWithModifiers.NullableAnnotation.IsAnyNullable())
            {
                newAnnotation = NullableAnnotation == NullableAnnotation.Annotated || newTypeWithModifiers.NullableAnnotation == NullableAnnotation.Annotated ?
                    NullableAnnotation.Annotated : NullableAnnotation.Nullable;
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
                typeWithAnnotationsPredicateOpt: (t, a, b) => t.NullableAnnotation != NullableAnnotation.Unknown && !t.TypeSymbol.IsErrorType() && !t.TypeSymbol.IsValueType,
                typePredicateOpt: null,
                arg: (object)null);
            return (object)type != null;
        }

        public void AddNullableTransforms(ArrayBuilder<byte> transforms)
        {
            var typeSymbol = TypeSymbol;
            byte flag;

            if (NullableAnnotation == NullableAnnotation.Unknown || typeSymbol.IsValueType)
            {
                flag = (byte)NullableAnnotation.Unknown;
            }
            else if (NullableAnnotation.IsAnyNullable())
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

                case NullableAnnotation.Unknown:
                    if (result.NullableAnnotation != NullableAnnotation.Unknown &&
                        !(result.NullableAnnotation.IsAnyNullable() && oldTypeSymbol.IsNullableType())) // Preserve nullable annotation on Nullable<T>.
                    {
                        result = CreateNonLazyType(newTypeSymbol, NullableAnnotation.Unknown, result.CustomModifiers);
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
            if (NullableAnnotation == NullableAnnotation.NotNullable || (typeSymbol.IsValueType && !typeSymbol.IsNullableType()))
            {
                return this;
            }

            return CreateNonLazyType(typeSymbol, NullableAnnotation.NotNullable, CustomModifiers);
        }

        public TypeSymbolWithAnnotations SetUnknownNullabilityForReferenceTypes()
        {
            var typeSymbol = TypeSymbol;

            if (NullableAnnotation != NullableAnnotation.Unknown)
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
