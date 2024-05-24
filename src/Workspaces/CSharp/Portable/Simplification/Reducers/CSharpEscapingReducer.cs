// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal partial class CSharpEscapingReducer : AbstractCSharpReducer
{
    private static readonly ObjectPool<IReductionRewriter> s_pool = new(
        () => new Rewriter(s_pool));

    private static readonly Func<SyntaxToken, SemanticModel, CSharpSimplifierOptions, CancellationToken, SyntaxToken> s_simplifyIdentifierToken = SimplifyIdentifierToken;

    public CSharpEscapingReducer() : base(s_pool)
    {
    }

    protected override bool IsApplicable(CSharpSimplifierOptions options)
       => true;

    private static SyntaxToken SimplifyIdentifierToken(
        SyntaxToken token,
        SemanticModel semanticModel,
        CSharpSimplifierOptions options,
        CancellationToken cancellationToken)
    {
        var unescapedIdentifier = token.ValueText;

        var enclosingXmlNameAttr = token.GetAncestors(n => n is XmlNameAttributeSyntax).FirstOrDefault();

        // always escape keywords
        if (SyntaxFacts.GetKeywordKind(unescapedIdentifier) != SyntaxKind.None && enclosingXmlNameAttr == null)
        {
            return CreateNewIdentifierTokenFromToken(token, escape: true);
        }

        // Escape the Await Identifier if within the Single Line Lambda & Multi Line Context
        // and async method

        var parent = token.Parent;

        if (SyntaxFacts.GetContextualKeywordKind(unescapedIdentifier) == SyntaxKind.AwaitKeyword)
        {
            var enclosingLambdaExpression = parent.GetAncestorsOrThis(n => (n is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax)).FirstOrDefault();
            if (enclosingLambdaExpression != null)
            {
                if (enclosingLambdaExpression is SimpleLambdaExpressionSyntax simpleLambda)
                {
                    if (simpleLambda.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword)
                    {
                        return token;
                    }
                }

                if (enclosingLambdaExpression is ParenthesizedLambdaExpressionSyntax parenLamdba)
                {
                    if (parenLamdba.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword)
                    {
                        return token;
                    }
                }
            }

            var enclosingMethodBlock = parent.GetAncestorsOrThis(n => n is MethodDeclarationSyntax).FirstOrDefault();

            if (enclosingMethodBlock != null && ((MethodDeclarationSyntax)enclosingMethodBlock).Modifiers.Any(SyntaxKind.AsyncKeyword))
            {
                return token;
            }
        }

        // within a query all contextual query keywords need to be escaped, even if they appear in a non query context.
        if (token.GetAncestors(n => n is QueryExpressionSyntax).Any())
        {
            switch (SyntaxFacts.GetContextualKeywordKind(unescapedIdentifier))
            {
                case SyntaxKind.FromKeyword:
                case SyntaxKind.WhereKeyword:
                case SyntaxKind.SelectKeyword:
                case SyntaxKind.GroupKeyword:
                case SyntaxKind.IntoKeyword:
                case SyntaxKind.OrderByKeyword:
                case SyntaxKind.JoinKeyword:
                case SyntaxKind.LetKeyword:
                case SyntaxKind.InKeyword:
                case SyntaxKind.OnKeyword:
                case SyntaxKind.EqualsKeyword:
                case SyntaxKind.ByKeyword:
                case SyntaxKind.AscendingKeyword:
                case SyntaxKind.DescendingKeyword:
                    return CreateNewIdentifierTokenFromToken(token, escape: true);
            }
        }

        var result = token.Kind() == SyntaxKind.IdentifierToken ? CreateNewIdentifierTokenFromToken(token, escape: false) : token;

        // we can't remove the escaping if this would change the semantic. This can happen in cases
        // where there are two attribute declarations: one with and one without the attribute
        // suffix.
        if (SyntaxFacts.IsAttributeName(parent))
        {
            var expression = (SimpleNameSyntax)parent;
            var newExpression = expression.WithIdentifier(result);
            var speculationAnalyzer = new SpeculationAnalyzer(expression, newExpression, semanticModel, cancellationToken);
            if (speculationAnalyzer.ReplacementChangesSemantics())
            {
                return CreateNewIdentifierTokenFromToken(token, escape: true);
            }
        }

        // TODO: handle crefs and param names of xml doc comments.
        // crefs have the same escaping rules than csharp, param names do not allow escaping in Dev11, but 
        // we may want to change that for Roslyn (Bug 17984, " Could treat '@' specially in <param>, <typeparam>, etc")

        return result;
    }

    private static SyntaxToken CreateNewIdentifierTokenFromToken(SyntaxToken originalToken, bool escape)
    {
        var isVerbatimIdentifier = originalToken.IsVerbatimIdentifier();
        if (isVerbatimIdentifier == escape)
        {
            return originalToken;
        }

        var unescapedText = isVerbatimIdentifier ? originalToken.ToString()[1..] : originalToken.ToString();

        return escape
            ? originalToken.CopyAnnotationsTo(SyntaxFactory.VerbatimIdentifier(originalToken.LeadingTrivia, unescapedText, originalToken.ValueText, originalToken.TrailingTrivia))
            : originalToken.CopyAnnotationsTo(SyntaxFactory.Identifier(originalToken.LeadingTrivia, SyntaxKind.IdentifierToken, unescapedText, originalToken.ValueText, originalToken.TrailingTrivia));
    }
}
