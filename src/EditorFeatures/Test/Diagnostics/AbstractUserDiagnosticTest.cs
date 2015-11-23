// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateType;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.UnitTests.Diagnostics;
using Microsoft.VisualStudio.Text.Differencing;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public abstract class AbstractUserDiagnosticTest : AbstractCodeActionOrUserDiagnosticTest
    {
        internal abstract IEnumerable<Tuple<Diagnostic, CodeFixCollection>> GetDiagnosticAndFixes(TestWorkspace workspace, string fixAllActionEquivalenceKey);
        internal abstract IEnumerable<Diagnostic> GetDiagnostics(TestWorkspace workspace);

        protected override IList<CodeAction> GetCodeActionsWorker(TestWorkspace workspace, string fixAllActionEquivalenceKey)
        {
            var diagnostics = GetDiagnosticAndFix(workspace, fixAllActionEquivalenceKey);
            return diagnostics?.Item2?.Fixes.Select(f => f.Action).ToList();
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

        protected FixAllScope? GetFixAllScope(string annotation)
        {
            if (annotation == null)
            {
                return null;
            }

            switch (annotation)
            {
                case "FixAllInDocument":
                    return FixAllScope.Document;

                case "FixAllInProject":
                    return FixAllScope.Project;

                case "FixAllInSolution":
                    return FixAllScope.Solution;

                case "FixAllInSelection":
                    return FixAllScope.Custom;
            }

            throw new InvalidProgramException("Incorrect FixAll annotation in test");
        }

        internal IEnumerable<Tuple<Diagnostic, CodeFixCollection>> GetDiagnosticAndFixes(
            IEnumerable<Diagnostic> diagnostics,
            DiagnosticAnalyzer provider,
            CodeFixProvider fixer,
            TestDiagnosticAnalyzerDriver testDriver,
            Document document,
            TextSpan span,
            string annotation,
            string fixAllActionId)
        {
            if (diagnostics.IsEmpty())
            {
                return SpecializedCollections.EmptyEnumerable<Tuple<Diagnostic, CodeFixCollection>>();
            }

            FixAllScope? scope = GetFixAllScope(annotation);
            return GetDiagnosticAndFixes(diagnostics, provider, fixer, testDriver, document, span, scope, fixAllActionId);
        }

        private IEnumerable<Tuple<Diagnostic, CodeFixCollection>> GetDiagnosticAndFixes(
            IEnumerable<Diagnostic> diagnostics,
            DiagnosticAnalyzer provider,
            CodeFixProvider fixer,
            TestDiagnosticAnalyzerDriver testDriver,
            Document document,
            TextSpan span,
            FixAllScope? scope,
            string fixAllActionId)
        {
            Assert.NotEmpty(diagnostics);

            if (scope == null)
            {
                // Simple code fix.
                foreach (var diagnostic in diagnostics)
                {
                    var fixes = new List<CodeFix>();
                    var context = new CodeFixContext(document, diagnostic, (a, d) => fixes.Add(new CodeFix(document.Project, a, d)), CancellationToken.None);

                    fixer.RegisterCodeFixesAsync(context).Wait();
                    if (fixes.Any())
                    {
                        var codeFix = new CodeFixCollection(fixer, diagnostic.Location.SourceSpan, fixes);
                        yield return Tuple.Create(diagnostic, codeFix);
                    }
                }
            }
            else
            {
                // Fix all fix.
                var fixAllProvider = fixer.GetFixAllProvider();
                Assert.NotNull(fixAllProvider);

                var fixAllContext = GetFixAllContext(diagnostics, provider, fixer, testDriver, document, scope.Value, fixAllActionId);
                var fixAllFix = fixAllProvider.GetFixAsync(fixAllContext).WaitAndGetResult(CancellationToken.None);
                if (fixAllFix != null)
                {
                    // Same fix applies to each diagnostic in scope.
                    foreach (var diagnostic in diagnostics)
                    {
                        var diagnosticSpan = diagnostic.Location.IsInSource ? diagnostic.Location.SourceSpan : default(TextSpan);
                        var codeFix = new CodeFixCollection(fixAllProvider, diagnosticSpan, ImmutableArray.Create(new CodeFix(document.Project, fixAllFix, diagnostic)));
                        yield return Tuple.Create(diagnostic, codeFix);
                    }
                }
            }
        }

        private static FixAllContext GetFixAllContext(
            IEnumerable<Diagnostic> diagnostics,
            DiagnosticAnalyzer provider,
            CodeFixProvider fixer,
            TestDiagnosticAnalyzerDriver testDriver,
            Document document,
            FixAllScope scope,
            string fixAllActionId)
        {
            Assert.NotEmpty(diagnostics);

            if (scope == FixAllScope.Custom)
            {
                // Bulk fixing diagnostics in selected scope.                    
                var diagnosticsToFix = ImmutableDictionary.CreateRange(SpecializedCollections.SingletonEnumerable(KeyValuePair.Create(document, diagnostics.ToImmutableArray())));
                return FixMultipleContext.Create(diagnosticsToFix, fixer, fixAllActionId, CancellationToken.None);
            }

            var diagnostic = diagnostics.First();
            Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync =
                (d, diagIds, c) =>
                {
                    var root = d.GetSyntaxRootAsync().Result;
                    var diags = testDriver.GetDocumentDiagnostics(provider, d, root.FullSpan);
                    diags = diags.Where(diag => diagIds.Contains(diag.Id));
                    return Task.FromResult(diags);
                };

            Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync =
                (p, includeAllDocumentDiagnostics, diagIds, c) =>
                {
                    var diags = includeAllDocumentDiagnostics ?
                        testDriver.GetAllDiagnostics(provider, p) :
                        testDriver.GetProjectDiagnostics(provider, p);
                    diags = diags.Where(diag => diagIds.Contains(diag.Id));
                    return Task.FromResult(diags);
                };

            var diagnosticIds = ImmutableHashSet.Create(diagnostic.Id);
            var fixAllDiagnosticProvider = new FixAllCodeActionContext.FixAllDiagnosticProvider(diagnosticIds, getDocumentDiagnosticsAsync, getProjectDiagnosticsAsync);
            return diagnostic.Location.IsInSource ?
                new FixAllContext(document, fixer, scope, fixAllActionId, diagnosticIds, fixAllDiagnosticProvider, CancellationToken.None) :
                new FixAllContext(document.Project, fixer, scope, fixAllActionId, diagnosticIds, fixAllDiagnosticProvider, CancellationToken.None);
        }

        protected void TestEquivalenceKey(string initialMarkup, string equivalenceKey)
        {
            using (var workspace = CreateWorkspaceFromFile(initialMarkup, parseOptions: null, compilationOptions: null))
            {
                var diagnosticAndFix = GetDiagnosticAndFix(workspace);
                Assert.Equal(equivalenceKey, diagnosticAndFix.Item2.Fixes.ElementAt(index: 0).Action.EquivalenceKey);
            }
        }

        protected void TestActionCountInAllFixes(
            string initialMarkup,
            int count,
            ParseOptions parseOptions = null, CompilationOptions compilationOptions = null)
        {
            using (var workspace = CreateWorkspaceFromFile(initialMarkup, parseOptions, compilationOptions))
            {
                var diagnosticAndFix = GetDiagnosticAndFixes(workspace, null);
                var diagnosticCount = diagnosticAndFix.Select(x => x.Item2.Fixes.Count()).Sum();

                Assert.Equal(count, diagnosticCount);
            }
        }

        protected void TestSpans(
            string initialMarkup, string expectedMarkup,
            int index = 0,
            ParseOptions parseOptions = null, CompilationOptions compilationOptions = null,
            string diagnosticId = null, string fixAllActionEquivalenceId = null)
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

        protected async Task TestAddDocument(
            string initialMarkup, string expectedMarkup,
            IList<string> expectedContainers,
            string expectedDocumentName,
            int index = 0,
            bool compareTokens = true, bool isLine = true)
        {
            await TestAddDocument(initialMarkup, expectedMarkup, index, expectedContainers, expectedDocumentName, null, null, compareTokens, isLine).ConfigureAwait(true);
            await TestAddDocument(initialMarkup, expectedMarkup, index, expectedContainers, expectedDocumentName, GetScriptOptions(), null, compareTokens, isLine).ConfigureAwait(true);
        }

        private async Task TestAddDocument(
            string initialMarkup, string expectedMarkup,
            int index,
            IList<string> expectedContainers,
            string expectedDocumentName,
            ParseOptions parseOptions, CompilationOptions compilationOptions,
            bool compareTokens, bool isLine)
        {
            using (var workspace = isLine ? CreateWorkspaceFromFile(initialMarkup, parseOptions, compilationOptions) : TestWorkspaceFactory.CreateWorkspace(initialMarkup))
            {
                var codeActions = GetCodeActions(workspace, fixAllActionEquivalenceKey: null);
                await TestAddDocument(workspace, expectedMarkup, index, expectedContainers, expectedDocumentName,
                    codeActions, compareTokens).ConfigureAwait(true);
            }
        }

        private async Task TestAddDocument(
            TestWorkspace workspace,
            string expectedMarkup,
            int index,
            IList<string> expectedFolders,
            string expectedDocumentName,
            IList<CodeAction> actions,
            bool compareTokens)
        {
            var operations = VerifyInputsAndGetOperations(index, actions);
            await TestAddDocument(
                workspace,
                expectedMarkup,
                operations,
                hasProjectChange: false,
                modifiedProjectId: null,
                expectedFolders: expectedFolders,
                expectedDocumentName: expectedDocumentName,
                compareTokens: compareTokens).ConfigureAwait(true);
        }

        private async Task<Tuple<Solution, Solution>> TestAddDocument(
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
                var content = await editHandler.GetPreviews(workspace, operations, CancellationToken.None).TakeNextPreviewAsync().ConfigureAwait(true);
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
                while ((preview = await contents.TakeNextPreviewAsync().ConfigureAwait(true)) != null)
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

        internal async Task TestWithMockedGenerateTypeDialog(
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

                var fixActions = MassageActions(fixes.Select(f => f.Action).ToList());
                Assert.NotNull(fixActions);

                // Since the dialog option is always fed as the last CodeAction
                var index = fixActions.Count() - 1;
                var action = fixActions.ElementAt(index);

                Assert.Equal(action.Title, FeaturesResources.GenerateNewType);
                var operations = action.GetOperationsAsync(CancellationToken.None).Result;
                Tuple<Solution, Solution> oldSolutionAndNewSolution = null;

                if (!isNewFile)
                {
                    oldSolutionAndNewSolution = TestOperations(
                        testState.Workspace, expected, operations,
                        conflictSpans: null, renameSpans: null, warningSpans: null,
                        compareTokens: false, expectedChangedDocumentId: testState.ExistingDocument.Id);
                }
                else
                {
                    oldSolutionAndNewSolution = await TestAddDocument(
                        testState.Workspace,
                        expected,
                        operations,
                        projectName != null,
                        testState.ProjectToBeModified.Id,
                        newFileFolderContainers,
                        newFileName,
                        compareTokens: false).ConfigureAwait(true);
                }

                if (checkIfUsingsIncluded)
                {
                    Assert.NotNull(expectedTextWithUsings);
                    TestOperations(testState.Workspace, expectedTextWithUsings, operations,
                        conflictSpans: null, renameSpans: null, warningSpans: null, compareTokens: false,
                        expectedChangedDocumentId: testState.InvocationDocument.Id);
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
