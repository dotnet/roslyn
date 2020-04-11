// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents the <see cref="Cci.RootModuleType"/> that will be emitted. Used for <see
    /// cref="SynthesizedRootModuleTypeStaticConstructor.ContainingType"/> so that tests can use proper metadata names.
    /// See <see cref="CompilationTestData.GetMethodsByName"/>.
    /// </summary>
    internal sealed class SynthesizedRootModuleType : NamedTypeSymbol
    {
        private readonly Cci.RootModuleType _rootModuleType;

        public SynthesizedRootModuleType(NamespaceSymbol globalNamespace, Cci.RootModuleType rootModuleType)
        {
            ContainingSymbol = globalNamespace;
            _rootModuleType = rootModuleType;
        }

        public override Symbol ContainingSymbol { get; }

        public override string Name => _rootModuleType.Name;

        internal override bool MangleName => _rootModuleType.MangleName;

        internal override bool HasSpecialName => _rootModuleType.IsSpecialName;

        public override int Arity => 0;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override NamedTypeSymbol ConstructedFrom => this;

        public override bool MightContainExtensionMethods => throw ExceptionUtilities.Unreachable;

        public override IEnumerable<string> MemberNames => throw ExceptionUtilities.Unreachable;

        public override Accessibility DeclaredAccessibility => throw ExceptionUtilities.Unreachable;

        public override bool IsSerializable => false;

        public override bool AreLocalsZeroed => ContainingModule.AreLocalsZeroed;

        public override TypeKind TypeKind => TypeKind.Module;

        public override bool IsRefLikeType => false;

        public override bool IsReadOnly => false;

        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override bool IsStatic => throw ExceptionUtilities.Unreachable;

        public override bool IsAbstract => throw ExceptionUtilities.Unreachable;

        public override bool IsSealed => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics => throw ExceptionUtilities.Unreachable;

        internal override bool HasCodeAnalysisEmbeddedAttribute => throw ExceptionUtilities.Unreachable;

        internal override bool IsComImport => false;

        internal override bool IsWindowsRuntimeImport => throw ExceptionUtilities.Unreachable;

        internal override bool ShouldAddWinRTMembers => throw ExceptionUtilities.Unreachable;

        internal override TypeLayout Layout => throw ExceptionUtilities.Unreachable;

        internal override CharSet MarshallingCharSet => throw ExceptionUtilities.Unreachable;

        internal override bool HasDeclarativeSecurity => throw ExceptionUtilities.Unreachable;

        internal override bool IsInterface => throw ExceptionUtilities.Unreachable;

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => throw ExceptionUtilities.Unreachable;

        internal override ObsoleteAttributeData ObsoleteAttributeData => throw ExceptionUtilities.Unreachable;

        internal override NamedTypeSymbol NativeIntegerUnderlyingType => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<Symbol> GetMembers() => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<Symbol> GetMembers(string name) => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name) => throw ExceptionUtilities.Unreachable;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity) => throw ExceptionUtilities.Unreachable;

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData) => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => throw ExceptionUtilities.Unreachable;

        internal override AttributeUsageInfo GetAttributeUsageInfo() => throw ExceptionUtilities.Unreachable;

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved) => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved) => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => throw ExceptionUtilities.Unreachable;

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => throw ExceptionUtilities.Unreachable;

        internal override IEnumerable<Cci.SecurityAttribute> GetSecurityInformation() => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => throw ExceptionUtilities.Unreachable;

        internal override NamedTypeSymbol AsNativeInteger() => throw ExceptionUtilities.Unreachable;
    }
}
