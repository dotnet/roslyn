// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Debugging
{
    internal static class DataTipInfoGetter
    {
        internal static async Task<DebugDataTipInfo> GetInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            try
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (root == null)
                {
                    return default;
                }

                var token = root.FindToken(position);

                if (token.Parent is not ExpressionSyntax expression)
                {
                    return token.IsKind(SyntaxKind.IdentifierToken)
                        ? new DebugDataTipInfo(token.Span, text: null)
                        : default;
                }

                if (expression.IsAnyLiteralExpression())
                {
                    // If the user hovers over a literal, give them a DataTip for the type of the
                    // literal they're hovering over.
                    // Partial semantics should always be sufficient because the (unconverted) type
                    // of a literal can always easily be determined.
                    var (_, semanticModel) = await document.GetFullOrPartialSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
                    return type == null
                        ? default
                        : new DebugDataTipInfo(expression.Span, type.ToNameDisplayString());
                }

                if (expression.IsRightSideOfDotOrArrow())
                {
                    var curr = expression.GetRootConditionalAccessExpression() ?? expression;
                    if (curr == expression)
                    {
                        // NB: Parent.Span, not Span as below.
                        return new DebugDataTipInfo(expression.Parent.Span, text: null);
                    }

                    // NOTE: There may not be an ExpressionSyntax corresponding to the range we want.
                    // For example, for input a?.$$B?.C, we want span [|a?.B|]?.C.
                    return new DebugDataTipInfo(TextSpan.FromBounds(curr.SpanStart, expression.Span.End), text: null);
                }

                // NOTE(cyrusn): This behavior is to mimic what we did in Dev10, I'm not sure if it's
                // necessary or not.
                if (expression is InvocationExpressionSyntax invocation)
                {
                    expression = invocation.Expression;
                }

                string textOpt = null;
                if (expression is TypeSyntax typeSyntax && typeSyntax.IsVar)
                {
                    // If the user is hovering over 'var', then pass back the full type name that 'var'
                    // binds to.
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var type = semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type;
                    if (type != null)
                    {
                        textOpt = type.ToNameDisplayString();
                    }
                }

                return new DebugDataTipInfo(expression.Span, textOpt);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return default;
            }
        }
    }
}
