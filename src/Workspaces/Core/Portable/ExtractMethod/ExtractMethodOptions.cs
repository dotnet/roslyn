// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExtractMethod;

[DataContract]
internal readonly record struct ExtractMethodOptions
{
    [DataMember] public bool DoNotPutOutOrRefOnStruct { get; init; } = true;

    public ExtractMethodOptions()
    {
    }

    public static readonly ExtractMethodOptions Default = new();
}

/// <summary>
/// All options needed to perform method extraction.
/// Combines global <see cref="ExtractOptions"/> with document specific code generation options.
/// </summary>
[DataContract]
internal readonly record struct ExtractMethodGenerationOptions
{
    [DataMember] public required CodeGenerationOptions CodeGenerationOptions { get; init; }
    [DataMember] public ExtractMethodOptions ExtractOptions { get; init; } = ExtractMethodOptions.Default;
    [DataMember] public AddImportPlacementOptions AddImportOptions { get; init; } = AddImportPlacementOptions.Default;
    [DataMember] public LineFormattingOptions LineFormattingOptions { get; init; } = LineFormattingOptions.Default;

    public static ExtractMethodGenerationOptions GetDefault(LanguageServices languageServices)
        => new()
        {
            CodeGenerationOptions = CodeGenerationOptions.GetDefault(languageServices)
        };

    public ExtractMethodGenerationOptions()
    {
    }
}

internal static class ExtractMethodGenerationOptionsProviders
{
    public static async ValueTask<ExtractMethodGenerationOptions> GetExtractMethodGenerationOptionsAsync(this Document document, ExtractMethodGenerationOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        fallbackOptions ??= ExtractMethodGenerationOptions.GetDefault(document.Project.Services);

        return new ExtractMethodGenerationOptions()
        {
            CodeGenerationOptions = await document.GetCodeGenerationOptionsAsync(fallbackOptions.Value.CodeGenerationOptions, cancellationToken).ConfigureAwait(false),
            ExtractOptions = fallbackOptions.Value.ExtractOptions,
            AddImportOptions = await document.GetAddImportPlacementOptionsAsync(fallbackOptions.Value.AddImportOptions, cancellationToken).ConfigureAwait(false),
            LineFormattingOptions = await document.GetLineFormattingOptionsAsync(fallbackOptions.Value.LineFormattingOptions, cancellationToken).ConfigureAwait(false),
        };
    }

    public static ValueTask<ExtractMethodGenerationOptions> GetExtractMethodGenerationOptionsAsync(this Document document, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        => document.GetExtractMethodGenerationOptionsAsync(fallbackOptions.GetExtractMethodGenerationOptions(document.Project.Services), cancellationToken);
}
