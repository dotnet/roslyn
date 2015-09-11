// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            return
                IsValidContextInForEachClause(context) ||
                IsValidContextInFromClause(context, cancellationToken) ||
                IsValidContextInJoinClause(context, cancellationToken) ||
                context.TargetToken.IsTypeParameterVarianceContext();
        }

        private bool IsValidContextInForEachClause(CSharpSyntaxContext context)
        {
            // cases:
            //   foreach (var v |
            //   foreach (var v i|

            var token = context.TargetToken;

            if (token.Kind() == SyntaxKind.IdentifierToken)
            {
                var statement = token.GetAncestor<ForEachStatementSyntax>();
                if (statement != null && token == statement.Identifier)
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

                var fromClause = token.Parent as FromClauseSyntax;
                if (fromClause != null)
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
