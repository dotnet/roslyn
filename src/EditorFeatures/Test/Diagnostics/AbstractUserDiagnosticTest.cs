// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateType;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Text.Differencing;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public abstract class AbstractUserDiagnosticTest
    {
        protected abstract string GetLanguage();
        protected abstract ParseOptions GetScriptOptions();
        protected abstract TestWorkspace CreateWorkspaceFromFile(string definition, ParseOptions parseOptions, CompilationOptions compilationOptions);
        internal abstract IEnumerable<Tuple<Diagnostic, CodeFixCollection>> GetDiagnosticAndFixes(TestWorkspace workspace, string fixAllActionEquivalenceKey);
        internal abstract IEnumerable<Diagnostic> GetDiagnostics(TestWorkspace workspace);

        protected virtual void TestMissing(string initial, IDictionary<OptionKey, object> options = null, string fixAllActionEquivalenceKey = null)
        {
            TestMissing(initial, null, options, fixAllActionEquivalenceKey);
            TestMissing(initial, GetScriptOptions(), options, fixAllActionEquivalenceKey);
        }

        protected virtual void TestMissing(string initial, ParseOptions parseOptions, IDictionary<OptionKey, object> options = null, string fixAllActionEquivalenceKey = null)
        {
            TestMissing(initial, parseOptions, null, options, fixAllActionEquivalenceKey);
        }

        protected void TestMissing(string initialMarkup, ParseOptions parseOptions, CompilationOptions compilationOptions, IDictionary<OptionKey, object> options = null, string fixAllActionEquivalenceKey = null)
        {
            using (var workspace = CreateWorkspaceFromFile(initialMarkup, parseOptions, compilationOptions))
            {
                if (options != null)
                {
                    ApplyOptionsToWorkspace(options, workspace);
                }

                var diagnostics = GetDiagnosticAndFix(workspace, fixAllActionEquivalenceKey);
                Assert.False(diagnostics?.Item2?.Fixes.IsEmpty == true);
            }
        }

        protected void Test(
            string initial,
            string expected,
            int index = 0,
            bool compareTokens = true,
            bool isLine = true,
            IDictionary<OptionKey, object> options = null,
            bool isAddedDocument = false,
            string fixAllActionEquivalenceKey = null)
        {
            Test(initial, expected, null, index, compareTokens, isLine, options, isAddedDocument, fixAllActionEquivalenceKey);
            Test(initial, expected, GetScriptOptions(), index, compareTokens, isLine, options, isAddedDocument, fixAllActionEquivalenceKey);
        }

        protected void Test(
            string initial,
            string expected,
            ParseOptions parseOptions,
            int index = 0,
            bool compareTokens = true,
            bool isLine = true,
            IDictionary<OptionKey, object> options = null,
            bool isAddedDocument = false,
            string fixAllActionEquivalenceKey = null)
        {
            Test(initial, expected, parseOptions, null, index, compareTokens, isLine, options, isAddedDocument, fixAllActionEquivalenceKey);
        }

        protected void Test(
            string initialMarkup,
            string expectedMarkup,
            ParseOptions parseOptions,
            CompilationOptions compilationOptions,
            int index = 0,
            bool compareTokens = true,
            bool isLine = true,
            IDictionary<OptionKey, object> options = null,
            bool isAddedDocument = false,
            string fixAllActionEquivalenceKey = null)
        {
            string expected;
            IDictionary<string, IList<TextSpan>> spanMap;
            MarkupTestFile.GetSpans(expectedMarkup.NormalizeLineEndings(), out expected, out spanMap);

            var conflictSpans = spanMap.GetOrAdd("Conflict", _ => new List<TextSpan>());
            var renameSpans = spanMap.GetOrAdd("Rename", _ => new List<TextSpan>());
            var warningSpans = spanMap.GetOrAdd("Warning", _ => new List<TextSpan>());

            using (var workspace = isLine ? CreateWorkspaceFromFile(initialMarkup, parseOptions, compilationOptions) : TestWorkspaceFactory.CreateWorkspace(initialMarkup))
            {
                if (options != null)
                {
                    ApplyOptionsToWorkspace(options, workspace);
                }

                var diagnosticAndFixes = GetDiagnosticAndFix(workspace, fixAllActionEquivalenceKey);
                Assert.NotNull(diagnosticAndFixes);
                TestActions(
                    workspace, expected, index,
                    diagnosticAndFixes.Item2.Fixes.Select(f => f.Action).ToList(),
                    conflictSpans, renameSpans, warningSpans,
                    compareTokens: compareTokens,
                    isAddedDocument: isAddedDocument);
            }
        }

        private static void ApplyOptionsToWorkspace(IDictionary<OptionKey, object> options, TestWorkspace workspace)
        {
            var optionService = workspace.Services.GetService<IOptionService>();
            var optionSet = optionService.GetOptions();
            foreach (var option in options)
            {
                optionSet = optionSet.WithChangedOption(option.Key, option.Value);
            }

            optionService.SetOptions(optionSet);
        }

        internal Tuple<Diagnostic, CodeFixCollection> GetDiagnosticAndFix(TestWorkspace workspace, string fixAllActionEquivalenceKey = null)
        {
            return GetDiagnosticAndFixes(workspace, fixAllActionEquivalenceKey).FirstOrDefault();
        }

        protected Document GetDocumentAndSelectSpan(TestWorkspace workspace, out TextSpan span)
        {
            var hostDocument = workspace.Documents.Single(d => d.SelectedSpans.Any());
            span = hostDocument.SelectedSpans.Single();
            return workspace.CurrentSolution.GetDocument(hostDocument.Id);
        }

        protected bool TryGetDocumentAndSelectSpan(TestWorkspace workspace, out Document document, out TextSpan span)
        {
            var hostDocument = workspace.Documents.FirstOrDefault(d => d.SelectedSpans.Any());
            if (hostDocument == null)
            {
                document = null;
                span = default(TextSpan);
                return false;
            }

            span = hostDocument.SelectedSpans.Single();
            document = workspace.CurrentSolution.GetDocument(hostDocument.Id);
            return true;
        }

        protected Document GetDocumentAndAnnotatedSpan(TestWorkspace workspace, out string annotation, out TextSpan span)
        {
            var hostDocument = workspace.Documents.Single(d => d.AnnotatedSpans.Any());
            var annotatedSpan = hostDocument.AnnotatedSpans.Single();
            annotation = annotatedSpan.Key;
            span = annotatedSpan.Value.Single();
            return workspace.CurrentSolution.GetDocument(hostDocument.Id);
        }

        protected FixAllScope GetFixAllScope(string annotation)
        {
            switch (annotation)
            {
                case "FixAllInDocument":
                    return FixAllScope.Document;

                case "FixAllInProject":
                    return FixAllScope.Project;

                case "FixAllInSolution":
                    return FixAllScope.Solution;
            }

            throw new InvalidProgramException("Incorrect FixAll annotation in test");
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
                var diagnosticAndFix = GetDiagnosticAndFix(workspace);
                Assert.Equal(displayText, diagnosticAndFix.Item2.Fixes.ElementAt(index).Action.Title);
            }
        }

        protected void TestEquivalenceKey(
            string initialMarkup,
            string equivalenceKey,
            int index = 0,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null)
        {
            using (var workspace = CreateWorkspaceFromFile(initialMarkup, parseOptions, compilationOptions))
            {
                var diagnosticAndFix = GetDiagnosticAndFix(workspace);
                Assert.Equal(equivalenceKey, diagnosticAndFix.Item2.Fixes.ElementAt(index).Action.EquivalenceKey);
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
                var diagnosticAndFix = GetDiagnosticAndFix(workspace);

                var actualActionSet = diagnosticAndFix.Item2.Fixes.Select(f => f.Action.Title);
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
                var diagnosticAndFix = GetDiagnosticAndFix(workspace);

                Assert.Equal(count, diagnosticAndFix.Item2.Fixes.Count());
            }
        }

        protected void TestActionCountInAllFixes(
            string initialMarkup,
            int count,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null)
        {
            using (var workspace = CreateWorkspaceFromFile(initialMarkup, parseOptions, compilationOptions))
            {
                var diagnosticAndFix = GetDiagnosticAndFixes(workspace, null);
                var diagnosticCount = diagnosticAndFix.Select(x => x.Item2.Fixes.Count()).Sum();

                Assert.Equal(count, diagnosticCount);
            }
        }

        protected Tuple<Solution, Solution> ApplyOperationsAndGetSolution(
            TestWorkspace workspace,
            IEnumerable<CodeActionOperation> operations)
        {
            var applyChangesOperation = operations.OfType<ApplyChangesOperation>().First();
            var oldSolution = workspace.CurrentSolution;
            var newSolution = applyChangesOperation.ChangedSolution;

            return Tuple.Create(oldSolution, newSolution);
        }

        protected Tuple<Solution, Solution> TestActions(
            TestWorkspace workspace,
            string expectedText,
            IEnumerable<CodeActionOperation> operations,
            DocumentId expectedChangedDocumentId = null,
            IList<TextSpan> expectedConflictSpans = null,
            IList<TextSpan> expectedRenameSpans = null,
            IList<TextSpan> expectedWarningSpans = null,
            bool compareTokens = true,
            bool isAddedDocument = false)
        {
            var appliedChanges = ApplyOperationsAndGetSolution(workspace, operations);
            var oldSolution = appliedChanges.Item1;
            var newSolution = appliedChanges.Item2;

            Document document = null;

            if (expectedText.TrimStart('\r', '\n', ' ').StartsWith("<Workspace>", StringComparison.Ordinal))
            {
                using (var expectedWorkspace = TestWorkspaceFactory.CreateWorkspace(expectedText))
                {
                    var expectedSolution = expectedWorkspace.CurrentSolution;
                    Assert.Equal(expectedSolution.Projects.Count(), newSolution.Projects.Count());
                    foreach (var project in newSolution.Projects)
                    {
                        var expectedProject = expectedSolution.GetProjectsByName(project.Name).Single();
                        Assert.Equal(expectedProject.Documents.Count(), project.Documents.Count());

                        foreach (var doc in project.Documents)
                        {
                            var root = doc.GetSyntaxRootAsync().Result;
                            var expectedDocument = expectedProject.Documents.Single(d => d.Name == doc.Name);
                            var expectedRoot = expectedDocument.GetSyntaxRootAsync().Result;
                            Assert.Equal(expectedRoot.ToFullString(), root.ToFullString());
                        }
                    }
                }
            }
            else
            {
                // If the expectedChangedDocumentId is not mentioned then we expect only single document to be changed 
                if (expectedChangedDocumentId == null)
                {
                    if (!isAddedDocument)
                    {
                        // This method assumes that only one document changed and rest(Project state) remains unchanged
                        document = SolutionUtilities.GetSingleChangedDocument(oldSolution, newSolution);
                    }
                    else
                    {
                        // This method assumes that only one document added and rest(Project state) remains unchanged
                        document = SolutionUtilities.GetSingleAddedDocument(oldSolution, newSolution);
                        Assert.Empty(SolutionUtilities.GetChangedDocuments(oldSolution, newSolution));
                    }
                }
                else
                {
                    // This method obtains only the document changed and does not check the project state.
                    document = newSolution.GetDocument(expectedChangedDocumentId);
                }

                var fixedRoot = document.GetSyntaxRootAsync().Result;
                var actualText = compareTokens ? fixedRoot.ToString() : fixedRoot.ToFullString();

                if (compareTokens)
                {
                    TokenUtilities.AssertTokensEqual(expectedText, actualText, GetLanguage());
                }
                else
                {
                    Assert.Equal(expectedText, actualText);
                }

                TestAnnotations(expectedText, expectedConflictSpans, fixedRoot, ConflictAnnotation.Kind, compareTokens);
                TestAnnotations(expectedText, expectedRenameSpans, fixedRoot, RenameAnnotation.Kind, compareTokens);
                TestAnnotations(expectedText, expectedWarningSpans, fixedRoot, WarningAnnotation.Kind, compareTokens);
            }

            return Tuple.Create(oldSolution, newSolution);
        }

        protected void TestActions(
            TestWorkspace workspace,
            string expectedText,
            int index,
            IList<CodeAction> actions,
            IList<TextSpan> expectedConflictSpans = null,
            IList<TextSpan> expectedRenameSpans = null,
            IList<TextSpan> expectedWarningSpans = null,
            bool compareTokens = true,
            bool isAddedDocument = false)
        {
            Assert.NotNull(actions);
            if (actions.Count == 1)
            {
                var suppressionAction = actions.Single() as SuppressionCodeAction;
                if (suppressionAction != null)
                {
                    actions = suppressionAction.NestedActions.ToList();
                }
            }

            Assert.InRange(index, 0, actions.Count - 1);

            var operations = actions[index].GetOperationsAsync(CancellationToken.None).Result;
            TestActions(
                workspace,
                expectedText,
                operations,
                expectedConflictSpans: expectedConflictSpans,
                expectedRenameSpans: expectedRenameSpans,
                expectedWarningSpans: expectedWarningSpans,
                compareTokens: compareTokens,
                isAddedDocument: isAddedDocument);
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

        protected void TestSpans(
            string initialMarkup,
            string expectedMarkup,
            int index = 0,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            string diagnosticId = null,
            string fixAllActionEquivalenceId = null)
        {
            IList<TextSpan> spansList;
            string unused;
            MarkupTestFile.GetSpans(expectedMarkup, out unused, out spansList);

            var expectedTextSpans = spansList.ToSet();
            using (var workspace = CreateWorkspaceFromFile(initialMarkup, parseOptions, compilationOptions))
            {
                ISet<TextSpan> actualTextSpans;
                if (diagnosticId == null)
                {
                    var diagnosticsAndFixes = GetDiagnosticAndFixes(workspace, fixAllActionEquivalenceId);
                    var diagnostics = diagnosticsAndFixes.Select(t => t.Item1);
                    actualTextSpans = diagnostics.Select(d => d.Location.SourceSpan).ToSet();
                }
                else
                {
                    var diagnostics = GetDiagnostics(workspace);
                    actualTextSpans = diagnostics.Where(d => d.Id == diagnosticId).Select(d => d.Location.SourceSpan).ToSet();
                }

                Assert.True(expectedTextSpans.SetEquals(actualTextSpans));
            }
        }

        protected void TestAddDocument(
            string initial,
            string expected,
            IList<string> expectedContainers,
            string expectedDocumentName,
            int index = 0,
            bool compareTokens = true,
            bool isLine = true)
        {
            TestAddDocument(initial, expected, index, expectedContainers, expectedDocumentName, null, null, compareTokens, isLine);
            TestAddDocument(initial, expected, index, expectedContainers, expectedDocumentName, GetScriptOptions(), null, compareTokens, isLine);
        }

        private void TestAddDocument(
            string initialMarkup,
            string expected,
            int index,
            IList<string> expectedContainers,
            string expectedDocumentName,
            ParseOptions parseOptions,
            CompilationOptions compilationOptions,
            bool compareTokens,
            bool isLine)
        {
            using (var workspace = isLine ? CreateWorkspaceFromFile(initialMarkup, parseOptions, compilationOptions) : TestWorkspaceFactory.CreateWorkspace(initialMarkup))
            {
                var diagnosticAndFix = GetDiagnosticAndFix(workspace);
                TestAddDocument(workspace, expected, index, expectedContainers, expectedDocumentName,
                    diagnosticAndFix.Item2.Fixes.Select(f => f.Action).ToList(), compareTokens);
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
            TestAddDocument(
                workspace,
                expected,
                operations,
                hasProjectChange: false,
                modifiedProjectId: null,
                expectedFolders: expectedFolders,
                expectedDocumentName: expectedDocumentName,
                compareTokens: compareTokens);
        }

        private Tuple<Solution, Solution> TestAddDocument(
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
                    expected, addedDocument.GetTextAsync().Result.ToString(), GetLanguage());
            }
            else
            {
                Assert.Equal(expected, addedDocument.GetTextAsync().Result.ToString());
            }

            var editHandler = workspace.ExportProvider.GetExportedValue<ICodeActionEditHandlerService>();
            if (!hasProjectChange)
            {
                // If there is just one document change then we expect the preview to be a WpfTextView
                var content = editHandler.GetPreviews(workspace, operations, CancellationToken.None).TakeNextPreviewAsync().PumpingWaitResult();
                var diffView = content as IWpfDifferenceViewer;
                Assert.NotNull(diffView);
                diffView.Close();
            }
            else
            {
                // If there are more changes than just the document we need to browse all the changes and get the document change
                var contents = editHandler.GetPreviews(workspace, operations, CancellationToken.None);
                bool hasPreview = false;
                object preview;
                while ((preview = contents.TakeNextPreviewAsync().PumpingWaitResult()) != null)
                {
                    var diffView = preview as IWpfDifferenceViewer;
                    if (diffView != null)
                    {
                        hasPreview = true;
                        diffView.Close();
                        break;
                    }
                }

                Assert.True(hasPreview);
            }

            return Tuple.Create(oldSolution, newSolution);
        }

        internal void TestWithMockedGenerateTypeDialog(
            string initial,
            string languageName,
            string typeName,
            string expected = null,
            bool isLine = true,
            bool isMissing = false,
            Accessibility accessibility = Accessibility.NotApplicable,
            TypeKind typeKind = TypeKind.Class,
            string projectName = null,
            bool isNewFile = false,
            string existingFilename = null,
            IList<string> newFileFolderContainers = null,
            string fullFilePath = null,
            string newFileName = null,
            string assertClassName = null,
            bool checkIfUsingsIncluded = false,
            bool checkIfUsingsNotIncluded = false,
            string expectedTextWithUsings = null,
            string defaultNamespace = "",
            bool areFoldersValidIdentifiers = true,
            GenerateTypeDialogOptions assertGenerateTypeDialogOptions = null,
            IList<TypeKindOptions> assertTypeKindPresent = null,
            IList<TypeKindOptions> assertTypeKindAbsent = null,
            bool isCancelled = false)
        {
            using (var testState = new GenerateTypeTestState(initial, isLine, projectName, typeName, existingFilename, languageName))
            {
                // Initialize the viewModel values
                testState.TestGenerateTypeOptionsService.SetGenerateTypeOptions(
                    accessibility: accessibility,
                    typeKind: typeKind,
                    typeName: testState.TypeName,
                    project: testState.ProjectToBeModified,
                    isNewFile: isNewFile,
                    newFileName: newFileName,
                    folders: newFileFolderContainers,
                    fullFilePath: fullFilePath,
                    existingDocument: testState.ExistingDocument,
                    areFoldersValidIdentifiers: areFoldersValidIdentifiers,
                    isCancelled: isCancelled);

                testState.TestProjectManagementService.SetDefaultNamespace(
                    defaultNamespace: defaultNamespace);

                var diagnosticsAndFixes = GetDiagnosticAndFixes(testState.Workspace, null);
                var generateTypeDiagFixes = diagnosticsAndFixes.SingleOrDefault(df => GenerateTypeTestState.FixIds.Contains(df.Item1.Id));

                if (isMissing)
                {
                    Assert.Null(generateTypeDiagFixes);
                    return;
                }

                var fixes = generateTypeDiagFixes.Item2.Fixes;
                Assert.NotNull(fixes);

                var fixActions = fixes.Select(f => f.Action);
                Assert.NotNull(fixActions);

                // Since the dialog option is always fed as the last CodeAction
                var index = fixActions.Count() - 1;
                var action = fixActions.ElementAt(index);

                Assert.Equal(action.Title, FeaturesResources.GenerateNewType);
                var options = ((CodeActionWithOptions)action).GetOptions(CancellationToken.None);
                var operations = ((CodeActionWithOptions)action).GetOperationsAsync(options, CancellationToken.None).Result;
                Tuple<Solution, Solution> oldSolutionAndNewSolution = null;

                if (!isNewFile)
                {
                    oldSolutionAndNewSolution = TestActions(testState.Workspace, expected, operations, testState.ExistingDocument.Id, compareTokens: false);
                }
                else
                {
                    oldSolutionAndNewSolution = TestAddDocument(
                        testState.Workspace,
                        expected,
                        operations,
                        projectName != null,
                        testState.ProjectToBeModified.Id,
                        newFileFolderContainers,
                        newFileName,
                        compareTokens: false);
                }

                if (checkIfUsingsIncluded)
                {
                    Assert.NotNull(expectedTextWithUsings);
                    TestActions(testState.Workspace, expectedTextWithUsings, operations, testState.InvocationDocument.Id, compareTokens: false);
                }

                if (checkIfUsingsNotIncluded)
                {
                    var oldSolution = oldSolutionAndNewSolution.Item1;
                    var newSolution = oldSolutionAndNewSolution.Item2;
                    var changedDocumentIds = SolutionUtilities.GetChangedDocuments(oldSolution, newSolution);

                    Assert.False(changedDocumentIds.Contains(testState.InvocationDocument.Id));
                }

                // Added into a different project than the triggering project
                if (projectName != null)
                {
                    var appliedChanges = ApplyOperationsAndGetSolution(testState.Workspace, operations);
                    var newSolution = appliedChanges.Item2;
                    var triggeredProject = newSolution.GetProject(testState.TriggeredProject.Id);

                    // Make sure the Project reference is present
                    Assert.True(triggeredProject.ProjectReferences.Any(pr => pr.ProjectId == testState.ProjectToBeModified.Id));
                }

                // Assert Option Calculation
                if (assertClassName != null)
                {
                    Assert.True(assertClassName == testState.TestGenerateTypeOptionsService.ClassName);
                }

                if (assertGenerateTypeDialogOptions != null || assertTypeKindPresent != null || assertTypeKindAbsent != null)
                {
                    var generateTypeDialogOptions = testState.TestGenerateTypeOptionsService.GenerateTypeDialogOptions;

                    if (assertGenerateTypeDialogOptions != null)
                    {
                        Assert.True(assertGenerateTypeDialogOptions.IsPublicOnlyAccessibility == generateTypeDialogOptions.IsPublicOnlyAccessibility);
                        Assert.True(assertGenerateTypeDialogOptions.TypeKindOptions == generateTypeDialogOptions.TypeKindOptions);
                        Assert.True(assertGenerateTypeDialogOptions.IsAttribute == generateTypeDialogOptions.IsAttribute);
                    }

                    if (assertTypeKindPresent != null)
                    {
                        foreach (var typeKindPresentEach in assertTypeKindPresent)
                        {
                            Assert.True((typeKindPresentEach & generateTypeDialogOptions.TypeKindOptions) != 0);
                        }
                    }

                    if (assertTypeKindAbsent != null)
                    {
                        foreach (var typeKindPresentEach in assertTypeKindAbsent)
                        {
                            Assert.True((typeKindPresentEach & generateTypeDialogOptions.TypeKindOptions) == 0);
                        }
                    }
                }
            }
        }
    }
}
