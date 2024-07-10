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

    private protected CodeGenerationOptions(IOptionsReader options, string language)
    {
        NamingStyle = options.GetOption(NamingStyleOptions.NamingPreferences, language);
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

    internal CodeAndImportGenerationOptionsProvider CreateProvider()
        => new Provider(this);

    private sealed class Provider(CodeAndImportGenerationOptions options) : CodeAndImportGenerationOptionsProvider
    {
        ValueTask<CodeAndImportGenerationOptions> OptionsProvider<CodeAndImportGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(options);
    }
#endif
}

internal interface CodeAndImportGenerationOptionsProvider
#if !CODE_STYLE
    : OptionsProvider<CodeAndImportGenerationOptions>
#endif    
{
}

internal static class CodeGenerationOptionsProviders
{
#if !CODE_STYLE
    public static CodeGenerationOptions GetCodeGenerationOptions(this IOptionsReader options, LanguageServices languageServices)
        => languageServices.GetRequiredService<ICodeGenerationService>().GetCodeGenerationOptions(options);

    public static CodeAndImportGenerationOptions GetCodeAndImportGenerationOptions(this IOptionsReader options, LanguageServices languageServices, bool? allowImportsInHiddenRegions = null)
        => new()
        {
            GenerationOptions = options.GetCodeGenerationOptions(languageServices),
            AddImportOptions = options.GetAddImportPlacementOptions(languageServices, allowImportsInHiddenRegions)
        };

    public static CleanCodeGenerationOptions GetCleanCodeGenerationOptions(this IOptionsReader options, LanguageServices languageServices, bool? allowImportsInHiddenRegions = null)
        => new()
        {
            GenerationOptions = options.GetCodeGenerationOptions(languageServices),
            CleanupOptions = options.GetCodeCleanupOptions(languageServices, allowImportsInHiddenRegions)
        };

    public static async ValueTask<CodeGenerationOptions> GetCodeGenerationOptionsAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetCodeGenerationOptions(document.Project.Services);
    }

    public static async ValueTask<CodeGenerationContextInfo> GetCodeGenerationInfoAsync(this Document document, CodeGenerationContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(document.Project.ParseOptions);

        var options = await GetCodeGenerationOptionsAsync(document, cancellationToken).ConfigureAwait(false);
        var service = document.Project.Services.GetRequiredService<ICodeGenerationService>();
        return service.GetInfo(context, options, document.Project.ParseOptions);
    }
#endif
}
