using System.Threading;
using Roslyn.Compilers.CSharp;

namespace Roslyn.Services.CSharp.Simplification
{
    internal partial class CSharpLambdaParameterSimplificationService
    {
        private class Rewriter : AbstractExpressionRewriter
        {
            public Rewriter(CancellationToken cancellationToken)
                : base(cancellationToken)
            {
            }

            public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
            {
                return SimplifyExpression(
                    node,
                    newNode: base.VisitParenthesizedLambdaExpression(node),
                    simplifier: SimplifyLambdaParamters);
            }
        }
    }
}
