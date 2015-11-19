// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.CSharp.Formatting.Indentation;
using Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Projection;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting.Indentation
{
    public class FormatterTestsBase
    {
        protected const string HtmlMarkup = @"<html>
    <body>
        <%{|S1:|}%>
    </body>
</html>";
        protected const int BaseIndentationOfNugget = 8;

        protected static async Task<int> GetSmartTokenFormatterIndentationWorkerAsync(
            TestWorkspace workspace,
            ITextBuffer buffer,
            int indentationLine,
            char ch)
        {
            await TokenFormatWorkerAsync(workspace, buffer, indentationLine, ch);

            return buffer.CurrentSnapshot.GetLineFromLineNumber(indentationLine).GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(TestEditorOptions.Instance);
        }

        protected static async Task<string> TokenFormatAsync(
            TestWorkspace workspace,
            ITextBuffer buffer,
            int indentationLine,
            char ch)
        {
            await TokenFormatWorkerAsync(workspace, buffer, indentationLine, ch);

            return buffer.CurrentSnapshot.GetText();
        }

        private static async Task TokenFormatWorkerAsync(TestWorkspace workspace, ITextBuffer buffer, int indentationLine, char ch)
        {
            Document document = buffer.CurrentSnapshot.GetRelatedDocumentsWithChanges().First();
            var root = (CompilationUnitSyntax)document.GetSyntaxRootAsync().Result;

            var line = root.GetText().Lines[indentationLine];

            var index = line.ToString().LastIndexOf(ch);
            Assert.InRange(index, 0, int.MaxValue);

            // get token
            var position = line.Start + index;
            var token = root.FindToken(position);

            var formattingRuleProvider = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();

            var rules = formattingRuleProvider.CreateRule(document, position).Concat(Formatter.GetDefaultFormattingRules(document));

            var formatter = new SmartTokenFormatter(workspace.Options, rules, root);
            var changes = await formatter.FormatTokenAsync(workspace, token, CancellationToken.None);

            ApplyChanges(buffer, changes);
        }

        private static void ApplyChanges(ITextBuffer buffer, IList<TextChange> changes)
        {
            using (var edit = buffer.CreateEdit())
            {
                foreach (var change in changes)
                {
                    edit.Replace(change.Span.ToSpan(), change.NewText);
                }

                edit.Apply();
            }
        }

        protected async Task<int> GetSmartTokenFormatterIndentationAsync(
            string code,
            int indentationLine,
            char ch,
            int? baseIndentation = null,
            TextSpan span = default(TextSpan))
        {
            // create tree service
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(code))
            {
                if (baseIndentation.HasValue)
                {
                    var factory = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>()
                                as TestFormattingRuleFactoryServiceFactory.Factory;

                    factory.BaseIndentation = baseIndentation.Value;
                    factory.TextSpan = span;
                }

                var buffer = workspace.Documents.First().GetTextBuffer();
                return await GetSmartTokenFormatterIndentationWorkerAsync(workspace, buffer, indentationLine, ch);
            }
        }

        internal static async Task TestIndentationAsync(int point, int? expectedIndentation, ITextView textView, TestHostDocument subjectDocument)
        {
            var textUndoHistory = new Mock<ITextUndoHistoryRegistry>();
            var editorOperationsFactory = new Mock<IEditorOperationsFactoryService>();
            var editorOperations = new Mock<IEditorOperations>();
            editorOperationsFactory.Setup(x => x.GetEditorOperations(textView)).Returns(editorOperations.Object);

            var snapshot = subjectDocument.TextBuffer.CurrentSnapshot;
            var indentationLineFromBuffer = snapshot.GetLineFromPosition(point);

            var commandHandler = new SmartTokenFormatterCommandHandler(textUndoHistory.Object, editorOperationsFactory.Object);
            commandHandler.ExecuteCommand(new ReturnKeyCommandArgs(textView, subjectDocument.TextBuffer), () => { });
            var newSnapshot = subjectDocument.TextBuffer.CurrentSnapshot;

            int? actualIndentation;
            if (newSnapshot.Version.VersionNumber > snapshot.Version.VersionNumber)
            {
                actualIndentation = newSnapshot.GetLineFromLineNumber(indentationLineFromBuffer.LineNumber).GetFirstNonWhitespaceOffset();
            }
            else
            {
                var provider = new SmartIndent(textView);
                actualIndentation = await provider.GetDesiredIndentationAsync(indentationLineFromBuffer);
            }

            Assert.Equal(expectedIndentation, actualIndentation.Value);
        }

        public static async Task TestIndentationAsync(int indentationLine, int? expectedIndentation, TestWorkspace workspace)
        {
            var snapshot = workspace.Documents.First().TextBuffer.CurrentSnapshot;
            var bufferGraph = new Mock<IBufferGraph>(MockBehavior.Strict);
            bufferGraph.Setup(x => x.MapUpToSnapshot(It.IsAny<SnapshotPoint>(),
                                                     It.IsAny<PointTrackingMode>(),
                                                     It.IsAny<PositionAffinity>(),
                                                     It.IsAny<ITextSnapshot>()))
                .Returns<SnapshotPoint, PointTrackingMode, PositionAffinity, ITextSnapshot>((p, m, a, s) =>
                {
                    var factory = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>()
                                    as TestFormattingRuleFactoryServiceFactory.Factory;

                    if (factory != null && factory.BaseIndentation != 0 && factory.TextSpan.Contains(p.Position))
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
            var actualIndentation = await provider.GetDesiredIndentationAsync(indentationLineFromBuffer);

            Assert.Equal(expectedIndentation, actualIndentation);
        }
    }
}
