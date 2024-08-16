// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.LanguageService;

internal static class IFileBannerFactsExtensions
{
    public static ImmutableArray<SyntaxTrivia> GetTriviaAfterLeadingBlankLines(
        this IFileBannerFacts bannerService, SyntaxNode node)
    {
        var leadingBlankLines = bannerService.GetLeadingBlankLines(node);
        return node.GetLeadingTrivia().Skip(leadingBlankLines.Length).ToImmutableArray();
    }
}
