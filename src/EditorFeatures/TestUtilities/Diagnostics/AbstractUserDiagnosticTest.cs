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
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
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
        internal abstract Task<IEnumerable<Tuple<Diagnostic, CodeFixCollection>>> GetDiagnosticAndFixesAsync(
            TestWorkspace workspace, string fixAllActionEquivalenceKey, object fixProviderData);
        internal abstract Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(TestWorkspace workspace, object fixProviderData);

        protected override async Task<IList<CodeAction>> GetCodeActionsWorkerAsync(
            TestWorkspace workspace, string fixAllActionEquivalenceKey, object fixProviderData)
        {
            var diagnostics = await GetDiagnosticAndFixAsync(workspace, fixAllActionEquivalenceKey, fixProviderData);
            return diagnostics?.Item2?.Fixes.Select(f => f.Action).ToList();
        }

        internal async Task<Tuple<Diagnostic, CodeFixCollection>> GetDiagnosticAndFixAsync(
            TestWorkspace workspace, string fixAllActionEquivalenceKey = null, object fixProviderData = null)
        {
            return (await GetDiagnosticAndFixesAsync(workspace, fixAllActionEquivalenceKey, fixProviderData)).FirstOrDefault();
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

        internal async Task<IEnumerable<Tuple<Diagnostic, CodeFixCollection>>> GetDiagnosticAndFixesAsync(
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
            return await GetDiagnosticAndFixesAsync(diagnostics, provider, fixer, testDriver, document, span, scope, fixAllActionId);
        }

        private async Task<IEnumerable<Tuple<Diagnostic, CodeFixCollection>>> GetDiagnosticAndFixesAsync(
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
            var result = new List<Tuple<Diagnostic, CodeFixCollection>>();
            if (scope == null)
            {
                // Simple code fix.
                foreach (var diagnostic in diagnostics)
                {
                    var fixes = new List<CodeFix>();
                    var context = new CodeFixContext(document, diagnostic, (a, d) => fixes.Add(new CodeFix(document.Project, a, d)), CancellationToken.None);

                    await fixer.RegisterCodeFixesAsync(context);
                    if (fixes.Any())
                    {
                        var codeFix = new CodeFixCollection(
                            fixer, diagnostic.Location.SourceSpan, fixes.ToImmutableArray(),
                            fixAllState: null, supportedScopes: ImmutableArray<FixAllScope>.Empty, firstDiagnostic: null);
                        result.Add(Tuple.Create(diagnostic, codeFix));
                    }
                }
            }
            else
            {
                // Fix all fix.
                var fixAllProvider = fixer.GetFixAllProvider();
                Assert.NotNull(fixAllProvider);

                var fixAllState = GetFixAllState(fixAllProvider, diagnostics, provider, fixer, testDriver, document, scope.Value, fixAllActionId);
                var fixAllContext = fixAllState.CreateFixAllContext(new ProgressTracker(), CancellationToken.None);
                var fixAllFix = await fixAllProvider.GetFixAsync(fixAllContext);
                if (fixAllFix != null)
                {
                    // Same fix applies to each diagnostic in scope.
                    foreach (var diagnostic in diagnostics)
                    {
                        var diagnosticSpan = diagnostic.Location.IsInSource ? diagnostic.Location.SourceSpan : default(TextSpan);
                        var codeFix = new CodeFixCollection(
                            fixAllProvider, diagnosticSpan, ImmutableArray.Create(new CodeFix(document.Project, fixAllFix, diagnostic)),
                            fixAllState: null, supportedScopes: ImmutableArray<FixAllScope>.Empty, firstDiagnostic: null);
                        result.Add(Tuple.Create(diagnostic, codeFix));
                    }
                }
            }

            return result;
        }

        private static FixAllState GetFixAllState(
            FixAllProvider fixAllProvider,
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
                return FixAllState.Create(fixAllProvider, diagnosticsToFix, fixer, fixAllActionId);
            }

            var diagnostic = diagnostics.First();
            Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync =
                async (d, diagIds, c) =>
                {
                    var root = await d.GetSyntaxRootAsync();
                    var diags = await testDriver.GetDocumentDiagnosticsAsync(provider, d, root.FullSpan);
                    diags = diags.Where(diag => diagIds.Contains(diag.Id));
                    return diags;
                };

            Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync =
                async (p, includeAllDocumentDiagnostics, diagIds, c) =>
                {
                    var diags = includeAllDocumentDiagnostics
                        ? await testDriver.GetAllDiagnosticsAsync(provider, p)
                        : await testDriver.GetProjectDiagnosticsAsync(provider, p);
                    diags = diags.Where(diag => diagIds.Contains(diag.Id));
                    return diags;
                };

            var diagnosticIds = ImmutableHashSet.Create(diagnostic.Id);
            var fixAllDiagnosticProvider = new FixAllState.FixAllDiagnosticProvider(diagnosticIds, getDocumentDiagnosticsAsync, getProjectDiagnosticsAsync);
            return diagnostic.Location.IsInSource
                ? new FixAllState(fixAllProvider, document, fixer, scope, fixAllActionId, diagnosticIds, fixAllDiagnosticProvider)
                : new FixAllState(fixAllProvider, document.Project, fixer, scope, fixAllActionId, diagnosticIds, fixAllDiagnosticProvider);
        }

        protected async Task TestEquivalenceKeyAsync(string initialMarkup, string equivalenceKey)
        {
            using (var workspace = await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions: null, compilationOptions: null))
            {
                var diagnosticAndFix = await GetDiagnosticAndFixAsync(workspace);
                Assert.Equal(equivalenceKey, diagnosticAndFix.Item2.Fixes.ElementAt(index: 0).Action.EquivalenceKey);
            }
        }

        protected async Task TestActionCountInAllFixesAsync(
            string initialMarkup,
            int count,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            object fixProviderData = null)
        {
            using (var workspace = await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions, compilationOptions))
            {
                var diagnosticAndFix = await GetDiagnosticAndFixesAsync(workspace, fixAllActionEquivalenceKey: null, fixProviderData: fixProviderData);
                var diagnosticCount = diagnosticAndFix.Select(x => x.Item2.Fixes.Count()).Sum();

                Assert.Equal(count, diagnosticCount);
            }
        }

        protected async Task TestSpansAsync(
            string initialMarkup, string expectedMarkup,
            int index = 0,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            IDictionary<OptionKey, object> featureOptions = null,
            string diagnosticId = null,
            string fixAllActionEquivalenceId = null,
            object fixProviderData = null)
        {
            IList<TextSpan> spansList;
            string unused;
            MarkupTestFile.GetSpans(expectedMarkup, out unused, out spansList);

            var expectedTextSpans = spansList.ToSet();
            using (var workspace = await CreateWorkspaceFromFileAsync(initialMarkup, parseOptions, compilationOptions))
            {
                if (featureOptions != null)
                {
                    workspace.ApplyOptions(featureOptions);
                }

                ISet<TextSpan> actualTextSpans;
                if (diagnosticId == null)
                {
                    var diagnosticsAndFixes = await GetDiagnosticAndFixesAsync(workspace, fixAllActionEquivalenceId, fixProviderData);
                    var diagnostics = diagnosticsAndFixes.Select(t => t.Item1);
                    actualTextSpans = diagnostics.Select(d => d.Location.SourceSpan).ToSet();
                }
                else
                {
                    var diagnostics = await GetDiagnosticsAsync(workspace, fixProviderData);
                    actualTextSpans = diagnostics.Where(d => d.Id == diagnosticId).Select(d => d.Location.SourceSpan).ToSet();
                }

                Assert.True(expectedTextSpans.SetEquals(actualTextSpans));
            }
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
            using (var testState = await GenerateTypeTestState.CreateAsync(initial, isLine, projectName, typeName, existingFilename, languageName))
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

                var diagnosticsAndFixes = await GetDiagnosticAndFixesAsync(testState.Workspace, fixAllActionEquivalenceKey: null, fixProviderData: null);
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

                Assert.Equal(action.Title, FeaturesResources.Generate_new_type);
                var operations = await action.GetOperationsAsync(CancellationToken.None);
                Tuple<Solution, Solution> oldSolutionAndNewSolution = null;

                if (!isNewFile)
                {
                    oldSolutionAndNewSolution = await TestOperationsAsync(
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
                        compareTokens: false);
                }

                if (checkIfUsingsIncluded)
                {
                    Assert.NotNull(expectedTextWithUsings);
                    await TestOperationsAsync(testState.Workspace, expectedTextWithUsings, operations,
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
