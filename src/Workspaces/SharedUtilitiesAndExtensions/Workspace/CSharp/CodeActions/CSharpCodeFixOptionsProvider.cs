// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CodeActions;

internal readonly struct CSharpCodeFixOptionsProvider
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

    public CSharpCodeFixOptionsProvider(AnalyzerConfigOptions options, CodeActionOptionsProvider fallbackOptions, HostLanguageServices languageServices)
    {
        _options = options;
        _fallbackOptions = fallbackOptions;
        _languageServices = languageServices;
    }

    // LineFormattingOptions

    public string NewLine => GetOption(FormattingOptions2.NewLine, FallbackLineFormattingOptions.NewLine);

    // SimplifierOptions

    public CodeStyleOption2<bool> VarForBuiltInTypes => GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes, FallbackSimplifierOptions.VarForBuiltInTypes);
    public CodeStyleOption2<bool> VarElsewhere => GetOption(CSharpCodeStyleOptions.VarElsewhere, FallbackSimplifierOptions.VarElsewhere);

    public SimplifierOptions GetSimplifierOptions()
        => CSharpSimplifierOptions.Create(_options, FallbackSimplifierOptions);

    // FormattingOptions

    internal SyntaxFormattingOptions GetFormattingOptions()
        => CSharpSyntaxFormattingOptions.Create(_options, FallbackSyntaxFormattingOptions);

    // CodeStyleOptions

    public CodeStyleOption2<string> PreferredModifierOrder => GetOption(CSharpCodeStyleOptions.PreferredModifierOrder, FallbackCodeStyleOptions.PreferredModifierOrder);
    public CodeStyleOption2<bool> PreferTopLevelStatements => GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements, FallbackCodeStyleOptions.PreferTopLevelStatements);
    public CodeStyleOption2<AccessibilityModifiersRequired> RequireAccessibilityModifiers => GetOption(CodeStyleOptions2.RequireAccessibilityModifiers, FallbackCodeStyleOptions.Common.RequireAccessibilityModifiers);

    // CodeGenerationOptions

    public CodeStyleOption2<NamespaceDeclarationPreference> NamespaceDeclarations => GetOption(CSharpCodeStyleOptions.NamespaceDeclarations, FallbackCodeStyleOptions.NamespaceDeclarations);
    public CodeStyleOption2<AddImportPlacement> PreferredUsingDirectivePlacement => GetOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, FallbackCodeStyleOptions.PreferredUsingDirectivePlacement);

    private TValue GetOption<TValue>(Option2<TValue> option, TValue defaultValue)
        => _options.GetEditorConfigOption(option, defaultValue);

    private TValue GetOption<TValue>(PerLanguageOption2<TValue> option, TValue defaultValue)
        => _options.GetEditorConfigOption(option, defaultValue);

#if CODE_STYLE
    private CSharpIdeCodeStyleOptions FallbackCodeStyleOptions
        => CSharpIdeCodeStyleOptions.Default;
#else
    private CSharpIdeCodeStyleOptions FallbackCodeStyleOptions
        => (CSharpIdeCodeStyleOptions)_fallbackOptions.GetOptions(_languageServices).CodeStyleOptions;
#endif

#if CODE_STYLE
    private CSharpSimplifierOptions FallbackSimplifierOptions
        => CSharpSimplifierOptions.Default;
#else
    private CSharpSimplifierOptions FallbackSimplifierOptions
        => (CSharpSimplifierOptions)_fallbackOptions.GetOptions(_languageServices).CleanupOptions.SimplifierOptions;
#endif

#if CODE_STYLE
    private CSharpSyntaxFormattingOptions FallbackSyntaxFormattingOptions
        => CSharpSyntaxFormattingOptions.Default;
#else
    private CSharpSyntaxFormattingOptions FallbackSyntaxFormattingOptions
        => (CSharpSyntaxFormattingOptions)_fallbackOptions.GetOptions(_languageServices).CleanupOptions.FormattingOptions;
#endif

#if CODE_STYLE
    private LineFormattingOptions FallbackLineFormattingOptions
        => LineFormattingOptions.Default;
#else
    private LineFormattingOptions FallbackLineFormattingOptions
        => _fallbackOptions.GetOptions(_languageServices).CleanupOptions.FormattingOptions.LineFormatting;
#endif
}

internal static class CSharpCodeFixOptionsProviders
{
    public static async ValueTask<CSharpCodeFixOptionsProvider> GetCSharpCodeFixOptionsProviderAsync(this Document document, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var configOptions = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
        return new CSharpCodeFixOptionsProvider(configOptions, fallbackOptions, document.Project.GetExtendedLanguageServices());
    }
}
