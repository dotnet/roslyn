// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeActions;

internal readonly struct CodeFixOptionsProvider
{
    /// <summary>
    /// Document editorconfig options.
    /// </summary>
    private readonly AnalyzerConfigOptions _options;

    /// <summary>
    /// C# language services.
    /// </summary>
    private readonly HostLanguageServices _languageServices;

    /// <summary>
    /// Fallback options provider - default options provider in Code Style layer.
    /// </summary>
    private readonly CodeActionOptionsProvider _fallbackOptions;

    public CodeFixOptionsProvider(AnalyzerConfigOptions options, CodeActionOptionsProvider fallbackOptions, HostLanguageServices languageServices)
    {
        _options = options;
        _fallbackOptions = fallbackOptions;
        _languageServices = languageServices;
    }

    // LineFormattingOptions

    public string NewLine => GetOption(FormattingOptions2.NewLine, FallbackLineFormattingOptions.NewLine);

    private TValue GetOption<TValue>(Option2<TValue> option, TValue defaultValue)
        => _options.GetEditorConfigOption(option, defaultValue);

    private TValue GetOption<TValue>(PerLanguageOption2<TValue> option, TValue defaultValue)
        => _options.GetEditorConfigOption(option, defaultValue);

#if CODE_STYLE
    private static LineFormattingOptions FallbackLineFormattingOptions
        => LineFormattingOptions.Default;
#else
    private LineFormattingOptions FallbackLineFormattingOptions
        => _fallbackOptions.GetOptions(_languageServices).CleanupOptions.FormattingOptions.LineFormatting;
#endif
}

internal static class CodeFixOptionsProviders
{
    public static async ValueTask<CodeFixOptionsProvider> GetCodeFixOptionsProviderAsync(this Document document, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var configOptions = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
        return new CodeFixOptionsProvider(configOptions, fallbackOptions, document.Project.GetExtendedLanguageServices());
    }
}
