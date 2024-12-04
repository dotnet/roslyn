// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;


namespace Microsoft.CodeAnalysis.AddFileBanner;

internal static class AddFileBannerHelpers
{
    private const string BannerFileNamePlaceholder = "{filename}";

    public static string GetBannerTextWithoutFileName(Document document, ImmutableArray<SyntaxTrivia> banner)
    {
        var bannerText = banner.Select(trivia => trivia.ToFullString()).Join(string.Empty);

        var fileName = IOUtilities.PerformIO(() => Path.GetFileName(document.FilePath));
        if (!string.IsNullOrEmpty(fileName))
            bannerText = bannerText.Replace(fileName, BannerFileNamePlaceholder);

        return bannerText;
    }

    public static ImmutableArray<SyntaxTrivia> GetBannerTriviaWithFileName(string bannerText, Document document, string? fileName)
    {
        if (!string.IsNullOrEmpty(fileName))
            bannerText = bannerText.Replace(BannerFileNamePlaceholder, fileName);

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var token = syntaxFacts.ParseToken(bannerText);

        var bannerService = document.GetRequiredLanguageService<IFileBannerFactsService>();
        return bannerService.GetFileBanner(token);
    }
}
