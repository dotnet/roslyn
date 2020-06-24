// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
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

            foreach (var invo in invocations)
            {
                foreach (var argument in invo.ArgumentList.Arguments)
                {
                    if (argument.NameColon == null && IsExpressionWithNoName(argument.Expression))
                    {
                        var param = argument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                        if (param != null)
                        {
                            spans.Add(new NameAndSpan(param.Name, argument.Span));
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
        private static bool IsExpressionWithNoName(ExpressionSyntax arg)
        {
            if (arg is LiteralExpressionSyntax)
            {
                return true;
            }
            if (arg is ObjectCreationExpressionSyntax)
            {
                return true;
            }
            if (arg is CastExpressionSyntax cast)
            {
                // Recurse until we find a literal
                // If so, then we should add the adornment
                IsExpressionWithNoName(cast.Expression);
            }
            if (arg is PrefixUnaryExpressionSyntax negation)
            {
                // Recurse until we find a literal
                // If so, then we should add the adornment
                IsExpressionWithNoName(negation.Operand);
            }
            return false;
        }
    }
}
