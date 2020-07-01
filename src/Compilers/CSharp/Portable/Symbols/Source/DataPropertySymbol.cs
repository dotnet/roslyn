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
        public static DataPropertySymbol Create(
            SourceMemberContainerTypeSymbol containingType,
            DataPropertyDeclarationSyntax syntax,
            DiagnosticBag diagnostics)
        {
            return new DataPropertySymbol(
                containingType,
                syntax,
                makeModifiers(syntax.Modifiers, syntax.Identifier.GetLocation()),
                syntax.Identifier.ValueText,
                syntax.Identifier.GetLocation(),
                diagnostics);

            DeclarationModifiers makeModifiers(
                SyntaxTokenList modifiers,
                Location location)
            {
                bool isInterface = containingType.IsInterface;
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
                    out _);

                if (isInterface)
                {
                    mods = ModifierUtils.AdjustModifiersForAnInterfaceMember(
                        mods,
                        hasBody: false,
                        isExplicitInterfaceImplementation: false);
                }

                return mods;
            }
        }

        private DataPropertySymbol(
            SourceMemberContainerTypeSymbol containingType,
            DataPropertyDeclarationSyntax syntax,
            DeclarationModifiers modifiers,
            string name,
            Location location,
            DiagnosticBag diagnostics)
           : base(
                containingType,
                binder: null,
                syntax,
                getSyntax: syntax,
                setSyntax: syntax,
                arrowExpression: null,
                interfaceSpecifier: null,
                modifiers,
                isIndexer: false,
                hasInitializer: HasInitializer(syntax),
                isAutoProperty: true,
                hasAccessorList: false,
                isInitOnly: true,
                syntax.Type.GetRefKind(),
                name,
                location,
                typeOpt: default,
                hasParameters: false,
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

        private static bool HasInitializer(DataPropertyDeclarationSyntax syntax)
            => syntax.Initializer is object;

        protected override void CheckForBlockAndExpressionBody(CSharpSyntaxNode syntax, DiagnosticBag diagnostics)
        {
            // Nothing to do here
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
    }
}