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
    public static CodeCleanupOptions GetCodeCleanupOptions(this IOptionsReader options, LanguageServices languageServices, bool? allowImportsInHiddenRegions = null)
        => new()
        {
            FormattingOptions = options.GetSyntaxFormattingOptions(languageServices),
            SimplifierOptions = options.GetSimplifierOptions(languageServices),
            AddImportOptions = options.GetAddImportPlacementOptions(languageServices, allowImportsInHiddenRegions),
            DocumentFormattingOptions = options.GetDocumentFormattingOptions(),
        };

    public static async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable RS0030 // Do not use banned APIs
        return configOptions.GetCodeCleanupOptions(document.Project.LanguageServices.LanguageServices, document.AllowImportsInHiddenRegions());
#pragma warning restore RS0030 // Do not use banned APIs
#pragma warning restore CS0618 // Type or member is obsolete
    }

    public static CodeCleanupOptions GetDefault(LanguageServices languageServices)
        => new()
        {
            FormattingOptions = SyntaxFormattingOptionsProviders.GetDefault(languageServices),
            SimplifierOptions = SimplifierOptionsProviders.GetDefault(languageServices)
        };
}

