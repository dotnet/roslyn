// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Either a SubstitutedNestedTypeSymbol or a ConstructedNamedTypeSymbol, which share in common that they
    /// have type parameters substituted.
    /// </summary>
    internal abstract class SubstitutedNamedTypeSymbol : NamedTypeSymbol
    {
        private static readonly Func<Symbol, NamedTypeSymbol, Symbol> s_symbolAsMemberFunc = SymbolExtensions.SymbolAsMember;

        private readonly bool _unbound;
        private readonly NamedTypeSymbol _originalDefinition;
        private readonly TypeMap _inputMap;

        // The container of a substituted named type symbol is typically a named type or a namespace. 
        // However, in some error-recovery scenarios it might be some other container. For example,
        // consider "int Foo = 123; Foo<string> x = null;" What is the type of x? We construct an error
        // type symbol of arity one associated with local variable symbol Foo; when we construct
        // that error type symbol with <string>, the resulting substituted named type symbol has
        // the same containing symbol as the local: it is contained in the method.
        private readonly Symbol _newContainer;

        private TypeMap _lazyMap;
        private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;

        // computed on demand
        private int _hashCode;

        // lazily created, does not need to be unique
        private ConcurrentCache<string, ImmutableArray<Symbol>> _lazyMembersByNameCache;

        protected SubstitutedNamedTypeSymbol(Symbol newContainer, TypeMap map, NamedTypeSymbol originalDefinition, NamedTypeSymbol constructedFrom = null, bool unbound = false)
        {
            Debug.Assert(originalDefinition.IsDefinition);
            _originalDefinition = originalDefinition;
            _newContainer = newContainer;
            _inputMap = map;
            _unbound = unbound;

            // if we're substituting to create a new unconstructed type as a member of a constructed type,
            // then we must alpha rename the type parameters.
            if ((object)constructedFrom != null)
            {
                Debug.Assert(ReferenceEquals(constructedFrom.ConstructedFrom, constructedFrom));
                _lazyTypeParameters = constructedFrom.TypeParameters;
                _lazyMap = map;
            }
        }

        public sealed override bool IsUnboundGenericType
        {
            get
            {
                return _unbound;
            }
        }

        private TypeMap Map
        {
            get
            {
                EnsureMapAndTypeParameters();
                return _lazyMap;
            }
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                EnsureMapAndTypeParameters();
                return _lazyTypeParameters;
            }
        }

        private void EnsureMapAndTypeParameters()
        {
            if (!_lazyTypeParameters.IsDefault)
            {
                return;
            }

            ImmutableArray<TypeParameterSymbol> typeParameters;

            // We're creating a new unconstructed Method from another; alpha-rename type parameters.
            var newMap = _inputMap.WithAlphaRename(_originalDefinition, this, out typeParameters);

            var prevMap = Interlocked.CompareExchange(ref _lazyMap, newMap, null);
            if (prevMap != null)
            {
                // There is a race with another thread who has already set the map
                // need to ensure that typeParameters, matches the map
                typeParameters = prevMap.SubstituteTypeParameters(_originalDefinition.TypeParameters);
            }

            ImmutableInterlocked.InterlockedCompareExchange(ref _lazyTypeParameters, typeParameters, default(ImmutableArray<TypeParameterSymbol>));
            Debug.Assert(_lazyTypeParameters != null);
        }

        public sealed override Symbol ContainingSymbol
        {
            get { return _newContainer; }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _newContainer as NamedTypeSymbol;
            }
        }

        public sealed override string Name
        {
            get { return _originalDefinition.Name; }
        }

        internal sealed override bool MangleName
        {
            get { return _originalDefinition.MangleName; }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal sealed override bool HasSpecialName
        {
            get { return _originalDefinition.HasSpecialName; }
        }

        public sealed override int Arity
        {
            get { return _originalDefinition.Arity; }
        }

        public sealed override Accessibility DeclaredAccessibility
        {
            get { return _originalDefinition.DeclaredAccessibility; }
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get { return _originalDefinition.Locations; }
        }

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return _originalDefinition.DeclaringSyntaxReferences; }
        }

        public sealed override SymbolKind Kind
        {
            get { return _originalDefinition.Kind; }
        }

        public sealed override NamedTypeSymbol OriginalDefinition
        {
            get { return _originalDefinition; }
        }

        public sealed override TypeKind TypeKind
        {
            get { return _originalDefinition.TypeKind; }
        }

        internal sealed override bool IsInterface
        {
            get { return _originalDefinition.IsInterface; }
        }

        public sealed override bool IsStatic
        {
            get { return _originalDefinition.IsStatic; }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get
            {
                return _originalDefinition.IsImplicitlyDeclared;
            }
        }

        internal sealed override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
        {
            return _unbound ? null : Map.SubstituteNamedType(OriginalDefinition.GetDeclaredBaseType(basesBeingResolved));
        }

        internal sealed override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            return _unbound ? ImmutableArray<NamedTypeSymbol>.Empty : Map.SubstituteNamedTypes(OriginalDefinition.GetDeclaredInterfaces(basesBeingResolved));
        }

        internal sealed override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                return _unbound ? null : Map.SubstituteNamedType(OriginalDefinition.BaseTypeNoUseSiteDiagnostics);
            }
        }

        internal sealed override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved)
        {
            return _unbound ? ImmutableArray<NamedTypeSymbol>.Empty : Map.SubstituteNamedTypes(OriginalDefinition.InterfacesNoUseSiteDiagnostics(basesBeingResolved));
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            throw ExceptionUtilities.Unreachable;
        }

        protected sealed override ImmutableArray<NamedTypeSymbol> MakeAllInterfaces()
        {
            // Because declared types will have been checked for "uniqueness of implemented interfaces" (C# 4 spec, 13.4.2),
            // we are guaranteed that none of these substitutions collide in a correct program.  Consequently, we can simply
            // substitute the original interfaces.
            return _unbound ? ImmutableArray<NamedTypeSymbol>.Empty : Map.SubstituteNamedTypes(OriginalDefinition.AllInterfacesNoUseSiteDiagnostics);
        }

        public sealed override IEnumerable<string> MemberNames
        {
            get
            {
                return _unbound ? new List<string>(GetTypeMembersUnordered().Select(s => s.Name).Distinct()) : _originalDefinition.MemberNames;
            }
        }

        public sealed override bool IsSealed
        {
            get { return _originalDefinition.IsSealed; }
        }

        public sealed override bool IsAbstract
        {
            get { return _originalDefinition.IsAbstract; }
        }

        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _originalDefinition.GetAttributes();
        }

        public sealed override bool MightContainExtensionMethods
        {
            get { return _originalDefinition.MightContainExtensionMethods; }
        }

        internal sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembersUnordered()
        {
            return _originalDefinition.GetTypeMembersUnordered().SelectAsArray((t, self) => t.AsMember(self), this);
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return _originalDefinition.GetTypeMembers().SelectAsArray((t, self) => t.AsMember(self), this);
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return _originalDefinition.GetTypeMembers(name).SelectAsArray((t, self) => t.AsMember(self), this);
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return _originalDefinition.GetTypeMembers(name, arity).SelectAsArray((t, self) => t.AsMember(self), this);
        }

        public sealed override ImmutableArray<Symbol> GetMembers()
        {
            var builder = ArrayBuilder<Symbol>.GetInstance();

            if (_unbound)
            {
                // Preserve order of members.
                foreach (var t in _originalDefinition.GetMembers())
                {
                    if (t.Kind == SymbolKind.NamedType)
                    {
                        builder.Add(((NamedTypeSymbol)t).AsMember(this));
                    }
                }
            }
            else
            {
                foreach (var t in _originalDefinition.GetMembers())
                {
                    builder.Add(t.SymbolAsMember(this));
                }
            }

            return builder.ToImmutableAndFree();
        }

        internal sealed override ImmutableArray<Symbol> GetMembersUnordered()
        {
            var builder = ArrayBuilder<Symbol>.GetInstance();

            if (_unbound)
            {
                foreach (var t in _originalDefinition.GetMembersUnordered())
                {
                    if (t.Kind == SymbolKind.NamedType)
                    {
                        builder.Add(((NamedTypeSymbol)t).AsMember(this));
                    }
                }
            }
            else
            {
                foreach (var t in _originalDefinition.GetMembersUnordered())
                {
                    builder.Add(t.SymbolAsMember(this));
                }
            }

            return builder.ToImmutableAndFree();
        }

        public sealed override ImmutableArray<Symbol> GetMembers(string name)
        {
            if (_unbound) return StaticCast<Symbol>.From(GetTypeMembers(name));

            ImmutableArray<Symbol> result;
            var cache = _lazyMembersByNameCache;
            if (cache != null && cache.TryGetValue(name, out result))
            {
                return result;
            }

            return GetMembersWorker(name);
        }

        private ImmutableArray<Symbol> GetMembersWorker(string name)
        {
            var originalMembers = _originalDefinition.GetMembers(name);
            if (originalMembers.IsDefaultOrEmpty)
            {
                return originalMembers;
            }

            var builder = ArrayBuilder<Symbol>.GetInstance(originalMembers.Length);
            foreach (var t in originalMembers)
            {
                builder.Add(t.SymbolAsMember(this));
            }

            var substitutedMembers = builder.ToImmutableAndFree();

            // cache of size 8 seems reasonable here.
            // considering that substituted methods have about 10 reference fields,
            // reusing just one may make the cache profitable.
            var cache = _lazyMembersByNameCache ??
                        (_lazyMembersByNameCache = new ConcurrentCache<string, ImmutableArray<Symbol>>(8));

            cache.TryAdd(name, substitutedMembers);
            return substitutedMembers;
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            return _unbound
                ? GetMembers()
                : _originalDefinition.GetEarlyAttributeDecodingMembers().SelectAsArray(s_symbolAsMemberFunc, this);
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            if (_unbound) return GetMembers(name);

            var builder = ArrayBuilder<Symbol>.GetInstance();
            foreach (var t in _originalDefinition.GetEarlyAttributeDecodingMembers(name))
            {
                builder.Add(t.SymbolAsMember(this));
            }

            return builder.ToImmutableAndFree();
        }

        public sealed override NamedTypeSymbol EnumUnderlyingType
        {
            get
            {
                return _originalDefinition.EnumUnderlyingType;
            }
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                _hashCode = this.ComputeHashCode();
            }

            return _hashCode;
        }

        internal sealed override TypeMap TypeSubstitution
        {
            get { return this.Map; }
        }

        internal sealed override bool IsComImport
        {
            get { return _originalDefinition.IsComImport; }
        }

        internal sealed override NamedTypeSymbol ComImportCoClass
        {
            get { return _originalDefinition.ComImportCoClass; }
        }

        internal sealed override bool ShouldAddWinRTMembers
        {
            get { return _originalDefinition.ShouldAddWinRTMembers; }
        }

        internal sealed override bool IsWindowsRuntimeImport
        {
            get { return _originalDefinition.IsWindowsRuntimeImport; }
        }

        internal sealed override TypeLayout Layout
        {
            get { return _originalDefinition.Layout; }
        }

        internal override CharSet MarshallingCharSet
        {
            get { return _originalDefinition.MarshallingCharSet; }
        }

        internal sealed override bool IsSerializable
        {
            get { return _originalDefinition.IsSerializable; }
        }

        internal sealed override bool HasDeclarativeSecurity
        {
            get { return _originalDefinition.HasDeclarativeSecurity; }
        }

        internal sealed override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            return _originalDefinition.GetSecurityInformation();
        }

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return _originalDefinition.GetAppliedConditionalSymbols();
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return _originalDefinition.ObsoleteAttributeData; }
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return _originalDefinition.GetAttributeUsageInfo();
        }
    }
}
