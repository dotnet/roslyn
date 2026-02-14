// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class SynthesizedPrivateImplementationDetailsType : NamedTypeSymbol
    {
        private readonly PrivateImplementationDetails _privateImplementationDetails;
        private readonly NamespaceSymbol _globalNamespace;
        private readonly NamedTypeSymbol _objectType;

        public SynthesizedPrivateImplementationDetailsType(PrivateImplementationDetails privateImplementationDetails, NamespaceSymbol globalNamespace, NamedTypeSymbol objectType)
        {
            Debug.Assert(globalNamespace.IsGlobalNamespace);
            Debug.Assert(objectType.IsObjectType());

            _privateImplementationDetails = privateImplementationDetails;
            _globalNamespace = globalNamespace;
            _objectType = objectType;
        }

        public PrivateImplementationDetails PrivateImplementationDetails => _privateImplementationDetails;

        public override bool IsImplicitlyDeclared => true;

        public override int Arity => 0;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override NamedTypeSymbol ConstructedFrom => this;

        public override bool MightContainExtensions => false;

        public override string Name => _privateImplementationDetails.Name;

        public override IEnumerable<string> MemberNames => SpecializedCollections.EmptyEnumerable<string>();

        public override Accessibility DeclaredAccessibility => Accessibility.Internal;

        public override bool IsSerializable => false;

        public override bool AreLocalsZeroed => false;

        public override TypeKind TypeKind => TypeKind.Class;

        public override bool IsRefLikeType => false;

        internal override string? ExtensionGroupingName => null;

        internal override string? ExtensionMarkerName => null;

        public override bool IsReadOnly => false;

        public override Symbol ContainingSymbol => _globalNamespace;

        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override bool IsStatic => _privateImplementationDetails.IsSealed && _privateImplementationDetails.IsAbstract;

        public override bool IsAbstract => _privateImplementationDetails.IsAbstract && !_privateImplementationDetails.IsSealed;

        public override bool IsSealed => _privateImplementationDetails.IsSealed && !_privateImplementationDetails.IsAbstract;

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics => ImmutableArray<TypeWithAnnotations>.Empty;

        internal override bool IsFileLocal => false;

        internal override FileIdentifier? AssociatedFileIdentifier => null;

        internal override bool MangleName => false;

        internal override bool HasDeclaredRequiredMembers => false;

        internal override bool HasCodeAnalysisEmbeddedAttribute => false;

        internal override bool HasCompilerLoweringPreserveAttribute => false;

        internal override bool HasUnionAttribute => false;

        internal override bool IsInterpolatedStringHandlerType => false;

        internal sealed override ParameterSymbol? ExtensionParameter => null;

        internal override bool HasSpecialName => _privateImplementationDetails.IsSpecialName;

        internal override bool IsComImport => false;

        internal override bool IsWindowsRuntimeImport => false;

        internal override bool ShouldAddWinRTMembers => false;

        internal override TypeLayout Layout => new TypeLayout(_privateImplementationDetails.Layout, (int)_privateImplementationDetails.SizeOf, (byte)_privateImplementationDetails.Alignment);

        internal override CharSet MarshallingCharSet => _privateImplementationDetails.StringFormat;

        internal override bool HasDeclarativeSecurity => false;

        internal override bool IsInterface => false;

        internal override NamedTypeSymbol? NativeIntegerUnderlyingType => null;

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => _objectType;

        internal override bool IsRecord => false;

        internal override bool IsRecordStruct => false;

        internal override ObsoleteAttributeData? ObsoleteAttributeData => null;

        public override ImmutableArray<Symbol> GetMembers() => ImmutableArray<Symbol>.Empty;

        public override ImmutableArray<Symbol> GetMembers(string name) => ImmutableArray<Symbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name, int arity) => ImmutableArray<NamedTypeSymbol>.Empty;

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name) => ImmutableArray<NamedTypeSymbol>.Empty;

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override NamedTypeSymbol AsNativeInteger()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved) => _objectType;

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers() => ImmutableArray<Symbol>.Empty;

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name) => ImmutableArray<Symbol>.Empty;

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit() => throw ExceptionUtilities.Unreachable();

        internal override bool GetGuidString(out string? guidString)
        {
            guidString = null;
            return false;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit() => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation() => SpecializedCollections.EmptyEnumerable<SecurityAttribute>();

        internal override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            builderArgument = null;
            return false;
        }

        internal override bool HasCollectionBuilderAttribute(out TypeSymbol? builderType, out string? methodName)
        {
            builderType = null;
            methodName = null;
            return false;
        }

        internal override bool HasInlineArrayAttribute(out int length)
        {
            length = 0;
            return false;
        }

        internal override bool HasPossibleWellKnownCloneMethod() => false;

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls()
        {
            return SpecializedCollections.EmptyEnumerable<(MethodSymbol Body, MethodSymbol Implemented)>();
        }
    }
}
