// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.Implementation.Preview;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
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
        public struct TestParameters
        {
            internal readonly IDictionary<OptionKey, object> options;
            internal readonly string fixAllActionEquivalenceKey;
            internal readonly object fixProviderData;
            internal readonly ParseOptions parseOptions;
            internal readonly CompilationOptions compilationOptions;

            public TestParameters(
                ParseOptions parseOptions = null,
                CompilationOptions compilationOptions = null,
                IDictionary<OptionKey, object> options = null,
                string fixAllActionEquivalenceKey = null,
                object fixProviderData = null)
            {
                this.parseOptions = parseOptions;
                this.compilationOptions = compilationOptions;
                this.options = options;
                this.fixAllActionEquivalenceKey = fixAllActionEquivalenceKey;
                this.fixProviderData = fixProviderData;
            }

            public TestParameters WithParseOptions(ParseOptions parseOptions)
                => new TestParameters(parseOptions, compilationOptions, options, fixAllActionEquivalenceKey, fixProviderData);

            public TestParameters WithFixProviderData(object fixProviderData)
                => new TestParameters(parseOptions, compilationOptions, options, fixAllActionEquivalenceKey, fixProviderData);
        }

        protected abstract string GetLanguage();
        protected abstract ParseOptions GetScriptOptions();

        protected TestWorkspace CreateWorkspaceFromOptions(
            string initialMarkup, TestParameters parameters)
        {
            var workspace = TestWorkspace.IsWorkspaceElement(initialMarkup)
                 ? TestWorkspace.Create(initialMarkup, openDocuments: false)
                 : CreateWorkspaceFromFile(initialMarkup, parameters);

            workspace.ApplyOptions(parameters.options);

            return workspace;
        }

        protected abstract TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters);

        protected async Task TestMissingInRegularAndScriptAsync(
            string initialMarkup,
            TestParameters parameters = default(TestParameters))
        {
            await TestMissingAsync(initialMarkup, parameters.WithParseOptions(null));
            await TestMissingAsync(initialMarkup, parameters.WithParseOptions(GetScriptOptions()));
        }

        protected async Task TestMissingAsync(
            string initialMarkup,
            TestParameters parameters = default(TestParameters))
        {
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var actions = await GetCodeActionsAsync(workspace, parameters);
                Assert.True(actions.Length == 0, "An action was offered when none was expected");
            }
        }

        protected async Task TestDiagnosticMissingAsync(
            string initialMarkup,
            TestParameters parameters = default(TestParameters))
        {
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var diagnostics = await GetDiagnosticsWorkerAsync(workspace, parameters);
                Assert.Equal(0, diagnostics.Length);
            }
        }

        protected async Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(
            TestWorkspace workspace, TestParameters parameters)
        {
            return MassageActions(await GetCodeActionsWorkerAsync(workspace, parameters));
        }

        protected abstract Task<ImmutableArray<CodeAction>> GetCodeActionsWorkerAsync(
            TestWorkspace workspace, TestParameters parameters);


        protected abstract Task<ImmutableArray<Diagnostic>> GetDiagnosticsWorkerAsync(
            TestWorkspace workspace, TestParameters parameters);

        protected async Task TestSmartTagTextAsync(
            string initialMarkup,
            string displayText,
            int index = 0,
            TestParameters parameters = default(TestParameters))
        {
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var actions = await GetCodeActionsAsync(workspace, parameters);
                Assert.Equal(displayText, actions.ElementAt(index).Title);
            }
        }

        protected async Task TestExactActionSetOfferedAsync(
            string initialMarkup,
            IEnumerable<string> expectedActionSet,
            TestParameters parameters = default(TestParameters))
        {
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var actions = await GetCodeActionsAsync(workspace, parameters);

                var actualActionSet = actions.Select(a => a.Title);
                Assert.True(actualActionSet.SequenceEqual(expectedActionSet),
                    "Expected: " + string.Join(", ", expectedActionSet) +
                    "\nActual: " + string.Join(", ", actualActionSet));
            }
        }

        protected async Task TestActionCountAsync(
            string initialMarkup,
            int count,
            TestParameters parameters = default(TestParameters))
        {
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var actions = await GetCodeActionsAsync(workspace, parameters);

                Assert.Equal(count, actions.Count());
            }
        }

        protected async Task TestAddDocumentInRegularAndScriptAsync(
            string initialMarkup, string expectedMarkup,
            ImmutableArray<string> expectedContainers,
            string expectedDocumentName,
            int index = 0,
            TestParameters parameters = default(TestParameters))
        {
            await TestAddDocument(
                initialMarkup, expectedMarkup,
                expectedContainers, expectedDocumentName,
                index, parameters.WithParseOptions(null));
            await TestAddDocument(
                initialMarkup, expectedMarkup,
                expectedContainers, expectedDocumentName,
                index, parameters.WithParseOptions(GetScriptOptions()));
        }

        protected async Task<Tuple<Solution, Solution>> TestAddDocumentAsync(
            TestParameters parameters,
            TestWorkspace workspace,
            string expectedMarkup,
            int index,
            string expectedDocumentName,
            ImmutableArray<string> expectedContainers)
        {
            var codeActions = await GetCodeActionsAsync(workspace, parameters);
            return await TestAddDocument(
                workspace, expectedMarkup, index, expectedContainers,
                expectedDocumentName, codeActions);
        }

        protected async Task TestAddDocument(
            string initialMarkup,
            string expectedMarkup,
            ImmutableArray<string> expectedContainers,
            string expectedDocumentName,
            int index = 0,
            TestParameters parameters = default)
        {
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var codeActions = await GetCodeActionsAsync(workspace, parameters);
                await TestAddDocument(
                    workspace, expectedMarkup, index, expectedContainers,
                    expectedDocumentName, codeActions);
            }
        }

        private async Task<Tuple<Solution, Solution>> TestAddDocument(
            TestWorkspace workspace,
            string expectedMarkup,
            int index,
            ImmutableArray<string> expectedFolders,
            string expectedDocumentName,
            ImmutableArray<CodeAction> actions)
        {
            var operations = await VerifyInputsAndGetOperationsAsync(index, actions);
            return await TestAddDocument(
                workspace,
                expectedMarkup,
                operations,
                hasProjectChange: false,
                modifiedProjectId: null,
                expectedFolders: expectedFolders,
                expectedDocumentName: expectedDocumentName);
        }

        protected async Task<Tuple<Solution, Solution>> TestAddDocument(
            TestWorkspace workspace,
            string expected,
            ImmutableArray<CodeActionOperation> operations,
            bool hasProjectChange,
            ProjectId modifiedProjectId,
            ImmutableArray<string> expectedFolders,
            string expectedDocumentName)
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
            Assert.Equal(expected, (await addedDocument.GetTextAsync()).ToString());

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
                var hasPreview = false;
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

        internal Task TestInRegularAndScriptAsync(
            string initialMarkup,
            string expectedMarkup,
            int index = 0,
            CodeActionPriority? priority = null,
            CompilationOptions compilationOptions = null,
            IDictionary<OptionKey, object> options = null,
            string fixAllActionEquivalenceKey = null,
            object fixProviderData = null)
        {
            return TestInRegularAndScript1Async(
                initialMarkup, expectedMarkup, index, priority,
                new TestParameters(null, compilationOptions, options, fixAllActionEquivalenceKey, fixProviderData));
        }

        internal async Task TestInRegularAndScript1Async(
            string initialMarkup,
            string expectedMarkup,
            int index = 0,
            CodeActionPriority? priority = null,
            TestParameters parameters = default(TestParameters))
        {
            await TestAsync(initialMarkup, expectedMarkup, index, priority, parameters.WithParseOptions(null));
            await TestAsync(initialMarkup, expectedMarkup, index, priority, parameters.WithParseOptions(GetScriptOptions()));
        }

        internal Task TestAsync(
            string initialMarkup, string expectedMarkup,
            ParseOptions parseOptions,
            CompilationOptions compilationOptions = null,
            int index = 0, IDictionary<OptionKey, object> options = null,
            string fixAllActionEquivalenceKey = null,
            object fixProviderData = null,
            CodeActionPriority? priority = null)
        {
            return TestAsync(
                initialMarkup,
                expectedMarkup, index, priority,
                new TestParameters(
                    parseOptions, compilationOptions,
                    options, fixAllActionEquivalenceKey, fixProviderData));
        }

        private async Task TestAsync(
            string initialMarkup,
            string expectedMarkup,
            int index,
            CodeActionPriority? priority,
            TestParameters parameters)
        {
            MarkupTestFile.GetSpans(
                expectedMarkup.NormalizeLineEndings(),
                out var expected, out IDictionary<string, ImmutableArray<TextSpan>> spanMap);

            var conflictSpans = spanMap.GetOrAdd("Conflict", _ => ImmutableArray<TextSpan>.Empty);
            var renameSpans = spanMap.GetOrAdd("Rename", _ => ImmutableArray<TextSpan>.Empty);
            var warningSpans = spanMap.GetOrAdd("Warning", _ => ImmutableArray<TextSpan>.Empty);
            var navigationSpans = spanMap.GetOrAdd("Navigation", _ => ImmutableArray<TextSpan>.Empty);

            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                // Currently, OOP diagnostics don't work with code action tests.
                workspace.Options = workspace.Options.WithChangedOption(
                    RemoteFeatureOptions.DiagnosticsEnabled, false);

                var actions = await GetCodeActionsAsync(workspace, parameters);
                await TestActionsAsync(
                    workspace, expected, index,
                    actions,
                    conflictSpans, renameSpans, warningSpans, navigationSpans,
                    parseOptions: parameters.parseOptions,
                    priority: priority);
            }
        }

        internal async Task<Tuple<Solution, Solution>> TestActionsAsync(
            TestWorkspace workspace, string expected,
            int index, ImmutableArray<CodeAction> actions,
            ImmutableArray<TextSpan> conflictSpans,
            ImmutableArray<TextSpan> renameSpans,
            ImmutableArray<TextSpan> warningSpans,
            ImmutableArray<TextSpan> navigationSpans,
            ParseOptions parseOptions = null,
            CodeActionPriority? priority = null)
        {
            var operations = await VerifyInputsAndGetOperationsAsync(index, actions, priority);
            return await TestOperationsAsync(
                workspace, expected, operations, conflictSpans, renameSpans,
                warningSpans, navigationSpans, expectedChangedDocumentId: null, parseOptions: parseOptions);
        }

        protected async Task<Tuple<Solution, Solution>> TestOperationsAsync(
            TestWorkspace workspace,
            string expectedText,
            ImmutableArray<CodeActionOperation> operations,
            ImmutableArray<TextSpan> conflictSpans,
            ImmutableArray<TextSpan> renameSpans,
            ImmutableArray<TextSpan> warningSpans,
            ImmutableArray<TextSpan> navigationSpans,
            DocumentId expectedChangedDocumentId,
            ParseOptions parseOptions = null)
        {
            var appliedChanges = ApplyOperationsAndGetSolution(workspace, operations);
            var oldSolution = appliedChanges.Item1;
            var newSolution = appliedChanges.Item2;

            if (TestWorkspace.IsWorkspaceElement(expectedText))
            {
                await VerifyAgainstWorkspaceDefinitionAsync(expectedText, newSolution);
                return Tuple.Create(oldSolution, newSolution);
            }

            var document = GetDocumentToVerify(expectedChangedDocumentId, oldSolution, newSolution);

            var fixedRoot = await document.GetSyntaxRootAsync();
            var actualText = fixedRoot.ToFullString();

            Assert.Equal(expectedText, actualText);

            TestAnnotations(conflictSpans, ConflictAnnotation.Kind);
            TestAnnotations(renameSpans, RenameAnnotation.Kind);
            TestAnnotations(warningSpans, WarningAnnotation.Kind);
            TestAnnotations(navigationSpans, NavigationAnnotation.Kind);

            return Tuple.Create(oldSolution, newSolution);

            void TestAnnotations(ImmutableArray<TextSpan> expectedSpans, string annotationKind)
            {
                var annotatedTokens = fixedRoot.GetAnnotatedNodesAndTokens(annotationKind).Select(n => (SyntaxToken)n).ToList();

                Assert.Equal(expectedSpans.Length, annotatedTokens.Count);

                if (expectedSpans.Length > 0)
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
            using (var expectedWorkspace = TestWorkspace.Create(expectedText))
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

        internal static async Task<ImmutableArray<CodeActionOperation>> VerifyInputsAndGetOperationsAsync(
            int index, ImmutableArray<CodeAction> actions, CodeActionPriority? priority = null)
        {
            Assert.NotNull(actions);
            if (actions.Length == 1)
            {
                if (actions.Single() is TopLevelSuppressionCodeAction suppressionAction)
                {
                    actions = suppressionAction.NestedCodeActions;
                }
            }

            Assert.InRange(index, 0, actions.Length - 1);

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
                    var oldSolution = workspace.CurrentSolution;
                    operation.TryApply(workspace, new ProgressTracker(), CancellationToken.None);
                    var newSolution = workspace.CurrentSolution;
                    result = Tuple.Create(oldSolution, newSolution);
                }
            }

            if (result == null)
            {
                throw new InvalidOperationException("No ApplyChangesOperation found");
            }

            return result;
        }

        protected virtual ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => actions;

        protected static ImmutableArray<CodeAction> FlattenActions(ImmutableArray<CodeAction> codeActions)
        {
            return codeActions.SelectMany(a => a.NestedCodeActions.Length > 0
                ? a.NestedCodeActions
                : ImmutableArray.Create(a)).ToImmutableArray();
        }

        protected (OptionKey, object) SingleOption<T>(Option<T> option, T enabled)
            => (new OptionKey(option), enabled);

        protected (OptionKey, object) SingleOption<T>(PerLanguageOption<T> option, T value)
            => (new OptionKey(option, this.GetLanguage()), value);

        protected (OptionKey, object) SingleOption<T>(Option<CodeStyleOption<T>> option, T enabled, NotificationOption notification)
            => SingleOption(option, new CodeStyleOption<T>(enabled, notification));

        protected (OptionKey, object) SingleOption<T>(Option<CodeStyleOption<T>> option, CodeStyleOption<T> codeStyle)
            => (new OptionKey(option), codeStyle);

        protected (OptionKey, object) SingleOption<T>(PerLanguageOption<CodeStyleOption<T>> option, T enabled, NotificationOption notification)
            => SingleOption(option, new CodeStyleOption<T>(enabled, notification));

        protected (OptionKey, object) SingleOption<T>(PerLanguageOption<CodeStyleOption<T>> option, CodeStyleOption<T> codeStyle)
            => SingleOption(option, codeStyle, language: GetLanguage());

        protected static (OptionKey, object) SingleOption<T>(PerLanguageOption<CodeStyleOption<T>> option, CodeStyleOption<T> codeStyle, string language)
            => (new OptionKey(option, language), codeStyle);

        protected IDictionary<OptionKey, object> Option<T>(Option<CodeStyleOption<T>> option, T enabled, NotificationOption notification)
            => OptionsSet(SingleOption(option, enabled, notification));

        protected IDictionary<OptionKey, object> Option<T>(Option<CodeStyleOption<T>> option, CodeStyleOption<T> codeStyle)
            => OptionsSet(SingleOption(option, codeStyle));

        protected IDictionary<OptionKey, object> Option<T>(PerLanguageOption<CodeStyleOption<T>> option, T enabled, NotificationOption notification)
            => OptionsSet(SingleOption(option, enabled, notification));

        protected IDictionary<OptionKey, object> Option<T>(PerLanguageOption<T> option, T value)
            => OptionsSet(SingleOption(option, value));

        protected IDictionary<OptionKey, object> Option<T>(PerLanguageOption<CodeStyleOption<T>> option, CodeStyleOption<T> codeStyle)
            => OptionsSet(SingleOption(option, codeStyle));

        protected static IDictionary<OptionKey, object> OptionsSet(
            params (OptionKey key, object value)[] options)
        {
            var result = new Dictionary<OptionKey, object>();
            foreach (var option in options)
            {
                result.Add(option.key, option.value);
            }

            return result;
        }
    }
}
