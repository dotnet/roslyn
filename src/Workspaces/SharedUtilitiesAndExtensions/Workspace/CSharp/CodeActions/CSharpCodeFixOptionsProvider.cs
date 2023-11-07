// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

// to avoid excessive #ifdefs
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0052 // Remove unread private members

namespace Microsoft.CodeAnalysis.CodeActions;

internal readonly struct CSharpCodeFixOptionsProvider
{
    /// <summary>
    /// Document editorconfig options.
    /// </summary>
    private readonly IOptionsReader _options;

    /// <summary>
    /// C# language services.
    /// </summary>
    private readonly HostLanguageServices _languageServices;

    /// <summary>
    /// Fallback options provider - default options provider in Code Style layer.
    /// </summary>
    private readonly CodeActionOptionsProvider _fallbackOptions;

    public CSharpCodeFixOptionsProvider(IOptionsReader options, CodeActionOptionsProvider fallbackOptions, HostLanguageServices languageServices)
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
        => new CSharpSimplifierOptions(_options, FallbackSimplifierOptions);

    // FormattingOptions

    public CodeStyleOption2<NamespaceDeclarationPreference> NamespaceDeclarations => GetOption(CSharpCodeStyleOptions.NamespaceDeclarations, FallbackSyntaxFormattingOptions.NamespaceDeclarations);
    public CodeStyleOption2<bool> PreferTopLevelStatements => GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements, FallbackSyntaxFormattingOptions.PreferTopLevelStatements);

    internal CSharpSyntaxFormattingOptions GetFormattingOptions()
        => new(_options, FallbackSyntaxFormattingOptions);

    // AddImportPlacementOptions

    public CodeStyleOption2<AddImportPlacement> UsingDirectivePlacement => GetOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, FallbackAddImportPlacementOptions.UsingDirectivePlacement);

    // CodeStyleOptions

    public CodeStyleOption2<string> PreferredModifierOrder => GetOption(CSharpCodeStyleOptions.PreferredModifierOrder, FallbackCodeStyleOptions.PreferredModifierOrder);
    public CodeStyleOption2<AccessibilityModifiersRequired> AccessibilityModifiersRequired => GetOption(CodeStyleOptions2.AccessibilityModifiersRequired, FallbackCodeStyleOptions.AccessibilityModifiersRequired);

    private TValue GetOption<TValue>(Option2<TValue> option, TValue defaultValue)
        => _options.GetOption(option, defaultValue);

    private TValue GetOption<TValue>(PerLanguageOption2<TValue> option, TValue defaultValue)
        => _options.GetOption(option, _languageServices.Language, defaultValue);

    private CSharpIdeCodeStyleOptions FallbackCodeStyleOptions
#if CODE_STYLE
        => CSharpIdeCodeStyleOptions.Default;
#else
        => (CSharpIdeCodeStyleOptions)_fallbackOptions.GetOptions(_languageServices.LanguageServices).CodeStyleOptions;
#endif

    private CSharpSimplifierOptions FallbackSimplifierOptions
#if CODE_STYLE
        => CSharpSimplifierOptions.Default;
#else
        => (CSharpSimplifierOptions)_fallbackOptions.GetOptions(_languageServices.LanguageServices).CleanupOptions.SimplifierOptions;
#endif

    private CSharpSyntaxFormattingOptions FallbackSyntaxFormattingOptions
#if CODE_STYLE
        => CSharpSyntaxFormattingOptions.Default;
#else
        => (CSharpSyntaxFormattingOptions)_fallbackOptions.GetOptions(_languageServices.LanguageServices).CleanupOptions.FormattingOptions;
#endif

    private LineFormattingOptions FallbackLineFormattingOptions
#if CODE_STYLE
        => LineFormattingOptions.Default;
#else
        => _fallbackOptions.GetOptions(_languageServices.LanguageServices).CleanupOptions.FormattingOptions.LineFormatting;
#endif

    private AddImportPlacementOptions FallbackAddImportPlacementOptions
#if CODE_STYLE
        => AddImportPlacementOptions.Default;
#else
        => _fallbackOptions.GetOptions(_languageServices.LanguageServices).CleanupOptions.AddImportOptions;
#endif
}

internal static class CSharpCodeFixOptionsProviders
{
    public static async ValueTask<CSharpCodeFixOptionsProvider> GetCSharpCodeFixOptionsProviderAsync(this Document document, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return new CSharpCodeFixOptionsProvider(configOptions.GetOptionsReader(), fallbackOptions, document.Project.GetExtendedLanguageServices());
    }
}
