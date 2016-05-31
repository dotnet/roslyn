using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertToInterpolatedString
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertToInterpolatedString), Shared]
    internal partial class ConvertToInterpolatedStringRefactoringProvider : AbstractConvertToInterpolatedStringRefactoringProvider<InterpolatedStringExpressionSyntax, InvocationExpressionSyntax, ExpressionSyntax, ArgumentSyntax, LiteralExpressionSyntax>
    {
        protected override SeparatedSyntaxList<ArgumentSyntax>? GetArguments(InvocationExpressionSyntax invocation) =>
            invocation?.ArgumentList?.Arguments;

        protected override ImmutableArray<ExpressionSyntax> GetExpandedArguments(
            SemanticModel semanticModel, 
            SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            var builder = ImmutableArray.CreateBuilder<ExpressionSyntax>();
            for (int i = 1; i < arguments.Count; i++)
            {
                builder.Add(CastAndParenthesize(arguments[i].Expression, semanticModel));
            }

            var expandedArguments = builder.ToImmutable();
            return expandedArguments;
        }

        protected override LiteralExpressionSyntax GetFirstArgument(SeparatedSyntaxList<ArgumentSyntax> arguments) =>
            arguments[0]?.Expression as LiteralExpressionSyntax;

        protected override InterpolatedStringExpressionSyntax GetInterpolatedString(string text) =>
            (InterpolatedStringExpressionSyntax)ParseExpression("$" + text);

        protected override string GetText(SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            var stringToken = ((LiteralExpressionSyntax)arguments[0].Expression).Token;
            var text = stringToken.ToString();

            if (stringToken.IsVerbatimStringLiteral())
            {
                // We need to escape braces as this is an ambiguous case in interpolated strings that is 
                // not ambiguous in verbatim strings
                text = text.Replace(@"'{'", @"'{{'").Replace(@"'}'", @"'}}'");
            }

            return text;
        }

        protected override bool IsArgumentListCorrect(
            InvocationExpressionSyntax invocation,
            ISymbol invocationSymbol,
            ImmutableArray<ISymbol> formatMethods,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (invocation.ArgumentList != null &&
                invocation.ArgumentList.Arguments.Count >= 2 &&
                invocation.ArgumentList.Arguments[0].Expression.IsKind(SyntaxKind.StringLiteralExpression))
            {
                // We do not want to substitute the expression if it is being passed to params array argument
                // Example: 
                // string[] args;
                // String.Format("{0}{1}{2}", args);
                return IsArgumentListNotPassingArrayToParams(
                    invocation.ArgumentList.Arguments[1].Expression,
                    invocationSymbol,
                    formatMethods,
                    semanticModel,
                    cancellationToken);
            }

            return false;
        }

        protected override bool IsStringLiteral(LiteralExpressionSyntax firstArgument) =>
            firstArgument?.Token.IsKind(SyntaxKind.StringLiteralToken) == true;

        protected override InterpolatedStringExpressionSyntax VisitArguments(
            ImmutableArray<ExpressionSyntax> expandedArguments,
            InterpolatedStringExpressionSyntax interpolatedString)
        {
            return interpolatedString.ReplaceNodes(interpolatedString.Contents, (oldNode, newNode) =>
            {
                var node = newNode as InterpolationSyntax;
                if (node == null)
                {
                    return newNode;
                }

                var literalExpression = node.Expression as LiteralExpressionSyntax;
                if (literalExpression != null && literalExpression.IsKind(SyntaxKind.NumericLiteralExpression))
                {
                    var index = (int)literalExpression.Token.Value;
                    if (index >= 0 && index < expandedArguments.Length)
                    {
                        return node.WithExpression(FixTrivia(expandedArguments[index]));
                    }
                }

                return newNode;
            });
        }

        /// <summary>
        /// Since C# interpolations cannot be on more than one line, we need to remove newlines if possible.
        /// </summary>
        public static ExpressionSyntax FixTrivia(ExpressionSyntax node) =>
            node.ReplaceTokens(node.DescendantTokens(descendIntoTrivia: true), RemoveTriviaForTokens);

        private static SyntaxToken RemoveTriviaForTokens(SyntaxToken originalToken, SyntaxToken rewrittenToken) =>
            rewrittenToken
                .WithLeadingTrivia(
                    ReplaceSyntaxKindsWithElasticSpace(rewrittenToken.LeadingTrivia, SyntaxKind.WhitespaceTrivia, SyntaxKind.EndOfLineTrivia))
                .WithTrailingTrivia(
                    ReplaceSyntaxKindsWithElasticSpace(rewrittenToken.TrailingTrivia, SyntaxKind.WhitespaceTrivia, SyntaxKind.EndOfLineTrivia));

        private static SyntaxTriviaList ReplaceSyntaxKindsWithElasticSpace(SyntaxTriviaList trivialList, params SyntaxKind[] kinds) =>
            trivialList.Select(x => kinds.Any(y => x.IsKind(y)) ? SyntaxFactory.ElasticSpace : x).ToSyntaxTriviaList();

        private static ExpressionSyntax CastAndParenthesize(ExpressionSyntax expression, SemanticModel semanticModel) =>
            Parenthesize(Cast(expression, semanticModel.GetTypeInfo(expression).ConvertedType));

        private static ExpressionSyntax Cast(ExpressionSyntax expression, ITypeSymbol targetType)
        {
            if (targetType == null)
            {
                return expression;
            }

            var type = ParseTypeName(targetType.ToDisplayString());

            return CastExpression(type, Parenthesize(expression))
                .WithAdditionalAnnotations(Simplifier.Annotation);
        }

        private static ExpressionSyntax Parenthesize(ExpressionSyntax expression) =>
            expression.IsKind(SyntaxKind.ParenthesizedExpression)
                ? expression
                : ParenthesizedExpression(
                    openParenToken: Token(SyntaxTriviaList.Empty, SyntaxKind.OpenParenToken, SyntaxTriviaList.Empty),
                    expression: expression,
                    closeParenToken: Token(SyntaxTriviaList.Empty, SyntaxKind.CloseParenToken, SyntaxTriviaList.Empty))
                    .WithAdditionalAnnotations(Simplifier.Annotation);
    }
}
