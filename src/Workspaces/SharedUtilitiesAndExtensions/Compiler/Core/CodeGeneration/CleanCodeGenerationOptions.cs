// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration;

[DataContract]
internal readonly record struct CleanCodeGenerationOptions(
    [property: DataMember(Order = 0)] CodeGenerationOptions GenerationOptions,
    [property: DataMember(Order = 1)] CodeCleanupOptions CleanupOptions)
{
#if !CODE_STYLE
    public static CleanCodeGenerationOptions GetDefault(HostLanguageServices languageServices)
        => new(CodeGenerationOptions.GetDefault(languageServices),
               CodeCleanupOptions.GetDefault(languageServices));

    public CodeAndImportGenerationOptions CodeAndImportGenerationOptions
        => new(GenerationOptions, CleanupOptions.AddImportOptions);
#endif
}

internal interface CleanCodeGenerationOptionsProvider :
#if !CODE_STYLE
    OptionsProvider<CleanCodeGenerationOptions>,
#endif
    CodeGenerationOptionsProvider,
    CodeCleanupOptionsProvider,
    CodeAndImportGenerationOptionsProvider
{
}

#if !CODE_STYLE
internal abstract class AbstractCleanCodeGenerationOptionsProvider : AbstractCodeCleanupOptionsProvider, CleanCodeGenerationOptionsProvider
{
    public abstract ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken);

    public sealed override async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCleanCodeGenerationOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).CleanupOptions;

    ValueTask<CleanCodeGenerationOptions> OptionsProvider<CleanCodeGenerationOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        => GetCleanCodeGenerationOptionsAsync(languageServices, cancellationToken);

    async ValueTask<CodeAndImportGenerationOptions> OptionsProvider<CodeAndImportGenerationOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCleanCodeGenerationOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).CodeAndImportGenerationOptions;

    async ValueTask<CodeGenerationOptions> OptionsProvider<CodeGenerationOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCleanCodeGenerationOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).GenerationOptions;

    async ValueTask<NamingStylePreferences> OptionsProvider<NamingStylePreferences>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCleanCodeGenerationOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).GenerationOptions.NamingStyle;
}

internal static class CleanCodeGenerationOptionsProviders
{
    public static async ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(this Document document, CleanCodeGenerationOptions fallbackOptions, CancellationToken cancellationToken)
        => new(
            await document.GetCodeGenerationOptionsAsync(fallbackOptions.GenerationOptions, cancellationToken).ConfigureAwait(false),
            await document.GetCodeCleanupOptionsAsync(fallbackOptions.CleanupOptions, cancellationToken).ConfigureAwait(false));

    public static async ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(this Document document, CleanCodeGenerationOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await document.GetCleanCodeGenerationOptionsAsync(await ((OptionsProvider<CleanCodeGenerationOptions>)fallbackOptionsProvider).GetOptionsAsync(document.Project.LanguageServices, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
}
#endif
