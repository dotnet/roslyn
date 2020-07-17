// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class InferredIndentationOptions
    {
        public static async Task<DocumentOptionSet> GetDocumentOptionsWithInferredIndentationAsync(
            this Document document,
            bool explicitFormat,
            IIndentationManagerService indentationManagerService,
            CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = text.FindCorrespondingEditorTextSnapshot();

            if (snapshot != null)
            {
                indentationManagerService.GetIndentation(snapshot.TextBuffer, explicitFormat, out var convertTabsToSpaces, out var tabSize, out var indentSize);

                options = options.WithChangedOption(FormattingOptions.UseTabs, !convertTabsToSpaces)
                                 .WithChangedOption(FormattingOptions.IndentationSize, indentSize)
                                 .WithChangedOption(FormattingOptions.TabSize, tabSize);
            }

            return options;
        }
    }
}
