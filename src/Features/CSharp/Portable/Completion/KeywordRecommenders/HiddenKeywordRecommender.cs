// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class HiddenKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public HiddenKeywordRecommender()
            : base(SyntaxKind.HiddenKeyword, isValidInPreprocessorContext: true)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // cases:
            //   #line |
            //   #line h|
            //   # line |
            //   # line h|

            var previousToken1 = context.TargetToken;
            var previousToken2 = previousToken1.GetPreviousToken(includeSkipped: true);

            return
                previousToken1.Kind() == SyntaxKind.LineKeyword &&
                previousToken2.Kind() == SyntaxKind.HashToken;
        }
    }
}
