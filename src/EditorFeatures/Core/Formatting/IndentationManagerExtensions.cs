// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal static class IndentationManagerExtensions
    {
        public static async Task<SyntaxFormattingOptions> GetInferredFormattingOptionsAsync(this IIndentationManagerService indentationManager, Document document, bool explicitFormat, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = text.FindCorrespondingEditorTextSnapshot();

            var options = await SyntaxFormattingOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
            if (snapshot == null)
            {
                return options;
            }

            indentationManager.GetIndentation(snapshot.TextBuffer, explicitFormat, out var convertTabsToSpaces, out var tabSize, out var indentSize);

            return options.With(
                useTabs: !convertTabsToSpaces,
                indentationSize: indentSize,
                tabSize: tabSize);
        }
    }
}
