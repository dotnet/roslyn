// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private static readonly Func<Symbol, NamedTypeSymbol, Symbol> SymbolAsMemberFunc = SymbolExtensions.SymbolAsMember;

        private readonly bool unbound = false;
        private readonly NamedTypeSymbol originalDefinition;
        private readonly TypeMap inputMap;

        // The container of a substituted named type symbol is typically a named type or a namespace. 
        // However, in some error-recovery scenarios it might be some other container. For example,
        // consider "int Foo = 123; Foo<string> x = null;" What is the type of x? We construct an error
        // type symbol of arity one associated with local variable symbol Foo; when we construct
        // that error type symbol with <string>, the resulting substituted named type symbol has
        // the same containing symbol as the local: it is contained in the method.
        private readonly Symbol newContainer;

        private TypeMap lazyMap;
        private ImmutableArray<TypeParameterSymbol> lazyTypeParameters;

        // computed on demand
        private int hashCode;

        // lazily created, does not need to be unique
        private ConcurrentCache<string, ImmutableArray<Symbol>> lazyMembersByNameCache;

        protected SubstitutedNamedTypeSymbol(Symbol newContainer, TypeMap map, NamedTypeSymbol originalDefinition, NamedTypeSymbol constructedFrom = null, bool unbound = false)
        {
            Debug.Assert(originalDefinition.IsDefinition);
            this.originalDefinition = originalDefinition;
            this.newContainer = newContainer;
            this.inputMap = map;
            this.unbound = unbound;

            // if we're substituting to create a new unconstructed type as a member of a constructed type,
            // then we must alpha rename the type parameters.
            if ((object)constructedFrom != null)
            {
                Debug.Assert(ReferenceEquals(constructedFrom.ConstructedFrom, constructedFrom));
                this.lazyTypeParameters = constructedFrom.TypeParameters;
                this.lazyMap = map;
            }
        }

        public sealed override bool IsUnboundGenericType
        {
            get
            {
                return unbound;
            }
        }

        private TypeMap Map
        {
            get
            {
                EnsureMapAndTypeParameters();
                return lazyMap;
            }
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                EnsureMapAndTypeParameters();
                return this.lazyTypeParameters;
            }
        }

        private void EnsureMapAndTypeParameters()
        {
            if (!this.lazyTypeParameters.IsDefault)
            {
                return;
            }

            ImmutableArray<TypeParameterSymbol> typeParameters;

            // We're creating a new unconstructed Method from another; alpha-rename type parameters.
            var newMap = this.inputMap.WithAlphaRename(this.originalDefinition, this, out typeParameters);

            var prevMap = Interlocked.CompareExchange(ref this.lazyMap, newMap, null);
            if (prevMap != null)
            {
                // There is a race with another thread who has already set the map
                // need to ensure that typeParameters, matches the map
                typeParameters = prevMap.SubstituteTypeParameters(this.originalDefinition.TypeParameters);
            }

            ImmutableInterlocked.InterlockedCompareExchange(ref this.lazyTypeParameters, typeParameters, default(ImmutableArray<TypeParameterSymbol>));
            Debug.Assert(this.lazyTypeParameters != null);
        }

        public sealed override Symbol ContainingSymbol
        {
            get { return newContainer; }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return newContainer as NamedTypeSymbol;
            }
        }

        public sealed override string Name
        {
            get { return originalDefinition.Name; }
        }

        internal sealed override bool MangleName
        {
            get { return originalDefinition.MangleName; }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return originalDefinition.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal sealed override bool HasSpecialName
        {
            get { return originalDefinition.HasSpecialName; }
        }

        public sealed override int Arity
        {
            get { return originalDefinition.Arity; }
        }

        public sealed override Accessibility DeclaredAccessibility
        {
            get { return originalDefinition.DeclaredAccessibility; }
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get { return originalDefinition.Locations; }
        }

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return originalDefinition.DeclaringSyntaxReferences; }
        }

        public sealed override SymbolKind Kind
        {
            get { return originalDefinition.Kind; }
        }

        public sealed override NamedTypeSymbol OriginalDefinition
        {
            get { return originalDefinition; }
        }

        public sealed override TypeKind TypeKind
        {
            get { return originalDefinition.TypeKind; }
        }

        internal sealed override bool IsInterface
        {
            get { return originalDefinition.IsInterface; }
        }

        public sealed override bool IsStatic
        {
            get { return originalDefinition.IsStatic; }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get
            {
                return this.originalDefinition.IsImplicitlyDeclared;
            }
        }

        internal sealed override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
        {
            return unbound ? null : Map.SubstituteNamedType(OriginalDefinition.GetDeclaredBaseType(basesBeingResolved));
        }

        internal sealed override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            return unbound ? ImmutableArray<NamedTypeSymbol>.Empty : Map.SubstituteNamedTypes(OriginalDefinition.GetDeclaredInterfaces(basesBeingResolved));
        }

        internal sealed override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                return unbound ? null : Map.SubstituteNamedType(OriginalDefinition.BaseTypeNoUseSiteDiagnostics);
            }
        }

        internal sealed override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics
        {
            get
            {
                return unbound ? ImmutableArray<NamedTypeSymbol>.Empty : Map.SubstituteNamedTypes(OriginalDefinition.InterfacesNoUseSiteDiagnostics);
            }
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
            return unbound ? ImmutableArray<NamedTypeSymbol>.Empty : Map.SubstituteNamedTypes(OriginalDefinition.AllInterfacesNoUseSiteDiagnostics);
        }

        public sealed override IEnumerable<string> MemberNames
        {
            get
            {
                return unbound ? new List<string>(GetTypeMembersUnordered().Select(s => s.Name).Distinct()) : originalDefinition.MemberNames;
            }
        }

        public sealed override bool IsSealed
        {
            get { return originalDefinition.IsSealed; }
        }

        public sealed override bool IsAbstract
        {
            get { return originalDefinition.IsAbstract; }
        }

        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.originalDefinition.GetAttributes();
        }

        public sealed override bool MightContainExtensionMethods
        {
            get { return originalDefinition.MightContainExtensionMethods; }
        }

        internal sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembersUnordered()
        {
            return originalDefinition.GetTypeMembersUnordered().SelectAsArray((t, self) => t.AsMember(self), this);
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return originalDefinition.GetTypeMembers().SelectAsArray((t, self) => t.AsMember(self), this);
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return originalDefinition.GetTypeMembers(name).SelectAsArray((t, self) => t.AsMember(self), this);
        }

        public sealed override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return originalDefinition.GetTypeMembers(name, arity).SelectAsArray((t, self) => t.AsMember(self), this);
        }

        public sealed override ImmutableArray<Symbol> GetMembers()
        {
            var builder = ArrayBuilder<Symbol>.GetInstance();

            if (unbound)
            {
                // Preserve order of members.
                foreach (var t in originalDefinition.GetMembers())
                {
                    if (t.Kind == SymbolKind.NamedType)
                    {
                        builder.Add(((NamedTypeSymbol)t).AsMember(this));
                    }
                }
            }
            else
            {
                foreach (var t in originalDefinition.GetMembers())
                {
                    builder.Add(t.SymbolAsMember(this));
                }
            }

            return builder.ToImmutableAndFree();
        }

        internal sealed override ImmutableArray<Symbol> GetMembersUnordered()
        {
            var builder = ArrayBuilder<Symbol>.GetInstance();

            if (unbound)
            {
                foreach (var t in originalDefinition.GetMembersUnordered())
                {
                    if (t.Kind == SymbolKind.NamedType)
                    {
                        builder.Add(((NamedTypeSymbol)t).AsMember(this));
                    }
                }
            }
            else
            {
                foreach (var t in originalDefinition.GetMembersUnordered())
                {
                    builder.Add(t.SymbolAsMember(this));
                }
            }

            return builder.ToImmutableAndFree();
        }

        public sealed override ImmutableArray<Symbol> GetMembers(string name)
        {
            if (unbound) return StaticCast<Symbol>.From(GetTypeMembers(name));

            ImmutableArray<Symbol> result;
            var cache = this.lazyMembersByNameCache;
            if (cache != null && cache.TryGetValue(name, out result))
            {
                return result;
            }

            return GetMembersWorker(name);
        }

        private ImmutableArray<Symbol> GetMembersWorker(string name)
        {
            var originalMembers = originalDefinition.GetMembers(name);
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
            var cache = this.lazyMembersByNameCache ??
                        (this.lazyMembersByNameCache = new ConcurrentCache<string, ImmutableArray<Symbol>>(8));

            cache.TryAdd(name, substitutedMembers);
            return substitutedMembers;
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            return unbound
                ? GetMembers()
                : originalDefinition.GetEarlyAttributeDecodingMembers().SelectAsArray(SymbolAsMemberFunc, this);
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            if (unbound) return GetMembers(name);

            var builder = ArrayBuilder<Symbol>.GetInstance();
            foreach (var t in originalDefinition.GetEarlyAttributeDecodingMembers(name))
            {
                builder.Add(t.SymbolAsMember(this));
            }

            return builder.ToImmutableAndFree();
        }

        public sealed override NamedTypeSymbol EnumUnderlyingType
        {
            get
            {
                return originalDefinition.EnumUnderlyingType;
            }
        }

        public override int GetHashCode()
        {
            if (this.hashCode == 0)
            {
                this.hashCode = this.ComputeHashCode();
            }

            return this.hashCode;
        }

        internal sealed override TypeMap TypeSubstitution
        {
            get { return this.Map; }
        }

        internal sealed override bool IsComImport
        {
            get { return originalDefinition.IsComImport; }
        }

        internal sealed override NamedTypeSymbol ComImportCoClass
        {
            get { return originalDefinition.ComImportCoClass; }
        }

        internal sealed override bool ShouldAddWinRTMembers
        {
            get { return originalDefinition.ShouldAddWinRTMembers; }
        }

        internal sealed override bool IsWindowsRuntimeImport
        {
            get { return originalDefinition.IsWindowsRuntimeImport; }
        }

        internal sealed override TypeLayout Layout
        {
            get { return originalDefinition.Layout; }
        }

        internal override CharSet MarshallingCharSet
        {
            get { return originalDefinition.MarshallingCharSet; }
        }

        internal sealed override bool IsSerializable
        {
            get { return originalDefinition.IsSerializable; }
        }

        internal sealed override bool HasDeclarativeSecurity
        {
            get { return originalDefinition.HasDeclarativeSecurity; }
        }

        internal sealed override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            return originalDefinition.GetSecurityInformation();
        }

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return originalDefinition.GetAppliedConditionalSymbols();
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return originalDefinition.ObsoleteAttributeData; }
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return originalDefinition.GetAttributeUsageInfo();
        }
    }
}
