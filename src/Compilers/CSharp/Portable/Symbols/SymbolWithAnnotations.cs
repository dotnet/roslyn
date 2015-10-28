// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

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

        public static TypeSymbolWithAnnotations Create(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
        {
            if (customModifiers.IsDefaultOrEmpty)
            {
                return new WithoutCustomModifiers(typeSymbol);
            }

            return new WithCustomModifiers(typeSymbol, customModifiers);
        }

        public abstract TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers);

        public sealed override Symbol Symbol => TypeSymbol;
        public sealed override NamespaceOrTypeSymbol NamespaceOrTypeSymbol => TypeSymbol;

        public abstract TypeSymbol TypeSymbol { get; }

        /// <summary>
        /// Is this a nullable reference or value type.
        /// If it is a nullable value type, <see cref="TypeSymbol"/>
        /// returns symbol for constructed System.Nullable`1 type.
        /// If it is a nullable reference type, <see cref="TypeSymbol"/>
        /// simply returns a symbol for the reference type.
        /// </summary>
        public abstract bool IsNullable { get; }

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
        public bool IsStatic => TypeSymbol.IsStatic;
        public bool IsNullableTypeOrTypeParameter() => TypeSymbol.IsNullableTypeOrTypeParameter();


        public bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes)
        {
            return TypeSymbol.GetUnificationUseSiteDiagnosticRecursive(ref result, owner, ref checkedTypes) ||
                   Symbol.GetUnificationUseSiteDiagnosticRecursive(ref result, this.CustomModifiers, owner, ref checkedTypes);
        }

        public virtual void CheckAllConstraints(ConversionsBase conversions, Location location, DiagnosticBag diagnostics)
        {
            TypeSymbol.CheckAllConstraints(conversions, location, diagnostics);
        }

        public virtual bool IsAtLeastAsVisibleAs(Symbol sym, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return TypeSymbol.IsAtLeastAsVisibleAs(sym, ref useSiteDiagnostics);
        }

        public TypeSymbolWithAnnotations SubstituteType(AbstractTypeMap typeMap)
        {
            var newCustomModifiers = typeMap.SubstituteCustomModifiers(this.CustomModifiers);
            var newTypeWithModifiers = typeMap.SubstituteType(this.TypeSymbol);
            if (!newTypeWithModifiers.Is(this.TypeSymbol) || newCustomModifiers != this.CustomModifiers)
            {
                return DoUpdate(newTypeWithModifiers.TypeSymbol, newCustomModifiers.Concat(newTypeWithModifiers.CustomModifiers));
            }
            else
            {
                return this; // substitution had no effect on the type or modifiers
            }
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
            if (CustomModifiers != customModifiers || TypeSymbol != typeSymbol)
            {
                return DoUpdate(typeSymbol, customModifiers);
            }

            return this;
        }

        protected virtual TypeSymbolWithAnnotations DoUpdate(TypeSymbol typeSymbol, ImmutableArray<CustomModifier> customModifiers)
        {
            return TypeSymbolWithAnnotations.Create(typeSymbol, customModifiers);
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
            public sealed override bool IsNullable => _typeSymbol.IsNullableType();
            public override ImmutableArray<CustomModifier> CustomModifiers => ImmutableArray<CustomModifier>.Empty;

            public override TypeSymbolWithAnnotations WithModifiers(ImmutableArray<CustomModifier> customModifiers)
            {
                if (customModifiers.IsDefaultOrEmpty)
                {
                    return this;
                }

                return new WithCustomModifiers(_typeSymbol, customModifiers);
            }

            public override TypeSymbol AsTypeSymbolOnly()
            {
                return _typeSymbol;
            }

            public override bool Is(TypeSymbol other)
            {
                return _typeSymbol == other;
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
                return _typeSymbol;
            }

            public override bool Is(TypeSymbol other)
            {
                return false; // have custom modifiers
            }
        }
    }
}
