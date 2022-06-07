// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class InKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public InKeywordRecommender()
            : base(SyntaxKind.InKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            return
                IsValidContextInForEachClause(context) ||
                IsValidContextInFromClause(context, cancellationToken) ||
                IsValidContextInJoinClause(context, cancellationToken) ||
                IsInParameterModifierContext(position, context) ||
                syntaxTree.IsAnonymousMethodParameterModifierContext(position, context.LeftToken) ||
                syntaxTree.IsPossibleLambdaParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                context.TargetToken.IsConstructorOrMethodParameterArgumentContext() ||
                context.TargetToken.IsTypeParameterVarianceContext();
        }

        private static bool IsInParameterModifierContext(int position, CSharpSyntaxContext context)
        {
            if (context.SyntaxTree.IsParameterModifierContext(
                    position, context.LeftToken, includeOperators: true, out var parameterIndex, out var previousModifier))
            {
                if (previousModifier == SyntaxKind.None)
                {
                    return true;
                }

                if (previousModifier == SyntaxKind.ThisKeyword &&
                    parameterIndex == 0 &&
                    context.SyntaxTree.IsPossibleExtensionMethodContext(context.LeftToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidContextInForEachClause(CSharpSyntaxContext context)
        {
            // cases:
            //   foreach (var v |
            //   foreach (var v i|
            //   foreach (var (x, y) |

            var token = context.TargetToken;

            if (token.Kind() == SyntaxKind.IdentifierToken)
            {
                if (token.Parent is ForEachStatementSyntax statement && token == statement.Identifier)
                {
                    return true;
                }
            }
            else if (token.Kind() == SyntaxKind.CloseParenToken)
            {
                var statement = token.GetAncestor<ForEachVariableStatementSyntax>();
                if (statement != null && token.Span.End == statement.Variable.Span.End)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidContextInFromClause(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var token = context.TargetToken;

            if (token.Kind() == SyntaxKind.IdentifierToken)
            {
                // case:
                //   from x |
                if (token.GetPreviousToken(includeSkipped: true).IsKindOrHasMatchingText(SyntaxKind.FromKeyword))
                {
                    var typeSyntax = token.Parent as TypeSyntax;
                    if (!typeSyntax.IsPotentialTypeName(context.SemanticModel, cancellationToken))
                    {
                        return true;
                    }
                }

                if (token.Parent is FromClauseSyntax fromClause)
                {
                    // case:
                    //   from int x |
                    if (token == fromClause.Identifier && fromClause.Type != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsValidContextInJoinClause(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var token = context.TargetToken;

            if (token.Kind() == SyntaxKind.IdentifierToken)
            {
                var joinClause = token.Parent.FirstAncestorOrSelf<JoinClauseSyntax>();
                if (joinClause != null)
                {
                    // case:
                    //   join int x |
                    if (token == joinClause.Identifier && joinClause.Type != null)
                    {
                        return true;
                    }

                    // case:
                    //   join x |
                    if (joinClause.Type != null &&
                        joinClause.Type.IsKind(SyntaxKind.IdentifierName, out IdentifierNameSyntax joinIdentifier) &&
                        token == joinIdentifier.Identifier &&
                        !joinClause.Type.IsPotentialTypeName(context.SemanticModel, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
