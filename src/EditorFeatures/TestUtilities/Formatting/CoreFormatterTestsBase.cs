// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation.Formatting.Indentation;
using Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Formatting
{
    public abstract class CoreFormatterTestsBase
    {
        internal abstract AbstractSmartTokenFormatterCommandHandler CreateSmartTokenFormatterCommandHandler(
            ITextUndoHistoryRegistry registry, IEditorOperationsFactoryService operations);

        protected void TestIndentation(
            TestWorkspace workspace, int point, int? expectedIndentation,
            ITextView textView, TestHostDocument subjectDocument)
        {
            var textUndoHistory = new Mock<ITextUndoHistoryRegistry>();
            var editorOperationsFactory = new Mock<IEditorOperationsFactoryService>();
            var editorOperations = new Mock<IEditorOperations>();
            editorOperationsFactory.Setup(x => x.GetEditorOperations(textView)).Returns(editorOperations.Object);

            var snapshot = subjectDocument.TextBuffer.CurrentSnapshot;
            var indentationLineFromBuffer = snapshot.GetLineFromPosition(point);

            var commandHandler = CreateSmartTokenFormatterCommandHandler(textUndoHistory.Object, editorOperationsFactory.Object);
            commandHandler.ExecuteCommandWorker(new ReturnKeyCommandArgs(textView, subjectDocument.TextBuffer), CancellationToken.None);
            var newSnapshot = subjectDocument.TextBuffer.CurrentSnapshot;

            int? actualIndentation;
            if (newSnapshot.Version.VersionNumber > snapshot.Version.VersionNumber)
            {
                actualIndentation = newSnapshot.GetLineFromLineNumber(indentationLineFromBuffer.LineNumber).GetFirstNonWhitespaceOffset();
            }
            else
            {
                var provider = new SmartIndent(textView);
                actualIndentation = provider.GetDesiredIndentation(indentationLineFromBuffer);
            }

            Assert.Equal(expectedIndentation, actualIndentation.Value);

            TestBlankLineIndentationService(
                workspace, textView, indentationLineFromBuffer.LineNumber, expectedIndentation);
        }

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
