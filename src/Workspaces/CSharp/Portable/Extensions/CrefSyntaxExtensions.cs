// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

    internal static class CrefSyntaxExtensions
    {
        public static bool TryReduceOrSimplifyExplicitName(
            this QualifiedCrefSyntax crefSyntax,
            SemanticModel semanticModel,
            out CrefSyntax replacementNode,
            out TextSpan issueSpan,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default;

            var memberCref = crefSyntax.Member;

            // Currently we are dealing with only the NameMemberCrefs
            if (SimplificationHelpers.PreferPredefinedTypeKeywordInMemberAccess(optionSet, semanticModel.Language) &&
                memberCref.IsKind(SyntaxKind.NameMemberCref, out NameMemberCrefSyntax nameMemberCref))
            {
                var symbolInfo = semanticModel.GetSymbolInfo(nameMemberCref.Name, cancellationToken);
                var symbol = symbolInfo.Symbol;

                if (symbol == null)
                    return false;

                // 1. Check for Predefined Types
                if (symbol is INamedTypeSymbol namedSymbol)
                {
                    var keywordKind = ExpressionSyntaxExtensions.GetPredefinedKeywordKind(namedSymbol.SpecialType);

                    if (keywordKind != SyntaxKind.None)
                    {
                        replacementNode = CreateReplacement(crefSyntax, keywordKind);
                        replacementNode = crefSyntax.CopyAnnotationsTo(replacementNode);

                        // we want to show the whole name expression as unnecessary
                        issueSpan = crefSyntax.Span;

                        return true;
                    }
                }
            }

            return TryReduceOrSimplifyQualifiedCref(
                crefSyntax, semanticModel, memberCref,
                out replacementNode, out issueSpan, cancellationToken);
        }

        private static TypeCrefSyntax CreateReplacement(QualifiedCrefSyntax crefSyntax, SyntaxKind keywordKind)
        {
            var annotation = new SyntaxAnnotation(nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess));
            var token = Token(crefSyntax.GetLeadingTrivia(), keywordKind, crefSyntax.GetTrailingTrivia());
            return TypeCref(PredefinedType(token)).WithAdditionalAnnotations(annotation);
        }

        public static bool TryReduceOrSimplifyQualifiedCref(
            this QualifiedCrefSyntax crefSyntax, SemanticModel semanticModel,
            CrefSyntax replacement, out CrefSyntax replacementNode, out TextSpan issueSpan,
            CancellationToken cancellationToken)
        {
            var oldSymbol = semanticModel.GetSymbolInfo(crefSyntax, cancellationToken).Symbol;
            if (oldSymbol != null)
            {
                var speculativeBindingOption = SpeculativeBindingOption.BindAsExpression;
                if (oldSymbol is INamespaceOrTypeSymbol)
                {
                    speculativeBindingOption = SpeculativeBindingOption.BindAsTypeOrNamespace;
                }

                var newSymbol = semanticModel.GetSpeculativeSymbolInfo(crefSyntax.SpanStart, replacement, speculativeBindingOption).Symbol;

                if (Equals(newSymbol, oldSymbol))
                {
                    // Copy Trivia and Annotations
                    replacement = replacement.WithLeadingTrivia(crefSyntax.GetLeadingTrivia());
                    replacement = crefSyntax.CopyAnnotationsTo(replacement);
                    issueSpan = crefSyntax.Container.Span;
                    replacementNode = replacement;
                    return true;
                }
            }

            replacementNode = default;
            issueSpan = default;
            return false;
        }
    }
}
