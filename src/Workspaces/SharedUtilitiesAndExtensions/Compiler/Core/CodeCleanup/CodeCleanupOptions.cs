// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.OrganizeImports;
#endif

namespace Microsoft.CodeAnalysis.CodeCleanup;

[DataContract]
internal sealed record class CodeCleanupOptions
{
    [DataMember] public required SyntaxFormattingOptions FormattingOptions { get; init; }
    [DataMember] public required SimplifierOptions SimplifierOptions { get; init; }
    [DataMember] public AddImportPlacementOptions AddImportOptions { get; init; } = AddImportPlacementOptions.Default;
    [DataMember] public DocumentFormattingOptions DocumentFormattingOptions { get; init; } = DocumentFormattingOptions.Default;

#if !CODE_STYLE
    public static CodeCleanupOptions GetDefault(LanguageServices languageServices)
        => new()
        {
            FormattingOptions = SyntaxFormattingOptions.GetDefault(languageServices),
            SimplifierOptions = SimplifierOptions.GetDefault(languageServices)
        };

    public OrganizeImportsOptions GetOrganizeImportsOptions()
        => new()
        {
            SeparateImportDirectiveGroups = FormattingOptions.SeparateImportDirectiveGroups,
            PlaceSystemNamespaceFirst = AddImportOptions.PlaceSystemNamespaceFirst,
            NewLine = FormattingOptions.LineFormatting.NewLine,
        };
#endif
}

internal interface CodeCleanupOptionsProvider :
#if !CODE_STYLE
    OptionsProvider<CodeCleanupOptions>,
#endif
    SyntaxFormattingOptionsProvider,
    SimplifierOptionsProvider,
    AddImportPlacementOptionsProvider,
    DocumentFormattingOptionsProvider
{
}

#if !CODE_STYLE
internal abstract class AbstractCodeCleanupOptionsProvider : CodeCleanupOptionsProvider
{
    public abstract ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken);

    ValueTask<CodeCleanupOptions> OptionsProvider<CodeCleanupOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => GetCodeCleanupOptionsAsync(languageServices, cancellationToken);

    async ValueTask<LineFormattingOptions> OptionsProvider<LineFormattingOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCodeCleanupOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).FormattingOptions.LineFormatting;

    async ValueTask<DocumentFormattingOptions> OptionsProvider<DocumentFormattingOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCodeCleanupOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).DocumentFormattingOptions;

    async ValueTask<SyntaxFormattingOptions> OptionsProvider<SyntaxFormattingOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCodeCleanupOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).FormattingOptions;

    async ValueTask<SimplifierOptions> OptionsProvider<SimplifierOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCodeCleanupOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).SimplifierOptions;

    async ValueTask<AddImportPlacementOptions> OptionsProvider<AddImportPlacementOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCodeCleanupOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).AddImportOptions;
}

#endif

internal static class CodeCleanupOptionsProviders
{
#if !CODE_STYLE
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
        return configOptions.GetCodeCleanupOptions(document.Project.Services, document.AllowImportsInHiddenRegions());
    }
#endif
}

