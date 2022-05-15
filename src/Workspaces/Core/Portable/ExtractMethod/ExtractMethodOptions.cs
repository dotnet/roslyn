// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.ExtractMethod;

[DataContract]
internal readonly record struct ExtractMethodOptions(
    [property: DataMember(Order = 0)] bool DontPutOutOrRefOnStruct = true)
{
    public ExtractMethodOptions()
        : this(DontPutOutOrRefOnStruct: true)
    {
    }

    public static readonly ExtractMethodOptions Default = new();
}

/// <summary>
/// All options needed to perform method extraction.
/// Combines global <paramref name="ExtractOptions"/> with document specific code generation options.
/// </summary>
internal readonly record struct ExtractMethodGenerationOptions(
    ExtractMethodOptions ExtractOptions,
    CodeGenerationOptions CodeGenerationOptions,
    AddImportPlacementOptions AddImportOptions,
    NamingStylePreferencesProvider NamingPreferences)
{
    public static ExtractMethodGenerationOptions GetDefault(HostLanguageServices languageServices)
        => new(ExtractMethodOptions.Default,
               CodeGenerationOptions.GetDefault(languageServices),
               AddImportPlacementOptions.Default,
               new NamingStylePreferencesProvider(_ => NamingStylePreferences.Default));
}

internal static class ExtractMethodGenerationOptionsProviders
{
    public static async ValueTask<ExtractMethodGenerationOptions> GetExtractMethodGenerationOptionsAsync(this Document document, ExtractMethodGenerationOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        fallbackOptions ??= ExtractMethodGenerationOptions.GetDefault(document.Project.LanguageServices);

        var extractOptions = fallbackOptions.Value.ExtractOptions;
        var codeGenerationOptions = await document.GetCodeGenerationOptionsAsync(fallbackOptions.Value.CodeGenerationOptions, cancellationToken).ConfigureAwait(false);
        var addImportOptions = await document.GetAddImportPlacementOptionsAsync(fallbackOptions.Value.AddImportOptions, cancellationToken).ConfigureAwait(false);

        var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
        var namingPreferences = documentOptions.GetOption(NamingStyleOptions.NamingPreferences, document.Project.Language);
        var namingPreferencesProvider = new NamingStylePreferencesProvider(language => namingPreferences);

        return new ExtractMethodGenerationOptions(
            extractOptions,
            codeGenerationOptions,
            addImportOptions,
            namingPreferencesProvider);
    }

    public static ValueTask<ExtractMethodGenerationOptions> GetExtractMethodGenerationOptionsAsync(this Document document, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        => document.GetExtractMethodGenerationOptionsAsync(fallbackOptions.GetExtractMethodGenerationOptions(document.Project.LanguageServices), cancellationToken);
}
