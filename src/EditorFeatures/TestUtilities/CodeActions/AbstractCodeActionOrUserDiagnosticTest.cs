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
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    [UseExportProvider]
    public abstract class AbstractCodeActionOrUserDiagnosticTest
    {
        public struct TestParameters
        {
            internal readonly IDictionary<OptionKey, object> options;
            internal readonly object fixProviderData;
            internal readonly ParseOptions parseOptions;
            internal readonly CompilationOptions compilationOptions;
            internal readonly int index;
            internal readonly CodeActionPriority? priority;
            internal readonly bool retainNonFixableDiagnostics;
            internal readonly string title;

            internal TestParameters(
                ParseOptions parseOptions = null,
                CompilationOptions compilationOptions = null,
                IDictionary<OptionKey, object> options = null,
                object fixProviderData = null,
                int index = 0,
                CodeActionPriority? priority = null,
                bool retainNonFixableDiagnostics = false,
                string title = null)
            {
                this.parseOptions = parseOptions;
                this.compilationOptions = compilationOptions;
                this.options = options;
                this.fixProviderData = fixProviderData;
                this.index = index;
                this.priority = priority;
                this.retainNonFixableDiagnostics = retainNonFixableDiagnostics;
                this.title = title;
            }

            public TestParameters WithParseOptions(ParseOptions parseOptions)
                => new TestParameters(parseOptions, compilationOptions, options, fixProviderData, index, priority, title: title);

            public TestParameters WithOptions(IDictionary<OptionKey, object> options)
                => new TestParameters(parseOptions, compilationOptions, options, fixProviderData, index, priority, title: title);

            public TestParameters WithFixProviderData(object fixProviderData)
                => new TestParameters(parseOptions, compilationOptions, options, fixProviderData, index, priority, title: title);

            public TestParameters WithIndex(int index)
                => new TestParameters(parseOptions, compilationOptions, options, fixProviderData, index, priority, title: title);

            public TestParameters WithRetainNonFixableDiagnostics(bool retainNonFixableDiagnostics)
                => new TestParameters(parseOptions, compilationOptions, options, fixProviderData, index, priority, title: title, retainNonFixableDiagnostics: retainNonFixableDiagnostics);
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

        private TestParameters WithRegularOptions(TestParameters parameters)
            => parameters.WithParseOptions(parameters.parseOptions?.WithKind(SourceCodeKind.Regular));

        private TestParameters WithScriptOptions(TestParameters parameters)
            => parameters.WithParseOptions(parameters.parseOptions?.WithKind(SourceCodeKind.Script) ?? GetScriptOptions());

        protected async Task TestMissingInRegularAndScriptAsync(
            string initialMarkup,
            TestParameters parameters = default)
        {
            await TestMissingAsync(initialMarkup, WithRegularOptions(parameters));
            await TestMissingAsync(initialMarkup, WithScriptOptions(parameters));
        }

        protected async Task TestMissingAsync(
            string initialMarkup,
            TestParameters parameters = default)
        {
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var (actions, _) = await GetCodeActionsAsync(workspace, parameters);
                Assert.True(actions.Length == 0, "An action was offered when none was expected");
            }
        }

        protected async Task TestDiagnosticMissingAsync(
            string initialMarkup, TestParameters parameters = default)
        {
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var diagnostics = await GetDiagnosticsWorkerAsync(workspace, parameters);
                Assert.True(0 == diagnostics.Length, $"Expected no diagnostics, but got {diagnostics.Length}");
            }
        }

        protected abstract Task<(ImmutableArray<CodeAction>, CodeAction actionToInvoke)> GetCodeActionsAsync(
            TestWorkspace workspace, TestParameters parameters);

        protected abstract Task<ImmutableArray<Diagnostic>> GetDiagnosticsWorkerAsync(
            TestWorkspace workspace, TestParameters parameters);

        protected Task TestSmartTagTextAsync(string initialMarkup, string displayText, int index)
            => TestSmartTagTextAsync(initialMarkup, displayText, new TestParameters(index: index));

        protected Task TestSmartTagGlyphTagsAsync(string initialMarkup, ImmutableArray<string> glyphTags, int index)
            => TestSmartTagGlyphTagsAsync(initialMarkup, glyphTags, new TestParameters(index: index));

        protected async Task TestSmartTagTextAsync(
            string initialMarkup,
            string displayText,
            TestParameters parameters = default)
        {
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var (_, action) = await GetCodeActionsAsync(workspace, parameters);
                Assert.Equal(displayText, action.Title);
            }
        }

        protected async Task TestSmartTagGlyphTagsAsync(
            string initialMarkup,
            ImmutableArray<string> glyph,
            TestParameters parameters = default)
        {
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var (_, action) = await GetCodeActionsAsync(workspace, parameters);
                Assert.Equal(glyph, action.Tags);
            }
        }

        protected async Task TestExactActionSetOfferedAsync(
            string initialMarkup,
            IEnumerable<string> expectedActionSet,
            TestParameters parameters = default)
        {
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var (actions, _) = await GetCodeActionsAsync(workspace, parameters);

                var actualActionSet = actions.Select(a => a.Title);
                Assert.True(actualActionSet.SequenceEqual(expectedActionSet),
                    "Expected: " + string.Join(", ", expectedActionSet) +
                    "\nActual: " + string.Join(", ", actualActionSet));
            }
        }

        protected async Task TestActionCountAsync(
            string initialMarkup,
            int count,
            TestParameters parameters = default)
        {
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var (actions, _) = await GetCodeActionsAsync(workspace, parameters);

                Assert.Equal(count, actions.Length);
            }
        }

        protected async Task TestAddDocumentInRegularAndScriptAsync(
            string initialMarkup, string expectedMarkup,
            ImmutableArray<string> expectedContainers,
            string expectedDocumentName,
            TestParameters parameters = default)
        {
            await TestAddDocument(
                initialMarkup, expectedMarkup,
                expectedContainers, expectedDocumentName,
                WithRegularOptions(parameters));
            await TestAddDocument(
                initialMarkup, expectedMarkup,
                expectedContainers, expectedDocumentName,
                WithScriptOptions(parameters));
        }

        protected async Task<Tuple<Solution, Solution>> TestAddDocumentAsync(
            TestParameters parameters,
            TestWorkspace workspace,
            string expectedMarkup,
            string expectedDocumentName,
            ImmutableArray<string> expectedContainers)
        {
            var (_, action) = await GetCodeActionsAsync(workspace, parameters);
            return await TestAddDocument(
                workspace, expectedMarkup, expectedContainers,
                expectedDocumentName, action);
        }

        protected async Task TestAddDocument(
            string initialMarkup,
            string expectedMarkup,
            ImmutableArray<string> expectedContainers,
            string expectedDocumentName,
            TestParameters parameters = default)
        {
            using (var workspace = CreateWorkspaceFromOptions(initialMarkup, parameters))
            {
                var (_, action) = await GetCodeActionsAsync(workspace, parameters);
                await TestAddDocument(
                    workspace, expectedMarkup, expectedContainers,
                    expectedDocumentName, action);
            }
        }

        private async Task<Tuple<Solution, Solution>> TestAddDocument(
            TestWorkspace workspace,
            string expectedMarkup,
            ImmutableArray<string> expectedFolders,
            string expectedDocumentName,
            CodeAction action)
        {
            var operations = await VerifyActionAndGetOperationsAsync(workspace, action, default);
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
            object fixProviderData = null,
            ParseOptions parseOptions = null,
            string title = null)
        {
            return TestInRegularAndScript1Async(
                initialMarkup, expectedMarkup, index, priority,
                new TestParameters(parseOptions, compilationOptions, options, fixProviderData, index, title: title));
        }

        internal async Task TestInRegularAndScript1Async(
            string initialMarkup,
            string expectedMarkup,
            int index = 0,
            CodeActionPriority? priority = null,
            TestParameters parameters = default)
        {
            parameters = parameters.WithIndex(index);
            await TestAsync(initialMarkup, expectedMarkup, priority, WithRegularOptions(parameters));
            await TestAsync(initialMarkup, expectedMarkup, priority, WithScriptOptions(parameters));
        }

        internal Task TestAsync(
            string initialMarkup, string expectedMarkup,
            ParseOptions parseOptions,
            CompilationOptions compilationOptions = null,
            int index = 0, IDictionary<OptionKey, object> options = null,
            object fixProviderData = null,
            CodeActionPriority? priority = null)
        {
            return TestAsync(
                initialMarkup,
                expectedMarkup, priority,
                new TestParameters(
                    parseOptions, compilationOptions, options, fixProviderData, index));
        }

        private async Task TestAsync(
            string initialMarkup,
            string expectedMarkup,
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
                var (_, action) = await GetCodeActionsAsync(workspace, parameters);
                await TestActionAsync(
                    workspace, expected, action,
                    conflictSpans, renameSpans, warningSpans, navigationSpans,
                    parameters);
            }
        }

        internal async Task<Tuple<Solution, Solution>> TestActionAsync(
            TestWorkspace workspace, string expected,
            CodeAction action,
            ImmutableArray<TextSpan> conflictSpans,
            ImmutableArray<TextSpan> renameSpans,
            ImmutableArray<TextSpan> warningSpans,
            ImmutableArray<TextSpan> navigationSpans,
            TestParameters parameters)
        {
            var operations = await VerifyActionAndGetOperationsAsync(workspace, action, parameters);
            return await TestOperationsAsync(
                workspace, expected, operations, conflictSpans, renameSpans,
                warningSpans, navigationSpans, expectedChangedDocumentId: null, parseOptions: parameters.parseOptions);
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

            // To help when a user just writes a test (and supplied no 'expectedText') just print
            // out the entire 'actualText' (without any trimming).  in the case that we have both,
            // call the normal Assert helper which will print out a good trimmed diff.
            if (expectedText == "")
            {
                Assert.Equal((object)expectedText, actualText);
            }
            else
            {
                Assert.Equal(expectedText, actualText);
            }

            TestAnnotations(conflictSpans, ConflictAnnotation.Kind);
            TestAnnotations(renameSpans, RenameAnnotation.Kind);
            TestAnnotations(warningSpans, WarningAnnotation.Kind);
            TestAnnotations(navigationSpans, NavigationAnnotation.Kind);

            return Tuple.Create(oldSolution, newSolution);

            void TestAnnotations(ImmutableArray<TextSpan> expectedSpans, string annotationKind)
            {
                var annotatedItems = fixedRoot.GetAnnotatedNodesAndTokens(annotationKind).OrderBy(s => s.SpanStart).ToList();

                Assert.True(expectedSpans.Length == annotatedItems.Count,
                    $"Annotations of kind '{annotationKind}' didn't match. Expected: {expectedSpans.Length}. Actual: {annotatedItems.Count}.");

                for (var i = 0; i < Math.Min(expectedSpans.Length, annotatedItems.Count); i++)
                {
                    var actual = annotatedItems[i].Span;
                    var expected = expectedSpans[i];
                    Assert.Equal(expected, actual);
                }
            }
        }

        protected static Document GetDocumentToVerify(DocumentId expectedChangedDocumentId, Solution oldSolution, Solution newSolution)
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
                        VerifyExpectedDocumentText(expectedRoot.ToFullString(), root.ToFullString());
                    }

                    foreach (var additionalDoc in project.AdditionalDocuments)
                    {
                        var root = await additionalDoc.GetTextAsync();
                        var expectedDocument = expectedProject.AdditionalDocuments.Single(d => d.Name == additionalDoc.Name);
                        var expectedRoot = await expectedDocument.GetTextAsync();
                        VerifyExpectedDocumentText(expectedRoot.ToString(), root.ToString());
                    }

                    foreach (var analyzerConfigDoc in project.AnalyzerConfigDocuments)
                    {
                        var root = await analyzerConfigDoc.GetTextAsync();
                        var expectedDocument = expectedProject.AnalyzerConfigDocuments.Single(d => d.FilePath == analyzerConfigDoc.FilePath);
                        var expectedRoot = await expectedDocument.GetTextAsync();
                        VerifyExpectedDocumentText(expectedRoot.ToString(), root.ToString());
                    }
                }
            }

            return;

            // Local functions.
            static void VerifyExpectedDocumentText(string expected, string actual)
            {
                if (expected == "")
                {
                    Assert.Equal((object)expected, actual);
                }
                else
                {
                    Assert.Equal(expected, actual);
                }
            }
        }

        internal async Task<ImmutableArray<CodeActionOperation>> VerifyActionAndGetOperationsAsync(
            TestWorkspace workspace, CodeAction action, TestParameters parameters)
        {
            if (action is null)
            {
                var diagnostics = await GetDiagnosticsWorkerAsync(workspace, parameters.WithRetainNonFixableDiagnostics(true));

                throw new Exception("No action was offered when one was expected. Diagnostics from the compilation: " + string.Join("", diagnostics.Select(d => Environment.NewLine + d.ToString())));
            }

            if (parameters.priority != null)
            {
                Assert.Equal(parameters.priority.Value, action.Priority);
            }

            if (parameters.title != null)
            {
                Assert.Equal(parameters.title, action.Title);
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

        protected static ImmutableArray<CodeAction> GetNestedActions(ImmutableArray<CodeAction> codeActions)
            => codeActions.SelectMany(a => a.NestedCodeActions).ToImmutableArray();

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

        /// <summary>
        /// Tests all the code actions for the given <paramref name="input"/> string.  Each code
        /// action must produce the corresponding output in the <paramref name="outputs"/> array.
        /// 
        /// Will throw if there are more outputs than code actions or more code actions than outputs.
        /// </summary>
        protected Task TestAllInRegularAndScriptAsync(
            string input,
            params string[] outputs)
        {
            return TestAllInRegularAndScriptAsync(input, parameters: default, outputs);
        }

        protected async Task TestAllInRegularAndScriptAsync(
            string input,
            TestParameters parameters,
            params string[] outputs)
        {
            for (var index = 0; index < outputs.Length; index++)
            {
                var output = outputs[index];
                await TestInRegularAndScript1Async(input, output, index, parameters: parameters);
            }

            await TestActionCountAsync(input, outputs.Length, parameters);
        }
    }
}
