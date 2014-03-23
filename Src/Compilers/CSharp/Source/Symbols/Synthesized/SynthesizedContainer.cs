// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Debug = System.Diagnostics.Debug;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A container synthesized for a lambda, iterator method, async method, or dynamic-sites.
    /// </summary>
    internal class SynthesizedContainer : NamedTypeSymbol
    {
        protected readonly NamespaceOrTypeSymbol containingSymbol;
        protected readonly string name;
        internal readonly TypeMap TypeMap;
        protected readonly ImmutableArray<TypeParameterSymbol> typeParameters;
        private readonly TypeKind typeKind;

        internal SynthesizedContainer(MethodSymbol topLevelMethod, string name, TypeKind typeKind)
        {
            this.typeKind = typeKind;
            this.containingSymbol = topLevelMethod.ContainingType;
            this.name = name;
            this.TypeMap = TypeMap.Empty.WithAlphaRename(topLevelMethod, this, out this.typeParameters);
        }

        internal SynthesizedContainer(NamedTypeSymbol containingType, string name, TypeKind typeKind)
        {
            this.typeKind = typeKind;
            this.containingSymbol = containingType;
            this.name = name;
            this.TypeMap = TypeMap.Empty;
            this.typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
        }

        /// <summary>
        /// Used for <see cref="SynthesizedDelegateSymbol"/> construction.
        /// </summary>
        protected SynthesizedContainer(NamespaceOrTypeSymbol containingSymbol, string name, int parameterCount, bool returnsVoid)
        {
            var typeParameters = new TypeParameterSymbol[parameterCount + (returnsVoid ? 0 : 1)];
            for (int i = 0; i < parameterCount; i++)
            {
                typeParameters[i] = new AnonymousTypeManager.AnonymousTypeParameterSymbol(this, i, "T" + (i + 1));
            }

            if (!returnsVoid)
            {
                typeParameters[parameterCount] = new AnonymousTypeManager.AnonymousTypeParameterSymbol(this, parameterCount, "TResult");
            }

            this.containingSymbol = containingSymbol;
            this.name = name;
            this.TypeMap = TypeMap.Empty;
            this.typeParameters = typeParameters.AsImmutableOrNull();
        }

        internal virtual MethodSymbol Constructor
        {
            get { return null; }
        }

        public override TypeKind TypeKind
        {
            get { return this.typeKind; }
        }

        internal override bool IsInterface
        {
            get { return this.TypeKind == TypeKind.Interface; }
        }

        internal override void AddSynthesizedAttributes(ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(ref attributes);

            if (containingSymbol.Kind == SymbolKind.NamedType && containingSymbol.IsImplicitlyDeclared)
            {
                return;
            }

            var compilation = containingSymbol.DeclaringCompilation;

            // this can only happen if frame is not nested in a source type/namespace (so far we do not do this)
            // if this happens for whatever reason, we do not need "CompilerGenerated" anyways
            Debug.Assert(compilation != null, "SynthesizedClass is not contained in a source module?");

            AddSynthesizedAttribute(ref attributes, compilation.SynthesizeAttribute(
                WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return typeParameters; }
        }

        public override Symbol ContainingSymbol
        {
            get { return this.containingSymbol; }
        }

        public override string Name
        {
            get { return name; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray<Location>.Empty; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return ImmutableArray<SyntaxReference>.Empty; }
        }

        public override IEnumerable<string> MemberNames
        {
            get { return SpecializedCollections.EmptyEnumerable<string>(); }
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get { return this; }
        }

        public override bool IsSealed
        {
            get { return true; }
        }

        public override bool IsAbstract
        {
            get { return (object)Constructor == null; }
        }

        internal override ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics
        {
            get { return StaticCast<TypeSymbol>.From(TypeParameters); }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            Symbol constructor = (Symbol)this.Constructor;
            return (object)constructor == null ? ImmutableArray<Symbol>.Empty : ImmutableArray.Create<Symbol>(constructor);
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            var ctor = Constructor;
            return ((object)ctor != null && name == ctor.Name) ? ImmutableArray.Create<Symbol>(ctor) : ImmutableArray<Symbol>.Empty;
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            foreach (var m in this.GetMembers())
            {
                switch (m.Kind)
                {
                    case SymbolKind.Field:
                        yield return (FieldSymbol)m;
                        break;
                }
            }
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            return this.GetMembersUnordered();
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            return this.GetMembers(name);
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Private; }
        }

        public override bool IsStatic
        {
            get { return false; }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics
        {
            get { return ImmutableArray<NamedTypeSymbol>.Empty; }
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            return CalculateInterfacesToEmit();
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get { return ContainingAssembly.GetSpecialType(this.typeKind == TypeKind.Struct ? SpecialType.System_ValueType : SpecialType.System_Object); }
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
        {
            return BaseTypeNoUseSiteDiagnostics;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            return InterfacesNoUseSiteDiagnostics;
        }

        public override bool MightContainExtensionMethods
        {
            get { return false; }
        }

        public override int Arity
        {
            get { return TypeParameters.Length; }
        }

        internal override bool MangleName
        {
            get { return Arity > 0; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal override bool ShouldAddWinRTMembers
        {
            get { return false; }
        }

        internal override bool IsWindowsRuntimeImport
        {
            get { return false; }
        }

        internal override bool IsComImport
        {
            get { return false; }
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal override CharSet MarshallingCharSet
        {
            get { return DefaultMarshallingCharSet; }
        }

        internal override bool IsSerializable
        {
            get { return false; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return default(AttributeUsageInfo);
        }

        internal override TypeLayout Layout
        {
            get { return default(TypeLayout); }
        }

        internal override bool HasSpecialName
        {
            get { return false; }
        }
    }
}
