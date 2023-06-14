// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

// to avoid excessive #ifdefs
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0052 // Remove unread private members

namespace Microsoft.CodeAnalysis.CodeActions;

/// <param name="options">
/// Document editorconfig options.
/// </param>
/// <param name="fallbackOptions">
/// Fallback options provider - default options provider in Code Style layer.
/// </param>
/// <param name="languageServices">
/// C# language services.
/// </param>
internal readonly struct CodeFixOptionsProvider(IOptionsReader options, CodeActionOptionsProvider fallbackOptions, HostLanguageServices languageServices)
{

    // LineFormattingOptions

    public string NewLine => GetOption(FormattingOptions2.NewLine, FallbackLineFormattingOptions.NewLine);

    public LineFormattingOptions GetLineFormattingOptions()
        => options.GetLineFormattingOptions(languageServices.Language, FallbackLineFormattingOptions);

    // SyntaxFormattingOptions

    public SyntaxFormattingOptions GetFormattingOptions(ISyntaxFormatting formatting)
        => formatting.GetFormattingOptions(options, FallbackSyntaxFormattingOptions);

    public AccessibilityModifiersRequired AccessibilityModifiersRequired => options.GetOptionValue(CodeStyleOptions2.AccessibilityModifiersRequired, languageServices.Language, FallbackCommonSyntaxFormattingOptions.AccessibilityModifiersRequired);

    private TValue GetOption<TValue>(PerLanguageOption2<TValue> option, TValue defaultValue)
        => options.GetOption(option, languageServices.Language, defaultValue);

    private LineFormattingOptions FallbackLineFormattingOptions
#if CODE_STYLE
        => LineFormattingOptions.Default;
#else
        => fallbackOptions.GetOptions(languageServices.LanguageServices).CleanupOptions.FormattingOptions.LineFormatting;
#endif

    private SyntaxFormattingOptions? FallbackSyntaxFormattingOptions
#if CODE_STYLE
        => null;
#else
        => fallbackOptions.GetOptions(languageServices.LanguageServices).CleanupOptions.FormattingOptions;
#endif

    private SyntaxFormattingOptions FallbackCommonSyntaxFormattingOptions
#if CODE_STYLE
        => SyntaxFormattingOptions.CommonDefaults;
#else
        => fallbackOptions.GetOptions(languageServices.LanguageServices).CleanupOptions.FormattingOptions;
#endif
}

internal static class CodeFixOptionsProviders
{
    public static async ValueTask<CodeFixOptionsProvider> GetCodeFixOptionsAsync(this Document document, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return new CodeFixOptionsProvider(configOptions.GetOptionsReader(), fallbackOptions, document.Project.GetExtendedLanguageServices());
    }
}
