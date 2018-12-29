// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordPropertySymbol : SourceOrRecordPropertySymbol
    {
        private readonly NamedTypeSymbol _containingType;
        private readonly ParameterSymbol _backingParameter;

        public SynthesizedRecordPropertySymbol(
            NamedTypeSymbol containingType,
            ParameterSymbol backingParameter)
            : base(backingParameter.Locations[0])
        {
            _containingType = containingType;
            GetMethod = new GetAccessorSymbol(this);
            _backingParameter = backingParameter;
        }

        public override RefKind RefKind => RefKind.None;

        public override TypeSymbolWithAnnotations Type => _backingParameter.Type;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

        public override bool IsIndexer => false;

        public override MethodSymbol GetMethod { get; }

        public override MethodSymbol SetMethod => null;

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations => ImmutableArray<PropertySymbol>.Empty;

        public override Symbol ContainingSymbol => _containingType;

        public override ImmutableArray<Location> Locations => _backingParameter.Locations;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => _backingParameter.DeclaringSyntaxReferences;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override bool IsStatic => false;

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsExtern => false;

        internal override bool HasSpecialName => false;

        internal override CallingConvention CallingConvention => throw new NotImplementedException();

        internal override bool MustCallMethodsDirectly => throw new NotImplementedException();

        internal override ObsoleteAttributeData ObsoleteAttributeData => throw new NotImplementedException();

        public override string Name => _backingParameter.Name;

        protected override IAttributeTargetSymbol AttributesOwner => this;

        protected override AttributeLocation AllowedAttributeLocations => AttributeLocation.None;

        protected override AttributeLocation DefaultAttributeLocation => AttributeLocation.None;

        internal override bool HasPointerType => Type.IsPointerType();

        public override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList => throw new NotImplementedException();

        private sealed class GetAccessorSymbol : SynthesizedInstanceMethodSymbol
        {
            private readonly SynthesizedRecordPropertySymbol _property;

            public GetAccessorSymbol(SynthesizedRecordPropertySymbol property)
            {
                _property = property;
            }

            public override MethodKind MethodKind => throw new NotImplementedException();

            public override int Arity => throw new NotImplementedException();

            public override bool IsExtensionMethod => throw new NotImplementedException();

            public override bool HidesBaseMethodsByName => throw new NotImplementedException();

            public override bool IsVararg => throw new NotImplementedException();

            public override bool ReturnsVoid => false;

            public override bool IsAsync => false;

            public override RefKind RefKind => RefKind.None;

            public override TypeSymbolWithAnnotations ReturnType => _property.Type;

            public override ImmutableArray<TypeSymbolWithAnnotations> TypeArguments => ImmutableArray<TypeSymbolWithAnnotations>.Empty;

            public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

            public override ImmutableArray<ParameterSymbol> Parameters => _property.Parameters;

            public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

            public override ImmutableArray<CustomModifier> RefCustomModifiers => _property.RefCustomModifiers;

            public override Symbol AssociatedSymbol => _property;

            public override Symbol ContainingSymbol => _property;

            public override ImmutableArray<Location> Locations => _property.Locations;

            public override Accessibility DeclaredAccessibility => _property.DeclaredAccessibility;

            public override bool IsStatic => _property.IsStatic;

            public override bool IsVirtual => _property.IsVirtual;

            public override bool IsOverride => _property.IsOverride;

            public override bool IsAbstract => _property.IsAbstract;

            public override bool IsSealed => _property.IsSealed;

            public override bool IsExtern => _property.IsExtern;

            internal override bool HasSpecialName => _property.HasSpecialName;

            internal override MethodImplAttributes ImplementationAttributes => throw new NotImplementedException();

            internal override bool HasDeclarativeSecurity => throw new NotImplementedException();

            internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation => throw new NotImplementedException();

            internal override bool RequiresSecurityObject => throw new NotImplementedException();

            internal override CallingConvention CallingConvention => throw new NotImplementedException();

            internal override bool GenerateDebugInfo => throw new NotImplementedException();

            public override DllImportData GetDllImportData()
            {
                throw new NotImplementedException();
            }

            internal override ImmutableArray<string> GetAppliedConditionalSymbols()
            {
                throw new NotImplementedException();
            }

            internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
            {
                throw new NotImplementedException();
            }

            internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
            {
                throw new NotImplementedException();
            }

            internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
            {
                throw new NotImplementedException();
            }
        }
    }
}
