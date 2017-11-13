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
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated and embedded attribute type.
    /// This type has the following properties:
    /// 1) It is non-generic, sealed, internal, non-static class.
    /// 2) It derives from System.Attribute
    /// 3) It has Microsoft.CodeAnalysis.EmbdeddedAttribute
    /// 4) It has System.Runtime.CompilerServices.CompilerGeneratedAttribute
    /// 5) It has a parameter-less constructor
    /// </summary>
    internal sealed class SynthesizedEmbeddedAttributeSymbol : NamedTypeSymbol
    {
        private readonly string _name;
        private readonly NamedTypeSymbol _baseType;
        private readonly NamespaceSymbol _namespace;
        private readonly ImmutableArray<Symbol> _members;
        private readonly ModuleSymbol _module;

        public SynthesizedEmbeddedAttributeSymbol(AttributeDescription description, CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            _name = description.Name;
            _baseType = compilation.GetWellKnownType(WellKnownType.System_Attribute);

            // Report errors in case base type was missing or bad
            Binder.ReportUseSiteDiagnostics(_baseType, diagnostics, Location.None);

            Constructor = new SynthesizedEmbeddedAttributeConstructorSymbol(this);
            _members = ImmutableArray.Create<Symbol>(Constructor);
            _module = compilation.SourceModule;

            _namespace = compilation.SourceModule.GlobalNamespace;
            foreach (var part in description.Namespace.Split('.'))
            {
                _namespace = new MissingNamespaceSymbol(_namespace, part);
            }
        }

        public SynthesizedEmbeddedAttributeConstructorSymbol Constructor { get; private set; }

        public override int Arity => 0;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override NamedTypeSymbol ConstructedFrom => this;

        public override bool MightContainExtensionMethods => false;

        public override string Name => _name;

        public override IEnumerable<string> MemberNames => SpecializedCollections.SingletonEnumerable(Constructor.Name);

        public override Accessibility DeclaredAccessibility => Accessibility.Internal;

        public override TypeKind TypeKind => TypeKind.Class;

        public override Symbol ContainingSymbol => _namespace;

        internal override ModuleSymbol ContainingModule => _module;

        public override AssemblySymbol ContainingAssembly => _module.ContainingAssembly;

        public override NamespaceSymbol ContainingNamespace => _namespace;

        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override bool IsStatic => false;

        internal override bool IsByRefLikeType => false;

        internal override bool IsReadOnly => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => true;

        internal override bool HasTypeArgumentsCustomModifiers => false;

        internal override ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics => ImmutableArray<TypeSymbol>.Empty;

        internal override bool MangleName => false;

        internal override bool HasCodeAnalysisEmbeddedAttribute => true;

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

        public override ImmutableArray<Symbol> GetMembers(string name) => Constructor.Name == name ? _members : ImmutableArray<Symbol>.Empty;

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

            AddSynthesizedAttribute(
                ref attributes,
                moduleBuilder.Compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));

            AddSynthesizedAttribute(
                ref attributes,
                moduleBuilder.SynthesizeEmbeddedAttribute());
        }

        internal sealed class SynthesizedEmbeddedAttributeConstructorSymbol : SynthesizedInstanceConstructor
        {
            public SynthesizedEmbeddedAttributeConstructorSymbol(NamedTypeSymbol containingType)
                : base(containingType)
            {
            }

            internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
            {
                if (ContainingType.BaseType is MissingMetadataTypeSymbol)
                {
                    // System_Attribute is missing. Don't generate anything
                    return;
                }

                var factory = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
                factory.CurrentMethod = this;

                var baseConstructorCall = MethodCompiler.GenerateBaseParameterlessConstructorInitializer(this, diagnostics);
                if (baseConstructorCall == null)
                {
                    // This may happen if Attribute..ctor is not found or is inaccessible
                    return;
                }

                var block = factory.Block(
                    factory.ExpressionStatement(baseConstructorCall),
                    factory.Return());

                factory.CloseMethod(block);
            }
        }
    }
}
