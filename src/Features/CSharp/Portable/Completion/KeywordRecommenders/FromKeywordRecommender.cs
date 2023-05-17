// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class FromKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public FromKeywordRecommender()
            : base(SyntaxKind.FromKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            return
                context.IsGlobalStatementContext ||
                syntaxTree.IsValidContextForFromClause(position, context.LeftToken, cancellationToken, semanticModelOpt: context.SemanticModel);
        }
    }
}
