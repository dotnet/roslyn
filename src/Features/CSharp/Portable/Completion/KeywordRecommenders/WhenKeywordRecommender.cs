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

        protected override bool IsValidContext(int position, CSharpSyntaxContext context,
            CancellationToken cancellationToken)
        {
            return context.IsCatchFilterContext || IsAfterCompleteExpressionOrPatternInCaseLabel(context, cancellationToken);
        }

        private static bool IsAfterCompleteExpressionOrPatternInCaseLabel(CSharpSyntaxContext context,
            CancellationToken cancellationToken)
        {
            var switchLabel = GetAncestorUntilStatement<SwitchLabelSyntax>(context.TargetToken);
            if (switchLabel == null)
            {
                return false;
            }

            var expressionOrPattern = switchLabel.ChildNodes().FirstOrDefault();
            if (expressionOrPattern == null) // Oh well. It must have been a default label.
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
            if (lastToken.IsMissing)
            {
                return false;
            }

            // Only offer if we're at the last token of the expression/pattern, not inside it.

            if (lastToken == context.TargetToken)
            {
                return NotATypeName(expressionOrPattern);
            }
            else if (lastToken == context.LeftToken && expressionOrPattern is DeclarationPatternSyntax declarationPattern)
            {
                // case constant w|

                // We might have a partially written 'when' keyword, which causes this to be parsed as a pattern.
                // lastToken will be 'w' (LeftToken) as opposed to 'constant' (TargetToken). We handle this as a special case.

                return NotATypeName(declarationPattern.Type);
            }

            return false;

            bool NotATypeName(SyntaxNode node)
            {
                // Syntactically, everything works out. We're in a pretty good spot to show 'when' now.
                // But let's not do it just yet... Consider these cases:
                // case SyntaxNode |
                // case SyntaxNode w|
                // If what we have here is known to be a type, we don't want to clutter the variable name suggestion list
                // with 'when' since we know that the resulting code would be semantically invalid.

                if (node is TypeSyntax typeSyntax)
                {
                    return !typeSyntax.IsPotentialTypeName(context.SemanticModel, cancellationToken);
                }

                return true;
            }
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
