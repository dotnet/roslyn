// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.CodeCleanup;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration;

/// <summary>
/// Document-specific options for controlling the code produced by code generation.
/// </summary>
internal record CodeGenerationOptions
{
    /// <summary>
    /// Language agnostic defaults.
    /// </summary>
    internal static readonly CodeGenerationOptions CommonDefaults = new();

    [DataMember] public NamingStylePreferences NamingStyle { get; init; } = NamingStylePreferences.Default;

    private protected CodeGenerationOptions()
    {
    }

    private protected CodeGenerationOptions(IOptionsReader options, CodeGenerationOptions fallbackOptions, string language)
    {
        NamingStyle = options.GetOption(NamingStyleOptions.NamingPreferences, language, fallbackOptions.NamingStyle);
    }

#if !CODE_STYLE
    public static CodeGenerationOptions GetDefault(LanguageServices languageServices)
        => languageServices.GetRequiredService<ICodeGenerationService>().DefaultOptions;
#endif
}

[DataContract]
internal readonly record struct CodeAndImportGenerationOptions
{
    [DataMember]
    public required CodeGenerationOptions GenerationOptions { get; init; }

    [DataMember]
    public required AddImportPlacementOptions AddImportOptions { get; init; }

#if !CODE_STYLE
    internal static CodeAndImportGenerationOptions GetDefault(LanguageServices languageServices)
        => new()
        {
            GenerationOptions = CodeGenerationOptions.GetDefault(languageServices),
            AddImportOptions = AddImportPlacementOptions.Default
        };

    internal ICodeAndImportGenerationOptionsProvider CreateProvider()
        => new Provider(this);

    private sealed class Provider(CodeAndImportGenerationOptions options) : ICodeAndImportGenerationOptionsProvider
    {
        ValueTask<CodeAndImportGenerationOptions> IOptionsProvider<CodeAndImportGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options);

        ValueTask<CodeGenerationOptions> IOptionsProvider<CodeGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options.GenerationOptions);

        ValueTask<NamingStylePreferences> IOptionsProvider<NamingStylePreferences>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options.GenerationOptions.NamingStyle);

        ValueTask<AddImportPlacementOptions> IOptionsProvider<AddImportPlacementOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options.AddImportOptions);
    }
#endif
}

internal interface ICodeGenerationOptionsProvider :
#if !CODE_STYLE
    IOptionsProvider<CodeGenerationOptions>,
#endif
    INamingStylePreferencesProvider
{
}

internal interface ICodeAndImportGenerationOptionsProvider :
#if !CODE_STYLE
    IOptionsProvider<CodeAndImportGenerationOptions>,
#endif
    ICodeGenerationOptionsProvider,
    IAddImportPlacementOptionsProvider
{
}

internal static class CodeGenerationOptionsProviders
{
#if !CODE_STYLE
    public static CodeGenerationOptions GetCodeGenerationOptions(this IOptionsReader options, LanguageServices languageServices, CodeGenerationOptions? fallbackOptions)
        => languageServices.GetRequiredService<ICodeGenerationService>().GetCodeGenerationOptions(options, fallbackOptions);

    public static CodeAndImportGenerationOptions GetCodeAndImportGenerationOptions(this IOptionsReader options, LanguageServices languageServices, bool? allowImportsInHiddenRegions, CodeAndImportGenerationOptions? fallbackOptions)
        => new()
        {
            GenerationOptions = options.GetCodeGenerationOptions(languageServices, fallbackOptions?.GenerationOptions),
            AddImportOptions = options.GetAddImportPlacementOptions(languageServices, allowImportsInHiddenRegions, fallbackOptions?.AddImportOptions)
        };

    public static CleanCodeGenerationOptions GetCleanCodeGenerationOptions(this IOptionsReader options, LanguageServices languageServices, bool? allowImportsInHiddenRegions, CleanCodeGenerationOptions? fallbackOptions)
        => new()
        {
            GenerationOptions = options.GetCodeGenerationOptions(languageServices, fallbackOptions?.GenerationOptions),
            CleanupOptions = options.GetCodeCleanupOptions(languageServices, allowImportsInHiddenRegions, fallbackOptions?.CleanupOptions)
        };

    public static async ValueTask<CodeGenerationOptions> GetCodeGenerationOptionsAsync(this Document document, CodeGenerationOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetCodeGenerationOptions(document.Project.Services, fallbackOptions);
    }

    public static async ValueTask<CodeGenerationOptions> GetCodeGenerationOptionsAsync(this Document document, ICodeGenerationOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await GetCodeGenerationOptionsAsync(document, await ((IOptionsProvider<CodeGenerationOptions>)fallbackOptionsProvider).GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    public static async ValueTask<CodeGenerationContextInfo> GetCodeGenerationInfoAsync(this Document document, CodeGenerationContext context, ICodeGenerationOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(document.Project.ParseOptions);

        var options = await GetCodeGenerationOptionsAsync(document, fallbackOptionsProvider, cancellationToken).ConfigureAwait(false);
        var service = document.Project.Services.GetRequiredService<ICodeGenerationService>();
        return service.GetInfo(context, options, document.Project.ParseOptions);
    }
#endif
}
