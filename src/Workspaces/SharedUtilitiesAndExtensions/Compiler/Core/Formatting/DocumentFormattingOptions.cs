// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Formatting;

[DataContract]
internal sealed record class DocumentFormattingOptions
{
    public static readonly DocumentFormattingOptions Default = new();

    [DataMember] public string FileHeaderTemplate { get; init; } = "";
    [DataMember] public bool InsertFinalNewLine { get; init; } = false;
}

internal interface DocumentFormattingOptionsProvider
#if !CODE_STYLE
    : OptionsProvider<DocumentFormattingOptions>
#endif
{
}

internal static class DocumentFormattingOptionsProviders
{
    public static DocumentFormattingOptions GetDocumentFormattingOptions(this IOptionsReader options, DocumentFormattingOptions? fallbackOptions)
    {
        fallbackOptions ??= DocumentFormattingOptions.Default;

        return new()
        {
            FileHeaderTemplate = options.GetOption(CodeStyleOptions2.FileHeaderTemplate, fallbackOptions.FileHeaderTemplate),
            InsertFinalNewLine = options.GetOption(FormattingOptions2.InsertFinalNewLine, fallbackOptions.InsertFinalNewLine)
        };
    }

#if !CODE_STYLE
    public static async ValueTask<DocumentFormattingOptions> GetDocumentFormattingOptionsAsync(this Document document, DocumentFormattingOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetDocumentFormattingOptions(fallbackOptions);
    }

    public static async ValueTask<DocumentFormattingOptions> GetDocumentFormattingOptionsAsync(this Document document, DocumentFormattingOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await document.GetDocumentFormattingOptionsAsync(await fallbackOptionsProvider.GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
#endif
}
