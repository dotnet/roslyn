// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class UndefKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public UndefKeywordRecommender()
            : base(SyntaxKind.UndefKeyword, isValidInPreprocessorContext: true)
        {
        }

        protected override async Task<bool> IsValidContextAsync(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            return
                context.IsPreProcessorKeywordContext &&
                await syntaxTree.IsBeforeFirstTokenAsync(position, cancellationToken).ConfigureAwait(false);
        }
    }
}
