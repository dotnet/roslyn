// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.Symbols.SynthesizedAutoPropAccessorSymbol;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class DataPropertySymbol : SourcePropertySymbolBase
    {
        private TypeWithAnnotations.Boxed? _lazyType;

        public override string Name { get; }
        internal override SynthesizedBackingFieldSymbol BackingField { get; }
        public override MethodSymbol GetMethod { get; }
        public override MethodSymbol SetMethod { get; }

        public DataPropertySymbol(
            SourceMemberContainerTypeSymbol containingType,
            Binder bodyBinder,
            DataPropertyDeclarationSyntax syntax,
            DiagnosticBag diagnostics)
            : base(containingType, syntax.GetReference(), syntax.Location)
        {
            string name = syntax.Identifier.ValueText;
            Name = name;
            BackingField = new SynthesizedBackingFieldSymbol(
                this,
                GeneratedNames.MakeBackingFieldName(name),
                isReadOnly: true,
                isStatic: false,
                hasInitializer: true);
            GetMethod = new SynthesizedAutoPropAccessorSymbol(this, name, AccessorKind.Get, diagnostics);
            SetMethod = new SynthesizedAutoPropAccessorSymbol(this, name, AccessorKind.Init, diagnostics);
            _ = ModifierUtils.CheckModifiers(
                syntax.Modifiers.ToDeclarationModifiers(diagnostics),
                DeclarationModifiers.None,
                syntax.Location,
                diagnostics,
                modifierTokens: syntax.Modifiers,
                out _);

            if (containingType.IsInterface)
            {
                diagnostics.Add(ErrorCode.ERR_InterfacesCantContainFields, syntax.Location);
            }
        }

        public override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
            => SyntaxNode.AttributeLists;

        public override RefKind RefKind => RefKind.None;

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get
            {
                if (_lazyType == null)
                {
                    var diagnostics = DiagnosticBag.GetInstance();
                    var compilation = this.DeclaringCompilation;
                    var syntax = (DataPropertyDeclarationSyntax)SyntaxNode;
                    var binderFactory = compilation.GetBinderFactory(syntax.SyntaxTree);
                    var binder = binderFactory.GetBinder(syntax, syntax, this)
                        .WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);
                    var result = this.ComputeType(binder, syntax.Type, diagnostics);
                    if (Interlocked.CompareExchange(ref _lazyType, new TypeWithAnnotations.Boxed(result), null) == null)
                    {
                        this.AddDeclarationDiagnostics(diagnostics);
                    }
                    diagnostics.Free();
                }

                return _lazyType.Value;
            }
        }

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

        protected override IAttributeTargetSymbol AttributesOwner => this;

        protected override AttributeLocation AllowedAttributeLocations
            => AttributeLocation.Property | AttributeLocation.Field;

        protected override AttributeLocation DefaultAttributeLocation => AttributeLocation.Property;

        internal override bool IsAutoProperty => true;

        internal override bool HasPointerType
        {
            get
            {
                if (_lazyType != null)
                {
                    var hasPointerType = _lazyType.Value.DefaultType.IsPointerOrFunctionPointer();
                    Debug.Assert(hasPointerType == IsPointerType(SyntaxNode.Type));
                    return hasPointerType;
                }

                return IsPointerType(SyntaxNode.Type);
            }
        }

        protected override Location TypeLocation => SyntaxNode.Type.Location;

        private DataPropertyDeclarationSyntax SyntaxNode => (DataPropertyDeclarationSyntax)SyntaxReference.GetSyntax();
    }
}