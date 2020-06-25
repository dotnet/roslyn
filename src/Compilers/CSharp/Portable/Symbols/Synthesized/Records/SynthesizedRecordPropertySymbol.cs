﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordPropertySymbol : SourcePropertySymbolBase, IAttributeTargetSymbol
    {
        public ParameterSymbol BackingParameter { get; }

        public SynthesizedRecordPropertySymbol(
            SourceMemberContainerTypeSymbol containingType,
            ParameterSymbol backingParameter,
            DiagnosticBag diagnostics)
            : base(containingType,
                binder: null,
                backingParameter.GetNonNullSyntaxNode(),
                RefKind.None,
                backingParameter.Name,
                backingParameter.Locations[0],
                diagnostics)
        {
            BackingParameter = backingParameter;
        }

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner => this;

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations => AttributeLocation.None;

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation => AttributeLocation.None;

        protected override Location TypeLocation
            => ((ParameterSyntax)CSharpSyntaxNode).Type!.Location;

        protected override SyntaxTokenList GetModifierTokens(SyntaxNode syntax)
            => new SyntaxTokenList();

        protected override ArrowExpressionClauseSyntax? GetArrowExpression(SyntaxNode syntax)
            => null;

        protected override bool HasInitializer(SyntaxNode syntax)
            => true; // Synthesized record properties always have a synthesized initializer

        public override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
            => new SyntaxList<AttributeListSyntax>();

        protected override void GetAccessorDeclarations(
            CSharpSyntaxNode syntax,
            DiagnosticBag diagnostics,
            out bool isAutoProperty,
            out bool hasAccessorList,
            out bool accessorsHaveImplementation,
            out bool isInitOnly,
            out CSharpSyntaxNode? getSyntax,
            out CSharpSyntaxNode? setSyntax)
        {
            isAutoProperty = true;
            hasAccessorList = false;
            getSyntax = setSyntax = syntax;
            isInitOnly = true;
            accessorsHaveImplementation = false;
        }

        protected override void CheckForBlockAndExpressionBody(CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            // Nothing to do here
        }

        protected override DeclarationModifiers MakeModifiers(
            SyntaxTokenList modifiers,
            bool isExplicitInterfaceImplementation,
            bool isIndexer,
            bool accessorsHaveImplementation,
            Location location,
            DiagnosticBag diagnostics,
            out bool modifierErrors)
        {
            Debug.Assert(!isExplicitInterfaceImplementation);
            Debug.Assert(!isIndexer);
            modifierErrors = false;

            return DeclarationModifiers.Public;
        }

        protected override SourcePropertyAccessorSymbol CreateAccessorSymbol(
            bool isGet,
            CSharpSyntaxNode? syntax,
            PropertySymbol? explicitlyImplementedPropertyOpt,
            string aliasQualifierOpt,
            bool isAutoPropertyAccessor,
            bool isExplicitInterfaceImplementation,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(syntax is object);
            Debug.Assert(isAutoPropertyAccessor);
            return SourcePropertyAccessorSymbol.CreateAccessorSymbol(
                isGet,
                usesInit: !isGet, // the setter is always init-only
                ContainingType,
                this,
                _modifiers,
                _sourceName,
                ((ParameterSyntax)syntax).Identifier.GetLocation(),
                syntax,
                diagnostics);
        }

        protected override SourcePropertyAccessorSymbol CreateExpressionBodiedAccessor(
            ArrowExpressionClauseSyntax syntax,
            PropertySymbol? explicitlyImplementedPropertyOpt,
            string aliasQualifierOpt,
            bool isExplicitInterfaceImplementation,
            DiagnosticBag diagnostics)
        {
            // There should be no expression-bodied synthesized record properties
            throw ExceptionUtilities.Unreachable;
        }

        protected override ImmutableArray<ParameterSymbol> ComputeParameters(Binder? binder, CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            return ImmutableArray<ParameterSymbol>.Empty;
        }

        protected override TypeWithAnnotations ComputeType(Binder? binder, SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            return BackingParameter.TypeWithAnnotations;
        }

        protected override bool HasPointerTypeSyntactically
            // Since we already bound the type, don't bother looking at syntax
            => TypeWithAnnotations.DefaultType.IsPointerOrFunctionPointer();

        protected override ExplicitInterfaceSpecifierSyntax? GetExplicitInterfaceSpecifier(SyntaxNode syntax)
            => null;

        protected override BaseParameterListSyntax? GetParameterListSyntax(CSharpSyntaxNode syntax)
            => null;
    }
}
