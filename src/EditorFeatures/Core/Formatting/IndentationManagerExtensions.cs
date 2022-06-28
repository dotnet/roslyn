// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal static class IndentationManagerExtensions
    {
        public static SyntaxFormattingOptions GetInferredFormattingOptions(
            this IIndentationManagerService indentationManager,
            ITextBuffer textBuffer,
            IEditorOptionsFactoryService editorOptionsFactory,
            HostLanguageServices languageServices,
            SyntaxFormattingOptions fallbackOptions,
            bool explicitFormat)
        {
            var configOptions = new EditorAnalyzerConfigOptions(editorOptionsFactory.GetOptions(textBuffer));
            var options = configOptions.GetSyntaxFormattingOptions(fallbackOptions, languageServices);

            indentationManager.GetIndentation(textBuffer, explicitFormat, out var convertTabsToSpaces, out var tabSize, out var indentSize);

            return options.With(new LineFormattingOptions()
            {
                UseTabs = !convertTabsToSpaces,
                IndentationSize = indentSize,
                TabSize = tabSize,
                NewLine = options.NewLine
            });
        }
    }
}
