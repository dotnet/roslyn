// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public abstract class AbstractCodeActionTest
    {
        protected abstract string GetLanguage();
        protected abstract ParseOptions GetScriptOptions();
        protected abstract TestWorkspace CreateWorkspaceFromFile(string definition, ParseOptions parseOptions, CompilationOptions compilationOptions);
        protected abstract object CreateCodeRefactoringProvider(Workspace workspace);

        protected virtual void TestMissing(
            string initial,
            Func<dynamic, dynamic> nodeLocator = null)
        {
            TestMissing(initial, null, nodeLocator);
            TestMissing(initial, GetScriptOptions(), nodeLocator);
        }

        protected virtual void TestMissing(
            string initial,
            ParseOptions parseOptions,
            Func<dynamic, dynamic> nodeLocator = null)
        {
            TestMissing(initial, parseOptions, null, nodeLocator);
        }

        protected void TestMissing(
            string initialMarkup,
            ParseOptions parseOptions,
            CompilationOptions compilationOptions,
            Func<dynamic, dynamic> nodeLocator = null)
        {
            using (var workspace = CreateWorkspaceFromFile(initialMarkup, parseOptions, compilationOptions))
            {
                var codeIssueOrRefactoring = GetCodeRefactoring(workspace, nodeLocator);
                Assert.Null(codeIssueOrRefactoring);
            }
        }

        protected void Test(
            string initial,
            string expected,
            int index = 0,
            bool compareTokens = true,
            Func<dynamic, dynamic> nodeLocator = null,
            IDictionary<OptionKey, object> options = null)
        {
            Test(initial, expected, null, index, compareTokens, nodeLocator, options);
            Test(initial, expected, GetScriptOptions(), index, compareTokens, nodeLocator, options);
        }

        protected void Test(
            string initial,
            string expected,
            ParseOptions parseOptions,
            int index = 0,
            bool compareTokens = true,
            Func<dynamic, dynamic> nodeLocator = null,
            IDictionary<OptionKey, object> options = null)
        {
            Test(initial, expected, parseOptions, null, index, compareTokens, nodeLocator, options);
        }

        private void ApplyOptionsToWorkspace(Workspace workspace, IDictionary<OptionKey, object> options)
        {
            var optionService = workspace.Services.GetService<IOptionService>();
            var optionSet = optionService.GetOptions();
            foreach (var option in options)
            {
                optionSet = optionSet.WithChangedOption(option.Key, option.Value);
            }

            optionService.SetOptions(optionSet);
        }

        protected void Test(
            string initialMarkup,
            string expectedMarkup,
            ParseOptions parseOptions,
            CompilationOptions compilationOptions,
            int index = 0,
            bool compareTokens = true,
            Func<dynamic, dynamic> nodeLocator = null,
            IDictionary<OptionKey, object> options = null)
        {
            string expected;
            IDictionary<string, IList<TextSpan>> spanMap;
            MarkupTestFile.GetSpans(expectedMarkup.NormalizeLineEndings(), out expected, out spanMap);

            var conflictSpans = spanMap.GetOrAdd("Conflict", _ => new List<TextSpan>());
            var renameSpans = spanMap.GetOrAdd("Rename", _ => new List<TextSpan>());
            var warningSpans = spanMap.GetOrAdd("Warning", _ => new List<TextSpan>());

            using (var workspace = CreateWorkspaceFromFile(initialMarkup, parseOptions, compilationOptions))
            {
                if (options != null)
                {
                    ApplyOptionsToWorkspace(workspace, options);
                }

                var codeIssueOrRefactoring = GetCodeRefactoring(workspace, nodeLocator);
                TestActions(
                    workspace, expected, index,
                    codeIssueOrRefactoring.Actions.ToList(),
                    conflictSpans, renameSpans, warningSpans,
                    compareTokens: compareTokens);
            }
        }

        internal ICodeRefactoring GetCodeRefactoring(
            TestWorkspace workspace,
            Func<dynamic, dynamic> nodeLocator)
        {
            return GetCodeRefactorings(workspace, nodeLocator).FirstOrDefault();
        }

        private IEnumerable<ICodeRefactoring> GetCodeRefactorings(
            TestWorkspace workspace,
            Func<dynamic, dynamic> nodeLocator)
        {
            var provider = CreateCodeRefactoringProvider(workspace);
            return SpecializedCollections.SingletonEnumerable(
                GetCodeRefactoring((CodeRefactoringProvider)provider, workspace));
        }

        protected virtual IEnumerable<SyntaxNode> GetNodes(SyntaxNode root, TextSpan span)
        {
            IEnumerable<SyntaxNode> nodes;
            nodes = root.FindToken(span.Start, findInsideTrivia: true).GetAncestors<SyntaxNode>().Where(a => span.Contains(a.Span)).Reverse();
            return nodes;
        }

        private CodeRefactoring GetCodeRefactoring(
            CodeRefactoringProvider provider,
            TestWorkspace workspace)
        {
            var document = GetDocument(workspace);
            var span = workspace.Documents.Single(d => !d.IsLinkFile).SelectedSpans.Single();
            var actions = new List<CodeAction>();
            var context = new CodeRefactoringContext(document, span, (a) => actions.Add(a), CancellationToken.None);
            provider.ComputeRefactoringsAsync(context).Wait();
            return actions.Count > 0 ? new CodeRefactoring(provider, actions) : null;
        }

        protected void TestSmartTagText(
            string initialMarkup,
            string displayText,
            int index = 0,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null)
        {
            using (var workspace = CreateWorkspaceFromFile(initialMarkup, parseOptions, compilationOptions))
            {
                var refactoring = GetCodeRefactoring(workspace, nodeLocator: null);
                Assert.Equal(displayText, refactoring.Actions.ElementAt(index).Title);
            }
        }

        protected void TestExactActionSetOffered(
            string initialMarkup,
            IEnumerable<string> expectedActionSet,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null)
        {
            using (var workspace = CreateWorkspaceFromFile(initialMarkup, parseOptions, compilationOptions))
            {
                var codeIssueOrRefactoring = GetCodeRefactoring(workspace, nodeLocator: null);

                var actualActionSet = codeIssueOrRefactoring.Actions.Select(a => a.Title);
                Assert.True(actualActionSet.SequenceEqual(expectedActionSet),
                    "Expected: " + string.Join(", ", expectedActionSet) +
                    "\nActual: " + string.Join(", ", actualActionSet));
            }
        }

        protected void TestActionCount(
            string initialMarkup,
            int count,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null)
        {
            using (var workspace = CreateWorkspaceFromFile(initialMarkup, parseOptions, compilationOptions))
            {
                var codeIssueOrRefactoring = GetCodeRefactoring(workspace, nodeLocator: null);

                Assert.Equal(count, codeIssueOrRefactoring.Actions.Count());
            }
        }

        protected void TestActions(
            TestWorkspace workspace,
            string expectedText,
            int index,
            IList<CodeAction> actions,
            IList<TextSpan> expectedConflictSpans = null,
            IList<TextSpan> expectedRenameSpans = null,
            IList<TextSpan> expectedWarningSpans = null,
            string expectedPreviewContents = null,
            bool compareTokens = true,
            bool compareExpectedTextAfterApply = false)
        {
            var operations = VerifyInputsAndGetOperations(index, actions);

            VerifyPreviewContents(workspace, expectedPreviewContents, operations);

            // Test annotations from the operation's new solution

            var applyChangesOperation = operations.OfType<ApplyChangesOperation>().First();

            var oldSolution = workspace.CurrentSolution;

            var newSolutionFromOperation = applyChangesOperation.ChangedSolution;
            var documentFromOperation = SolutionUtilities.GetSingleChangedDocument(oldSolution, newSolutionFromOperation);
            var fixedRootFromOperation = documentFromOperation.GetSyntaxRootAsync().Result;

            TestAnnotations(expectedText, expectedConflictSpans, fixedRootFromOperation, ConflictAnnotation.Kind, compareTokens);
            TestAnnotations(expectedText, expectedRenameSpans, fixedRootFromOperation, RenameAnnotation.Kind, compareTokens);
            TestAnnotations(expectedText, expectedWarningSpans, fixedRootFromOperation, WarningAnnotation.Kind, compareTokens);

            // Test final text

            string actualText;
            if (compareExpectedTextAfterApply)
            {
                applyChangesOperation.Apply(workspace, CancellationToken.None);
                var newSolutionAfterApply = workspace.CurrentSolution;

                var documentFromAfterApply = SolutionUtilities.GetSingleChangedDocument(oldSolution, newSolutionAfterApply);
                var fixedRootFromAfterApply = documentFromAfterApply.GetSyntaxRootAsync().Result;
                actualText = compareTokens ? fixedRootFromAfterApply.ToString() : fixedRootFromAfterApply.ToFullString();
            }
            else
            {
                actualText = compareTokens ? fixedRootFromOperation.ToString() : fixedRootFromOperation.ToFullString();
            }

            if (compareTokens)
            {
                TokenUtilities.AssertTokensEqual(expectedText, actualText, GetLanguage());
            }
            else
            {
                Assert.Equal(expectedText, actualText);
            }
        }

        protected void TestActionsOnLinkedFiles(
            TestWorkspace workspace,
            string expectedText,
            int index,
            IList<CodeAction> actions,
            string expectedPreviewContents = null,
            bool compareTokens = true)
        {
            var operations = VerifyInputsAndGetOperations(index, actions);

            VerifyPreviewContents(workspace, expectedPreviewContents, operations);

            var applyChangesOperation = operations.OfType<ApplyChangesOperation>().First();
            applyChangesOperation.Apply(workspace, CancellationToken.None);

            foreach (var document in workspace.Documents)
            {
                var fixedRoot = workspace.CurrentSolution.GetDocument(document.Id).GetSyntaxRootAsync().Result;
                var actualText = compareTokens ? fixedRoot.ToString() : fixedRoot.ToFullString();

                if (compareTokens)
                {
                    TokenUtilities.AssertTokensEqual(expectedText, actualText, GetLanguage());
                }
                else
                {
                    Assert.Equal(expectedText, actualText);
                }
            }
        }

        private static IEnumerable<CodeActionOperation> VerifyInputsAndGetOperations(int index, IList<CodeAction> actions)
        {
            Assert.NotNull(actions);
            Assert.InRange(index, 0, actions.Count - 1);

            var action = actions[index];
            return action.GetOperationsAsync(CancellationToken.None).Result;
        }

        private static void VerifyPreviewContents(TestWorkspace workspace, string expectedPreviewContents, IEnumerable<CodeActionOperation> operations)
        {
            if (expectedPreviewContents != null)
            {
                var editHandler = workspace.ExportProvider.GetExportedValue<ICodeActionEditHandlerService>();
                var content = editHandler.GetPreviews(workspace, operations, CancellationToken.None).TakeNextPreviewAsync().PumpingWaitResult();
                var diffView = content as IWpfDifferenceViewer;
                Assert.NotNull(diffView);
                var previewContents = diffView.RightView.TextBuffer.AsTextContainer().CurrentText.ToString();
                diffView.Close();

                Assert.Equal(expectedPreviewContents, previewContents);
            }
        }

        private void TestAnnotations(
            string expectedText,
            IList<TextSpan> expectedSpans,
            SyntaxNode fixedRoot,
            string annotationKind,
            bool compareTokens)
        {
            expectedSpans = expectedSpans ?? new List<TextSpan>();
            var annotatedTokens = fixedRoot.GetAnnotatedNodesAndTokens(annotationKind).Select(n => (SyntaxToken)n).ToList();

            Assert.Equal(expectedSpans.Count, annotatedTokens.Count);

            if (expectedSpans.Count > 0)
            {
                var expectedTokens = TokenUtilities.GetTokens(TokenUtilities.GetSyntaxRoot(expectedText, GetLanguage()));
                var actualTokens = TokenUtilities.GetTokens(fixedRoot);

                for (var i = 0; i < Math.Min(expectedTokens.Count, actualTokens.Count); i++)
                {
                    var expectedToken = expectedTokens[i];
                    var actualToken = actualTokens[i];

                    var actualIsConflict = annotatedTokens.Contains(actualToken);
                    var expectedIsConflict = expectedSpans.Contains(expectedToken.Span);
                    Assert.Equal(expectedIsConflict, actualIsConflict);
                }
            }
        }

        protected static Document GetDocument(TestWorkspace workspace)
        {
            return workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
        }

        protected void TestAddDocument(
            string initial,
            string expected,
            IList<string> expectedContainers,
            string expectedDocumentName,
            int index = 0,
            bool compareTokens = true)
        {
            TestAddDocument(initial, expected, index, expectedContainers, expectedDocumentName, null, null, compareTokens);
            TestAddDocument(initial, expected, index, expectedContainers, expectedDocumentName, GetScriptOptions(), null, compareTokens);
        }

        private void TestAddDocument(
            string initialMarkup,
            string expected,
            int index,
            IList<string> expectedContainers,
            string expectedDocumentName,
            ParseOptions parseOptions,
            CompilationOptions compilationOptions,
            bool compareTokens)
        {
            using (var workspace = CreateWorkspaceFromFile(initialMarkup, parseOptions, compilationOptions))
            {
                var codeIssue = GetCodeRefactoring(workspace, nodeLocator: null);
                TestAddDocument(workspace, expected, index, expectedContainers, expectedDocumentName,
                    codeIssue.Actions.ToList(), compareTokens);
            }
        }

        private void TestAddDocument(
            TestWorkspace workspace,
            string expected,
            int index,
            IList<string> expectedFolders,
            string expectedDocumentName,
            IList<CodeAction> fixes,
            bool compareTokens)
        {
            Assert.NotNull(fixes);
            Assert.InRange(index, 0, fixes.Count - 1);

            var operations = fixes[index].GetOperationsAsync(CancellationToken.None).Result;
            var applyChangesOperation = operations.OfType<ApplyChangesOperation>().First();
            var oldSolution = workspace.CurrentSolution;
            var newSolution = applyChangesOperation.ChangedSolution;

            var addedDocument = SolutionUtilities.GetSingleAddedDocument(oldSolution, newSolution);
            Assert.NotNull(addedDocument);

            AssertEx.Equal(expectedFolders, addedDocument.Folders);
            Assert.Equal(expectedDocumentName, addedDocument.Name);
            if (compareTokens)
            {
                TokenUtilities.AssertTokensEqual(
                    expected, addedDocument.GetTextAsync().Result.ToString(), GetLanguage());
            }
            else
            {
                Assert.Equal(expected, addedDocument.GetTextAsync().Result.ToString());
            }

            var editHandler = workspace.ExportProvider.GetExportedValue<ICodeActionEditHandlerService>();
            var content = editHandler.GetPreviews(workspace, operations, CancellationToken.None).TakeNextPreviewAsync().PumpingWaitResult();
            var textView = content as IWpfTextView;
            Assert.NotNull(textView);

            textView.Close();
        }
    }
}
