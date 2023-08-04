// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A container synthesized for a lambda, iterator method, or async method.
    /// </summary>
    internal abstract class SynthesizedContainer : NamedTypeSymbol
    {
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly ImmutableArray<TypeParameterSymbol> _constructedFromTypeParameters;

        protected SynthesizedContainer(string name, MethodSymbol containingMethod)
        {
            Debug.Assert(name != null);
            Name = name;
            if (containingMethod == null)
            {
                TypeMap = TypeMap.Empty;
                _typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
            }
            else
            {
                TypeMap = TypeMap.Empty.WithConcatAlphaRename(containingMethod, this, out _typeParameters, out _constructedFromTypeParameters);
            }
        }

        protected SynthesizedContainer(string name, ImmutableArray<TypeParameterSymbol> typeParameters, TypeMap typeMap)
        {
            Debug.Assert(name != null);
            Debug.Assert(!typeParameters.IsDefault);
            Debug.Assert(typeMap != null);

            Name = name;
            _typeParameters = typeParameters;
            TypeMap = typeMap;
        }

        internal TypeMap TypeMap { get; }

        internal virtual MethodSymbol Constructor => null;

        internal sealed override bool IsInterface => this.TypeKind == TypeKind.Interface;

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            if (ContainingSymbol.Kind == SymbolKind.NamedType && ContainingSymbol.IsImplicitlyDeclared)
            {
                return;
            }

            var compilation = ContainingSymbol.DeclaringCompilation;

            // this can only happen if frame is not nested in a source type/namespace (so far we do not do this)
            // if this happens for whatever reason, we do not need "CompilerGenerated" anyways
            Debug.Assert(compilation != null, "SynthesizedClass is not contained in a source module?");

            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(
                WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
        }

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
            => throw ExceptionUtilities.Unreachable();

        internal ImmutableArray<TypeParameterSymbol> ConstructedFromTypeParameters => _constructedFromTypeParameters;

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters => _typeParameters;

        public sealed override string Name { get; }

        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override IEnumerable<string> MemberNames => SpecializedCollections.EmptyEnumerable<string>();

        public override NamedTypeSymbol ConstructedFrom => this;

        public override bool IsSealed => true;

        public override bool IsAbstract => (object)Constructor == null && this.TypeKind != TypeKind.Struct;

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
        {
            get { return GetTypeParametersAsTypeArguments(); }
        }

        internal override bool HasCodeAnalysisEmbeddedAttribute => false;

        internal sealed override bool IsInterpolatedStringHandlerType => false;

        internal sealed override bool HasDeclaredRequiredMembers => false;

        public override ImmutableArray<Symbol> GetMembers()
        {
            Symbol constructor = this.Constructor;
            return (object)constructor == null ? ImmutableArray<Symbol>.Empty : ImmutableArray.Create(constructor);
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

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => this.GetMembersUnordered();

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => this.GetMembers(name);

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name) => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity) => ImmutableArray<NamedTypeSymbol>.Empty;

        public override Accessibility DeclaredAccessibility => Accessibility.Private;

        public override bool IsStatic => false;

        public sealed override bool IsRefLikeType => false;

        public sealed override bool IsReadOnly => false;

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => CalculateInterfacesToEmit();

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => ContainingAssembly.GetSpecialType(this.TypeKind == TypeKind.Struct ? SpecialType.System_ValueType : SpecialType.System_Object);

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved) => BaseTypeNoUseSiteDiagnostics;

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved) => InterfacesNoUseSiteDiagnostics(basesBeingResolved);

        public override bool MightContainExtensionMethods => false;

        public override int Arity => TypeParameters.Length;

        internal override bool MangleName => Arity > 0;

#nullable enable
        internal sealed override bool IsFileLocal => false;
        internal sealed override FileIdentifier? AssociatedFileIdentifier => null;
#nullable disable

        public override bool IsImplicitlyDeclared => true;

        internal override bool ShouldAddWinRTMembers => false;

        internal override bool IsWindowsRuntimeImport => false;

        internal override bool IsComImport => false;

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData => null;

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;

        internal override bool HasDeclarativeSecurity => false;

        internal override CharSet MarshallingCharSet => DefaultMarshallingCharSet;

        public override bool IsSerializable => false;

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo() => default(AttributeUsageInfo);

        internal override TypeLayout Layout => default(TypeLayout);

        internal override bool HasSpecialName => false;

        internal sealed override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable();

        internal sealed override NamedTypeSymbol NativeIntegerUnderlyingType => null;

        internal sealed override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
        {
            return SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
        }

        internal sealed override bool HasInlineArrayAttribute(out int length)
        {
            length = 0;
            return false;
        }

#nullable enable
        internal sealed override bool HasCollectionBuilderAttribute(out TypeSymbol? builderType, out string? methodName)
        {
            builderType = null;
            methodName = null;
            return false;
        }
#nullable disable
    }
}
