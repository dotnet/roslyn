// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.LanguageService;

internal interface IFileBannerFacts
{
    ImmutableArray<SyntaxTrivia> GetFileBanner(SyntaxNode root);
    ImmutableArray<SyntaxTrivia> GetFileBanner(SyntaxToken firstToken);
    string GetBannerText(SyntaxNode? documentationCommentTriviaSyntax, int maxBannerLength, CancellationToken cancellationToken);

    ImmutableArray<SyntaxTrivia> GetLeadingBlankLines(SyntaxNode node);

    TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(TSyntaxNode node) where TSyntaxNode : SyntaxNode;
    TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(
        TSyntaxNode node, out ImmutableArray<SyntaxTrivia> strippedTrivia) where TSyntaxNode : SyntaxNode;

    ImmutableArray<SyntaxTrivia> GetLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(TSyntaxNode node) where TSyntaxNode : SyntaxNode;

    TSyntaxNode GetNodeWithoutLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(TSyntaxNode node) where TSyntaxNode : SyntaxNode;
    TSyntaxNode GetNodeWithoutLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(TSyntaxNode node, out ImmutableArray<SyntaxTrivia> strippedTrivia) where TSyntaxNode : SyntaxNode;
}
