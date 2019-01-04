// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class SafeOnlyKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public SafeOnlyKeywordRecommender()
            : base(SyntaxKind.SafeOnlyKeyword, isValidInPreprocessorContext: true)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var previousToken1 = context.TargetToken;
            var previousToken2 = previousToken1.GetPreviousToken(includeSkipped: true);

            // # nullable |
            // # nullable s|
            return previousToken1.Kind() == SyntaxKind.NullableKeyword &&
                previousToken2.Kind() == SyntaxKind.HashToken;
        }
    }
}
