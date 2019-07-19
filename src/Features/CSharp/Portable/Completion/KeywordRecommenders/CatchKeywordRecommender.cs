// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class CatchKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public CatchKeywordRecommender()
            : base(SyntaxKind.CatchKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
            => context.SyntaxTree.IsCatchOrFinallyContext(position, context.LeftToken);
    }
}
