// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.Formatting;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.EditorUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    [UseExportProvider]
    public class FormattingEngineTestBase
    {
        internal static void AssertFormatWithTransformation(Workspace workspace, string expected, OptionSet optionSet, IEnumerable<AbstractFormattingRule> rules, SyntaxNode root)
        {
            var newRootNode = Formatter.Format(root, SpecializedCollections.SingletonEnumerable(root.FullSpan), workspace, optionSet, rules, CancellationToken.None);

            Assert.Equal(expected, newRootNode.ToFullString());

            // test doesn't use parsing option. add one if needed later
            var newRootNodeFromString = SyntaxFactory.ParseCompilationUnit(expected);

            // simple check to see whether two nodes are equivalent each other.
            Assert.True(newRootNodeFromString.IsEquivalentTo(newRootNode));
        }

        internal static void AssertFormat(Workspace workspace, string expected, OptionSet optionSet, IEnumerable<AbstractFormattingRule> rules, ITextBuffer clonedBuffer, SyntaxNode root)
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
            using var workspace = TestWorkspace.CreateCSharp(code);
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

            var root = await syntaxTree.GetRootAsync();
            var rules = formattingRuleProvider.CreateRule(workspace.CurrentSolution.GetDocument(syntaxTree), 0).Concat(Formatter.GetDefaultFormattingRules(workspace, root.Language));
            AssertFormat(workspace, expected, options, rules, clonedBuffer, root, spans);

            // format with node and transform
            AssertFormatWithTransformation(workspace, expected, options, rules, root, spans);
        }

        internal static void AssertFormatWithTransformation(Workspace workspace, string expected, OptionSet optionSet, IEnumerable<AbstractFormattingRule> rules, SyntaxNode root, IEnumerable<TextSpan> spans)
        {
            var newRootNode = Formatter.Format(root, spans, workspace, optionSet, rules, CancellationToken.None);

            Assert.Equal(expected, newRootNode.ToFullString());

            // test doesn't use parsing option. add one if needed later
            var newRootNodeFromString = SyntaxFactory.ParseCompilationUnit(expected);

            // simple check to see whether two nodes are equivalent each other.
            Assert.True(newRootNodeFromString.IsEquivalentTo(newRootNode));
        }

        internal static void AssertFormat(Workspace workspace, string expected, OptionSet optionSet, IEnumerable<AbstractFormattingRule> rules, ITextBuffer clonedBuffer, SyntaxNode root, IEnumerable<TextSpan> spans)
        {
            var result = Formatter.GetFormattedTextChanges(root, spans, workspace, optionSet, rules, CancellationToken.None);
            var actual = ApplyResultAndGetFormattedText(clonedBuffer, result);

            Assert.Equal(expected, actual);
        }

        protected static void AssertFormatWithView(string expectedWithMarker, string codeWithMarker, params (PerLanguageOption<bool> option, bool enabled)[] options)
        {
            using var workspace = TestWorkspace.CreateCSharp(codeWithMarker);
            if (options != null)
            {
                foreach (var option in options)
                {
                    workspace.Options = workspace.Options.WithChangedOption(option.option, LanguageNames.CSharp, option.enabled);
                }
            }

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

        protected static void AssertFormatWithPasteOrReturn(string expectedWithMarker, string codeWithMarker, bool allowDocumentChanges, bool isPaste = true)
        {
            using var workspace = TestWorkspace.CreateCSharp(codeWithMarker);
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

        protected async Task AssertFormatWithBaseIndentAsync(string expected, string markupCode, int baseIndentation)
        {
            MarkupTestFile.GetSpan(markupCode, out var code, out var span);

            await AssertFormatAsync(
                expected,
                code,
            new List<TextSpan> { span },
            baseIndentation: baseIndentation);
        }

        /// <summary>
        /// Asserts formatting on an arbitrary <see cref="SyntaxNode"/> that is not part of a <see cref="SyntaxTree"/>
        /// </summary>
        /// <param name="node">the <see cref="SyntaxNode"/> to format.</param>
        /// <remarks>uses an <see cref="AdhocWorkspace"/> for formatting context, since the <paramref name="node"/> is not associated with a <see cref="SyntaxTree"/> </remarks>
        protected void AssertFormatOnArbitraryNode(SyntaxNode node, string expected)
        {
            var result = Formatter.Format(node, new AdhocWorkspace());
            var actual = result.GetText().ToString();

            Assert.Equal(expected, actual);
        }
    }
}
