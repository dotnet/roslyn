// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class NullableKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public NullableKeywordRecommender()
            : base(SyntaxKind.NullableKeyword, isValidInPreprocessorContext: true)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // #nullable
            if (context.IsPreProcessorKeywordContext)
            {
                return true;
            }

            var previousToken1 = context.TargetToken;
            var previousToken2 = previousToken1.GetPreviousToken(includeSkipped: true);
            var previousToken3 = previousToken2.GetPreviousToken(includeSkipped: true);
            var previousToken4 = previousToken3.GetPreviousToken(includeSkipped: true);

            return
               // # pragma warning <action> |
               (previousToken1.Kind() == SyntaxKind.DisableKeyword || previousToken1.Kind() == SyntaxKind.RestoreKeyword ||
                previousToken1.Kind() == SyntaxKind.EnableKeyword) &&
               previousToken2.Kind() == SyntaxKind.WarningKeyword &&
               previousToken3.Kind() == SyntaxKind.PragmaKeyword &&
               previousToken4.Kind() == SyntaxKind.HashToken;
        }
    }
}
