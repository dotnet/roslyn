// Licensed to the .NET Foundation under one or more agreements.
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

namespace Microsoft.CodeAnalysis.CodeCleanup;

[DataContract]
internal readonly record struct CodeCleanupOptions(
    [property: DataMember(Order = 0)] SyntaxFormattingOptions FormattingOptions,
    [property: DataMember(Order = 1)] SimplifierOptions SimplifierOptions,
    [property: DataMember(Order = 2)] AddImportPlacementOptions AddImportOptions,
    [property: DataMember(Order = 3)] DocumentFormattingOptions DocumentFormattingOptions)
{
#if !CODE_STYLE
    public static CodeCleanupOptions GetDefault(HostLanguageServices languageServices)
        => new(
            FormattingOptions: SyntaxFormattingOptions.GetDefault(languageServices),
            SimplifierOptions: SimplifierOptions.GetDefault(languageServices),
            AddImportOptions: AddImportPlacementOptions.Default,
            DocumentFormattingOptions: DocumentFormattingOptions.Default);
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
    public static async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(this Document document, CodeCleanupOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions?.FormattingOptions, cancellationToken).ConfigureAwait(false);
        var simplifierOptions = await document.GetSimplifierOptionsAsync(fallbackOptions?.SimplifierOptions, cancellationToken).ConfigureAwait(false);
        var addImportOptions = await document.GetAddImportPlacementOptionsAsync(fallbackOptions?.AddImportOptions, cancellationToken).ConfigureAwait(false);
        var documentFormattingOptions = await document.GetDocumentFormattingOptionsAsync(fallbackOptions?.DocumentFormattingOptions, cancellationToken).ConfigureAwait(false);
        return new CodeCleanupOptions(formattingOptions, simplifierOptions, addImportOptions, documentFormattingOptions);
    }

    public static async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(this Document document, CodeCleanupOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await document.GetCodeCleanupOptionsAsync(await ((OptionsProvider<CodeCleanupOptions>)fallbackOptionsProvider).GetOptionsAsync(document.Project.LanguageServices, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
#endif
}

