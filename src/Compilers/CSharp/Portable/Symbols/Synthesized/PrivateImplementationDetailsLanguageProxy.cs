// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Roslyn.Utilities;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // A language-specific Symbol proxy that takes the place of the PrivateImplementationDetails type.
    internal class PrivateImplementationDetailsLanguageProxy : NamedTypeSymbol
    {
        private readonly CSharpCompilation _compilation;
        private readonly PrivateImplementationDetails _privateImplDetails;

        public PrivateImplementationDetailsLanguageProxy(CSharpCompilation compilation, PrivateImplementationDetails privateImplDetails)
        {
            this._compilation = compilation;
            this._privateImplDetails = privateImplDetails;
        }

        public override int Arity => 0;
        public override NamedTypeSymbol ConstructedFrom => this;
        public override Symbol ContainingSymbol => _compilation.GlobalNamespace;
        public override Accessibility DeclaredAccessibility => Accessibility.Internal;
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;
        public override bool IsAbstract => false;
        public override bool IsSealed => true;
        public override bool IsStatic => true;
        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;
        public override IEnumerable<string> MemberNames => Enumerable.Empty<string>();
        public override bool MightContainExtensionMethods => false;
        public override string Name => _privateImplDetails.Name;
        public override TypeKind TypeKind => TypeKind.Class;
        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;
        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => _compilation.ObjectType;
        internal override bool HasDeclarativeSecurity => false;
        internal override bool HasSpecialName => true;
        internal override bool HasTypeArgumentsCustomModifiers => false;
        internal override bool IsComImport => false;
        internal override bool IsInterface => false;
        internal override bool IsSerializable => false;
        internal override bool IsWindowsRuntimeImport => false;
        internal override TypeLayout Layout => new TypeLayout(LayoutKind.Auto, 1, 0);
        internal override bool MangleName => false;
        internal override CharSet MarshallingCharSet => CharSet.Unicode;
        internal override ObsoleteAttributeData ObsoleteAttributeData => null;
        internal override bool ShouldAddWinRTMembers => false;
        internal override ImmutableArray<ImmutableArray<CustomModifier>> TypeArgumentsCustomModifiers => ImmutableArray<ImmutableArray<CustomModifier>>.Empty;
        internal override ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics => ImmutableArray<TypeSymbol>.Empty;
        public override ImmutableArray<Symbol> GetMembers() => ImmutableArray<Symbol>.Empty;
        public override ImmutableArray<Symbol> GetMembers(string name) => ImmutableArray<Symbol>.Empty;
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => ImmutableArray<NamedTypeSymbol>.Empty;
        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity) => ImmutableArray<NamedTypeSymbol>.Empty;
        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;
        internal override AttributeUsageInfo GetAttributeUsageInfo() => AttributeUsageInfo.Default;
        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved) => _compilation.ObjectType;
        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved) => ImmutableArray<NamedTypeSymbol>.Empty;
        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => ImmutableArray<Symbol>.Empty;
        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => ImmutableArray<Symbol>.Empty;
        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => Enumerable.Empty<FieldSymbol>();
        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => ImmutableArray<NamedTypeSymbol>.Empty;
        internal override IEnumerable<SecurityAttribute> GetSecurityInformation() => Enumerable.Empty<SecurityAttribute>();
        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved = null) => ImmutableArray<NamedTypeSymbol>.Empty;
    }
}