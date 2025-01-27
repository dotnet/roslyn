// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.AddFileBanner;

internal static class AddFileBannerHelpers
{
    public static async Task<Document> CopyBannerAsync(
        Document destinationDocument,
        string? destinationFilePath,
        Document sourceDocument,
        CancellationToken cancellationToken)
    {
        var bannerService = destinationDocument.GetRequiredLanguageService<IFileBannerFactsService>();

        var fromRoot = await sourceDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var sourceBanner = bannerService.GetFileBanner(fromRoot);

        sourceBanner = UpdateEmbeddedFileNames(
            sourceDocument, destinationFilePath, sourceBanner, bannerService.CreateTrivia);

        var destinationRoot = await destinationDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var destinationBanner = bannerService.GetFileBanner(destinationRoot);

        var firstToken = destinationRoot.GetFirstToken();
        var newRoot = destinationRoot.ReplaceToken(
            firstToken,
            firstToken.WithLeadingTrivia(
                sourceBanner.Concat(firstToken.LeadingTrivia.Skip(destinationBanner.Length))));
        return destinationDocument.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Looks at <paramref name="banner"/> to see if it contains the name of <paramref name="sourceDocument"/>
    /// in it.  If so, those names will be replaced with <paramref name="destinationFilePath"/>.
    /// </summary>
    private static ImmutableArray<SyntaxTrivia> UpdateEmbeddedFileNames(
        Document sourceDocument,
        string? destinationFilePath,
        ImmutableArray<SyntaxTrivia> banner,
        Func<SyntaxTrivia, string, SyntaxTrivia> createTrivia)
    {
        var sourceName = IOUtilities.PerformIO(() => Path.GetFileName(sourceDocument.FilePath));
        var destinationName = IOUtilities.PerformIO(() => Path.GetFileName(destinationFilePath));
        if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(destinationName))
            return banner;

        var result = new FixedSizeArrayBuilder<SyntaxTrivia>(banner.Length);
        foreach (var trivia in banner)
        {
            var updated = createTrivia(trivia, trivia.ToFullString().Replace(sourceName, destinationName));
            result.Add(updated);
        }

        return result.MoveToImmutable();
    }
}
