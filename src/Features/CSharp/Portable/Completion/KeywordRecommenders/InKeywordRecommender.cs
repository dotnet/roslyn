﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
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
                syntaxTree.IsParameterModifierContext(position, context.LeftToken, cancellationToken, includeOperators: true) ||
                syntaxTree.IsAnonymousMethodParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                syntaxTree.IsPossibleLambdaParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                context.TargetToken.IsConstructorOrMethodParameterArgumentContext() ||
                context.TargetToken.IsTypeParameterVarianceContext();
        }

        private bool IsValidContextInForEachClause(CSharpSyntaxContext context)
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

        private bool IsValidContextInFromClause(CSharpSyntaxContext context, CancellationToken cancellationToken)
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

        private bool IsValidContextInJoinClause(CSharpSyntaxContext context, CancellationToken cancellationToken)
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
                        joinClause.Type.IsKind(SyntaxKind.IdentifierName) &&
                        token == ((IdentifierNameSyntax)joinClause.Type).Identifier &&
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
