// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // PROTOTYPE(NullableReferenceTypes): Merge with implementation from
    // `ref readonly`. The one addition from this implementation is support
    // for additional constructors with parameters.
    internal sealed class SynthesizedEmbeddedAttributeSymbol : NamedTypeSymbol
    {
        private readonly string _name;
        private readonly NamedTypeSymbol _baseType;
        private readonly NamespaceSymbol _namespace;
        private readonly ImmutableArray<Symbol> _members;
        private readonly ModuleSymbol _module;

        public SynthesizedEmbeddedAttributeSymbol(
            CSharpCompilation compilation,
            string @namespace,
            string name,
            Func<CSharpCompilation, NamedTypeSymbol, DiagnosticBag, ImmutableArray<Symbol>> getMembers,
            DiagnosticBag diagnostics)
        {
            _name = name;
            _baseType = compilation.GetWellKnownType(WellKnownType.System_Attribute);
            Binder.ReportUseSiteDiagnostics(_baseType, diagnostics, Location.None);
            
            _module = compilation.SourceModule;
            _namespace = _module.GlobalNamespace;
            foreach (var part in @namespace.Split('.'))
            {
                _namespace = new MissingNamespaceSymbol(_namespace, part);
            }
            _members = getMembers(compilation, this, diagnostics);
        }

        public override int Arity => 0;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override NamedTypeSymbol ConstructedFrom => this;

        public override bool MightContainExtensionMethods => false;

        public override string Name => _name;

        public override IEnumerable<string> MemberNames => _members.Select(m => m.Name);

        public override Accessibility DeclaredAccessibility => Accessibility.Internal;

        public override TypeKind TypeKind => TypeKind.Class;

        public override Symbol ContainingSymbol => _namespace;

        internal override ModuleSymbol ContainingModule => _module;

        public override AssemblySymbol ContainingAssembly => _module.ContainingAssembly;

        public override NamespaceSymbol ContainingNamespace => _namespace;

        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override bool IsStatic => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => true;

        internal override bool MangleName => false;

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

        internal override ImmutableArray<TypeSymbolWithAnnotations> TypeArgumentsNoUseSiteDiagnostics => throw new NotImplementedException();

        public override ImmutableArray<Symbol> GetMembers() => _members;

        public override ImmutableArray<Symbol> GetMembers(string name) => name == WellKnownMemberNames.InstanceConstructorName ? _members : ImmutableArray<Symbol>.Empty;

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
    }

    internal sealed class SynthesizedEmbeddedAttributeConstructorSymbol : SynthesizedInstanceConstructor
    {
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        internal SynthesizedEmbeddedAttributeConstructorSymbol(
            NamedTypeSymbol containingType,
            Func<MethodSymbol, ImmutableArray<ParameterSymbol>> getParameters) :
            base(containingType)
        {
            _parameters = getParameters(this);
        }

        public override ImmutableArray<ParameterSymbol> Parameters => _parameters;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var factory = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            factory.CurrentMethod = this;

            var baseType = ContainingType.BaseType;
            var baseConstructor = baseType.InstanceConstructors.FirstOrDefault(m => m.ParameterCount == 0);
            if ((object)baseConstructor == null)
            {
                diagnostics.Add(ErrorCode.ERR_BadCtorArgCount, NoLocation.Singleton, baseType, 0);
                return;
            }

            var baseConstructorCall = factory.Call(factory.This(), baseConstructor);

            var block = factory.Block(
                factory.ExpressionStatement(baseConstructorCall),
                factory.Return());

            factory.CloseMethod(block);
        }
    }
}
