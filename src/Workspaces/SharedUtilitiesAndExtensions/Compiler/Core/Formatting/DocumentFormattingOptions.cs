// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Formatting;

internal readonly record struct DocumentFormattingOptions(
    string FileHeaderTemplate = "")
{
    public DocumentFormattingOptions()
        : this(FileHeaderTemplate: "")
    {
    }

    public static readonly DocumentFormattingOptions Default = new();

    public static DocumentFormattingOptions Create(AnalyzerConfigOptions options, DocumentFormattingOptions? fallbackOptions)
    {
        fallbackOptions ??= Default;

        return new(
            options.GetEditorConfigOption(CodeStyleOptions2.FileHeaderTemplate, fallbackOptions.Value.FileHeaderTemplate));
    }
}

internal interface DocumentFormattingOptionsProvider
#if !CODE_STYLE
    : OptionsProvider<DocumentFormattingOptions>
#endif
{
}

#if !CODE_STYLE
internal static class DocumentFormattingOptionsProviders
{
    public static async ValueTask<DocumentFormattingOptions> GetDocumentFormattingOptionsAsync(this Document document, DocumentFormattingOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var configOptions = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
        return DocumentFormattingOptions.Create(configOptions, fallbackOptions);
    }

    public static async ValueTask<DocumentFormattingOptions> GetDocumentFormattingOptionsAsync(this Document document, DocumentFormattingOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await document.GetDocumentFormattingOptionsAsync(await fallbackOptionsProvider.GetOptionsAsync(document.Project.LanguageServices, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

}
#endif
