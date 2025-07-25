// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CodeCleanup;

internal static class CodeCleanupOptionsProviders
{
    extension(IOptionsReader options)
    {
        public CodeCleanupOptions GetCodeCleanupOptions(LanguageServices languageServices, bool? allowImportsInHiddenRegions = null)
        => new()
        {
            FormattingOptions = options.GetSyntaxFormattingOptions(languageServices),
            SimplifierOptions = options.GetSimplifierOptions(languageServices),
            AddImportOptions = options.GetAddImportPlacementOptions(languageServices, allowImportsInHiddenRegions),
            DocumentFormattingOptions = options.GetDocumentFormattingOptions(),
        };
    }

    extension(Document document)
    {
        public async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(CancellationToken cancellationToken)
        {
            var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            return configOptions.GetCodeCleanupOptions(document.Project.GetExtendedLanguageServices().LanguageServices, document.AllowImportsInHiddenRegions());
        }
    }

    public static CodeCleanupOptions GetDefault(LanguageServices languageServices)
        => new()
        {
            FormattingOptions = SyntaxFormattingOptionsProviders.GetDefault(languageServices),
            SimplifierOptions = SimplifierOptionsProviders.GetDefault(languageServices)
        };
}

