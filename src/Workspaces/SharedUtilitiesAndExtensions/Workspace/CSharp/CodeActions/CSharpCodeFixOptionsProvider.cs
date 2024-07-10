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

namespace Microsoft.CodeAnalysis.CodeActions;

internal readonly struct CSharpCodeFixOptionsProvider(IOptionsReader options, HostLanguageServices languageServices)
{
    // LineFormattingOptions

    public string NewLine => GetOption(FormattingOptions2.NewLine);

    // SimplifierOptions

    public CodeStyleOption2<bool> VarForBuiltInTypes => GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes);
    public CodeStyleOption2<bool> VarElsewhere => GetOption(CSharpCodeStyleOptions.VarElsewhere);

    public SimplifierOptions GetSimplifierOptions()
        => new CSharpSimplifierOptions(options);

    // FormattingOptions

    public CodeStyleOption2<NamespaceDeclarationPreference> NamespaceDeclarations => GetOption(CSharpCodeStyleOptions.NamespaceDeclarations);
    public CodeStyleOption2<bool> PreferTopLevelStatements => GetOption(CSharpCodeStyleOptions.PreferTopLevelStatements);

    internal CSharpSyntaxFormattingOptions GetFormattingOptions()
        => new(options);

    // AddImportPlacementOptions

    public CodeStyleOption2<AddImportPlacement> UsingDirectivePlacement => GetOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement);

    // CodeStyleOptions

    public CodeStyleOption2<string> PreferredModifierOrder => GetOption(CSharpCodeStyleOptions.PreferredModifierOrder);
    public CodeStyleOption2<AccessibilityModifiersRequired> AccessibilityModifiersRequired => GetOption(CodeStyleOptions2.AccessibilityModifiersRequired);

    private TValue GetOption<TValue>(Option2<TValue> option)
        => options.GetOption(option);

    private TValue GetOption<TValue>(PerLanguageOption2<TValue> option)
        => options.GetOption(option, languageServices.Language);
}

internal static class CSharpCodeFixOptionsProviders
{
    public static async ValueTask<CSharpCodeFixOptionsProvider> GetCSharpCodeFixOptionsProviderAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return new CSharpCodeFixOptionsProvider(configOptions.GetOptionsReader(), document.Project.GetExtendedLanguageServices());
    }
}
