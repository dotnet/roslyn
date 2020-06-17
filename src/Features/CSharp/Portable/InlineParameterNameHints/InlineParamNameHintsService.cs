using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineParameterNameHintsService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.InlineParameterNameHints
{
    [ExportLanguageService(typeof(IInlineParamNameHintsService), LanguageNames.CSharp), Shared]
    internal class InlineParamNameHintsService : IInlineParamNameHintsService
    {
        [ImportingConstructor]
        public InlineParamNameHintsService()
        {
        }

        public async Task<IEnumerable<(string, TextSpan)>> GetInlineParameterNameHintsAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var node = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var spans = new List<(string name, TextSpan span)>();

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var invocations = node.Traverse<SyntaxNode>(textSpan, IsInvocationExpression);
            // var invocation = node.DescendantNodes()
            foreach (var invocationNode in invocations)
            {
                var invo = (InvocationExpressionSyntax)invocationNode;
                foreach (var argument in invo.ArgumentList.Arguments)
                {
                    if (argument.NameColon == null && IsLiteralOrNoNamedExpression(argument))
                    {
                        var param = argument.DetermineParameter(semanticModel, cancellationToken);
                        spans.Add(param.Name, argument.Span);
                    }
                }
            }

            return spans;
        }

        private static bool IsLiteralOrNoNamedExpression(ArgumentSyntax arg)
        {
            if (arg.Expression is LiteralExpressionSyntax)
            {
                return true;
            }
            if (arg.Expression is ObjectCreationExpressionSyntax)
            {
                return true;
            }
            if (arg.Expression is CastExpressionSyntax)
            {
                var cast = (CastExpressionSyntax)arg.Expression;
                if (cast.Expression is LiteralExpressionSyntax)
                {
                    return true;
                }
                return false;
            }
            if (arg.Expression is PrefixUnaryExpressionSyntax)
            {
                var negation = (PrefixUnaryExpressionSyntax)arg.Expression;
                if (negation.Operand is LiteralExpressionSyntax)
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        private static bool IsInvocationExpression(SyntaxNode node)
        {
            return node is InvocationExpressionSyntax;
        }
    }
}
