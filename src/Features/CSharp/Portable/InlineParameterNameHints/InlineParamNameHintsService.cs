using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InlineParameterNameHints;
using Microsoft.CodeAnalysis.Text;
#nullable enable

namespace Microsoft.CodeAnalysis.CSharp.InlineParameterNameHints
{
    /// <summary>
    /// The service to locate the positions in which the adornments should appear
    /// as well as associate the adornments back to the parameter name
    /// </summary>
    [ExportLanguageService(typeof(IInlineParamNameHintsService), LanguageNames.CSharp), Shared]
    internal class InlineParamNameHintsService : IInlineParamNameHintsService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineParamNameHintsService()
        {
        }

        public async Task<IEnumerable<NameAndSpan>> GetInlineParameterNameHintsAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var node = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var spans = new List<NameAndSpan>();

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var invocations = node.DescendantNodes().OfType<InvocationExpressionSyntax>();

            NameAndSpan nameAndSpan;
            foreach (var invo in invocations)
            {
                foreach (var argument in invo.ArgumentList.Arguments)
                {
                    if (argument.NameColon == null && IsLiteralOrNoNamedExpression(argument))
                    {
                        var param = argument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                        if (param != null)
                        {
                            nameAndSpan._name = param.Name;
                            nameAndSpan._span = argument.Span;
                            spans.Add(nameAndSpan);
                        }
                    }
                }
            }

            return spans;
        }

        /// <summary>
        /// Determines if the argument is of a type that should have an adornment appended
        /// </summary>
        /// <param name="arg">The argument that is being looked at</param>
        /// <returns>true when the adornment should be added</returns>
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
                // Determine if the descendant node from the cast is of literal type
                // If so, then we should add the adornment
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
                // Determine if the descendant node from the unary expression is of literal type
                // If so, then we should add the adornment
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
