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
    /// A simple class that combines a single symbol with annotations
    /// </summary>
    internal abstract class SymbolWithAnnotations 
    {
        public abstract Symbol Symbol { get; }

        public sealed override string ToString() => Symbol.ToString();
        public virtual string ToDisplayString(SymbolDisplayFormat format = null) => Symbol.ToDisplayString(format);
        public string Name => Symbol.Name;
        public SymbolKind Kind => Symbol.Kind;

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
        public static bool operator == (SymbolWithAnnotations x, SymbolWithAnnotations y)
        {
            throw ExceptionUtilities.Unreachable;
        }

        [Obsolete("Unsupported", error: true)]
        public static bool operator !=(SymbolWithAnnotations x, SymbolWithAnnotations y)
        {
            throw ExceptionUtilities.Unreachable;
        }

        [Obsolete("Unsupported", error: true)]
        public static bool operator ==(Symbol x, SymbolWithAnnotations y)
        {
            throw ExceptionUtilities.Unreachable;
        }

        [Obsolete("Unsupported", error: true)]
        public static bool operator !=(Symbol x, SymbolWithAnnotations y)
        {
            throw ExceptionUtilities.Unreachable;
        }

        [Obsolete("Unsupported", error: true)]
        public static bool operator ==(SymbolWithAnnotations x, Symbol y)
        {
            throw ExceptionUtilities.Unreachable;
        }

        [Obsolete("Unsupported", error: true)]
        public static bool operator !=(SymbolWithAnnotations x, Symbol y)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }

    internal abstract class NamespaceOrTypeOrAliasSymbolWithAnnotations : SymbolWithAnnotations
    {
        [Obsolete("Unsupported", error: true)]
        public new bool Equals(object other)
        {
            throw ExceptionUtilities.Unreachable;
        }

        [Obsolete("Unsupported", error: true)]
        public new int GetHashCode()
        {
            throw ExceptionUtilities.Unreachable;
        }

        public static NamespaceOrTypeOrAliasSymbolWithAnnotations Create(CSharpCompilation compilation, Symbol symbol)
        {
            if (symbol is null)
            {
                return null;
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Namespace:
                    return new NamespaceSymbolWithAnnotations((NamespaceSymbol)symbol);
                case SymbolKind.Alias:
                    return new AliasSymbolWithAnnotations((AliasSymbol)symbol);
                default:
                    return TypeSymbolWithAnnotations.Create(compilation, (TypeSymbol)symbol);
            }
        }

        public abstract bool IsAlias { get; }
        public abstract bool IsNamespace { get; }
        public abstract bool IsType { get; }
    }

    internal sealed class AliasSymbolWithAnnotations : NamespaceOrTypeOrAliasSymbolWithAnnotations
    {
        private readonly AliasSymbol _aliasSymbol;

        public AliasSymbolWithAnnotations(AliasSymbol aliasSymbol)
        {
            Debug.Assert((object)aliasSymbol != null);
            _aliasSymbol = aliasSymbol;
        }

        public sealed override Symbol Symbol => _aliasSymbol;
        public AliasSymbol AliasSymbol => _aliasSymbol;

        public override bool IsAlias => true;
        public override bool IsNamespace => false;
        public override bool IsType => false;
    }

    internal abstract class NamespaceOrTypeSymbolWithAnnotations : NamespaceOrTypeOrAliasSymbolWithAnnotations
    {
        public abstract NamespaceOrTypeSymbol NamespaceOrTypeSymbol { get; }

        public static NamespaceOrTypeSymbolWithAnnotations Create(CSharpCompilation compilation, NamespaceOrTypeSymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Namespace:
                    return new NamespaceSymbolWithAnnotations((NamespaceSymbol)symbol);
                default:
                    return TypeSymbolWithAnnotations.Create(compilation, (TypeSymbol)symbol);
            }
        }

        public override bool IsAlias => false;
    }

    internal sealed class NamespaceSymbolWithAnnotations : NamespaceOrTypeSymbolWithAnnotations
    {
        private readonly NamespaceSymbol _namespaceSymbol;

        public NamespaceSymbolWithAnnotations(NamespaceSymbol namespaceSymbol)
        {
            Debug.Assert((object)namespaceSymbol != null);
            _namespaceSymbol = namespaceSymbol;
        }

        public sealed override Symbol Symbol => _namespaceSymbol;
        public NamespaceSymbol NamespaceSymbol => _namespaceSymbol;
        public sealed override NamespaceOrTypeSymbol NamespaceOrTypeSymbol => _namespaceSymbol;

        public override bool IsNamespace => true;
        public override bool IsType => false;
    }

    /// <summary>
    /// A simple class that combines a single type symbol with annotations
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal abstract class TypeSymbolWithAnnotations : NamespaceOrTypeSymbolWithAnnotations
    {
        internal static readonly SymbolDisplayFormat DebuggerDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
            compilerInternalOptions: SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier);

        internal static TypeSymbolWithAnnotations Create(CSharpCompilation compilation, TypeSymbol typeSymbol)
        {
            return Create(compilation.SourceModule, typeSymbol);
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
        public static TypeSymbolWithAnnotations CreateNullableReferenceType(TypeSymbol typeSymbol)
        {
            if (typeSymbol is null)
            {
                return null;
            }

            // PROTOTYPE(NullableReferenceTypes): Consider if it makes
            // sense to cache and reuse instances, at least for definitions.
            return new NonLazyType(typeSymbol, isNullable: true, ImmutableArray<CustomModifier>.Empty);
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

            if (isNullableIfReferenceType == null && typeSymbol.TypeKind == TypeKind.TypeParameter)
            {
                return new NonLazyType(typeSymbol, isNullable: null, customModifiers);
            }

            if (isNullableIfReferenceType == false || !typeSymbol.IsReferenceType || typeSymbol.IsNullableType())
            {
                return Create(typeSymbol, customModifiers);
            }

            bool? isNullable = typeSymbol.IsNullableType() ? true : isNullableIfReferenceType;
            return new NonLazyType(typeSymbol, isNullable, customModifiers);
        }

        public TypeSymbolWithAnnotations AsNullableReferenceOrValueType(CSharpCompilation compilation, SyntaxReference nullableTypeSyntax)
        {
            var typeSymbol = this.TypeSymbol;

            Debug.Assert(CustomModifiers.IsEmpty);

            // It is not safe to check if a type parameter is a reference type right away, this can send us into a cycle.
            // In this case we delay asking this question as long as possible.
            if (typeSymbol.TypeKind != TypeKind.TypeParameter)
            {
                if (typeSymbol.IsReferenceType && ((CSharpParseOptions)nullableTypeSyntax.SyntaxTree.Options).IsFeatureEnabled(MessageID.IDS_FeatureStaticNullChecking))
                {
                    return new NonLazyType(typeSymbol, isNullable: true, this.CustomModifiers);
                }
                else
                {
                    return Create(compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(typeSymbol)));
                }
            }

            return new LazyNullableType(compilation, nullableTypeSyntax, this);
        }

        /// <summary>
        /// Return nullable type if the type is a non-nullable
        /// reference type and local nullability is inferred.
        /// </summary>
        public TypeSymbolWithAnnotations AsNullableReferenceTypeIfInferLocalNullability(SyntaxNode syntax)
        {
            if (IsReferenceType && IsNullable == false)
            {
                var flags = ((CSharpParseOptions)syntax.SyntaxTree.Options).GetNullableReferenceFlags();
                if ((flags & NullableReferenceFlags.InferLocalNullability) != 0)
                {
                    return AsNullableReferenceType();
                }
            }
            return this;
        }

        /// <summary>
        /// Adjust types in signatures coming from metadata.
        /// </summary>
        public abstract TypeSymbolWithAnnotations AsNullableReferenceType();
        public abstract TypeSymbolWithAnnotations AsNotNullableReferenceType();

        public abstract TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers);

        public sealed override Symbol Symbol => TypeSymbol;
        public sealed override NamespaceOrTypeSymbol NamespaceOrTypeSymbol => TypeSymbol;

        public override bool IsNamespace => false;
        public override bool IsType => true;

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

        public abstract override string ToDisplayString(SymbolDisplayFormat format = null);
        internal string GetDebuggerDisplay() => ToDisplayString(DebuggerDisplayFormat);

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

        public virtual TypeSymbolWithAnnotations SubstituteType(AbstractTypeMap typeMap)
        {
            return SubstituteType(typeMap, (map, type) => map.SubstituteType(type));
        }

        private TypeSymbolWithAnnotations SubstituteType(AbstractTypeMap typeMap, Func<AbstractTypeMap, TypeSymbol, TypeSymbolWithAnnotations> substituteType)
        {
            var newCustomModifiers = typeMap.SubstituteCustomModifiers(this.CustomModifiers);
            var newTypeWithModifiers = substituteType(typeMap, this.TypeSymbol);
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

        public virtual TypeSymbolWithAnnotations SubstituteTypeWithTupleUnification(AbstractTypeMap typeMap)
        {
            return SubstituteType(typeMap, (map, type) => map.SubstituteTypeWithTupleUnification(type));
        }

        /// <summary>
        /// Extract type under assumption that there should be no custom modifiers or annotations.
        /// The method asserts otherwise.
        /// </summary>
        public abstract TypeSymbol AsTypeSymbolOnly();

        /// <summary>
        /// Is this an equal type symbol without annotations/custom modifiers?
        /// </summary>
        public abstract bool Is(TypeSymbol other);

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

            if (IsNullable == true && !typeSymbol.IsNullableType() && typeSymbol.IsReferenceType)
            {
                return true;
            }

            return typeSymbol.ContainsNullableReferenceTypes();
        }

        public void AddNullableTransforms(ArrayBuilder<bool> transforms)
        {
            var typeSymbol = TypeSymbol;
            transforms.Add(IsNullable == true && !typeSymbol.IsNullableType() && typeSymbol.IsReferenceType);
            typeSymbol.AddNullableTransforms(transforms);
        }

        public bool ApplyNullableTransforms(ImmutableArray<bool> transforms, ref int position, out TypeSymbolWithAnnotations result)
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

            if (!oldTypeSymbol.ApplyNullableTransforms(transforms, ref position, out newTypeSymbol))
            {
                return false;
            }

            if ((object)oldTypeSymbol != newTypeSymbol)
            {
                result = result.DoUpdate(newTypeSymbol, result.CustomModifiers);
            }

            if (isNullable)
            {
                result = result.AsNullableReferenceType();
            }
            else
            {
                result = result.AsNotNullableReferenceType();
            }

            return true;
        }

        public TypeSymbolWithAnnotations SetUnknownNullabilityForReferenceTypesIfNecessary(ModuleSymbol module)
        {
            return module.UtilizesNullableReferenceTypes ?
                this :
                this.SetUnknownNullabilityForReferenceTypes();
        }

        public TypeSymbolWithAnnotations WithTopLevelNonNullability()
        {
            var typeSymbol = TypeSymbol;
            if (IsNullable == false || !typeSymbol.IsReferenceType)
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
                if (!typeSymbol.IsNullableType() && typeSymbol.IsReferenceType)
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
            public override bool Is(TypeSymbol other) => _typeSymbol.Equals(other, TypeCompareKind.CompareNullableModifiersForReferenceTypes) && _customModifiers.IsEmpty;

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

            // PROTOTYPE(NullableReferenceTypes): Move implementation to the base class.
            public override string ToDisplayString(SymbolDisplayFormat format)
            {
                var str = _typeSymbol.ToDisplayString(format);
                if (format != null && !_typeSymbol.IsValueType)
                {
                    switch (_isNullable)
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
        }

        /// <summary>
        /// Nullable type parameter. The underlying TypeSymbol is resolved
        /// lazily to avoid cycles when binding declarations.
        /// </summary>
        private sealed class LazyNullableType : TypeSymbolWithAnnotations
        {
            private readonly CSharpCompilation _compilation;
            private readonly SyntaxReference _nullableTypeSyntax;
            private readonly TypeSymbolWithAnnotations _underlying;
            private TypeSymbol _resolved;

            public LazyNullableType(CSharpCompilation compilation, SyntaxReference nullableTypeSyntax, TypeSymbolWithAnnotations underlying)
            {
                Debug.Assert(underlying.IsNullable == false);
                Debug.Assert(underlying.TypeKind == TypeKind.TypeParameter);
                Debug.Assert(underlying.CustomModifiers.IsEmpty);
                _compilation = compilation;
                _nullableTypeSyntax = nullableTypeSyntax;
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
                        if (!_underlying.IsValueType && ((CSharpParseOptions)_nullableTypeSyntax.SyntaxTree.Options).IsFeatureEnabled(MessageID.IDS_FeatureStaticNullChecking))
                        {
                            _resolved = _underlying.TypeSymbol;
                        }
                        else
                        {
                            Interlocked.CompareExchange(ref _resolved, 
                                _compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(
                                    ImmutableArray.Create(_underlying)), 
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

            public override SpecialType SpecialType
            {
                get
                {
                    var specialType = _underlying.SpecialType;
                    return specialType.IsValueType() ? SpecialType.None : specialType;
                }
            }

            public override bool IsRestrictedType(bool ignoreSpanLikeTypes) => _underlying.IsRestrictedType(ignoreSpanLikeTypes);

            public override TypeSymbol AsTypeSymbolOnly()
            {
                Debug.Assert(TypeSymbol.IsNullableType() && CustomModifiers.IsEmpty);
                return TypeSymbol;
            }

            public override bool Is(TypeSymbol other)
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

            public override TypeSymbolWithAnnotations SubstituteType(AbstractTypeMap typeMap)
            {
                if ((object)_resolved != null)
                {
                    return base.SubstituteType(typeMap);
                }

                var newUnderlying = typeMap.SubstituteType(this._underlying);
                if ((object)newUnderlying != this._underlying)
                {
                    if ((newUnderlying.TypeSymbol.Equals(this._underlying.TypeSymbol, TypeCompareKind.AllAspects) || 
                            newUnderlying.TypeSymbol is IndexedTypeParameterSymbolForOverriding) &&
                        newUnderlying.CustomModifiers.IsEmpty)
                    {
                        return new LazyNullableType(_compilation, _nullableTypeSyntax, newUnderlying);
                    }

                    return base.SubstituteType(typeMap);
                }
                else
                {
                    return this; // substitution had no effect on the type or modifiers
                }
            }

            public override TypeSymbolWithAnnotations SubstituteTypeWithTupleUnification(AbstractTypeMap typeMap)
            {
                if ((object)_resolved != null)
                {
                    return base.SubstituteTypeWithTupleUnification(typeMap);
                }

                var newUnderlying = typeMap.SubstituteTypeWithTupleUnification(this._underlying);
                if ((object)newUnderlying != this._underlying)
                {
                    if ((newUnderlying.TypeSymbol.Equals(this._underlying.TypeSymbol, TypeCompareKind.AllAspects) ||
                            newUnderlying.TypeSymbol is IndexedTypeParameterSymbolForOverriding) &&
                        newUnderlying.CustomModifiers.IsEmpty)
                    {
                        return new LazyNullableType(_compilation, _nullableTypeSyntax, newUnderlying);
                    }

                    return base.SubstituteTypeWithTupleUnification(typeMap);
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
                var otherLazy = other as LazyNullableType;

                if ((object)otherLazy != null)
                {
                    return _underlying.TypeSymbolEquals(otherLazy._underlying, comparison);
                }

                return base.TypeSymbolEquals(other, comparison);
            }

            public override string ToDisplayString(SymbolDisplayFormat format)
            {
                var underlyingType = _underlying.TypeSymbol;
                var str = underlyingType.ToDisplayString(format);
                return underlyingType.IsNullableType() ? str : str + "?";
            }
        }
    }
}
