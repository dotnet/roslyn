// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers
{
    using static SyntaxFactory;

    internal class QualifiedCrefSimplifier : AbstractCSharpSimplifier<QualifiedCrefSyntax, CrefSyntax>
    {
        public static readonly QualifiedCrefSimplifier Instance = new();

        private QualifiedCrefSimplifier()
        {
        }

        public override bool TrySimplify(
            QualifiedCrefSyntax crefSyntax,
            SemanticModel semanticModel,
            CSharpSimplifierOptions options,
            out CrefSyntax replacementNode,
            out TextSpan issueSpan,
            CancellationToken cancellationToken)
        {
            replacementNode = null;
            issueSpan = default;

            var memberCref = crefSyntax.Member;

            // Currently we are dealing with only the NameMemberCrefs
            if (options.PreferPredefinedTypeKeywordInMemberAccess.Value &&
                memberCref.IsKind(SyntaxKind.NameMemberCref, out NameMemberCrefSyntax nameMemberCref))
            {
                var symbolInfo = semanticModel.GetSymbolInfo(nameMemberCref.Name, cancellationToken);
                var symbol = symbolInfo.Symbol;

                if (symbol == null)
                    return false;

                // 1. Check for Predefined Types
                if (symbol is INamedTypeSymbol namedSymbol)
                {
                    var keywordKind = GetPredefinedKeywordKind(namedSymbol.SpecialType);

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

            return CanSimplifyWithReplacement(
                crefSyntax, semanticModel, memberCref,
                out replacementNode, out issueSpan, cancellationToken);
        }

        private static TypeCrefSyntax CreateReplacement(QualifiedCrefSyntax crefSyntax, SyntaxKind keywordKind)
        {
            var annotation = new SyntaxAnnotation(nameof(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess));
            var token = Token(crefSyntax.GetLeadingTrivia(), keywordKind, crefSyntax.GetTrailingTrivia());
            return TypeCref(PredefinedType(token)).WithAdditionalAnnotations(annotation);
        }

        public static bool CanSimplifyWithReplacement(
            QualifiedCrefSyntax crefSyntax, SemanticModel semanticModel,
            CrefSyntax replacement, CancellationToken cancellationToken)
        {
            return CanSimplifyWithReplacement(crefSyntax, semanticModel, replacement, out _, out _, cancellationToken);
        }

        private static bool CanSimplifyWithReplacement(
            QualifiedCrefSyntax crefSyntax, SemanticModel semanticModel,
            CrefSyntax replacement, out CrefSyntax replacementNode, out TextSpan issueSpan,
            CancellationToken cancellationToken)
        {
            var oldSymbol = semanticModel.GetSymbolInfo(crefSyntax, cancellationToken).Symbol;
            if (oldSymbol != null)
            {
                var speculativeBindingOption = oldSymbol is INamespaceOrTypeSymbol
                    ? SpeculativeBindingOption.BindAsTypeOrNamespace
                    : SpeculativeBindingOption.BindAsExpression;

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

            replacementNode = null;
            issueSpan = default;
            return false;
        }
    }
}
