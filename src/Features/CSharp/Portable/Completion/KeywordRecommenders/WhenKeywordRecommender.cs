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

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
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
            if (expressionOrPattern == null)
            {
                // It must have been a default label.
                return false;
            }

            // If the last token is missing, the expression is incomplete - possibly because of missing parentheses,
            // but not necessarily. We don't want to offer 'when' in those cases. Here are some examples that illustrate this:
            // case |
            // case x.|
            // case 1 + |
            // case (1 + 1 |

            // Also note that if there's a missing token inside the expression, that's fine and we do offer 'when':
            // case (1 + ) |

            // context.TargetToken does not include zero width so in case of a missing token, these will never be equal.
            var lastToken = expressionOrPattern.GetLastToken(includeZeroWidth: true);
            if (lastToken == context.TargetToken)
            {
                return true;
            }

            if (lastToken == context.LeftToken && expressionOrPattern is DeclarationPatternSyntax declarationPattern)
            {
                // case constant w|

                // The user is typing a new word (might be a partially written 'when' keyword), which causes this to be parsed
                // as a declaration pattern. lastToken will be 'w' (LeftToken) as opposed to 'constant' (TargetToken).
                // However we'd like to pretend that this is not the case and that we just a have single expression
                // with 'constant' as if the new word didn't exist. Let's do that by adjusting our variable.

                expressionOrPattern = declarationPattern.Type;
                return true;
            }

            return false;
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

            if (!(expression is TypeSyntax typeSyntax))
            {
                return false;
            }

            // We don't pass in the semantic model - let IsPotentialTypeName handle the cases where it's clear
            // from the syntax, but other than that, we need to do our own logic here.
            if (typeSyntax.IsPotentialTypeName(semanticModelOpt: null, cancellationToken))
            {
                return true;
            }

            var symbols = semanticModel.LookupName(typeSyntax, namespacesAndTypesOnly: false, cancellationToken);
            if (symbols.Length == 0)
            {
                // For all unknown identifiers except var, we return false (therefore 'when' will be offered),
                // because the user could later create a type with this name OR a constant. We give them both options.
                // But with var, when there is no type or constant with this name, we instead make the reasonable
                // assumption that the user didn't just type 'var' to then create a constant named 'var', but really
                // is about to declare a variable. Therefore we don't want to interfere with the declaration.
                // However note that if such a constant already exists, we do the right thing and do offer 'when'.
                return typeSyntax.IsVar;
            }

            return symbols.All(symbol => symbol is IAliasSymbol || symbol is ITypeSymbol);
        }
    }
}
