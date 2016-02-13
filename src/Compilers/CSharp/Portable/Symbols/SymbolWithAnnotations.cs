// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{

    /// <summary>
    /// A simple class that combines a single symbol with annotations
    /// </summary>
    internal abstract class SymbolWithAnnotations : IMessageSerializable 
    {
        public abstract Symbol Symbol { get; }

        public override string ToString() => Symbol.ToString();
        public string ToDisplayString(SymbolDisplayFormat format = null) => Symbol.ToDisplayString(format);
        public string Name => Symbol.Name;
        public SymbolKind Kind => Symbol.Kind;

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

        public static NamespaceOrTypeOrAliasSymbolWithAnnotations Create(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Namespace:
                    return new NamespaceSymbolWithAnnotations((NamespaceSymbol)symbol);
                case SymbolKind.Alias:
                    return new AliasSymbolWithAnnotations((AliasSymbol)symbol);
                default:
                    return TypeSymbolWithAnnotations.Create((TypeSymbol)symbol);
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

        public static NamespaceOrTypeSymbolWithAnnotations Create(NamespaceOrTypeSymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Namespace:
                    return new NamespaceSymbolWithAnnotations((NamespaceSymbol)symbol);
                default:
                    return TypeSymbolWithAnnotations.Create((TypeSymbol)symbol);
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
    internal abstract class TypeSymbolWithAnnotations : NamespaceOrTypeSymbolWithAnnotations
    {
        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol)
        {
            // TODO: Consider if it makes sense to cache and reuse instances, at least for definitions.
            return new WithoutCustomModifiers(typeSymbol);
        }

        public static TypeSymbolWithAnnotations CreateNullableReferenceType(TypeSymbol typeSymbol)
        {
            // TODO: Consider if it makes sense to cache and reuse instances, at least for definitions.
            return new NullableReferenceTypeWithoutCustomModifiers(typeSymbol);
        }

        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
        {
            if (customModifiers.IsDefaultOrEmpty)
            {
                return new WithoutCustomModifiers(typeSymbol);
            }

            return new WithCustomModifiers(typeSymbol, customModifiers);
        }

        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, bool? isNullableIfReferenceType)
        {
            if (isNullableIfReferenceType == false || !typeSymbol.IsReferenceType || typeSymbol.IsNullableType())
            {
                return Create(typeSymbol);
            }

            if (isNullableIfReferenceType == true)
            {
                return new NullableReferenceTypeWithoutCustomModifiers(typeSymbol);
            }

            return new ReferenceTypeUnknownNullabilityWithoutCustomModifiers(typeSymbol); 
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
                    return new NullableReferenceTypeWithoutCustomModifiers(typeSymbol);
                }
                else
                {
                    return Create(compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(ImmutableArray.Create(typeSymbol)));
                }
            }

            return new LazyNullableType(compilation, nullableTypeSyntax, this);
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
        public SpecialType SpecialType => TypeSymbol.SpecialType;
        public bool IsManagedType => TypeSymbol.IsManagedType;
        public Cci.PrimitiveTypeCode PrimitiveTypeCode => TypeSymbol.PrimitiveTypeCode;
        public bool IsEnumType() => TypeSymbol.IsEnumType();
        public bool IsDynamic() => TypeSymbol.IsDynamic();
        public bool IsRestrictedType() => TypeSymbol.IsRestrictedType();
        public bool IsPointerType() => TypeSymbol.IsPointerType();
        public bool IsUnsafe() => TypeSymbol.IsUnsafe();
        public virtual bool IsStatic => TypeSymbol.IsStatic;
        public bool IsNullableTypeOrTypeParameter() => TypeSymbol.IsNullableTypeOrTypeParameter();
        public virtual bool IsVoid => TypeSymbol.SpecialType == SpecialType.System_Void;
        public virtual bool IsSZArray() => TypeSymbol.IsSZArray();

        public bool Equals(TypeSymbolWithAnnotations other, TypeSymbolEqualityOptions options)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if ((object)other == null || !TypeSymbolEquals(other, options))
            {
                return false;
            }

            // Make sure custom modifiers are the same.
            if ((options & TypeSymbolEqualityOptions.IgnoreCustomModifiers) == 0 && !this.CustomModifiers.SequenceEqual(other.CustomModifiers))
            {
                return false;
            }

            if ((options & TypeSymbolEqualityOptions.CompareNullableModifiersForReferenceTypes) != 0 && other.IsNullable != this.IsNullable)
            {
                if ((options & TypeSymbolEqualityOptions.UnknownNullableModifierMatchesAny) == 0 || (this.IsNullable.HasValue && other.IsNullable.HasValue))
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual bool TypeSymbolEquals(TypeSymbolWithAnnotations other, TypeSymbolEqualityOptions options)
        {
            return this.TypeSymbol.Equals(other.TypeSymbol, options);
        }

        public bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return TypeSymbol.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes) ||
                   Symbol.GetUnificationUseSiteDiagnosticRecursive(ref result, this.CustomModifiers, owner, ref checkedTypes);
        }

        public virtual void CheckAllConstraints(ConversionsBase conversions, Location location, DiagnosticBag diagnostics)
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
            var newCustomModifiers = typeMap.SubstituteCustomModifiers(this.CustomModifiers);
            var newTypeWithModifiers = typeMap.SubstituteType(this.TypeSymbol);
            bool? newIsNullable = this.IsNullable;
            
            if (newIsNullable == false)
            {
                newIsNullable = newTypeWithModifiers.IsNullable;
            }
            else if (newIsNullable == null)
            {
                if (newTypeWithModifiers.IsNullable == true)
                {
                    newIsNullable = true;
                }
            }
            else
            {
                Debug.Assert(newIsNullable == true);
            }

            if (!TypeSymbolEquals(newTypeWithModifiers, TypeSymbolEqualityOptions.CompareNullableModifiersForReferenceTypes) ||
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
                    Debug.Assert(newTypeWithModifiers.IsNullable == false);
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

                if (newIsNullable == true)
                {
                    if (newCustomModifiers.IsEmpty)
                    {
                        return new NullableReferenceTypeWithoutCustomModifiers(newTypeWithModifiers.TypeSymbol);
                    }

                    return new NullableReferenceTypeWithCustomModifiers(newTypeWithModifiers.TypeSymbol, newCustomModifiers);
                }

                Debug.Assert(newIsNullable == null);

                if (newCustomModifiers.IsEmpty)
                {
                    return new ReferenceTypeUnknownNullabilityWithoutCustomModifiers(newTypeWithModifiers.TypeSymbol);
                }

                return new ReferenceTypeUnknownNullabilityWithCustomModifiers(newTypeWithModifiers.TypeSymbol, newCustomModifiers);
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
        /// Is this an equal type symbol without annotations/custom modifiers?
        /// </summary>
        public abstract bool Is(TypeSymbol other);

        public TypeSymbolWithAnnotations Update(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
        {
            if (CustomModifiers != customModifiers || !TypeSymbol.Equals(typeSymbol, TypeSymbolEqualityOptions.AllAspects))
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

            if (position < transforms.Length)
            {
                bool isNullable = transforms[position++];

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

            return false;
        }

        public TypeSymbolWithAnnotations SetUnknownNullabilityForRefernceTypes()
        {
            var typeSymbol = TypeSymbol;

            if (IsNullable.HasValue)
            {
                if (!typeSymbol.IsNullableType() && typeSymbol.IsReferenceType)
                {
                    typeSymbol = typeSymbol.SetUnknownNullabilityForRefernceTypes();
                    var customModifiers = CustomModifiers;

                    if (customModifiers.IsEmpty)
                    {
                        return new ReferenceTypeUnknownNullabilityWithoutCustomModifiers(typeSymbol);
                    }

                    return new ReferenceTypeUnknownNullabilityWithCustomModifiers(typeSymbol, customModifiers);
                }
            }

            var newTypeSymbol = typeSymbol.SetUnknownNullabilityForRefernceTypes();

            if ((object)newTypeSymbol != typeSymbol)
            {
                return DoUpdate(newTypeSymbol, CustomModifiers);
            }

            return this;
        }

        private class WithoutCustomModifiers : TypeSymbolWithAnnotations
        {
            protected readonly TypeSymbol _typeSymbol;

            public WithoutCustomModifiers(TypeSymbol typeSymbol)
            {
                Debug.Assert((object)typeSymbol != null);
                _typeSymbol = typeSymbol;
            }

            public sealed override TypeSymbol TypeSymbol => _typeSymbol;
            public sealed override bool? IsNullable => _typeSymbol.IsNullableType();
            public override ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;

            public override TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers)
            {
                if (customModifiers.IsDefaultOrEmpty)
                {
                    return this;
                }

                return new WithCustomModifiers(_typeSymbol, customModifiers);
            }

            public override TypeSymbol AsTypeSymbolOnly() => _typeSymbol;
            public override bool Is(TypeSymbol other) => _typeSymbol.Equals(other, TypeSymbolEqualityOptions.CompareNullableModifiersForReferenceTypes);

            protected sealed override TypeSymbolWithAnnotations DoUpdate(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                return TypeSymbolWithAnnotations.Create(typeSymbol, customModifiers);
            }

            public override TypeSymbolWithAnnotations AsNullableReferenceType()
            {
                if (_typeSymbol.IsNullableType())
                {
                    return this;
                }

                return new NullableReferenceTypeWithoutCustomModifiers(_typeSymbol);
            }

            public override TypeSymbolWithAnnotations AsNotNullableReferenceType()
            {
                return this;
            }
        }

        private class NullableReferenceTypeWithoutCustomModifiers : TypeSymbolWithAnnotations
        {
            protected readonly TypeSymbol _typeSymbol;

            public NullableReferenceTypeWithoutCustomModifiers(TypeSymbol typeSymbol)
            {
                Debug.Assert((object)typeSymbol != null);
                Debug.Assert(!typeSymbol.IsNullableType());
                _typeSymbol = typeSymbol;
            }

            public sealed override TypeSymbol TypeSymbol => _typeSymbol;
            public sealed override bool? IsNullable => true;
            public override ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;

            public override TypeSymbol AsTypeSymbolOnly()
            {
                Debug.Assert(this.IsNullable == false);
                return _typeSymbol;
            }

            public sealed override bool Is(TypeSymbol other) => false; // It has nullable annotation.

            public override TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers)
            {
                if (customModifiers.IsDefaultOrEmpty)
                {
                    return this;
                }

                return new NullableReferenceTypeWithCustomModifiers(_typeSymbol, customModifiers);
            }

            protected sealed override TypeSymbolWithAnnotations DoUpdate(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                if (customModifiers.IsDefaultOrEmpty)
                {
                    return new NullableReferenceTypeWithoutCustomModifiers(typeSymbol);
                }

                return new NullableReferenceTypeWithCustomModifiers(typeSymbol, customModifiers);
            }

            public sealed override TypeSymbolWithAnnotations AsNullableReferenceType()
            {
                return this;
            }

            public override TypeSymbolWithAnnotations AsNotNullableReferenceType()
            {
                return new WithoutCustomModifiers(_typeSymbol);
            }
        }

        private class ReferenceTypeUnknownNullabilityWithoutCustomModifiers : TypeSymbolWithAnnotations
        {
            protected readonly TypeSymbol _typeSymbol;

            public ReferenceTypeUnknownNullabilityWithoutCustomModifiers(TypeSymbol typeSymbol)
            {
                Debug.Assert((object)typeSymbol != null);
                Debug.Assert(!typeSymbol.IsNullableType());
                _typeSymbol = typeSymbol;
            }

            public sealed override TypeSymbol TypeSymbol => _typeSymbol;
            public sealed override bool? IsNullable => null;
            public override ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;

            public override TypeSymbol AsTypeSymbolOnly()
            {
                Debug.Assert(this.IsNullable == false);
                return _typeSymbol;
            }

            public sealed override bool Is(TypeSymbol other) => false; // It has nullable annotation, unknown is an annotation.

            public override TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers)
            {
                if (customModifiers.IsDefaultOrEmpty)
                {
                    return this;
                }

                return new ReferenceTypeUnknownNullabilityWithCustomModifiers(_typeSymbol, customModifiers);
            }

            protected sealed override TypeSymbolWithAnnotations DoUpdate(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                if (customModifiers.IsDefaultOrEmpty)
                {
                    return new ReferenceTypeUnknownNullabilityWithoutCustomModifiers(typeSymbol);
                }

                return new ReferenceTypeUnknownNullabilityWithCustomModifiers(typeSymbol, customModifiers);
            }

            public override TypeSymbolWithAnnotations AsNullableReferenceType()
            {
                return new NullableReferenceTypeWithoutCustomModifiers(_typeSymbol);
            }

            public override TypeSymbolWithAnnotations AsNotNullableReferenceType()
            {
                return new WithoutCustomModifiers(_typeSymbol);
            }
        }

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
                        if (_underlying.IsReferenceType && ((CSharpParseOptions)_nullableTypeSyntax.SyntaxTree.Options).IsFeatureEnabled(MessageID.IDS_FeatureStaticNullChecking))
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

            public override TypeSymbol NullableUnderlyingTypeOrSelf => _underlying.TypeSymbol;

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

                return TypeSymbol.Equals(other, TypeSymbolEqualityOptions.CompareNullableModifiersForReferenceTypes);
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

                return new NullableReferenceTypeWithCustomModifiers(typeSymbol, customModifiers);
            }

            protected override TypeSymbolWithAnnotations DoUpdate(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
            {
                if (typeSymbol.IsNullableType())
                {
                    return TypeSymbolWithAnnotations.Create(typeSymbol, customModifiers);
                }

                if (customModifiers.IsDefaultOrEmpty)
                {
                    return new NullableReferenceTypeWithoutCustomModifiers(typeSymbol);
                }

                return new NullableReferenceTypeWithCustomModifiers(typeSymbol, customModifiers);
            }

            public override TypeSymbolWithAnnotations AsNullableReferenceType() => this;

            public override TypeSymbolWithAnnotations AsNotNullableReferenceType()
            {
                if (_underlying.TypeSymbol.IsReferenceType)
                {
                    return _underlying;
                }

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
                    if ((newUnderlying.TypeSymbol.Equals(this._underlying.TypeSymbol, TypeSymbolEqualityOptions.AllAspects) || 
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

            protected override bool TypeSymbolEquals(TypeSymbolWithAnnotations other, TypeSymbolEqualityOptions options)
            {
                var otherLazy = other as LazyNullableType;

                if ((object)otherLazy != null)
                {
                    return _underlying.TypeSymbolEquals(otherLazy._underlying, options);
                }

                return base.TypeSymbolEquals(other, options);
            }
        }

        private class WithCustomModifiers : WithoutCustomModifiers
        {
            private readonly ImmutableArray<CustomModifier> _customModifiers;

            public WithCustomModifiers(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
                : base(typeSymbol)
            {
                Debug.Assert(!customModifiers.IsDefaultOrEmpty);
                _customModifiers = customModifiers;
            }

            public override TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers)
            {
                if (customModifiers.IsDefaultOrEmpty)
                {
                    return this;
                }

                return new WithCustomModifiers(_typeSymbol, _customModifiers.Concat(customModifiers));
            }

            public override ImmutableArray<CustomModifier> CustomModifiers => _customModifiers;

            public override TypeSymbol AsTypeSymbolOnly()
            {
                Debug.Assert(this.CustomModifiers.IsEmpty);
                return base.AsTypeSymbolOnly();
            }

            public override bool Is(TypeSymbol other)
            {
                return false; // have custom modifiers
            }

            public override TypeSymbolWithAnnotations AsNullableReferenceType()
            {
                if (_typeSymbol.IsNullableType())
                {
                    return this;
                }

                return new NullableReferenceTypeWithCustomModifiers(_typeSymbol, _customModifiers);
            }

            public override TypeSymbolWithAnnotations AsNotNullableReferenceType()
            {
                return this;
            }
        }

        private class NullableReferenceTypeWithCustomModifiers : NullableReferenceTypeWithoutCustomModifiers
        {
            private readonly ImmutableArray<CustomModifier> _customModifiers;

            public NullableReferenceTypeWithCustomModifiers(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
                : base(typeSymbol)
            {
                Debug.Assert(!customModifiers.IsDefaultOrEmpty);
                _customModifiers = customModifiers;
            }

            public override TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers)
            {
                if (customModifiers.IsDefaultOrEmpty)
                {
                    return this;
                }

                return new NullableReferenceTypeWithCustomModifiers(_typeSymbol, _customModifiers.Concat(customModifiers));
            }

            public override ImmutableArray<CustomModifier> CustomModifiers => _customModifiers;

            public override TypeSymbol AsTypeSymbolOnly()
            {
                Debug.Assert(this.CustomModifiers.IsEmpty);
                return base.AsTypeSymbolOnly();
            }

            public override TypeSymbolWithAnnotations AsNotNullableReferenceType()
            {
                return new WithCustomModifiers(_typeSymbol, _customModifiers);
            }
        }

        private class ReferenceTypeUnknownNullabilityWithCustomModifiers : ReferenceTypeUnknownNullabilityWithoutCustomModifiers
        {
            private readonly ImmutableArray<CustomModifier> _customModifiers;

            public ReferenceTypeUnknownNullabilityWithCustomModifiers(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
                : base(typeSymbol)
            {
                Debug.Assert(!customModifiers.IsDefaultOrEmpty);
                _customModifiers = customModifiers;
            }

            public override TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers)
            {
                if (customModifiers.IsDefaultOrEmpty)
                {
                    return this;
                }

                return new ReferenceTypeUnknownNullabilityWithCustomModifiers(_typeSymbol, _customModifiers.Concat(customModifiers));
            }

            public override ImmutableArray<CustomModifier> CustomModifiers => _customModifiers;

            public override TypeSymbol AsTypeSymbolOnly()
            {
                Debug.Assert(this.CustomModifiers.IsEmpty);
                return base.AsTypeSymbolOnly();
            }

            public override TypeSymbolWithAnnotations AsNullableReferenceType()
            {
                return new NullableReferenceTypeWithCustomModifiers(_typeSymbol, _customModifiers);
            }

            public override TypeSymbolWithAnnotations AsNotNullableReferenceType()
            {
                return new WithCustomModifiers(_typeSymbol, _customModifiers); ;
            }
        }
    }
}
