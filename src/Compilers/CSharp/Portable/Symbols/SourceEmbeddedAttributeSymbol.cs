// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated and embedded attribute type.
    /// This type has the following properties:
    /// 1) It is non-generic, sealed, non-static class.
    /// 2) It implements System.Attribute
    /// 3) It has Microsoft.CodeAnalysis.EmbdeddedAttribute
    /// 4) It has System.Runtime.CompilerServices.CompilerGeneratedAttribute
    /// 5) It has a parameter-less constructor
    /// </summary>
    internal sealed class SourceEmbeddedAttributeSymbol : NamedTypeSymbol
    {
        private string _name;

        private NamedTypeSymbol _baseType;
        private MissingNamespaceSymbol _namespace;
        private ImmutableArray<Symbol> _members;

        public SourceEmbeddedAttributeSymbol(WellKnownType wellKnownType, CSharpCompilation compilation)
        {
            var nameParts = WellKnownTypes.GetMetadataName(wellKnownType).Split('.');
            Debug.Assert(nameParts.Length > 1, "Attribute cannot be in global namespace?");

            _name = nameParts.Last();
            _baseType = compilation.GetWellKnownType(WellKnownType.System_Attribute);

            Constructor = new SynthesizedInstanceConstructor(this);
            _members = ImmutableArray.Create<Symbol>(Constructor);

            _namespace = new MissingNamespaceSymbol(compilation.GlobalNamespace, nameParts[0]);
            for (int i = 1; i + 1 < nameParts.Length; i++)
            {
                _namespace = new MissingNamespaceSymbol(_namespace, nameParts[i]);
            }
        }

        public SynthesizedInstanceConstructor Constructor { get; private set; }

        public override int Arity => 0;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override NamedTypeSymbol ConstructedFrom => this;

        public override bool MightContainExtensionMethods => false;

        public override string Name => _name;

        public override IEnumerable<string> MemberNames => _members.Select(member => member.Name);

        public override Accessibility DeclaredAccessibility => Accessibility.Internal;

        public override TypeKind TypeKind => TypeKind.Class;

        public override Symbol ContainingSymbol => _namespace;

        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override bool IsStatic => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => true;

        internal override bool HasTypeArgumentsCustomModifiers => false;

        internal override ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics => ImmutableArray<TypeSymbol>.Empty;

        internal override bool MangleName => false;

        internal override bool HasEmbeddedAttribute => true;

        internal override bool HasSpecialName => false;

        internal override bool IsComImport => false;

        internal override bool IsWindowsRuntimeImport => false;

        internal override bool ShouldAddWinRTMembers => false;

        internal override bool IsSerializable => false;

        internal override TypeLayout Layout => default(TypeLayout);

        internal override CharSet MarshallingCharSet => DefaultMarshallingCharSet;

        internal override bool HasDeclarativeSecurity => false;

        internal override bool IsInterface => false;

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => _baseType;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        public override ImmutableArray<Symbol> GetMembers() => _members;

        public override ImmutableArray<Symbol> GetMembers(string name) => _members.Where(member => member.Name == name).ToImmutableArray();

        public override ImmutableArray<CustomModifier> GetTypeArgumentCustomModifiers(int ordinal) => ImmutableArray<CustomModifier>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;

        internal override AttributeUsageInfo GetAttributeUsageInfo() => AttributeUsageInfo.Default;

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved) => _baseType;

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved) => ImmutableArray<NamedTypeSymbol>.Empty;
        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => GetMembers();

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => GetMembers(name);

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => SpecializedCollections.EmptyEnumerable<FieldSymbol>();

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation() => null;

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved = null) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeEmbeddedAttribute());
        }
    }
}
