// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation.Formatting.Indentation;
using Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Projection;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Formatting
{
    using Microsoft.CodeAnalysis.Indentation;

    public abstract class CoreFormatterTestsBase
    {
        internal abstract string GetLanguageName();
        internal abstract AbstractSmartTokenFormatterCommandHandler CreateSmartTokenFormatterCommandHandler(
            ITextUndoHistoryRegistry registry, IEditorOperationsFactoryService operations);

        protected void TestIndentation(
            TestWorkspace workspace, int point,
            int? expectedIndentation, int? expectedBlankLineIndentation,
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
                workspace, textView, indentationLineFromBuffer.LineNumber,
                expectedBlankLineIndentation ?? expectedIndentation.Value);
        }

        protected void TestBlankLineIndentationService(
            TestWorkspace workspace, ITextView textView,
            int indentationLine, int expectedIndentation)
        {
            var snapshot = workspace.Documents.First().TextBuffer.CurrentSnapshot;
            var indentationLineFromBuffer = snapshot.GetLineFromLineNumber(indentationLine);

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            var blankLineIndenter = document.GetLanguageService<IIndentationService>();
            var indentStyle = workspace.Options.GetOption(FormattingOptions.SmartIndent, GetLanguageName());
            var blankLineIndentResult = blankLineIndenter.GetBlankLineIndentation(
                document, indentationLine, indentStyle, CancellationToken.None);

            var blankLineIndentation = blankLineIndentResult.GetIndentation(textView, indentationLineFromBuffer);
            Assert.Equal(expectedIndentation, blankLineIndentation);
        }

        protected void TestIndentation(
            TestWorkspace workspace, int indentationLine,
            int? expectedIndentation, int? expectedBlankLineIndentation)
        {
            var snapshot = workspace.Documents.First().TextBuffer.CurrentSnapshot;
            var bufferGraph = new Mock<IBufferGraph>(MockBehavior.Strict);
            bufferGraph.Setup(x => x.MapUpToSnapshot(It.IsAny<SnapshotPoint>(),
                                                     It.IsAny<PointTrackingMode>(),
                                                     It.IsAny<PositionAffinity>(),
                                                     It.IsAny<ITextSnapshot>()))
                .Returns<SnapshotPoint, PointTrackingMode, PositionAffinity, ITextSnapshot>((p, m, a, s) =>
                {

                    if (workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>() is TestFormattingRuleFactoryServiceFactory.Factory factory && factory.BaseIndentation != 0 && factory.TextSpan.Contains(p.Position))
                    {
                        var line = p.GetContainingLine();
                        var projectedOffset = line.GetFirstNonWhitespaceOffset().Value - factory.BaseIndentation;
                        return new SnapshotPoint(p.Snapshot, p.Position - projectedOffset);
                    }

                    return p;
                });

            var projectionBuffer = new Mock<ITextBuffer>(MockBehavior.Strict);
            projectionBuffer.Setup(x => x.ContentType.DisplayName).Returns("None");

            var textView = new Mock<ITextView>(MockBehavior.Strict);
            textView.Setup(x => x.Options).Returns(TestEditorOptions.Instance);
            textView.Setup(x => x.BufferGraph).Returns(bufferGraph.Object);
            textView.SetupGet(x => x.TextSnapshot.TextBuffer).Returns(projectionBuffer.Object);

            var provider = new SmartIndent(textView.Object);

            var indentationLineFromBuffer = snapshot.GetLineFromLineNumber(indentationLine);
            var actualIndentation = provider.GetDesiredIndentation(indentationLineFromBuffer);

            Assert.Equal(expectedIndentation, actualIndentation);

            TestBlankLineIndentationService(
                workspace, textView.Object, indentationLine,
                expectedBlankLineIndentation ?? expectedIndentation.Value);
        }
    }
}
