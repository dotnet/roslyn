// Licensed to the .NET Foundation under one or more agreements.
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

internal interface ICleanCodeGenerationOptionsProvider :
#if !CODE_STYLE
    IOptionsProvider<CleanCodeGenerationOptions>,
#endif
    ICodeGenerationOptionsProvider,
    ICodeCleanupOptionsProvider,
    ICodeAndImportGenerationOptionsProvider
{
}

#if !CODE_STYLE
internal abstract class AbstractCleanCodeGenerationOptionsProvider : AbstractCodeCleanupOptionsProvider, ICleanCodeGenerationOptionsProvider
{
    public abstract ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken);

    public sealed override async ValueTask<CodeCleanupOptions> GetCodeCleanupOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCleanCodeGenerationOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).CleanupOptions;

    ValueTask<CleanCodeGenerationOptions> IOptionsProvider<CleanCodeGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => GetCleanCodeGenerationOptionsAsync(languageServices, cancellationToken);

    async ValueTask<CodeAndImportGenerationOptions> IOptionsProvider<CodeAndImportGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCleanCodeGenerationOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).CodeAndImportGenerationOptions;

    async ValueTask<CodeGenerationOptions> IOptionsProvider<CodeGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
        => (await GetCleanCodeGenerationOptionsAsync(languageServices, cancellationToken).ConfigureAwait(false)).GenerationOptions;

    async ValueTask<NamingStylePreferences> IOptionsProvider<NamingStylePreferences>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
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

    public static async ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(this Document document, ICleanCodeGenerationOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await document.GetCleanCodeGenerationOptionsAsync(await ((IOptionsProvider<CleanCodeGenerationOptions>)fallbackOptionsProvider).GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private sealed class Provider(IOptionsProvider<CleanCodeGenerationOptions> provider) : AbstractCleanCodeGenerationOptionsProvider
    {
        public override ValueTask<CleanCodeGenerationOptions> GetCleanCodeGenerationOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => provider.GetOptionsAsync(languageServices, cancellationToken);
    }

    public static ICleanCodeGenerationOptionsProvider ToCleanCodeGenerationOptionsProvider(this IOptionsProvider<CleanCodeGenerationOptions> provider)
        => new Provider(provider);
}
#endif
