﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
