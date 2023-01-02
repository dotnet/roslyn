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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.CodeGeneration;

/// <summary>
/// Document-specific options for controlling the code produced by code generation.
/// </summary>
internal abstract class CodeGenerationOptions
{
    [DataContract]
    internal sealed record class CommonOptions
    {
        public static readonly CommonOptions Default = new();

        [DataMember] public NamingStylePreferences NamingStyle { get; init; } = NamingStylePreferences.Default;
    }

    [DataMember]
    public CommonOptions Common { get; init; } = CommonOptions.Default;

    public NamingStylePreferences NamingStyle => Common.NamingStyle;

#if !CODE_STYLE
    public static CodeGenerationOptions GetDefault(LanguageServices languageServices)
        => languageServices.GetRequiredService<ICodeGenerationService>().DefaultOptions;

    public abstract CodeGenerationContextInfo GetInfo(CodeGenerationContext context, ParseOptions parseOptions);

    public CodeGenerationContextInfo GetInfo(CodeGenerationContext context, Project project)
    {
        Contract.ThrowIfNull(project.ParseOptions);
        return GetInfo(context, project.ParseOptions);
    }
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

    private sealed class Provider : CodeAndImportGenerationOptionsProvider
    {
        private readonly CodeAndImportGenerationOptions _options;

        public Provider(CodeAndImportGenerationOptions options)
            => _options = options;

        ValueTask<CodeAndImportGenerationOptions> OptionsProvider<CodeAndImportGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options);

        ValueTask<CodeGenerationOptions> OptionsProvider<CodeGenerationOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.GenerationOptions);

        ValueTask<NamingStylePreferences> OptionsProvider<NamingStylePreferences>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.GenerationOptions.NamingStyle);

        ValueTask<AddImportPlacementOptions> OptionsProvider<AddImportPlacementOptions>.GetOptionsAsync(LanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.AddImportOptions);
    }
#endif
}

internal interface CodeGenerationOptionsProvider :
#if !CODE_STYLE
    OptionsProvider<CodeGenerationOptions>,
#endif
    NamingStylePreferencesProvider
{
}

internal interface CodeAndImportGenerationOptionsProvider :
#if !CODE_STYLE
    OptionsProvider<CodeAndImportGenerationOptions>,
#endif
    CodeGenerationOptionsProvider,
    AddImportPlacementOptionsProvider
{
}

internal static class CodeGenerationOptionsProviders
{
    public static CodeGenerationOptions.CommonOptions GetCommonCodeGenerationOptions(this IOptionsReader options, string language, CodeGenerationOptions.CommonOptions? fallbackOptions)
    {
        fallbackOptions ??= CodeGenerationOptions.CommonOptions.Default;

        return new()
        {
            NamingStyle = options.GetOption(NamingStyleOptions.NamingPreferences, language, fallbackOptions.NamingStyle)
        };
    }

#if !CODE_STYLE
    public static CodeGenerationOptions GetCodeGenerationOptions(this IOptionsReader options, CodeGenerationOptions? fallbackOptions, LanguageServices languageServices)
        => languageServices.GetRequiredService<ICodeGenerationService>().GetCodeGenerationOptions(options, fallbackOptions);

    public static async ValueTask<CodeGenerationOptions> GetCodeGenerationOptionsAsync(this Document document, CodeGenerationOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetCodeGenerationOptions(fallbackOptions, document.Project.Services);
    }

    public static async ValueTask<CodeGenerationOptions> GetCodeGenerationOptionsAsync(this Document document, CodeGenerationOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await GetCodeGenerationOptionsAsync(document, await ((OptionsProvider<CodeGenerationOptions>)fallbackOptionsProvider).GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
#endif
}
