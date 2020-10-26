// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentSymbols;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.DocumentSymbols
{
    [ExportLanguageService(typeof(IDocumentSymbolsService), LanguageNames.CSharp), Shared]
    internal class CSharpDocumentSymbolsService : AbstractDocumentSymbolsService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpDocumentSymbolsService()
        {
        }

        protected override ISymbol? GetSymbol(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => node switch
            {
                NamespaceDeclarationSyntax or GlobalStatementSyntax => null,
                MemberDeclarationSyntax m => semanticModel.GetDeclaredSymbol(m, cancellationToken),
                LabeledStatementSyntax l => semanticModel.GetDeclaredSymbol(l, cancellationToken),
                SingleVariableDesignationSyntax s => semanticModel.GetDeclaredSymbol(s, cancellationToken),
                VariableDeclaratorSyntax v => semanticModel.GetDeclaredSymbol(v, cancellationToken),
                LocalFunctionStatementSyntax l => semanticModel.GetDeclaredSymbol(l, cancellationToken),
                TypeParameterSyntax t => semanticModel.GetDeclaredSymbol(t, cancellationToken),
                _ => null,
            };

        protected override bool ShouldSkipSyntaxChildren(SyntaxNode node, DocumentSymbolsOptions options)
        {
            // If we're not looking for the full hierarchy, we don't care about things nested inside type members
            if (options == DocumentSymbolsOptions.TypesAndMethodsOnly)
            {
                return node is BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax
                            or BaseFieldDeclarationSyntax or StatementSyntax or ExpressionSyntax;
            }

            // We don't return info about things nested inside lambdas for simplicity
            return node is LambdaExpressionSyntax or CastExpressionSyntax
                        or AttributeListSyntax or TypeParameterConstraintClauseSyntax;
        }

        protected override DocumentSymbolInfo GetInfoForType(INamedTypeSymbol type)
        {
            var members = type.GetMembers().SelectAsArray(
                predicate: member => member switch
                {
                    { IsImplicitlyDeclared: true } or
                    { Kind: SymbolKind.NamedType } or
                    IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet } => false,
                    _ => true
                },
                selector: member => (DocumentSymbolInfo)new CSharpDocumentSymbolInfo(member, ImmutableArray<DocumentSymbolInfo>.Empty));

            return new CSharpDocumentSymbolInfo(type, members.Sort((d1, d2) => d1.Text.CompareTo(d2.Text)));
        }

        protected override DocumentSymbolInfo CreateInfo(ISymbol symbol, ImmutableArray<DocumentSymbolInfo> childrenSymbols)
            => new CSharpDocumentSymbolInfo(symbol, childrenSymbols);

        protected override bool ConsiderNestedNodesChildren(ISymbol node) => node is not (ILabelSymbol or ILocalSymbol);
    }
}
