// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class LoadKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public LoadKeywordRecommender()
        : base(SyntaxKind.LoadKeyword, isValidInPreprocessorContext: true)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        var syntaxTree = context.SyntaxTree;
        return
            context.IsPreProcessorKeywordContext &&
            syntaxTree.IsScript() &&
            syntaxTree.IsBeforeFirstToken(position, cancellationToken);
    }
}
