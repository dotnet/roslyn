// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class AnnotationsKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public AnnotationsKeywordRecommender()
            : base(SyntaxKind.AnnotationsKeyword, isValidInPreprocessorContext: true)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var previousToken1 = context.TargetToken;
            var previousToken2 = previousToken1.GetPreviousToken(includeSkipped: true);
            var previousToken3 = previousToken2.GetPreviousToken(includeSkipped: true);

            // # nullable enable |
            // # nullable enable a|
            return
                (previousToken1.Kind() == SyntaxKind.EnableKeyword || previousToken1.Kind() == SyntaxKind.DisableKeyword || previousToken1.Kind() == SyntaxKind.RestoreKeyword) &&
                previousToken2.Kind() == SyntaxKind.NullableKeyword &&
                previousToken3.Kind() == SyntaxKind.HashToken;
        }
    }
}
