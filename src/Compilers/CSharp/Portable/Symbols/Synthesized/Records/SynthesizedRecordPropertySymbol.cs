// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.Symbols.SynthesizedAutoPropAccessorSymbol;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordPropertySymbol : SourcePropertySymbolBase
    {
        private readonly ParameterSymbol _backingParameter;
        internal override SynthesizedBackingFieldSymbol BackingField { get; }
        public override MethodSymbol GetMethod { get; }
        public override MethodSymbol SetMethod { get; }
        protected override Location TypeLocation { get; }

        public SynthesizedRecordPropertySymbol(
            SourceMemberContainerTypeSymbol containingType,
            ParameterSyntax syntax,
            ParameterSymbol backingParameter,
            DiagnosticBag diagnostics)
            : base(
                containingType,
                syntax.GetReference(),
                syntax.Identifier.GetLocation())
        {
            TypeLocation = syntax.Type!.Location;
            _backingParameter = backingParameter;
            string name = backingParameter.Name;
            BackingField = new SynthesizedBackingFieldSymbol(
                this,
                GeneratedNames.MakeBackingFieldName(name),
                isReadOnly: true,
                isStatic: false,
                hasInitializer: true);
            GetMethod = new SynthesizedAutoPropAccessorSymbol(this, name, AccessorKind.Get, diagnostics);
            SetMethod = new SynthesizedAutoPropAccessorSymbol(this, name, AccessorKind.Init, diagnostics);
        }

        public ParameterSymbol BackingParameter => _backingParameter;

        internal override bool IsAutoProperty => true;

        public override RefKind RefKind => RefKind.None;

        public override TypeWithAnnotations TypeWithAnnotations => _backingParameter.TypeWithAnnotations;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

        public override bool IsIndexer => false;

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations => ImmutableArray<PropertySymbol>.Empty;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override bool IsStatic => false;

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsExtern => false;

        public override string Name => _backingParameter.Name;

        protected override IAttributeTargetSymbol AttributesOwner => this;

        protected override AttributeLocation AllowedAttributeLocations => AttributeLocation.None;

        protected override AttributeLocation DefaultAttributeLocation => AttributeLocation.None;

        internal override bool HasPointerType => Type.IsPointerType();

        public override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList => new SyntaxList<AttributeListSyntax>();
    }
}
