// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class WhenKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public WhenKeywordRecommender()
            : base(SyntaxKind.WhenKeyword, isValidInPreprocessorContext: true)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context,
            CancellationToken cancellationToken)
        {
            return context.IsCatchFilterContext ||
                (IsAfterCompleteExpressionOrPatternInCaseLabel(context, out var expressionOrPattern) &&
                !IsTypeName(expressionOrPattern, context.SemanticModel, cancellationToken));
        }

        private static bool IsAfterCompleteExpressionOrPatternInCaseLabel(CSharpSyntaxContext context,
            out SyntaxNode expressionOrPattern)
        {
            var switchLabel = context.TargetToken.GetAncestor<SwitchLabelSyntax>();
            if (switchLabel == null)
            {
                expressionOrPattern = null;
                return false;
            }

            expressionOrPattern = switchLabel.ChildNodes().FirstOrDefault();
            if (expressionOrPattern == null) // Oh well. It must have been a default label.
            {
                return false;
            }

            // If the last token is missing, the expression is incomplete - possibly because of missing parentheses,
            // but not necessarily. We don't want to offer 'when' in those cases. Here are some examples that illustrate this:
            // case |
            // case 1 + |
            // case (1 + 1 |
            // case new |

            // Also note that if there's a missing token inside the expression, that's fine and we do offer 'when':
            // case (1 + ) |

            var lastToken = expressionOrPattern.GetLastToken(includeZeroWidth: true);
            if (lastToken.IsMissing)
            {
                return false;
            }

            if (lastToken == context.LeftToken && context.LeftToken != context.TargetToken &&
                expressionOrPattern is DeclarationPatternSyntax declarationPattern)
            {
                // case constant w|

                // The user is typing a new word (might be a partially written 'when' keyword), which causes this to be parsed
                // as a declaration pattern. lastToken will be 'w' (LeftToken) as opposed to 'constant' (TargetToken).
                // However we'd like to pretend that this is not the case and that we just a have single expression
                // with 'constant' as if the new word didn't exist. So let's do that by adjusting our variables.

                lastToken = context.TargetToken;
                expressionOrPattern = declarationPattern.Type;
            }

            // Only offer if we're at the last token of the expression/pattern, not inside it.
            return lastToken == context.TargetToken;
        }

        private static bool IsTypeName(SyntaxNode expressionOrPattern, SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // Syntactically, everything works out. We're in a pretty good spot to show 'when' now.
            // But let's not do it just yet... Consider these cases:
            // case SyntaxNode |
            // case SyntaxNode w|
            // If what we have here is known to be a type, we don't want to clutter the variable name suggestion list
            // with 'when' since we know that the resulting code would be semantically invalid.

            var expression = expressionOrPattern as ExpressionSyntax
                ?? (expressionOrPattern as ConstantPatternSyntax)?.Expression;

            return (expression as TypeSyntax)?.IsPotentialTypeName(semanticModel, cancellationToken) ?? false;
        }
    }
}
