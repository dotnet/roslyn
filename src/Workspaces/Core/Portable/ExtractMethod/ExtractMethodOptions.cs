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
    [DataMember] public bool DontPutOutOrRefOnStruct { get; init; } = true;

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
internal readonly record struct ExtractMethodGenerationOptions(
    [property: DataMember] CodeGenerationOptions CodeGenerationOptions)
{
    [DataMember] public ExtractMethodOptions ExtractOptions { get; init; } = ExtractMethodOptions.Default;
    [DataMember] public AddImportPlacementOptions AddImportOptions { get; init; } = AddImportPlacementOptions.Default;
    [DataMember] public LineFormattingOptions LineFormattingOptions { get; init; } = LineFormattingOptions.Default;

    public static ExtractMethodGenerationOptions GetDefault(LanguageServices languageServices)
        => new(CodeGenerationOptions.GetDefault(languageServices));
}

internal static class ExtractMethodGenerationOptionsProviders
{
    public static async ValueTask<ExtractMethodGenerationOptions> GetExtractMethodGenerationOptionsAsync(this Document document, ExtractMethodGenerationOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        fallbackOptions ??= ExtractMethodGenerationOptions.GetDefault(document.Project.Services);

        var extractOptions = fallbackOptions.Value.ExtractOptions;
        var codeGenerationOptions = await document.GetCodeGenerationOptionsAsync(fallbackOptions.Value.CodeGenerationOptions, cancellationToken).ConfigureAwait(false);
        var addImportOptions = await document.GetAddImportPlacementOptionsAsync(fallbackOptions.Value.AddImportOptions, cancellationToken).ConfigureAwait(false);
        var lineFormattingOptions = await document.GetLineFormattingOptionsAsync(fallbackOptions.Value.LineFormattingOptions, cancellationToken).ConfigureAwait(false);

        return new ExtractMethodGenerationOptions(codeGenerationOptions)
        {
            ExtractOptions = extractOptions,
            AddImportOptions = addImportOptions,
            LineFormattingOptions = lineFormattingOptions,
        };
    }

    public static ValueTask<ExtractMethodGenerationOptions> GetExtractMethodGenerationOptionsAsync(this Document document, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        => document.GetExtractMethodGenerationOptionsAsync(fallbackOptions.GetExtractMethodGenerationOptions(document.Project.Services), cancellationToken);
}
