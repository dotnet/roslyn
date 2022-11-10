// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class WhileKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public WhileKeywordRecommender()
            : base(SyntaxKind.WhileKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.IsStatementContext ||
                context.IsGlobalStatementContext)
            {
                return true;
            }

            // do {
            // } |

            // do {
            // } w|

            // Note: the case of
            //   do 
            //     Goo();
            //   |
            // is taken care of in the IsStatementContext case.

            var token = context.TargetToken;

            if (token.Kind() == SyntaxKind.CloseBraceToken &&
                token.Parent.IsKind(SyntaxKind.Block) &&
                token.Parent.IsParentKind(SyntaxKind.DoStatement))
            {
                return true;
            }

            return false;
        }
    }
}
