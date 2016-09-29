// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class IntoKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public IntoKeywordRecommender()
            : base(SyntaxKind.IntoKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                IsValidContextForJoin(context) ||
                IsValidContextForSelect(context) ||
                IsValidContextForGroup(context);
        }

        private bool IsValidContextForSelect(CSharpSyntaxContext context)
        {
            var token = context.TargetToken;

            var select = token.GetAncestor<SelectClauseSyntax>();
            if (select == null)
            {
                return false;
            }

            if (select.Expression.Width() == 0)
            {
                return false;
            }


            // cases:
            //   select x.|
            //   select x.i|
            var lastCompleteToken = token.GetPreviousTokenIfTouchingWord(context.Position);
            if (lastCompleteToken.Kind() == SyntaxKind.DotToken)
            {
                return false;
            }

            var lastToken = select.Expression.GetLastToken(includeSkipped: true);
            if (lastToken == token)
            {
                return true;
            }

            return false;
        }

        private bool IsValidContextForGroup(CSharpSyntaxContext context)
        {
            var token = context.TargetToken;

            var group = token.GetAncestor<GroupClauseSyntax>();
            if (group == null)
            {
                return false;
            }

            if (group.ByExpression.Width() == 0 ||
                group.GroupExpression.Width() == 0)
            {
                return false;
            }

            var lastToken = group.ByExpression.GetLastToken(includeSkipped: true);

            if (lastToken == token)
            {
                return true;
            }

            return false;
        }

        private static bool IsValidContextForJoin(CSharpSyntaxContext context)
        {
            // cases:
            //   join a in expr o1 equals o2 |
            //   join a in expr o1 equals o2 i|

            var token = context.TargetToken;
            var join = token.GetAncestor<JoinClauseSyntax>();

            if (join == null)
            {
                // happens for:
                //   join a in expr on o1 equals o2 e|
                if (!token.IntersectsWith(context.Position))
                {
                    return false;
                }

                token = token.GetPreviousToken(includeSkipped: true);
                join = token.GetAncestor<JoinClauseSyntax>();

                if (join == null)
                {
                    return false;
                }
            }

            var lastToken = join.RightExpression.GetLastToken(includeSkipped: true);

            // join a in expr on o1 equals o2 |
            if (token == lastToken &&
                !lastToken.IntersectsWith(context.Position))
            {
                return true;
            }

            return false;
        }
    }
}
