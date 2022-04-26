// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !CODE_STYLE
using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.AddImport;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration;

/// <summary>
/// Document-specific options for controlling the code produced by code generation.
/// </summary>
internal abstract class CodeGenerationOptions
{
    [DataMember(Order = 0)]
    public readonly bool AddNullChecksToConstructorsGeneratedFromMembers;

    protected const int BaseMemberCount = 1;

    public CodeGenerationOptions(
        bool addNullChecksToConstructorsGeneratedFromMembers)
    {
        AddNullChecksToConstructorsGeneratedFromMembers = addNullChecksToConstructorsGeneratedFromMembers;
    }

    public const bool DefaultAddNullChecksToConstructorsGeneratedFromMembers = false;

#if !CODE_STYLE
    public static CodeGenerationOptions GetDefault(HostLanguageServices languageServices)
        => languageServices.GetRequiredService<ICodeGenerationService>().DefaultOptions;

    public static CodeGenerationOptions Create(OptionSet options, CodeGenerationOptions? fallbackOptions, HostLanguageServices languageServices)
    {
        var formattingService = languageServices.GetRequiredService<ICodeGenerationService>();
        var configOptions = options.AsAnalyzerConfigOptions(languageServices.WorkspaceServices.GetRequiredService<IOptionService>(), languageServices.Language);
        return formattingService.GetCodeGenerationOptions(configOptions, fallbackOptions);
    }

    public abstract CodeGenerationContextInfo GetInfo(CodeGenerationContext context, ParseOptions parseOptions);

    public CodeGenerationContextInfo GetInfo(CodeGenerationContext context, Project project)
    {
        Contract.ThrowIfNull(project.ParseOptions);
        return GetInfo(context, project.ParseOptions);
    }
#endif
}

[DataContract]
internal readonly record struct CodeAndImportGenerationOptions(
    [property: DataMember(Order = 0)] CodeGenerationOptions GenerationOptions,
    [property: DataMember(Order = 1)] AddImportPlacementOptions AddImportOptions)
{
#if !CODE_STYLE
    internal static CodeAndImportGenerationOptions GetDefault(HostLanguageServices languageServices)
        => new(CodeGenerationOptions.GetDefault(languageServices), AddImportPlacementOptions.Default);

    internal CodeAndImportGenerationOptionsProvider CreateProvider()
        => new Provider(this);

    private sealed class Provider : CodeAndImportGenerationOptionsProvider
    {
        private readonly CodeAndImportGenerationOptions _options;

        public Provider(CodeAndImportGenerationOptions options)
            => _options = options;

        ValueTask<CodeAndImportGenerationOptions> OptionsProvider<CodeAndImportGenerationOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options);

        ValueTask<CodeGenerationOptions> OptionsProvider<CodeGenerationOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.GenerationOptions);

        ValueTask<AddImportPlacementOptions> OptionsProvider<AddImportPlacementOptions>.GetOptionsAsync(HostLanguageServices languageServices, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(_options.AddImportOptions);
    }
#endif
}

internal interface CodeGenerationOptionsProvider
#if !CODE_STYLE
    : OptionsProvider<CodeGenerationOptions>
#endif
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

#if !CODE_STYLE
internal static class CodeGenerationOptionsProviders
{
    public static async ValueTask<CodeGenerationOptions> GetCodeGenerationOptionsAsync(this Document document, CodeGenerationOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(document.Project.ParseOptions);

        var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
        return CodeGenerationOptions.Create(documentOptions, fallbackOptions, document.Project.LanguageServices);
    }

    public static async ValueTask<CodeGenerationOptions> GetCodeGenerationOptionsAsync(this Document document, CodeGenerationOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await GetCodeGenerationOptionsAsync(document, await fallbackOptionsProvider.GetOptionsAsync(document.Project.LanguageServices, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
}
#endif
