// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class RestoreKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public RestoreKeywordRecommender()
            : base(SyntaxKind.RestoreKeyword, isValidInPreprocessorContext: true)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // # pragma warning |
            // # pragma warning r|
            var previousToken1 = context.TargetToken;
            var previousToken2 = previousToken1.GetPreviousToken(includeSkipped: true);
            var previousToken3 = previousToken2.GetPreviousToken(includeSkipped: true);

            return
               previousToken1.Kind() == SyntaxKind.WarningKeyword &&
               previousToken2.Kind() == SyntaxKind.PragmaKeyword &&
               previousToken3.Kind() == SyntaxKind.HashToken;
        }
    }
}
