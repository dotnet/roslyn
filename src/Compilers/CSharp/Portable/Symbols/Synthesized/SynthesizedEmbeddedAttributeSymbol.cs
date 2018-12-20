// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a compiler generated and embedded attribute type.
    /// This type has the following properties:
    /// 1) It is non-generic, sealed, internal, non-static class.
    /// 2) It derives from System.Attribute
    /// 3) It has Microsoft.CodeAnalysis.EmbeddedAttribute
    /// 4) It has System.Runtime.CompilerServices.CompilerGeneratedAttribute
    /// 5) It has a parameter-less constructor
    /// </summary>
    internal class SynthesizedEmbeddedAttributeSymbol : NamedTypeSymbol
    {
        private readonly string _name;
        private readonly NamedTypeSymbol _baseType;
        private readonly NamespaceSymbol _namespace;
        private readonly ImmutableArray<MethodSymbol> _constructors;
        private readonly ModuleSymbol _module;

        public SynthesizedEmbeddedAttributeSymbol(
            AttributeDescription description,
            CSharpCompilation compilation,
            Func<CSharpCompilation, NamedTypeSymbol, DiagnosticBag, ImmutableArray<MethodSymbol>> getConstructors,
            DiagnosticBag diagnostics)
        {
            _name = description.Name;
            _baseType = MakeBaseType(compilation, diagnostics);

            if (getConstructors != null)
            {
                _constructors = getConstructors(compilation, this, diagnostics);
            }
            else
            {
                _constructors = ImmutableArray.Create<MethodSymbol>(new SynthesizedEmbeddedAttributeConstructorSymbol(this, m => ImmutableArray<ParameterSymbol>.Empty));
            }

            Debug.Assert(_constructors.Length == description.Signatures.Length);

            _module = compilation.SourceModule;

            _namespace = _module.GlobalNamespace;
            foreach (var part in description.Namespace.Split('.'))
            {
                _namespace = new MissingNamespaceSymbol(_namespace, part);
            }
        }

        public SynthesizedEmbeddedAttributeSymbol(
            AttributeDescription description,
            NamespaceSymbol containingNamespace,
            CSharpCompilation compilation,
            Func<CSharpCompilation, NamedTypeSymbol, DiagnosticBag, ImmutableArray<MethodSymbol>> getConstructors,
            DiagnosticBag diagnostics)
        {
            _name = description.Name;
            _baseType = MakeBaseType(compilation, diagnostics);
            _constructors = getConstructors(compilation, this, diagnostics);
            _namespace = containingNamespace;
            _module = containingNamespace.ContainingModule;
        }

        private static NamedTypeSymbol MakeBaseType(CSharpCompilation compilation, DiagnosticBag diagnostics)
        {
            NamedTypeSymbol result = compilation.GetWellKnownType(WellKnownType.System_Attribute);

            // Report errors in case base type was missing or bad
            Binder.ReportUseSiteDiagnostics(result, diagnostics, Location.None);
            return result;
        }

        public new ImmutableArray<MethodSymbol> Constructors => _constructors;

        public override int Arity => 0;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override bool IsImplicitlyDeclared => true;

        internal override bool IsManagedType => false;

        public override NamedTypeSymbol ConstructedFrom => this;

        public override bool MightContainExtensionMethods => false;

        public override string Name => _name;

        public override IEnumerable<string> MemberNames => _constructors.Select(m => m.Name);

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

        internal override ImmutableArray<TypeSymbolWithAnnotations> TypeArgumentsNoUseSiteDiagnostics => ImmutableArray<TypeSymbolWithAnnotations>.Empty;

        internal override bool MangleName => false;

        internal override bool HasCodeAnalysisEmbeddedAttribute => true;

        internal override bool HasSpecialName => false;

        internal override bool IsComImport => false;

        internal override bool IsWindowsRuntimeImport => false;

        internal override bool ShouldAddWinRTMembers => false;

        public override bool IsSerializable => false;

        internal override TypeLayout Layout => default(TypeLayout);

        internal override CharSet MarshallingCharSet => DefaultMarshallingCharSet;

        internal override bool HasDeclarativeSecurity => false;

        internal override bool IsInterface => false;

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => _baseType;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        public override ImmutableArray<Symbol> GetMembers() => _constructors.CastArray<Symbol>();

        public override ImmutableArray<Symbol> GetMembers(string name) => name == WellKnownMemberNames.InstanceConstructorName ? _constructors.CastArray<Symbol>() : ImmutableArray<Symbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;

        internal override AttributeUsageInfo GetAttributeUsageInfo() => AttributeUsageInfo.Default;

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved) => _baseType;

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => GetMembers();

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => GetMembers(name);

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => SpecializedCollections.EmptyEnumerable<FieldSymbol>();

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation() => null;

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved = null) => ImmutableArray<NamedTypeSymbol>.Empty;

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
            GenerateMethodBodyCore(compilationState, diagnostics);
        }
    }
}
