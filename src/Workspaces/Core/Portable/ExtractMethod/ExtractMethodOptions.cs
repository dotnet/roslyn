// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.ExtractMethod;

/// <summary>
/// All options needed to perform method extraction.
/// </summary>
[DataContract]
internal readonly record struct ExtractMethodGenerationOptions
{
    [DataMember] public required CodeGenerationOptions CodeGenerationOptions { get; init; }
    [DataMember] public required CodeCleanupOptions CodeCleanupOptions { get; init; }

    public static ExtractMethodGenerationOptions GetDefault(LanguageServices languageServices)
        => new()
        {
            CodeGenerationOptions = CodeGenerationOptionsProviders.GetDefault(languageServices),
            CodeCleanupOptions = CodeCleanupOptionsProviders.GetDefault(languageServices),
        };

    public ExtractMethodGenerationOptions()
    {
    }

    public AddImportPlacementOptions AddImportOptions => CodeCleanupOptions.AddImportOptions;
    public LineFormattingOptions LineFormattingOptions => CodeCleanupOptions.FormattingOptions.LineFormatting;
    public SimplifierOptions SimplifierOptions => CodeCleanupOptions.SimplifierOptions;
}

internal static class ExtractMethodGenerationOptionsProviders
{
    public static async ValueTask<ExtractMethodGenerationOptions> GetExtractMethodGenerationOptionsAsync(this Document document, CancellationToken cancellationToken)
        => new()
        {
            CodeGenerationOptions = await document.GetCodeGenerationOptionsAsync(cancellationToken).ConfigureAwait(false),
            CodeCleanupOptions = await document.GetCodeCleanupOptionsAsync(cancellationToken).ConfigureAwait(false),
        };
}
