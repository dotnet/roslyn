// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeActions;

internal readonly struct CodeFixOptionsProvider(IOptionsReader options, string language)
{
    // LineFormattingOptions

    public string NewLine => GetOption(FormattingOptions2.NewLine);

    public LineFormattingOptions GetLineFormattingOptions()
        => options.GetLineFormattingOptions(language);

    // SyntaxFormattingOptions

    public SyntaxFormattingOptions GetFormattingOptions(ISyntaxFormatting formatting)
        => formatting.GetFormattingOptions(options);

    public AccessibilityModifiersRequired AccessibilityModifiersRequired => options.GetOptionValue(CodeStyleOptions2.AccessibilityModifiersRequired, language);

    private TValue GetOption<TValue>(PerLanguageOption2<TValue> option)
        => options.GetOption(option, language);
}

internal static class CodeFixOptionsProviders
{
    public static async ValueTask<CodeFixOptionsProvider> GetCodeFixOptionsAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return new CodeFixOptionsProvider(configOptions.GetOptionsReader(), document.Project.Language);
    }
}
