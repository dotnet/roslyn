// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class CrefSyntaxExtensions
    {
        public static bool TryReduceOrSimplifyExplicitName(
            this CrefSyntax crefSyntax,
            SemanticModel semanticModel,
            out CrefSyntax replacementNode,
            out TextSpan issueSpan,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default(TextSpan);

            // Currently Qualified Cref is the only CrefSyntax We are handling separately
            if (crefSyntax.CSharpKind() != SyntaxKind.QualifiedCref)
            {
                return false;
            }

            var qualifiedCrefSyntax = (QualifiedCrefSyntax)crefSyntax;
            var memberCref = qualifiedCrefSyntax.Member;

            // Currently we are dealing with only the NameMemberCrefs
            if (memberCref.CSharpKind() == SyntaxKind.NameMemberCref)
            {
                var nameMemberCref = ((NameMemberCrefSyntax)memberCref).Name;
                var symbolInfo = semanticModel.GetSymbolInfo(nameMemberCref);
                var symbol = symbolInfo.Symbol;

                if (symbol == null)
                {
                    return false;
                }

                if (symbol is INamespaceOrTypeSymbol)
                {
                    var namespaceOrTypeSymbol = (INamespaceOrTypeSymbol)symbol;

                    // 1. Check for Predefined Types
                    if (symbol is INamedTypeSymbol)
                    {
                        var namedSymbol = (INamedTypeSymbol)symbol;
                        var keywordKind = ExpressionSyntaxExtensions.GetPredefinedKeywordKind(namedSymbol.SpecialType);

                        if (keywordKind != SyntaxKind.None)
                        {
                            replacementNode = SyntaxFactory.TypeCref(
                                                SyntaxFactory.PredefinedType(
                                                    SyntaxFactory.Token(crefSyntax.GetLeadingTrivia(), keywordKind, crefSyntax.GetTrailingTrivia())));

                            // SyntaxFactory.Token(crefSyntax.GetLeadingTrivia(), keywordKind, crefSyntax.GetTrailingTrivia()));

                            issueSpan = crefSyntax.Span; // we want to show the whole name expression as unnecessary

                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
