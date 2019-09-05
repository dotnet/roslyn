// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
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
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default;

            // Currently Qualified Cref is the only CrefSyntax We are handling separately
            if (crefSyntax.Kind() != SyntaxKind.QualifiedCref)
            {
                return false;
            }

            var qualifiedCrefSyntax = (QualifiedCrefSyntax)crefSyntax;
            var memberCref = qualifiedCrefSyntax.Member;

            // Currently we are dealing with only the NameMemberCrefs
            if (SimplificationHelpers.PreferPredefinedTypeKeywordInMemberAccess(optionSet, semanticModel.Language) &&
                (memberCref.Kind() == SyntaxKind.NameMemberCref))
            {
                var nameMemberCref = ((NameMemberCrefSyntax)memberCref).Name;
                var symbolInfo = semanticModel.GetSymbolInfo(nameMemberCref, cancellationToken);
                var symbol = symbolInfo.Symbol;

                if (symbol == null)
                {
                    return false;
                }

                if (symbol is INamespaceOrTypeSymbol namespaceOrTypeSymbol)
                {
                    // 1. Check for Predefined Types
                    if (symbol is INamedTypeSymbol namedSymbol)
                    {
                        var keywordKind = ExpressionSyntaxExtensions.GetPredefinedKeywordKind(namedSymbol.SpecialType);

                        if (keywordKind != SyntaxKind.None)
                        {
                            replacementNode = SyntaxFactory.TypeCref(
                                                SyntaxFactory.PredefinedType(
                                                    SyntaxFactory.Token(crefSyntax.GetLeadingTrivia(), keywordKind, crefSyntax.GetTrailingTrivia())));
                            replacementNode = crefSyntax.CopyAnnotationsTo(replacementNode);

                            // we want to show the whole name expression as unnecessary
                            issueSpan = crefSyntax.Span;

                            return true;
                        }
                    }
                }
            }

            var oldSymbol = semanticModel.GetSymbolInfo(crefSyntax, cancellationToken).Symbol;
            if (oldSymbol != null)
            {
                var speculativeBindingOption = SpeculativeBindingOption.BindAsExpression;
                if (oldSymbol is INamespaceOrTypeSymbol)
                {
                    speculativeBindingOption = SpeculativeBindingOption.BindAsTypeOrNamespace;
                }

                var newSymbol = semanticModel.GetSpeculativeSymbolInfo(crefSyntax.SpanStart, memberCref, speculativeBindingOption).Symbol;

                if (Equals(newSymbol, oldSymbol))
                {
                    // Copy Trivia and Annotations
                    memberCref = memberCref.WithLeadingTrivia(crefSyntax.GetLeadingTrivia());
                    memberCref = crefSyntax.CopyAnnotationsTo(memberCref);
                    issueSpan = qualifiedCrefSyntax.Container.Span;
                    replacementNode = memberCref;
                    return true;
                }
            }

            return false;
        }
    }
}
