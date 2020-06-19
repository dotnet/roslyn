using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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

            var invocations = node.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var invo in invocations)
            {
                // var invo = (InvocationExpressionSyntax)invocationNode;
                foreach (var argument in invo.ArgumentList.Arguments)
                {
                    if (argument.NameColon == null && IsLiteralOrNoNamedExpression(argument))
                    {
                        var param = argument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                        spans.Add((param.Name, argument.Span));
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
            if (arg.Expression is CastExpressionSyntax cast)
            {
                var literal = arg.DescendantNodes().OfType<LiteralExpressionSyntax>();
                foreach (var l in literal)
                {
                    if (l is LiteralExpressionSyntax)
                    {
                        return true;
                    }
                }
                return false;
            }
            if (arg.Expression is PrefixUnaryExpressionSyntax negation)
            {
                var literal = arg.DescendantNodes().OfType<LiteralExpressionSyntax>();
                foreach (var l in literal)
                {
                    if (l is LiteralExpressionSyntax)
                    {
                        return true;
                    }
                }
                return false;
            }
            return false;
        }
    }
}
