// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FileHeaders;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddFileBanner
{
    internal abstract class AbstractAddFileBannerNewDocumentFormattingProvider : INewDocumentFormattingProvider
    {
        protected abstract SyntaxGenerator SyntaxGenerator { get; }
        protected abstract SyntaxGeneratorInternal SyntaxGeneratorInternal { get; }
        protected abstract AbstractFileHeaderHelper FileHeaderHelper { get; }

        public async Task<Document> FormatNewDocumentAsync(Document document, Document? hintDocument, CodeCleanupOptions options, CancellationToken cancellationToken)
        {
            var rootToFormat = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Apply file header preferences
            var fileHeaderTemplate = options.DocumentFormattingOptions.FileHeaderTemplate;
            if (!string.IsNullOrEmpty(fileHeaderTemplate))
            {
                var newLineTrivia = SyntaxGeneratorInternal.EndOfLine(options.FormattingOptions.NewLine);
                var rootWithFileHeader = await AbstractFileHeaderCodeFixProvider.GetTransformedSyntaxRootAsync(
                        SyntaxGenerator.SyntaxFacts,
                        FileHeaderHelper,
                        newLineTrivia,
                        document,
                        fileHeaderTemplate,
                        cancellationToken).ConfigureAwait(false);

                return document.WithSyntaxRoot(rootWithFileHeader);
            }
            else if (hintDocument is not null)
            {
                // If there is no file header preference, see if we can use the one in the hint document
                var bannerService = hintDocument.GetRequiredLanguageService<IFileBannerFactsService>();
                var hintSyntaxRoot = await hintDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var fileBanner = bannerService.GetFileBanner(hintSyntaxRoot);

                var rootWithBanner = rootToFormat.WithPrependedLeadingTrivia(fileBanner);
                return document.WithSyntaxRoot(rootWithBanner);
            }

            return document;
        }
    }
}
