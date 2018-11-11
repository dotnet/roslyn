// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text.Editor;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Formatting
{
    public class CoreFormatterTestsBase
    {
        protected static void TestBlankLineIndentationService(
            TestWorkspace workspace, ITextView textView,
            int indentationLine, int? expectedIndentation)
        {
            var snapshot = workspace.Documents.First().TextBuffer.CurrentSnapshot;
            var indentationLineFromBuffer = snapshot.GetLineFromLineNumber(indentationLine);

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            var blankLineIndenter = (IBlankLineIndentationService)document.GetLanguageService<ISynchronousIndentationService>();
            var indentStyle = workspace.Options.GetOption(FormattingOptions.SmartIndent, LanguageNames.CSharp);
            var blankLineIndentResult = blankLineIndenter.GetBlankLineIndentation(
                document, indentationLine, indentStyle, CancellationToken.None);

            var blankLineIndentation = blankLineIndentResult.GetIndentation(textView, indentationLineFromBuffer);
            if (expectedIndentation == null)
            {
                if (indentStyle == FormattingOptions.IndentStyle.None)
                {
                    Assert.Equal(0, blankLineIndentation);
                }
            }
            else
            {
                Assert.Equal(expectedIndentation, blankLineIndentation);
            }
        }
    }
}
