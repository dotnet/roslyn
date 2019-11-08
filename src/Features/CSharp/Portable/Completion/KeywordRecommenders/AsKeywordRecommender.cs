// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class AsKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public AsKeywordRecommender()
            : base(SyntaxKind.AsKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return context is { IsInNonUserCode: false, IsIsOrAsOrSwitchExpressionContext: true };
        }
    }
}
