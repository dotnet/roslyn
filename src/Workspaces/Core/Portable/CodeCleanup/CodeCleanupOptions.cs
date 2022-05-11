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
    [property: DataMember(Order = 2)] AddImportPlacementOptions AddImportOptions)
{
    public static CodeCleanupOptions GetDefault(HostLanguageServices languageServices)
        => new(
            FormattingOptions: SyntaxFormattingOptions.GetDefault(languageServices),
            SimplifierOptions: SimplifierOptions.GetDefault(languageServices),
            AddImportOptions: AddImportPlacementOptions.Default);
}

internal interface CodeCleanupOptionsProvider :
    OptionsProvider<CodeCleanupOptions>,
    SyntaxFormattingOptionsProvider,
    SimplifierOptionsProvider,
    AddImportPlacementOptionsProvider
{
}

internal abstract class AbstractCodeCleanupOptionsProvider : CodeCleanupOptionsProvider
{
    public abstract ValueTask<CodeCleanupOptions> GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken);

    async ValueTask<SyntaxFormattingOptions> OptionsProvider<SyntaxFormattingOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).FormattingOptions;

    async ValueTask<SimplifierOptions> OptionsProvider<SimplifierOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).SimplifierOptions;

    async ValueTask<AddImportPlacementOptions> OptionsProvider<AddImportPlacementOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).AddImportOptions;
}

internal static class CodeCleanupOptionsProviders
{
    public static async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(this Document document, CodeCleanupOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions?.FormattingOptions, cancellationToken).ConfigureAwait(false);
        var simplifierOptions = await document.GetSimplifierOptionsAsync(fallbackOptions?.SimplifierOptions, cancellationToken).ConfigureAwait(false);
        var addImportOptions = await document.GetAddImportPlacementOptionsAsync(fallbackOptions?.AddImportOptions, cancellationToken).ConfigureAwait(false);
        return new CodeCleanupOptions(formattingOptions, simplifierOptions, addImportOptions);
    }

    public static async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(this Document document, CodeCleanupOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await document.GetCodeCleanupOptionsAsync(await ((OptionsProvider<CodeCleanupOptions>)fallbackOptionsProvider).GetOptionsAsync(document.Project.LanguageServices, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
}

