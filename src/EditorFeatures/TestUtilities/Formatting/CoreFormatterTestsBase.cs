// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Projection;
using Moq;
using Roslyn.Test.EditorUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Formatting
{
    public abstract class CoreFormatterTestsBase
    {
        private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeatures.AddParts(typeof(TestFormattingRuleFactoryServiceFactory));

        private readonly ITestOutputHelper _output;

        protected CoreFormatterTestsBase(ITestOutputHelper output)
            => _output = output;

        protected abstract string GetLanguageName();
        protected abstract SyntaxNode ParseCompilationUnit(string expected);

        internal static void TestIndentation(
            int point, int? expectedIndentation, ITextView textView, EditorTestHostDocument subjectDocument, EditorOptionsService editorOptionsService)
        {
            var textUndoHistory = new Mock<ITextUndoHistoryRegistry>();
            var editorOperationsFactory = new Mock<IEditorOperationsFactoryService>();
            var editorOperations = new Mock<IEditorOperations>();
            editorOperationsFactory.Setup(x => x.GetEditorOperations(textView)).Returns(editorOperations.Object);

            var snapshot = subjectDocument.GetTextBuffer().CurrentSnapshot;
            var indentationLineFromBuffer = snapshot.GetLineFromPosition(point);

            var provider = new SmartIndent(textView, editorOptionsService);
            var actualIndentation = provider.GetDesiredIndentation(indentationLineFromBuffer);

            Assert.Equal(expectedIndentation, actualIndentation.Value);
        }

        internal void TestIndentation(
            EditorTestWorkspace workspace,
            int indentationLine,
            int? expectedIndentation,
            FormattingOptions2.IndentStyle indentStyle,
            bool useTabs)
        {
            var language = GetLanguageName();

            var editorOptionsFactory = workspace.GetService<IEditorOptionsFactoryService>();
            var document = workspace.Documents.First();
            var textBuffer = document.GetTextBuffer();
            var editorOptions = editorOptionsFactory.GetOptions(textBuffer);

            editorOptions.SetOptionValue(DefaultOptions.IndentStyleId, indentStyle.ToEditorIndentStyle());
            editorOptions.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, !useTabs);

            // Remove once https://github.com/dotnet/roslyn/issues/62204 is fixed:
            workspace.GlobalOptions.SetGlobalOption(IndentationOptionsStorage.SmartIndent, document.Project.Language, indentStyle);

            var snapshot = textBuffer.CurrentSnapshot;
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

            var provider = new SmartIndent(
                textView.Object,
                workspace.GetService<EditorOptionsService>());

            var indentationLineFromBuffer = snapshot.GetLineFromLineNumber(indentationLine);
            var actualIndentation = provider.GetDesiredIndentation(indentationLineFromBuffer);

            Assert.Equal(expectedIndentation, actualIndentation);
        }

        private protected void AssertFormatWithView(string expectedWithMarker, string codeWithMarker, OptionsCollection options = null)
        {
            AssertFormatWithView(expectedWithMarker, codeWithMarker, parseOptions: null, options);
        }

        private protected void AssertFormatWithView(string expectedWithMarker, string codeWithMarker, ParseOptions parseOptions, OptionsCollection options = null)
        {
            using var workspace = CreateWorkspace(codeWithMarker, parseOptions);

            options?.SetGlobalOptions(workspace.GlobalOptions);

            // set up caret position
            var testDocument = workspace.Documents.Single();
            var view = testDocument.GetTextView();
            view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, testDocument.CursorPosition.Value));

            // get original buffer
            var buffer = workspace.Documents.First().GetTextBuffer();

            var commandHandler = workspace.GetService<FormatCommandHandler>();

            var commandArgs = new FormatDocumentCommandArgs(view, view.TextBuffer);
            commandHandler.ExecuteCommand(commandArgs, TestCommandExecutionContext.Create());
            MarkupTestFile.GetPosition(expectedWithMarker, out var expected, out int expectedPosition);

            Assert.Equal(expected, view.TextSnapshot.GetText());

            var caretPosition = view.Caret.Position.BufferPosition.Position;
            Assert.True(expectedPosition == caretPosition,
                string.Format("Caret positioned incorrectly. Should have been {0}, but was {1}.", expectedPosition, caretPosition));
        }

        private EditorTestWorkspace CreateWorkspace(string codeWithMarker, ParseOptions parseOptions = null)
            => this.GetLanguageName() == LanguageNames.CSharp
                ? EditorTestWorkspace.CreateCSharp(codeWithMarker, composition: s_composition, parseOptions: parseOptions)
                : EditorTestWorkspace.CreateVisualBasic(codeWithMarker, composition: s_composition, parseOptions: parseOptions);

        private static string ApplyResultAndGetFormattedText(ITextBuffer buffer, IList<TextChange> changes)
        {
            using (var edit = buffer.CreateEdit())
            {
                foreach (var change in changes)
                {
                    edit.Replace(change.Span.ToSpan(), change.NewText);
                }

                edit.Apply();
            }

            return buffer.CurrentSnapshot.GetText();
        }

        private protected async Task AssertFormatAsync(string expected, string code, IEnumerable<TextSpan> spans, OptionsCollection options = null, int? baseIndentation = null)
        {
            using var workspace = CreateWorkspace(code);
            var hostdoc = workspace.Documents.First();
            var buffer = hostdoc.GetTextBuffer();

            var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);
            var documentSyntax = await ParsedDocument.CreateAsync(document, CancellationToken.None).ConfigureAwait(false);

            // create new buffer with cloned content
            var clonedBuffer = EditorFactory.CreateBuffer(
                workspace.ExportProvider,
                buffer.ContentType,
                buffer.CurrentSnapshot.GetText());

            var formattingRuleProvider = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();
            if (baseIndentation.HasValue)
            {
                var factory = (TestFormattingRuleFactoryServiceFactory.Factory)formattingRuleProvider;
                factory.BaseIndentation = baseIndentation.Value;
                factory.TextSpan = spans?.First() ?? documentSyntax.Root.FullSpan;
            }

            var formattingService = document.GetRequiredLanguageService<ISyntaxFormattingService>();

            var formattingOptions = (options != null)
                ? formattingService.GetFormattingOptions(options)
                : formattingService.DefaultOptions;

            ImmutableArray<AbstractFormattingRule> rules = [formattingRuleProvider.CreateRule(documentSyntax, 0), .. Formatter.GetDefaultFormattingRules(document)];
            AssertFormat(workspace, expected, formattingOptions, rules, clonedBuffer, documentSyntax.Root, spans);

            // format with node and transform
            AssertFormatWithTransformation(workspace, expected, formattingOptions, rules, documentSyntax.Root, spans);
        }

        internal void AssertFormatWithTransformation(Workspace workspace, string expected, SyntaxFormattingOptions options, ImmutableArray<AbstractFormattingRule> rules, SyntaxNode root, IEnumerable<TextSpan> spans)
        {
            var newRootNode = Formatter.Format(root, spans, workspace.Services.SolutionServices, options, rules, CancellationToken.None);

            Assert.Equal(expected, newRootNode.ToFullString());

            // test doesn't use parsing option. add one if needed later
            var newRootNodeFromString = ParseCompilationUnit(expected);

            // simple check to see whether two nodes are equivalent each other.
            Assert.True(newRootNodeFromString.IsEquivalentTo(newRootNode));
        }

        internal void AssertFormat(Workspace workspace, string expected, SyntaxFormattingOptions options, ImmutableArray<AbstractFormattingRule> rules, ITextBuffer clonedBuffer, SyntaxNode root, IEnumerable<TextSpan> spans)
        {
            var result = Formatter.GetFormattedTextChanges(root, spans, workspace.Services.SolutionServices, options, rules, CancellationToken.None);
            var actual = ApplyResultAndGetFormattedText(clonedBuffer, result);

            if (actual != expected)
            {
                _output.WriteLine(actual);
                AssertEx.EqualOrDiff(expected, actual);
            }
        }

        protected void AssertFormatWithPasteOrReturn(string expectedWithMarker, string codeWithMarker, bool allowDocumentChanges, bool isPaste = true)
        {
            using var workspace = CreateWorkspace(codeWithMarker);
            workspace.CanApplyChangeDocument = allowDocumentChanges;

            // set up caret position
            var testDocument = workspace.Documents.Single();
            var view = testDocument.GetTextView();
            view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, testDocument.CursorPosition.Value));

            // get original buffer
            var buffer = workspace.Documents.First().GetTextBuffer();

            if (isPaste)
            {
                var commandHandler = workspace.GetService<FormatCommandHandler>();
                var commandArgs = new PasteCommandArgs(view, view.TextBuffer);
                commandHandler.ExecuteCommand(commandArgs, () => { }, TestCommandExecutionContext.Create());
            }
            else
            {
                // Return Key Command
                var commandHandler = workspace.GetService<FormatCommandHandler>();
                var commandArgs = new ReturnKeyCommandArgs(view, view.TextBuffer);
                commandHandler.ExecuteCommand(commandArgs, () => { }, TestCommandExecutionContext.Create());
            }

            MarkupTestFile.GetPosition(expectedWithMarker, out var expected, out int expectedPosition);

            Assert.Equal(expected, view.TextSnapshot.GetText());

            var caretPosition = view.Caret.Position.BufferPosition.Position;
            Assert.True(expectedPosition == caretPosition,
                string.Format("Caret positioned incorrectly. Should have been {0}, but was {1}.", expectedPosition, caretPosition));
        }

        private protected async Task AssertFormatWithBaseIndentAsync(string expected, string markupCode, int baseIndentation, OptionsCollection options = null)
        {
            TestFileMarkupParser.GetSpans(markupCode, out var code, out ImmutableArray<TextSpan> spans);
            await AssertFormatAsync(expected, code, spans, options, baseIndentation);
        }

        /// <summary>
        /// Asserts formatting on an arbitrary <see cref="SyntaxNode"/> that is not part of a <see cref="SyntaxTree"/>
        /// </summary>
        /// <param name="node">the <see cref="SyntaxNode"/> to format.</param>
        /// <remarks>uses an <see cref="AdhocWorkspace"/> for formatting context, since the <paramref name="node"/> is not associated with a <see cref="SyntaxTree"/> </remarks>
        protected static void AssertFormatOnArbitraryNode(SyntaxNode node, string expected)
        {
            using var workspace = new AdhocWorkspace();
            var formattingService = workspace.Services.GetLanguageServices(node.Language).GetRequiredService<ISyntaxFormattingService>();
            var options = formattingService.GetFormattingOptions(StructuredAnalyzerConfigOptions.Empty);
            var result = Formatter.Format(node, workspace.Services.SolutionServices, options, CancellationToken.None);
            var actual = result.GetText().ToString();

            Assert.Equal(expected, actual);
        }
    }
}
