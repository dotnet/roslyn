﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                return null;
            }

            return Create(typeSymbol, typeSymbol.IsNullableType(), customModifiers);
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
                return null;
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

            return new NonLazyType(typeSymbol, nonNullTypesContext, isAnnotated: isAnnotated, customModifiers);
        }

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
                    return new NonLazyType(typeSymbol, NonNullTypesContext, isAnnotated: true, this.CustomModifiers);
                }
                else
                {
                    return Create(compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(typeSymbol)));
                }
            }

            return new LazyNullableTypeParameter(compilation, this);
        }

        /// <summary>
        /// Adjust types in signatures coming from metadata.
        /// </summary>
        public abstract TypeSymbolWithAnnotations AsNullableReferenceType();
        public abstract TypeSymbolWithAnnotations AsNotNullableReferenceType();

        public abstract TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers);
        public abstract TypeSymbolWithAnnotations WithNonNullTypesContext(INonNullTypesContext nonNullTypesContext);

        public abstract TypeSymbol TypeSymbol { get; }
        public virtual TypeSymbol NullableUnderlyingTypeOrSelf => TypeSymbol.StrippedType();

        // PROTOTYPE(NullableReferenceTypes): Should review all the usages of IsNullable outside of NullableWalker.
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
        public abstract bool? IsNullable { get; }

        /// <summary>
        /// Returns:
        /// false for string, int, T;
        /// true for string?, T? where T : class; and
        /// true for int?, T? where T : struct.
        /// </summary>
        public abstract bool IsAnnotated { get; }

        /// <summary>
        /// [NonNullTypes] context used for determining whether unannotated types are not nullable.
        /// Allows us to get the information without eagerly pulling on the NonNullTypes property (which causes cycles).
        /// </summary>
        public abstract INonNullTypesContext NonNullTypesContext { get; }

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
                if (IsAnnotated)
                {
                    return true;
                }

                // A null NonNullTypes (ie. no attribute) means the same as NonNullTypes(false).
                return NonNullTypesContext.NonNullTypes == true ? false : (bool?)null;
            }
        }

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
            bool newIsAnnotated = this.IsAnnotated || newTypeWithModifiers.IsAnnotated;

            // PROTOTYPE(NullableReferenceTypes): Can we use Equals instead?
            if (TypeSymbolEquals(newTypeWithModifiers, TypeCompareKind.CompareNullableModifiersForReferenceTypes) &&
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
            return new NonLazyType(
                newTypeWithModifiers.TypeSymbol,
                newTypeWithModifiers.NonNullTypesContext,
                isAnnotated: newIsAnnotated,
                newCustomModifiers.Concat(newTypeWithModifiers.CustomModifiers));
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

        public TypeSymbolWithAnnotations WithTypeAndModifiers(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
        {
            // PROTOTYPE(NullableReferenceTypes): This method can cause cycles, since it pulls on NonNullTypes
            // Once TypeSymbolWithAnnotations is a struct, we can probably skip this optimization altogether
            //if (CustomModifiers != customModifiers || !TypeSymbol.Equals(typeSymbol, TypeCompareKind.AllAspects))
            //{
            //    return DoUpdate(typeSymbol, customModifiers);
            //}

            return new NonLazyType(typeSymbol, NonNullTypesContext, isAnnotated: IsAnnotated, customModifiers);
        }

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

            if (!result.IsValueType)
            {
                if (isAnnotated)
                {
                    result = result.AsNullableReferenceType();
                }
                else
                {
                    result = result.AsNotNullableReferenceType();
                }
            }

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

            return new NonLazyType(typeSymbol, NonNullTypesTrueContext.Instance, isAnnotated: false, CustomModifiers);
        }

        public TypeSymbolWithAnnotations SetUnknownNullabilityForReferenceTypes()
        {
            var typeSymbol = TypeSymbol;

            if (IsNullable.HasValue)
            {
                if (!typeSymbol.IsValueType)
                {
                    typeSymbol = typeSymbol.SetUnknownNullabilityForReferenceTypes();
                    return new NonLazyType(typeSymbol, NonNullTypesFalseContext.Instance,  isAnnotated: false, CustomModifiers);
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

        private sealed class NonLazyType : TypeSymbolWithAnnotations
        {
            private readonly TypeSymbol _typeSymbol;
            private readonly bool _isAnnotated;
            private readonly ImmutableArray<CustomModifier> _customModifiers;
            private readonly INonNullTypesContext _nonNullTypesContext;

            public NonLazyType(TypeSymbol typeSymbol, INonNullTypesContext nonNullTypesContext, bool isAnnotated, ImmutableArray<CustomModifier> customModifiers)
            {
                Debug.Assert((object)typeSymbol != null);
                Debug.Assert(!customModifiers.IsDefault);
                Debug.Assert(!typeSymbol.IsNullableType() || isAnnotated);
                Debug.Assert(nonNullTypesContext != null);

                _typeSymbol = typeSymbol;
                _nonNullTypesContext = nonNullTypesContext;
                _isAnnotated = isAnnotated;
                _customModifiers = customModifiers;
            }

            public override TypeSymbol TypeSymbol => _typeSymbol;

            // PROTOTYPE(NullableReferenceTypes): IsNullable depends on IsValueType which
            // can lead to cycles when IsNullable is queried early. Replace this property with
            // the Annotation property that depends on IsAnnotated and NonNullTypes only.
            public override bool? IsNullable
            {
                get
                {
                    if (_isAnnotated)
                    {
                        return true;
                    }
                    if (NonNullTypesContext.NonNullTypes == true)
                    {
                        return false;
                    }
                    if (_typeSymbol.IsValueType)
                    {
                        return false;
                    }
                    return null;
                }
            }

            public override bool IsAnnotated => _isAnnotated;
            public override ImmutableArray<CustomModifier> CustomModifiers => _customModifiers;
            public override INonNullTypesContext NonNullTypesContext => _nonNullTypesContext;

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
                return new NonLazyType(_typeSymbol, NonNullTypesContext, _isAnnotated, customModifiers);
            }

            public override TypeSymbolWithAnnotations WithNonNullTypesContext(INonNullTypesContext nonNullTypesContext)
            {
                Debug.Assert(nonNullTypesContext != null);
                return NonNullTypesContext == nonNullTypesContext ?
                    this :
                    new NonLazyType(_typeSymbol, nonNullTypesContext, _isAnnotated, _customModifiers);
            }

            public override TypeSymbol AsTypeSymbolOnly() => _typeSymbol;

            // PROTOTYPE(NullableReferenceTypes): Use WithCustomModifiers.Is() => false
            // and set IsNullable=null always for GetTypeParametersAsTypeArguments.
            public override bool Is(TypeParameterSymbol other) => _typeSymbol.Equals(other, TypeCompareKind.CompareNullableModifiersForReferenceTypes) && _customModifiers.IsEmpty;

            public override TypeSymbolWithAnnotations AsNullableReferenceType()
            {
                return _isAnnotated ?
                    this :
                    new NonLazyType(_typeSymbol, NonNullTypesContext, isAnnotated: true, _customModifiers);
            }

            public override TypeSymbolWithAnnotations AsNotNullableReferenceType()
            {
                return !_isAnnotated || _typeSymbol.IsNullableType() ?
                    this :
                    new NonLazyType(_typeSymbol, NonNullTypesContext, isAnnotated: false, _customModifiers);
            }
        }

        /// <summary>
        /// Nullable type parameter. The underlying TypeSymbol is resolved
        /// lazily to avoid cycles when binding declarations.
        /// </summary>
        private sealed class LazyNullableTypeParameter : TypeSymbolWithAnnotations
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

            public override bool? IsNullable => true;
            public override bool IsAnnotated => true;
            public override INonNullTypesContext NonNullTypesContext => _underlying.NonNullTypesContext;
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
                            Interlocked.CompareExchange(ref _resolved,
                                _compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(_underlying)),
                                null);
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

                return new NonLazyType(typeSymbol, NonNullTypesContext, isAnnotated: IsAnnotated, customModifiers);
            }

            public override TypeSymbolWithAnnotations WithNonNullTypesContext(INonNullTypesContext nonNullTypesContext)
            {
                return _underlying.NonNullTypesContext == nonNullTypesContext ?
                    this :
                    new LazyNullableTypeParameter(_compilation, _underlying.WithNonNullTypesContext(nonNullTypesContext));
            }

            public override TypeSymbolWithAnnotations AsNullableReferenceType() => this;

            public override TypeSymbolWithAnnotations AsNotNullableReferenceType()
            {
                if (!_underlying.TypeSymbol.IsValueType)
                {
                    return _underlying;
                }

                return this;
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
                        return new LazyNullableTypeParameter(_compilation, newUnderlying);
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
    }
}
