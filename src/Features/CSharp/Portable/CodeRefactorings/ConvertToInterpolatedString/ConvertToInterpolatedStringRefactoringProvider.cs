using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

        protected override IEnumerable<IFormattingRule> GetFormattingRules(Document document)
        {
            var rules = new List<IFormattingRule> { new MultiLineCommentInInterpolatedStringFormattingRule() };
            rules.AddRange(Formatter.GetDefaultFormattingRules(document));

            return rules;
        }

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
            InterpolatedStringExpressionSyntax interpolatedString) =>
            InterpolatedStringRewriter.Visit(interpolatedString, expandedArguments);

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
