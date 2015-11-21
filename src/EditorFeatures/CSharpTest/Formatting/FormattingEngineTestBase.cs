// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.Implementation.Formatting;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Moq;
using Roslyn.Test.EditorUtilities;
using Roslyn.Test.Utilities;
using Xunit;
using Roslyn.Utilities;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    public class FormattingEngineTestBase
    {
        protected async Task AssertFormatAsync(string expected, string code, bool debugMode = false, Dictionary<OptionKey, object> changedOptionSet = null, bool useTab = false, bool testWithTransformation = true)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(code))
            {
                var hostdoc = workspace.Documents.First();

                // get original buffer
                var buffer = hostdoc.GetTextBuffer();

                // create new buffer with cloned content
                var clonedBuffer = EditorFactory.CreateBuffer(
                    buffer.ContentType.TypeName,
                    workspace.ExportProvider,
                    buffer.CurrentSnapshot.GetText());

                var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);
                var syntaxTree = await document.GetSyntaxTreeAsync();

                var formattingRuleProvider = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();

                var options = workspace.Options;
                if (changedOptionSet != null)
                {
                    foreach (var entry in changedOptionSet)
                    {
                        options = options.WithChangedOption(entry.Key, entry.Value);
                    }
                }

                options = options.WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, useTab);

                var root = syntaxTree.GetRoot();
                var rules = formattingRuleProvider.CreateRule(workspace.CurrentSolution.GetDocument(syntaxTree), 0).Concat(Formatter.GetDefaultFormattingRules(workspace, root.Language));

                AssertFormat(workspace, expected, options, rules, clonedBuffer, root);

                if (testWithTransformation)
                {
                    // format with node and transform
                    AssertFormatWithTransformation(workspace, expected, options, rules, root);
                }
            }
        }

        internal static void AssertFormatWithTransformation(Workspace workspace, string expected, OptionSet optionSet, IEnumerable<IFormattingRule> rules, SyntaxNode root)
        {
            var newRootNode = Formatter.Format(root, SpecializedCollections.SingletonEnumerable(root.FullSpan), workspace, optionSet, rules, CancellationToken.None);

            Assert.Equal(expected, newRootNode.ToFullString());

            // test doesn't use parsing option. add one if needed later
            var newRootNodeFromString = SyntaxFactory.ParseCompilationUnit(expected);

            // simple check to see whether two nodes are equivalent each other.
            Assert.True(newRootNodeFromString.IsEquivalentTo(newRootNode));
        }

        internal static void AssertFormat(Workspace workspace, string expected, OptionSet optionSet, IEnumerable<IFormattingRule> rules, ITextBuffer clonedBuffer, SyntaxNode root)
        {
            var changes = Formatter.GetFormattedTextChanges(root, SpecializedCollections.SingletonEnumerable(root.FullSpan), workspace, optionSet, rules, CancellationToken.None);
            var actual = ApplyResultAndGetFormattedText(clonedBuffer, changes);

            Assert.Equal(expected, actual);
        }

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

        protected async Task AssertFormatAsync(string expected, string code, IEnumerable<TextSpan> spans, bool debugMode = false, Dictionary<OptionKey, object> changedOptionSet = null, int? baseIndentation = null)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(code))
            {
                var hostdoc = workspace.Documents.First();
                var buffer = hostdoc.GetTextBuffer();

                var document = workspace.CurrentSolution.GetDocument(hostdoc.Id);
                var syntaxTree = await document.GetSyntaxTreeAsync();

                // create new buffer with cloned content
                var clonedBuffer = EditorFactory.CreateBuffer(
                    buffer.ContentType.TypeName,
                    workspace.ExportProvider,
                    buffer.CurrentSnapshot.GetText());

                var formattingRuleProvider = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();
                if (baseIndentation.HasValue)
                {
                    var factory = formattingRuleProvider as TestFormattingRuleFactoryServiceFactory.Factory;
                    factory.BaseIndentation = baseIndentation.Value;
                    factory.TextSpan = spans.First();
                }

                var options = workspace.Options;
                if (changedOptionSet != null)
                {
                    foreach (var entry in changedOptionSet)
                    {
                        options = options.WithChangedOption(entry.Key, entry.Value);
                    }
                }

                var root = syntaxTree.GetRoot();
                var rules = formattingRuleProvider.CreateRule(workspace.CurrentSolution.GetDocument(syntaxTree), 0).Concat(Formatter.GetDefaultFormattingRules(workspace, root.Language));
                AssertFormat(workspace, expected, options, rules, clonedBuffer, root, spans);

                // format with node and transform
                AssertFormatWithTransformation(workspace, expected, options, rules, root, spans);
            }
        }

        internal static void AssertFormatWithTransformation(Workspace workspace, string expected, OptionSet optionSet, IEnumerable<IFormattingRule> rules, SyntaxNode root, IEnumerable<TextSpan> spans)
        {
            var newRootNode = Formatter.Format(root, spans, workspace, optionSet, rules, CancellationToken.None);

            Assert.Equal(expected, newRootNode.ToFullString());

            // test doesn't use parsing option. add one if needed later
            var newRootNodeFromString = SyntaxFactory.ParseCompilationUnit(expected);

            // simple check to see whether two nodes are equivalent each other.
            Assert.True(newRootNodeFromString.IsEquivalentTo(newRootNode));
        }

        internal static void AssertFormat(Workspace workspace, string expected, OptionSet optionSet, IEnumerable<IFormattingRule> rules, ITextBuffer clonedBuffer, SyntaxNode root, IEnumerable<TextSpan> spans)
        {
            var result = Formatter.GetFormattedTextChanges(root, spans, workspace, optionSet, rules, CancellationToken.None);
            var actual = ApplyResultAndGetFormattedText(clonedBuffer, result);

            Assert.Equal(expected, actual);
        }

        protected static async Task AssertFormatWithViewAsync(string expectedWithMarker, string codeWithMarker, bool debugMode = false)
        {
            var editorOperations = new Mock<IEditorOperations>(MockBehavior.Strict);
            var editorOperationsFactoryService = new Mock<IEditorOperationsFactoryService>(MockBehavior.Strict);

            editorOperations.Setup(o => o.AddAfterTextBufferChangePrimitive());
            editorOperations.Setup(o => o.AddBeforeTextBufferChangePrimitive());

            editorOperationsFactoryService.Setup(s => s.GetEditorOperations(It.IsAny<ITextView>())).Returns(editorOperations.Object);

            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(codeWithMarker))
            {
                // set up caret position
                var testDocument = workspace.Documents.Single();
                var view = testDocument.GetTextView();
                view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, testDocument.CursorPosition.Value));

                // get original buffer
                var buffer = workspace.Documents.First().GetTextBuffer();

                var commandHandler = new FormatCommandHandler(TestWaitIndicator.Default, workspace.GetService<ITextUndoHistoryRegistry>(), editorOperationsFactoryService.Object);

                var commandArgs = new FormatDocumentCommandArgs(view, view.TextBuffer);
                commandHandler.ExecuteCommand(commandArgs, () => { });

                string expected;
                int expectedPosition;
                MarkupTestFile.GetPosition(expectedWithMarker, out expected, out expectedPosition);

                Assert.Equal(expected, view.TextSnapshot.GetText());

                var caretPosition = view.Caret.Position.BufferPosition.Position;
                Assert.True(expectedPosition == caretPosition,
                    string.Format("Caret positioned incorrectly. Should have been {0}, but was {1}.", expectedPosition, caretPosition));
            }
        }

        protected static async Task AssertFormatWithPasteOrReturnAsync(string expectedWithMarker, string codeWithMarker, bool allowDocumentChanges, bool isPaste = true)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(codeWithMarker))
            {
                workspace.CanApplyChangeDocument = allowDocumentChanges;

                // set up caret position
                var testDocument = workspace.Documents.Single();
                var view = testDocument.GetTextView();
                view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, testDocument.CursorPosition.Value));

                // get original buffer
                var buffer = workspace.Documents.First().GetTextBuffer();

                var optionService = workspace.Services.GetService<IOptionService>();
                if (isPaste)
                {
                    optionService.SetOptions(optionService.GetOptions().WithChangedOption(FeatureOnOffOptions.FormatOnPaste, LanguageNames.CSharp, true));
                    var commandHandler = new FormatCommandHandler(TestWaitIndicator.Default, null, null);
                    var commandArgs = new PasteCommandArgs(view, view.TextBuffer);
                    commandHandler.ExecuteCommand(commandArgs, () => { });
                }
                else
                {
                    // Return Key Command
                    var textUndoHistory = new Mock<ITextUndoHistoryRegistry>();
                    var editorOperationsFactory = new Mock<IEditorOperationsFactoryService>();
                    var editorOperations = new Mock<IEditorOperations>();
                    editorOperationsFactory.Setup(x => x.GetEditorOperations(testDocument.GetTextView())).Returns(editorOperations.Object);
                    var commandHandler = new FormatCommandHandler(TestWaitIndicator.Default, textUndoHistory.Object, editorOperationsFactory.Object);
                    var commandArgs = new ReturnKeyCommandArgs(view, view.TextBuffer);
                    commandHandler.ExecuteCommand(commandArgs, () => { });
                }

                string expected;
                int expectedPosition;
                MarkupTestFile.GetPosition(expectedWithMarker, out expected, out expectedPosition);

                Assert.Equal(expected, view.TextSnapshot.GetText());

                var caretPosition = view.Caret.Position.BufferPosition.Position;
                Assert.True(expectedPosition == caretPosition,
                    string.Format("Caret positioned incorrectly. Should have been {0}, but was {1}.", expectedPosition, caretPosition));
            }
        }

        protected async Task AssertFormatWithBaseIndentAsync(string expected, string markupCode, int baseIndentation)
        {
            string code;
            TextSpan span;
            MarkupTestFile.GetSpan(markupCode, out code, out span);

            await AssertFormatAsync(
                expected,
                code,
            new List<TextSpan> { span },
            baseIndentation: baseIndentation);
        }
    }
}
