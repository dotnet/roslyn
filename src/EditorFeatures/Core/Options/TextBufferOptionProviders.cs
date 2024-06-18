// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace Microsoft.CodeAnalysis.Options;

internal static class TextBufferOptionProviders
{
    public static DocumentationCommentOptions GetDocumentationCommentOptions(this ITextBuffer textBuffer, EditorOptionsService optionsProvider, LanguageServices languageServices)
    {
        var editorOptions = optionsProvider.Factory.GetOptions(textBuffer);
        var lineFormattingOptions = GetLineFormattingOptionsImpl(textBuffer, editorOptions, optionsProvider.IndentationManager, explicitFormat: false);
        return optionsProvider.GlobalOptions.GetDocumentationCommentOptions(lineFormattingOptions, languageServices.Language);
    }

    public static LineFormattingOptions GetLineFormattingOptions(this ITextBuffer textBuffer, EditorOptionsService optionsProvider, bool explicitFormat)
       => GetLineFormattingOptionsImpl(textBuffer, optionsProvider.Factory.GetOptions(textBuffer), optionsProvider.IndentationManager, explicitFormat);

    private static LineFormattingOptions GetLineFormattingOptionsImpl(ITextBuffer textBuffer, IEditorOptions editorOptions, IIndentationManagerService indentationManager, bool explicitFormat)
    {
        indentationManager.GetIndentation(textBuffer, explicitFormat, out var convertTabsToSpaces, out var tabSize, out var indentSize);

        return new LineFormattingOptions()
        {
            UseTabs = !convertTabsToSpaces,
            IndentationSize = indentSize,
            TabSize = tabSize,
            NewLine = editorOptions.GetNewLineCharacter(),
        };
    }

    public static SyntaxFormattingOptions GetSyntaxFormattingOptions(this ITextBuffer textBuffer, EditorOptionsService optionsProvider, StructuredAnalyzerConfigOptions fallbackOptions, LanguageServices languageServices, bool explicitFormat)
        => GetSyntaxFormattingOptionsImpl(textBuffer, optionsProvider.Factory.GetOptions(textBuffer), fallbackOptions, optionsProvider.IndentationManager, languageServices, explicitFormat);

    private static SyntaxFormattingOptions GetSyntaxFormattingOptionsImpl(ITextBuffer textBuffer, IEditorOptions editorOptions, StructuredAnalyzerConfigOptions fallbackOptions, IIndentationManagerService indentationManager, LanguageServices languageServices, bool explicitFormat)
    {
        var configOptions = editorOptions.ToAnalyzerConfigOptions(fallbackOptions);
        var options = configOptions.GetSyntaxFormattingOptions(languageServices);
        var lineFormattingOptions = GetLineFormattingOptionsImpl(textBuffer, editorOptions, indentationManager, explicitFormat);

        return options with { LineFormatting = lineFormattingOptions };
    }

    public static IndentationOptions GetIndentationOptions(this ITextBuffer textBuffer, EditorOptionsService optionsProvider, StructuredAnalyzerConfigOptions fallbackOptions, LanguageServices languageServices, bool explicitFormat)
    {
        var editorOptions = optionsProvider.Factory.GetOptions(textBuffer);
        var formattingOptions = GetSyntaxFormattingOptionsImpl(textBuffer, editorOptions, fallbackOptions, optionsProvider.IndentationManager, languageServices, explicitFormat);

        return new IndentationOptions(formattingOptions)
        {
            AutoFormattingOptions = optionsProvider.GlobalOptions.GetAutoFormattingOptions(languageServices.Language),
            // TODO: Call editorOptions.GetIndentStyle() instead (see https://github.com/dotnet/roslyn/issues/62204):
            IndentStyle = optionsProvider.GlobalOptions.GetOption(IndentationOptionsStorage.SmartIndent, languageServices.Language)
        };
    }

    public static AddImportPlacementOptions GetAddImportPlacementOptions(this ITextBuffer textBuffer, EditorOptionsService optionsProvider, StructuredAnalyzerConfigOptions fallbackOptions, LanguageServices languageServices, bool allowInHiddenRegions)
    {
        var editorOptions = optionsProvider.Factory.GetOptions(textBuffer);
        var configOptions = editorOptions.ToAnalyzerConfigOptions(fallbackOptions);
        return configOptions.GetAddImportPlacementOptions(languageServices, allowInHiddenRegions, fallbackOptions: null);
    }

    public static CodeCleanupOptions GetCodeCleanupOptions(this ITextBuffer textBuffer, EditorOptionsService optionsProvider, StructuredAnalyzerConfigOptions fallbackOptions, LanguageServices languageServices, bool explicitFormat, bool allowImportsInHiddenRegions)
    {
        var editorOptions = optionsProvider.Factory.GetOptions(textBuffer);
        var configOptions = editorOptions.ToAnalyzerConfigOptions(fallbackOptions);

        var options = configOptions.GetCodeCleanupOptions(languageServices, allowImportsInHiddenRegions, fallbackOptions: null);
        var lineFormattingOptions = GetLineFormattingOptionsImpl(textBuffer, editorOptions, optionsProvider.IndentationManager, explicitFormat);

        return options with { FormattingOptions = options.FormattingOptions with { LineFormatting = lineFormattingOptions } };
    }

    public static IndentingStyle ToEditorIndentStyle(this FormattingOptions2.IndentStyle value)
        => value switch
        {
            FormattingOptions2.IndentStyle.Smart => IndentingStyle.Smart,
            FormattingOptions2.IndentStyle.Block => IndentingStyle.Block,
            _ => IndentingStyle.None,
        };

    public static FormattingOptions2.IndentStyle ToIndentStyle(this IndentingStyle value)
        => value switch
        {
            IndentingStyle.Smart => FormattingOptions2.IndentStyle.Smart,
            IndentingStyle.Block => FormattingOptions2.IndentStyle.Block,
            _ => FormattingOptions2.IndentStyle.None,
        };
}
