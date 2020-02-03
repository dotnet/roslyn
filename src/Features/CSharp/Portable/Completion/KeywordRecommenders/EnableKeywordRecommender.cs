// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class EnableKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public EnableKeywordRecommender()
            : base(SyntaxKind.EnableKeyword, isValidInPreprocessorContext: true)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var previousToken1 = context.TargetToken;
            var previousToken2 = previousToken1.GetPreviousToken(includeSkipped: true);

            // # nullable |
            // # nullable e|
            if (previousToken1.Kind() == SyntaxKind.NullableKeyword &&
                previousToken2.Kind() == SyntaxKind.HashToken)
            {
                return true;
            }

            var previousToken3 = previousToken2.GetPreviousToken(includeSkipped: true);

            return
               // # pragma warning |
               // # pragma warning e|
               previousToken1.Kind() == SyntaxKind.WarningKeyword &&
               previousToken2.Kind() == SyntaxKind.PragmaKeyword &&
               previousToken3.Kind() == SyntaxKind.HashToken;
        }
    }
}
