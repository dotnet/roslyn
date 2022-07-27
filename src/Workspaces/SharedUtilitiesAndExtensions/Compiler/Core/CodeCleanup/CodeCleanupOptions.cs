﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.OrganizeImports;
#endif

namespace Microsoft.CodeAnalysis.CodeCleanup;

[DataContract]
internal sealed record class CodeCleanupOptions(
    [property: DataMember] SyntaxFormattingOptions FormattingOptions,
    [property: DataMember] SimplifierOptions SimplifierOptions)
{
    [DataMember] public AddImportPlacementOptions AddImportOptions { get; init; } = AddImportPlacementOptions.Default;
    [DataMember] public DocumentFormattingOptions DocumentFormattingOptions { get; init; } = DocumentFormattingOptions.Default;

#if !CODE_STYLE
    public static CodeCleanupOptions GetDefault(HostLanguageServices languageServices)
        => new(
            FormattingOptions: SyntaxFormattingOptions.GetDefault(languageServices),
            SimplifierOptions: SimplifierOptions.GetDefault(languageServices));

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
    public abstract ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken);

    ValueTask<CodeCleanupOptions> OptionsProvider<CodeCleanupOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        => GetCodeCleanupOptionsAsync(languageServices, cancellationToken);

    async ValueTask<LineFormattingOptions> OptionsProvider<LineFormattingOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCodeCleanupOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).FormattingOptions.LineFormatting;

    async ValueTask<DocumentFormattingOptions> OptionsProvider<DocumentFormattingOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCodeCleanupOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).DocumentFormattingOptions;

    async ValueTask<SyntaxFormattingOptions> OptionsProvider<SyntaxFormattingOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCodeCleanupOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).FormattingOptions;

    async ValueTask<SimplifierOptions> OptionsProvider<SimplifierOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCodeCleanupOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).SimplifierOptions;

    async ValueTask<AddImportPlacementOptions> OptionsProvider<AddImportPlacementOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCodeCleanupOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).AddImportOptions;
}
#endif

internal static class CodeCleanupOptionsProviders
{
#if !CODE_STYLE
    public static CodeCleanupOptions GetCodeCleanupOptions(this AnalyzerConfigOptions options, bool allowImportsInHiddenRegions, CodeCleanupOptions? fallbackOptions, HostProjectServices languageServices)
    {
        var formattingOptions = options.GetSyntaxFormattingOptions(fallbackOptions?.FormattingOptions, languageServices);
        var simplifierOptions = options.GetSimplifierOptions(fallbackOptions?.SimplifierOptions, languageServices);
        var addImportOptions = options.GetAddImportPlacementOptions(allowImportsInHiddenRegions, fallbackOptions?.AddImportOptions, languageServices);
        var documentFormattingOptions = options.GetDocumentFormattingOptions(fallbackOptions?.DocumentFormattingOptions);

        return new CodeCleanupOptions(formattingOptions, simplifierOptions)
        {
            AddImportOptions = addImportOptions,
            DocumentFormattingOptions = documentFormattingOptions
        };
    }

    public static async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(this Document document, CodeCleanupOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetCodeCleanupOptions(document.AllowImportsInHiddenRegions(), fallbackOptions, document.Project.Services);
    }

    public static async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(this Document document, CodeCleanupOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await document.GetCodeCleanupOptionsAsync(await ((OptionsProvider<CodeCleanupOptions>)fallbackOptionsProvider).GetOptionsAsync(document.Project.LanguageServices, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
#endif
}

