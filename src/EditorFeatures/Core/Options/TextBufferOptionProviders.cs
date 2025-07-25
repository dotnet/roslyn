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
    extension(ITextBuffer textBuffer)
    {
        public DocumentationCommentOptions GetDocumentationCommentOptions(EditorOptionsService optionsProvider, LanguageServices languageServices)
        {
            var editorOptions = optionsProvider.Factory.GetOptions(textBuffer);
            var lineFormattingOptions = GetLineFormattingOptionsImpl(textBuffer, editorOptions, optionsProvider.IndentationManager, explicitFormat: false);
            return optionsProvider.GlobalOptions.GetDocumentationCommentOptions(lineFormattingOptions, languageServices.Language);
        }

        public LineFormattingOptions GetLineFormattingOptions(EditorOptionsService optionsProvider, bool explicitFormat)
           => GetLineFormattingOptionsImpl(textBuffer, optionsProvider.Factory.GetOptions(textBuffer), optionsProvider.IndentationManager, explicitFormat);

        public SyntaxFormattingOptions GetSyntaxFormattingOptions(EditorOptionsService optionsProvider, StructuredAnalyzerConfigOptions fallbackOptions, LanguageServices languageServices, bool explicitFormat)
            => GetSyntaxFormattingOptionsImpl(textBuffer, optionsProvider.Factory.GetOptions(textBuffer), fallbackOptions, optionsProvider.IndentationManager, languageServices, explicitFormat);

        public IndentationOptions GetIndentationOptions(EditorOptionsService optionsProvider, StructuredAnalyzerConfigOptions fallbackOptions, LanguageServices languageServices, bool explicitFormat)
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

        public AddImportPlacementOptions GetAddImportPlacementOptions(EditorOptionsService optionsProvider, StructuredAnalyzerConfigOptions fallbackOptions, LanguageServices languageServices, bool allowInHiddenRegions)
        {
            var editorOptions = optionsProvider.Factory.GetOptions(textBuffer);
            var configOptions = editorOptions.ToAnalyzerConfigOptions(fallbackOptions);
            return configOptions.GetAddImportPlacementOptions(languageServices, allowInHiddenRegions);
        }

        public CodeCleanupOptions GetCodeCleanupOptions(EditorOptionsService optionsProvider, StructuredAnalyzerConfigOptions fallbackOptions, LanguageServices languageServices, bool explicitFormat, bool allowImportsInHiddenRegions)
        {
            var editorOptions = optionsProvider.Factory.GetOptions(textBuffer);
            var configOptions = editorOptions.ToAnalyzerConfigOptions(fallbackOptions);

            var options = configOptions.GetCodeCleanupOptions(languageServices, allowImportsInHiddenRegions);
            var lineFormattingOptions = GetLineFormattingOptionsImpl(textBuffer, editorOptions, optionsProvider.IndentationManager, explicitFormat);

            return options with { FormattingOptions = options.FormattingOptions with { LineFormatting = lineFormattingOptions } };
        }
    }

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

    private static SyntaxFormattingOptions GetSyntaxFormattingOptionsImpl(ITextBuffer textBuffer, IEditorOptions editorOptions, StructuredAnalyzerConfigOptions fallbackOptions, IIndentationManagerService indentationManager, LanguageServices languageServices, bool explicitFormat)
    {
        var configOptions = editorOptions.ToAnalyzerConfigOptions(fallbackOptions);
        var options = configOptions.GetSyntaxFormattingOptions(languageServices);
        var lineFormattingOptions = GetLineFormattingOptionsImpl(textBuffer, editorOptions, indentationManager, explicitFormat);

        return options with { LineFormatting = lineFormattingOptions };
    }

    extension(FormattingOptions2.IndentStyle value)
    {
        public IndentingStyle ToEditorIndentStyle()
        => value switch
        {
            FormattingOptions2.IndentStyle.Smart => IndentingStyle.Smart,
            FormattingOptions2.IndentStyle.Block => IndentingStyle.Block,
            _ => IndentingStyle.None,
        };
    }

    extension(IndentingStyle value)
    {
        public FormattingOptions2.IndentStyle ToIndentStyle()
        => value switch
        {
            IndentingStyle.Smart => FormattingOptions2.IndentStyle.Smart,
            IndentingStyle.Block => FormattingOptions2.IndentStyle.Block,
            _ => FormattingOptions2.IndentStyle.None,
        };
    }
}
