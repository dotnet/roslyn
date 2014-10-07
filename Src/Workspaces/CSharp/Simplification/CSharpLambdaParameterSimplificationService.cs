using System.Linq;
using System.Threading;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Services.CSharp.Extensions;
using Roslyn.Services.Simplification;

namespace Roslyn.Services.CSharp.Simplification
{
    internal partial class CSharpLambdaParameterSimplificationService : AbstractCSharpExpressionSimplificationService
    {
        protected override IExpressionRewriter CreateExpressionRewriter(CancellationToken cancellationToken)
        {
            return new Rewriter(cancellationToken);
        }

        private static ParameterSyntax RemoveParameterType(ParameterSyntax parameter)
        {
            if (parameter.Type == null || parameter.Type.IsMissing)
            {
                return parameter;
            }

            // Make sure we don't drop any trivia from the type.
            var leadingTrivia = parameter.Type.GetLeadingTrivia();
            var trailingTrivia = parameter.Type.GetTrailingTrivia();

            return parameter
                .WithType(null)
                .WithPrependedLeadingTrivia(leadingTrivia.Concat(trailingTrivia));
        }

        private static ExpressionSyntax SimplifyLambdaParamters(
            ParenthesizedLambdaExpressionSyntax lambda,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // If the lambda parameters already don't have types, there's nothing to simplify
            if (lambda.ParameterList.Parameters.All(p => p.Type == null || p.Type.IsMissing))
            {
                return lambda;
            }

            ExpressionSyntax newLambda;
            if (lambda.ParameterList.Parameters.Count == 1)
            {
                newLambda = Syntax.SimpleLambdaExpression(
                    parameter: RemoveParameterType(lambda.ParameterList.Parameters[0]),
                    body: lambda.Body);
            }
            else
            {
                var newParameters = lambda.ParameterList.Parameters.Select(RemoveParameterType);

                var separators = lambda.ParameterList.Parameters.GetWithSeparators()
                    .Where(nodeOrToken => nodeOrToken.IsToken)
                    .Select(nodeOrToken => nodeOrToken.AsToken());

                var newParameterList = lambda.ParameterList.WithParameters(
                    Syntax.SeparatedList(newParameters, separators));

                newLambda = lambda.WithParameterList(newParameterList);
            }

            if (lambda.ReplacementChangesOverloadResolution(newLambda, semanticModel))
            {
                return lambda;
            }

            var resultNode = newLambda
                .WithLeadingTrivia(newLambda.GetLeadingTrivia())
                .WithTrailingTrivia(newLambda.GetTrailingTrivia());

            resultNode = SimplificationHelpers.CopyAnnotations(from: lambda, to: resultNode);

            return resultNode;
        }
    }
}
