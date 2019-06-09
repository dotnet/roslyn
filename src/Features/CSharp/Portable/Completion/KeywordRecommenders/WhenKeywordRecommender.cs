// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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
            out SyntaxNodeOrToken nodeOrToken)
        {
            nodeOrToken = null;

            var switchLabel = context.TargetToken.GetAncestor<SwitchLabelSyntax>();
            if (switchLabel == null)
            {
                return false;
            }

            var expressionOrPattern = switchLabel.ChildNodes().FirstOrDefault();
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

            if (expressionOrPattern.GetLastToken(includeZeroWidth: true).IsMissing)
            {
                return false;
            }

            // There are zero width tokens that are not "missing" (inserted by the parser) because they are optional,
            // such as the identifier in a recursive pattern. We want to ignore those now, so we exclude all zero width.

            var lastToken = expressionOrPattern.GetLastToken(includeZeroWidth: false);
            if (lastToken == context.TargetToken)
            {
                nodeOrToken = expressionOrPattern;
                return true;
            }

            if (lastToken == context.LeftToken)
            {
                // The user is typing a new word (might be a partially written 'when' keyword),
                // which is part of the pattern as opposed to appearing outside of it. In a few special cases,
                // this word can actually be replaced with 'when' and the resulting pattern would still be valid.

                if (expressionOrPattern is DeclarationPatternSyntax declarationPattern)
                {
                    // The new token causes this to be parsed as a declaration pattern:
                    // case constant w| ('w' = LeftToken, 'constant' = TargetToken)

                    // However 'constant' itself might end up being a valid constant pattern.
                    // We will pretend as if 'w' didn't exist so that the later check
                    // for whether 'constant' is actually a type can still work properly.
                    nodeOrToken = declarationPattern.Type;
                    return true;
                }

                if (expressionOrPattern is VarPatternSyntax varPattern)
                {
                    // The new token causes this to be parsed as a var pattern:
                    // case var w| ('w' = LeftToken, 'var' = TargetToken)

                    // However 'var' itself might end up being a valid constant pattern.
                    nodeOrToken = varPattern.VarKeyword;
                    return true;
                }

                if (expressionOrPattern is RecursivePatternSyntax recursivePattern)
                {
                    // The new token is consumed as the identifier in a recursive pattern:
                    // case { } w| ('w' = LeftToken, '}' = TargetToken)

                    // However the identifier is optional and can be replaced by 'when'.
                    nodeOrToken = recursivePattern.Type;
                    return true;
                }

                // In other cases, this would not be true because the pattern would be incomplete without this word:
                // case 1 + w|
            }

            return false;
        }

        private static bool IsTypeName(
            SyntaxNodeOrToken nodeOrToken,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // Syntactically, everything works out. We're in a pretty good spot to show 'when' now.
            // But let's not do it just yet... Consider these cases:
            // case SyntaxNode |
            // case SyntaxNode w|
            // If what we have here is known to be a type, we don't want to clutter the variable name suggestion list
            // with 'when' since we know that the resulting code would be semantically invalid.

            bool isVar;
            ImmutableArray<ISymbol> symbols;

            if (nodeOrToken.IsNode)
            {
                var node = nodeOrToken.AsNode();
                var expression = node as ExpressionSyntax
                    ?? (node as ConstantPatternSyntax)?.Expression;

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

                isVar = typeSyntax.IsVar;
                symbols = semanticModel.LookupName(typeSyntax, namespacesAndTypesOnly: false, cancellationToken);
            }
            else
            {
                // In a var pattern, the 'var' keyword is not wrapped in a type syntax, so we might just have a naked token.
                var token = nodeOrToken.AsToken();

                isVar = token.Text == SyntaxFacts.GetText(SyntaxKind.VarKeyword);
                symbols = semanticModel.LookupSymbols(nodeOrToken.AsToken().SpanStart, null, token.Text);
            }

            if (symbols.Length == 0)
            {
                // For all unknown identifiers except var, we return false (therefore 'when' will be offered),
                // because the user could later create a type with this name OR a constant. We give them both options.
                // But with var, when there is no type or constant with this name, we instead make the reasonable
                // assumption that the user didn't just type 'var' to then create a constant named 'var', but really
                // is about to declare a variable. Therefore we don't want to interfere with the declaration.
                // However note that if such a constant already exists, we do the right thing and do offer 'when'.
                return isVar;
            }

            return symbols.All(symbol => symbol is IAliasSymbol || symbol is ITypeSymbol);
        }
    }
}
