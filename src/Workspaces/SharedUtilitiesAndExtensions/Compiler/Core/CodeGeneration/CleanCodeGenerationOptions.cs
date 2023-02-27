﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration;

[DataContract]
internal readonly record struct CleanCodeGenerationOptions
{
    [DataMember]
    public required CodeGenerationOptions GenerationOptions { get; init; }

    [DataMember]
    public required CodeCleanupOptions CleanupOptions { get; init; }

#if !CODE_STYLE
    public static CleanCodeGenerationOptions GetDefault(LanguageServices languageServices)
        => new()
        {
            GenerationOptions = CodeGenerationOptions.GetDefault(languageServices),
            CleanupOptions = CodeCleanupOptions.GetDefault(languageServices)
        };

    public CodeAndImportGenerationOptions CodeAndImportGenerationOptions
        => new()
        {
            GenerationOptions = GenerationOptions,
            AddImportOptions = CleanupOptions.AddImportOptions
        };
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
    public abstract ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken);

    public sealed override async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCleanCodeGenerationOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).CleanupOptions;

    ValueTask<CleanCodeGenerationOptions> OptionsProvider<CleanCodeGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => GetCleanCodeGenerationOptionsAsync(languageServices, cancellationToken);

    async ValueTask<CodeAndImportGenerationOptions> OptionsProvider<CodeAndImportGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCleanCodeGenerationOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).CodeAndImportGenerationOptions;

    async ValueTask<CodeGenerationOptions> OptionsProvider<CodeGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCleanCodeGenerationOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).GenerationOptions;

    async ValueTask<NamingStylePreferences> OptionsProvider<NamingStylePreferences>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCleanCodeGenerationOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).GenerationOptions.NamingStyle;
}

internal static class CleanCodeGenerationOptionsProviders
{
    public static async ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(this Document document, CleanCodeGenerationOptions fallbackOptions, CancellationToken cancellationToken)
        => new()
        {
            GenerationOptions = await document.GetCodeGenerationOptionsAsync(fallbackOptions.GenerationOptions, cancellationToken).ConfigureAwait(false),
            CleanupOptions = await document.GetCodeCleanupOptionsAsync(fallbackOptions.CleanupOptions, cancellationToken).ConfigureAwait(false)
        };

    public static async ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(this Document document, CleanCodeGenerationOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await document.GetCleanCodeGenerationOptionsAsync(await ((OptionsProvider<CleanCodeGenerationOptions>)fallbackOptionsProvider).GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
}
#endif
