﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    /// </summary>
    internal abstract class SynthesizedEmbeddedAttributeSymbolBase : NamedTypeSymbol
    {
        private readonly string _name;
        private readonly NamedTypeSymbol _baseType;
        private readonly NamespaceSymbol _namespace;
        private readonly ModuleSymbol _module;

        public SynthesizedEmbeddedAttributeSymbolBase(
            string name,
            NamespaceSymbol containingNamespace,
            ModuleSymbol containingModule,
            NamedTypeSymbol baseType)
        {
            Debug.Assert(name is object);
            Debug.Assert(containingNamespace is object);
            Debug.Assert(containingModule is object);
            Debug.Assert(baseType is object);

            _name = name;
            _namespace = containingNamespace;
            _module = containingModule;
            _baseType = baseType;
        }

        public new abstract ImmutableArray<MethodSymbol> Constructors { get; }

        public override int Arity => 0;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override bool IsImplicitlyDeclared => true;

        internal override ManagedKind GetManagedKind(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo) => ManagedKind.Managed;

        public override NamedTypeSymbol ConstructedFrom => this;

        public override bool MightContainExtensionMethods => false;

        public override string Name => _name;

        public override IEnumerable<string> MemberNames => Constructors.Select(m => m.Name);

        public override Accessibility DeclaredAccessibility => Accessibility.Internal;

        public override TypeKind TypeKind => TypeKind.Class;

        public override Symbol ContainingSymbol => _namespace;

        internal override ModuleSymbol ContainingModule => _module;

        public override AssemblySymbol ContainingAssembly => _module.ContainingAssembly;

        public override NamespaceSymbol ContainingNamespace => _namespace;

        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override bool IsStatic => false;

        public override bool IsRefLikeType => false;

        public override bool IsReadOnly => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => true;

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics => ImmutableArray<TypeWithAnnotations>.Empty;

        internal override bool MangleName => false;

        internal override bool HasCodeAnalysisEmbeddedAttribute => true;

        internal override bool HasSpecialName => false;

        internal override bool IsComImport => false;

        internal override bool IsWindowsRuntimeImport => false;

        internal override bool ShouldAddWinRTMembers => false;

        public override bool IsSerializable => false;

        public sealed override bool AreLocalsZeroed => ContainingModule.AreLocalsZeroed;

        internal override TypeLayout Layout => default;

        internal override CharSet MarshallingCharSet => DefaultMarshallingCharSet;

        internal override bool HasDeclarativeSecurity => false;

        internal override bool IsInterface => false;

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => _baseType;

        internal override ObsoleteAttributeData ObsoleteAttributeData => null;

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData) => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<Symbol> GetMembers() => Constructors.CastArray<Symbol>();

        public override ImmutableArray<Symbol> GetMembers(string name) => name == WellKnownMemberNames.InstanceConstructorName ? Constructors.CastArray<Symbol>() : ImmutableArray<Symbol>.Empty;

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

            var usageInfo = GetAttributeUsageInfo();
            if (usageInfo != AttributeUsageInfo.Default)
            {
                AddSynthesizedAttribute(
                    ref attributes,
                    moduleBuilder.Compilation.SynthesizeAttributeUsageAttribute(usageInfo.ValidTargets, usageInfo.AllowMultiple, usageInfo.Inherited));
            }
        }

        internal sealed override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable;

        internal sealed override NamedTypeSymbol NativeIntegerUnderlyingType => null;
    }

    /// <summary>
    /// Represents a compiler generated and embedded attribute type with a single default constructor
    /// </summary>
    internal sealed class SynthesizedEmbeddedAttributeSymbol : SynthesizedEmbeddedAttributeSymbolBase
    {
        private readonly ImmutableArray<MethodSymbol> _constructors;

        public SynthesizedEmbeddedAttributeSymbol(
            string name,
            NamespaceSymbol containingNamespace,
            ModuleSymbol containingModule,
            NamedTypeSymbol baseType)
            : base(name, containingNamespace, containingModule, baseType)
        {
            _constructors = ImmutableArray.Create<MethodSymbol>(new SynthesizedEmbeddedAttributeConstructorSymbol(this, m => ImmutableArray<ParameterSymbol>.Empty));
        }

        public override ImmutableArray<MethodSymbol> Constructors => _constructors;

        internal override bool IsRecord => false;
        internal override bool HasPossibleWellKnownCloneMethod() => false;
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

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            GenerateMethodBodyCore(compilationState, diagnostics);
        }
    }
}
