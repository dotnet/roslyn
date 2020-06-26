// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class DataPropertySymbol : SourcePropertySymbolBase
    {
        public DataPropertySymbol(
            SourceMemberContainerTypeSymbol containingType,
            Binder binder,
            DataPropertyDeclarationSyntax syntax,
            DiagnosticBag diagnostics)
            : base(containingType,
                binder,
                syntax,
                syntax.Type.GetRefKind(),
                syntax.Identifier.ValueText,
                syntax.Identifier.GetLocation(),
                diagnostics)
        {
        }

        public override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
            => CSharpSyntaxNode.AttributeLists;

        protected override Location TypeLocation => CSharpSyntaxNode.Type.Location;

        public new DataPropertyDeclarationSyntax CSharpSyntaxNode
            => (DataPropertyDeclarationSyntax)base.CSharpSyntaxNode;

        protected override bool HasPointerTypeSyntactically
            => CSharpSyntaxNode.Type.IsPointerType();

        protected override SyntaxTokenList GetModifierTokens(SyntaxNode syntax)
            => ((DataPropertyDeclarationSyntax)syntax).Modifiers;

        protected override ArrowExpressionClauseSyntax? GetArrowExpression(SyntaxNode syntax)
            => null;

        protected override bool HasInitializer(SyntaxNode syntax)
            => ((DataPropertyDeclarationSyntax)syntax).Initializer is object;

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
            Debug.Assert(CSharpSyntaxNode.Modifiers == modifiers);
            Debug.Assert(!isExplicitInterfaceImplementation);
            Debug.Assert(!isIndexer);
            Debug.Assert(!accessorsHaveImplementation);

            bool isInterface = ContainingType.IsInterface;
            var defaultAccess = DeclarationModifiers.Public;

            var allowed = DeclarationModifiers.Public |
                DeclarationModifiers.New |
                DeclarationModifiers.Abstract |
                DeclarationModifiers.Virtual;

            if (!isInterface)
            {
                allowed |= DeclarationModifiers.Override;
            }

            var mods = ModifierUtils.MakeAndCheckNontypeMemberModifiers(
                modifiers,
                defaultAccess,
                allowed,
                location,
                diagnostics,
                out modifierErrors);

            if (isInterface)
            {
                mods = ModifierUtils.AdjustModifiersForAnInterfaceMember(
                    mods,
                    hasBody: false,
                    isExplicitInterfaceImplementation: false);
            }

            return mods;
        }

        protected override SourcePropertyAccessorSymbol? CreateAccessorSymbol(
            bool isGet,
            CSharpSyntaxNode? syntaxOpt,
            PropertySymbol? explicitlyImplementedPropertyOpt,
            string? aliasQualifierOpt,
            bool isAutoPropertyAccessor,
            bool isExplicitInterfaceImplementation,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(syntaxOpt is object);
            return SourcePropertyAccessorSymbol.CreateAccessorSymbol(
                isGet,
                usesInit: !isGet, // the setter is always init-only
                isAutoPropertyAccessor,
                ContainingType,
                this,
                _modifiers,
                _sourceName,
                ((DataPropertyDeclarationSyntax)syntaxOpt).DataKeyword.GetLocation(),
                syntaxOpt,
                diagnostics);
        }

        protected override SourcePropertyAccessorSymbol CreateExpressionBodiedAccessor(
            ArrowExpressionClauseSyntax syntax,
            PropertySymbol? explicitlyImplementedPropertyOpt,
            string? aliasQualifierOpt,
            bool isExplicitInterfaceImplementation,
            DiagnosticBag diagnostics)
        {
            // Should never occur
            throw ExceptionUtilities.Unreachable;
        }

        protected override ImmutableArray<ParameterSymbol> ComputeParameters(Binder? binder, CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
            => ImmutableArray<ParameterSymbol>.Empty;

        protected override TypeWithAnnotations ComputeType(Binder? binder, SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            return ComputeType(binder, ((DataPropertyDeclarationSyntax)syntax).Type, syntax, diagnostics);
        }

        protected override ExplicitInterfaceSpecifierSyntax? GetExplicitInterfaceSpecifier(SyntaxNode syntax)
            => null;

        protected override BaseParameterListSyntax? GetParameterListSyntax(CSharpSyntaxNode syntax)
            => null;
    }
}