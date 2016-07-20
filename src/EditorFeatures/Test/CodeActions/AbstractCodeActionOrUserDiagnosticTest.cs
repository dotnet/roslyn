// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Editor.Implementation.Preview;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public abstract class AbstractCodeActionOrUserDiagnosticTest
    {
        protected abstract string GetLanguage();
        protected abstract ParseOptions GetScriptOptions();
        protected abstract Task<TestWorkspace> CreateWorkspaceFromFileAsync(string definition, ParseOptions parseOptions, CompilationOptions compilationOptions);

        private void TestAnnotations(
            string expectedText,
            IList<TextSpan> expectedSpans,
            SyntaxNode fixedRoot,
            string annotationKind,
            bool compareTokens,
            ParseOptions parseOptions = null)
        {
            expectedSpans = expectedSpans ?? new List<TextSpan>();
            var annotatedTokens = fixedRoot.GetAnnotatedNodesAndTokens(annotationKind).Select(n => (SyntaxToken)n).ToList();

            Assert.Equal(expectedSpans.Count, annotatedTokens.Count);

            if (expectedSpans.Count > 0)
            {
                var expectedTokens = TokenUtilities.GetTokens(TokenUtilities.GetSyntaxRoot(expectedText, GetLanguage(), parseOptions));
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

        protected async Task TestMissingAsync(
            string initialMarkup,
            IDictionary<OptionKey, object> options = null,
            string fixAllActionEquivalenceKey = null,
            object fixProviderData = null)
        {
            await TestMissingAsync(initialMarkup, parseOptions: null, options: options, fixAllActionEquivalenceKey: fixAllActionEquivalenceKey, fixProviderData: fixProviderData);
            await TestMissingAsync(initialMarkup, parseOptions: GetScriptOptions(), options: options, fixAllActionEquivalenceKey: fixAllActionEquivalenceKey, fixProviderData: fixProviderData);
        }

        protected async Task TestMissingAsync(
            string initialMarkup,
            ParseOptions parseOptions,
            IDictionary<OptionKey, object> options = null,
            string fixAllActionEquivalenceKey = null,
            object fixProviderData = null,
            bool withScriptOption = false)
        {
            await TestMissingAsync(initialMarkup, parseOptions, compilationOptions: null, options: options, fixAllActionEquivalenceKey: fixAllActionEquivalenceKey, fixProviderData: fixProviderData);

            if (withScriptOption)
            {
                await TestMissingAsync(initialMarkup, parseOptions.WithKind(SourceCodeKind.Script), compilationOptions: null, options: options, fixAllActionEquivalenceKey: fixAllActionEquivalenceKey, fixProviderData: fixProviderData);
            }
        }

        protected async Task TestMissingAsync(
            string initialMarkup,
            ParseOptions parseOptions, CompilationOptions compilationOptions,
            IDictionary<OptionKey, object> options = null,
            string fixAllActionEquivalenceKey = null,
            object fixProviderData = null)
        {
            using (var workspace = await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions, compilationOptions))
            {
                workspace.ApplyOptions(options);

                var actions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey, fixProviderData);
                Assert.True(actions == null || actions.Count == 0);
            }
        }

        protected async Task<IList<CodeAction>> GetCodeActionsAsync(
            TestWorkspace workspace, string fixAllActionEquivalenceKey, object fixProviderData = null)
        {
            return MassageActions(await GetCodeActionsWorkerAsync(workspace, fixAllActionEquivalenceKey, fixProviderData));
        }

        protected abstract Task<IList<CodeAction>> GetCodeActionsWorkerAsync(
            TestWorkspace workspace, string fixAllActionEquivalenceKey, object fixProviderData = null);

        protected async Task TestSmartTagTextAsync(
            string initialMarkup,
            string displayText,
            int index = 0,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            object fixProviderData = null)
        {
            using (var workspace = await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions, compilationOptions))
            {
                var actions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey: null, fixProviderData: fixProviderData);
                Assert.Equal(displayText, actions.ElementAt(index).Title);
            }
        }

        protected async Task TestExactActionSetOfferedAsync(
            string initialMarkup,
            IEnumerable<string> expectedActionSet,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null)
        {
            using (var workspace = await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions, compilationOptions))
            {
                var actions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey: null);

                var actualActionSet = actions.Select(a => a.Title);
                Assert.True(actualActionSet.SequenceEqual(expectedActionSet),
                    "Expected: " + string.Join(", ", expectedActionSet) +
                    "\nActual: " + string.Join(", ", actualActionSet));
            }
        }

        protected async Task TestActionCountAsync(
            string initialMarkup,
            int count,
            ParseOptions parseOptions = null, CompilationOptions compilationOptions = null)
        {
            using (var workspace = await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions, compilationOptions))
            {
                var actions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey: null);

                Assert.Equal(count, actions.Count());
            }
        }

        protected async Task TestAddDocument(
            string initialMarkup, string expectedMarkup,
            IList<string> expectedContainers,
            string expectedDocumentName,
            int index = 0,
            bool compareTokens = true, bool isLine = true)
        {
            await TestAddDocument(initialMarkup, expectedMarkup, index, expectedContainers, expectedDocumentName, parseOptions: null, compilationOptions: null, compareTokens: compareTokens, isLine: isLine);
            await TestAddDocument(initialMarkup, expectedMarkup, index, expectedContainers, expectedDocumentName, GetScriptOptions(), compilationOptions: null, compareTokens: compareTokens, isLine: isLine);
        }

        protected async Task<Tuple<Solution, Solution>> TestAddDocumentAsync(
            TestWorkspace workspace,
            string expectedMarkup,
            int index,
            string expectedDocumentName,
            IList<string> expectedContainers,
            bool compareTokens = true)
        {
            var codeActions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey: null);
            return await TestAddDocument(workspace, expectedMarkup, index, expectedContainers, expectedDocumentName,
                codeActions, compareTokens);
        }

        private async Task TestAddDocument(
            string initialMarkup, string expectedMarkup,
            int index,
            IList<string> expectedContainers,
            string expectedDocumentName,
            ParseOptions parseOptions, CompilationOptions compilationOptions,
            bool compareTokens, bool isLine)
        {
            using (var workspace = isLine
                ? await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions, compilationOptions)
                : await TestWorkspace.CreateAsync(initialMarkup))
            {
                var codeActions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey: null);
                await TestAddDocument(workspace, expectedMarkup, index, expectedContainers, expectedDocumentName,
                    codeActions, compareTokens);
            }
        }

        private async Task<Tuple<Solution, Solution>> TestAddDocument(
            TestWorkspace workspace,
            string expectedMarkup,
            int index,
            IList<string> expectedFolders,
            string expectedDocumentName,
            IList<CodeAction> actions,
            bool compareTokens)
        {
            var operations = await VerifyInputsAndGetOperationsAsync(index, actions);
            return await TestAddDocument(
                workspace,
                expectedMarkup,
                operations,
                hasProjectChange: false,
                modifiedProjectId: null,
                expectedFolders: expectedFolders,
                expectedDocumentName: expectedDocumentName,
                compareTokens: compareTokens);
        }

        protected async Task<Tuple<Solution, Solution>> TestAddDocument(
            TestWorkspace workspace,
            string expected,
            IEnumerable<CodeActionOperation> operations,
            bool hasProjectChange,
            ProjectId modifiedProjectId,
            IList<string> expectedFolders,
            string expectedDocumentName,
            bool compareTokens)
        {
            var appliedChanges = ApplyOperationsAndGetSolution(workspace, operations);
            var oldSolution = appliedChanges.Item1;
            var newSolution = appliedChanges.Item2;

            Document addedDocument = null;
            if (!hasProjectChange)
            {
                addedDocument = SolutionUtilities.GetSingleAddedDocument(oldSolution, newSolution);
            }
            else
            {
                Assert.NotNull(modifiedProjectId);
                addedDocument = newSolution.GetProject(modifiedProjectId).Documents.SingleOrDefault(doc => doc.Name == expectedDocumentName);
            }

            Assert.NotNull(addedDocument);

            AssertEx.Equal(expectedFolders, addedDocument.Folders);
            Assert.Equal(expectedDocumentName, addedDocument.Name);
            if (compareTokens)
            {
                TokenUtilities.AssertTokensEqual(
                    expected, (await addedDocument.GetTextAsync()).ToString(), GetLanguage());
            }
            else
            {
                Assert.Equal(expected, (await addedDocument.GetTextAsync()).ToString());
            }

            var editHandler = workspace.ExportProvider.GetExportedValue<ICodeActionEditHandlerService>();
            if (!hasProjectChange)
            {
                // If there is just one document change then we expect the preview to be a WpfTextView
                var content = (await editHandler.GetPreviews(workspace, operations, CancellationToken.None).GetPreviewsAsync())[0];
                using (var diffView = content as DifferenceViewerPreview)
                {
                    Assert.NotNull(diffView.Viewer);
                }
            }
            else
            {
                // If there are more changes than just the document we need to browse all the changes and get the document change
                var contents = editHandler.GetPreviews(workspace, operations, CancellationToken.None);
                bool hasPreview = false;
                var previews = await contents.GetPreviewsAsync();
                if (previews != null)
                {
                    foreach (var preview in previews)
                    {
                        if (preview != null)
                        {
                            var diffView = preview as DifferenceViewerPreview;
                            if (diffView?.Viewer != null)
                            {
                                hasPreview = true;
                                diffView.Dispose();
                                break;
                            }
                        }
                    }
                }

                Assert.True(hasPreview);
            }

            return Tuple.Create(oldSolution, newSolution);
        }

        internal async Task TestAsync(
            string initialMarkup, string expectedMarkup,
            int index = 0, bool compareTokens = true,
            IDictionary<OptionKey, object> options = null,
            string fixAllActionEquivalenceKey = null,
            object fixProviderData = null,
            CodeActionPriority? priority = null)
        {
            await TestAsync(initialMarkup, expectedMarkup, null, index, compareTokens, options, fixAllActionEquivalenceKey, fixProviderData, priority: priority);
            await TestAsync(initialMarkup, expectedMarkup, GetScriptOptions(), index, compareTokens, options, fixAllActionEquivalenceKey, fixProviderData, priority: priority);
        }

        internal async Task TestAsync(
            string initialMarkup, string expectedMarkup,
            ParseOptions parseOptions,
            int index = 0, bool compareTokens = true,
            IDictionary<OptionKey, object> options = null,
            string fixAllActionEquivalenceKey = null,
            object fixProviderData = null,
            bool withScriptOption = false,
            CodeActionPriority? priority = null)
        {
            await TestAsync(initialMarkup, expectedMarkup, parseOptions, null, index, compareTokens, options, fixAllActionEquivalenceKey, fixProviderData, priority);

            if (withScriptOption)
            {
                await TestAsync(initialMarkup, expectedMarkup, parseOptions.WithKind(SourceCodeKind.Script), null, index, compareTokens, options, fixAllActionEquivalenceKey, fixProviderData, priority);
            }
        }

        internal async Task TestAsync(
            string initialMarkup, string expectedMarkup,
            ParseOptions parseOptions, CompilationOptions compilationOptions,
            int index = 0, bool compareTokens = true,
            IDictionary<OptionKey, object> options = null,
            string fixAllActionEquivalenceKey = null,
            object fixProviderData = null,
            CodeActionPriority? priority = null)
        {
            string expected;
            IDictionary<string, IList<TextSpan>> spanMap;
            MarkupTestFile.GetSpans(expectedMarkup.NormalizeLineEndings(), out expected, out spanMap);

            var conflictSpans = spanMap.GetOrAdd("Conflict", _ => new List<TextSpan>());
            var renameSpans = spanMap.GetOrAdd("Rename", _ => new List<TextSpan>());
            var warningSpans = spanMap.GetOrAdd("Warning", _ => new List<TextSpan>());

            using (var workspace = IsWorkspaceElement(initialMarkup)
                ? await TestWorkspace.CreateAsync(initialMarkup)
                : await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions, compilationOptions))
            {
                workspace.ApplyOptions(options);

                var actions = await GetCodeActionsAsync(workspace, fixAllActionEquivalenceKey, fixProviderData);
                await TestActionsAsync(
                    workspace, expected, index,
                    actions,
                    conflictSpans, renameSpans, warningSpans,
                    compareTokens: compareTokens,
                    parseOptions: parseOptions,
                    priority: priority);
            }
        }

        internal async Task<Tuple<Solution, Solution>> TestActionsAsync(
            TestWorkspace workspace, string expected,
            int index, IList<CodeAction> actions,
            IList<TextSpan> conflictSpans, IList<TextSpan> renameSpans, IList<TextSpan> warningSpans,
            bool compareTokens,
            ParseOptions parseOptions = null,
            CodeActionPriority? priority = null)
        {
            var operations = await VerifyInputsAndGetOperationsAsync(index, actions, priority);
            return await TestOperationsAsync(workspace, expected, operations.ToList(), conflictSpans, renameSpans, warningSpans, compareTokens, expectedChangedDocumentId: null, parseOptions: parseOptions);
        }

        private static bool IsWorkspaceElement(string text)
        {
            return text.TrimStart('\r', '\n', ' ').StartsWith("<Workspace>", StringComparison.Ordinal);
        }

        protected async Task<Tuple<Solution, Solution>> TestOperationsAsync(
            TestWorkspace workspace,
            string expectedText,
            IList<CodeActionOperation> operations,
            IList<TextSpan> conflictSpans,
            IList<TextSpan> renameSpans,
            IList<TextSpan> warningSpans,
            bool compareTokens,
            DocumentId expectedChangedDocumentId,
            ParseOptions parseOptions = null)
        {
            var appliedChanges = ApplyOperationsAndGetSolution(workspace, operations);
            var oldSolution = appliedChanges.Item1;
            var newSolution = appliedChanges.Item2;

            if (IsWorkspaceElement(expectedText))
            {
                await VerifyAgainstWorkspaceDefinitionAsync(expectedText, newSolution);
                return Tuple.Create(oldSolution, newSolution);
            }

            var document = GetDocumentToVerify(expectedChangedDocumentId, oldSolution, newSolution);

            var fixedRoot = await document.GetSyntaxRootAsync();
            var actualText = compareTokens ? fixedRoot.ToString() : fixedRoot.ToFullString();

            if (compareTokens)
            {
                TokenUtilities.AssertTokensEqual(expectedText, actualText, GetLanguage());
            }
            else
            {
                Assert.Equal(expectedText, actualText);
            }

            TestAnnotations(expectedText, conflictSpans, fixedRoot, ConflictAnnotation.Kind, compareTokens, parseOptions);
            TestAnnotations(expectedText, renameSpans, fixedRoot, RenameAnnotation.Kind, compareTokens, parseOptions);
            TestAnnotations(expectedText, warningSpans, fixedRoot, WarningAnnotation.Kind, compareTokens, parseOptions);

            return Tuple.Create(oldSolution, newSolution);
        }

        private static Document GetDocumentToVerify(DocumentId expectedChangedDocumentId, Solution oldSolution, Solution newSolution)
        {
            Document document;
            // If the expectedChangedDocumentId is not mentioned then we expect only single document to be changed
            if (expectedChangedDocumentId == null)
            {
                var projectDifferences = SolutionUtilities.GetSingleChangedProjectChanges(oldSolution, newSolution);
                var documentId = projectDifferences.GetChangedDocuments().FirstOrDefault() ?? projectDifferences.GetAddedDocuments().FirstOrDefault();
                Assert.NotNull(documentId);
                document = newSolution.GetDocument(documentId);
            }
            else
            {
                // This method obtains only the document changed and does not check the project state.
                document = newSolution.GetDocument(expectedChangedDocumentId);
            }

            return document;
        }

        private static async Task VerifyAgainstWorkspaceDefinitionAsync(string expectedText, Solution newSolution)
        {
            using (var expectedWorkspace = await TestWorkspace.CreateAsync(expectedText))
            {
                var expectedSolution = expectedWorkspace.CurrentSolution;
                Assert.Equal(expectedSolution.Projects.Count(), newSolution.Projects.Count());
                foreach (var project in newSolution.Projects)
                {
                    var expectedProject = expectedSolution.GetProjectsByName(project.Name).Single();
                    Assert.Equal(expectedProject.Documents.Count(), project.Documents.Count());

                    foreach (var doc in project.Documents)
                    {
                        var root = await doc.GetSyntaxRootAsync();
                        var expectedDocument = expectedProject.Documents.Single(d => d.Name == doc.Name);
                        var expectedRoot = await expectedDocument.GetSyntaxRootAsync();
                        Assert.Equal(expectedRoot.ToFullString(), root.ToFullString());
                    }
                }
            }
        }

        internal static async Task<IEnumerable<CodeActionOperation>> VerifyInputsAndGetOperationsAsync(
            int index, IList<CodeAction> actions, CodeActionPriority? priority = null)
        {
            Assert.NotNull(actions);
            if (actions.Count == 1)
            {
                var suppressionAction = actions.Single() as SuppressionCodeAction;
                if (suppressionAction != null)
                {
                    actions = suppressionAction.GetCodeActions().ToList();
                }
            }

            Assert.InRange(index, 0, actions.Count - 1);

            var action = actions[index];
            if (priority != null)
            {
                Assert.Equal(priority.Value, action.Priority);
            }
            return await action.GetOperationsAsync(CancellationToken.None);
        }

        protected Tuple<Solution, Solution> ApplyOperationsAndGetSolution(
            TestWorkspace workspace,
            IEnumerable<CodeActionOperation> operations)
        {
            Tuple<Solution, Solution> result = null;
            foreach (var operation in operations)
            {
                if (operation is ApplyChangesOperation && result == null)
                {
                    var oldSolution = workspace.CurrentSolution;
                    var newSolution = ((ApplyChangesOperation)operation).ChangedSolution;
                    result = Tuple.Create(oldSolution, newSolution);
                }
                else if (operation.ApplyDuringTests)
                {
                    operation.Apply(workspace, new ProgressTracker(), CancellationToken.None);
                }
            }

            if (result == null)
            {
                throw new InvalidOperationException("No ApplyChangesOperation found");
            }

            return result;
        }

        protected virtual IList<CodeAction> MassageActions(IList<CodeAction> actions)
        {
            return actions;
        }

        protected static IList<CodeAction> FlattenActions(IEnumerable<CodeAction> codeActions)
        {
            return codeActions?.SelectMany(a => a.HasCodeActions ? a.GetCodeActions().ToArray() : new[] { a }).ToList();
        }

        protected IDictionary<OptionKey, object> Option(PerLanguageOption<CodeStyle.CodeStyleOption<bool>> option, bool value, CodeStyle.NotificationOption notification)
        {
            return OptionsSet(Tuple.Create(option, value, notification));
        }

        protected IDictionary<OptionKey, object> OptionsSet(params Tuple<PerLanguageOption<CodeStyle.CodeStyleOption<bool>>, bool, CodeStyle.NotificationOption>[] optionsToSet)
        {
            var options = new Dictionary<OptionKey, object>();
            foreach (var triple in optionsToSet)
            {
                var option = triple.Item1;
                var value = triple.Item2;
                var notification = triple.Item3;
                options.Add(new OptionKey(option, GetLanguage()), new CodeStyle.CodeStyleOption<bool>(value, notification));
            }

            return options;
        }
    }
}
