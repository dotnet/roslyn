// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class WhenKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public WhenKeywordRecommender()
            : base(SyntaxKind.WhenKeyword, isValidInPreprocessorContext: true)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return context.IsCatchFilterContext || IsAfterCompleteExpressionOrPatternInCaseLabel(context);
        }

        private static bool IsAfterCompleteExpressionOrPatternInCaseLabel(CSharpSyntaxContext context)
        {
            var switchLabel = GetAncestorUntilStatement<SwitchLabelSyntax>(context.TargetToken);
            if (switchLabel == null)
            {
                return false;
            }

            var expressionOrPattern = switchLabel.ChildNodes().FirstOrDefault();
            if (expressionOrPattern == null)
            {
                return false;
            }

            // If the last token missing, the expression is incomplete - possibly because of missing parentheses,
            // but not necessarily. We don't want to offer 'when' in those cases. Here are some examples that illustrate this:
            // case |
            // case 1 + |
            // case (1 + 1 |
            // case new |

            // Also note that if there's a missing token inside the expression, that's fine and we do offer 'when':
            // case (1 + ) |

            var lastToken = expressionOrPattern.GetLastToken(includeZeroWidth: true);

            // Only offer if we're at the last token of the expression/pattern, not inside it.
            // Notice that when lastToken is missing, it will never compare equal to context.TargetToken
            // since TargetToken skips missing tokens. So if this passes, we're all good!
            return context.TargetToken == lastToken;
        }

        private static T GetAncestorUntilStatement<T>(SyntaxToken token) where T : SyntaxNode
        {
            for (var node = token.Parent; node != null; node = node.Parent)
            {
                if (node is StatementSyntax)
                {
                    break;
                }

                if (node is T tNode)
                {
                    return tNode;
                }
            }

            return null;
        }
    }
}
