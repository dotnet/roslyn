// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.GenerateType;
using Microsoft.CodeAnalysis.GenerateType;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public abstract partial class AbstractUserDiagnosticTest
    {
        // TODO: IInlineRenameService requires WPF (https://github.com/dotnet/roslyn/issues/46153)
        private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeaturesWpf
            .AddExcludedPartTypes(typeof(IDiagnosticUpdateSourceRegistrationService))
            .AddParts(
                typeof(MockDiagnosticUpdateSourceRegistrationService),
                typeof(TestGenerateTypeOptionsService),
                typeof(TestProjectManagementService));

        internal async Task TestWithMockedGenerateTypeDialog(
            string initial,
            string languageName,
            string typeName,
            string expected = null,
            bool isMissing = false,
            Accessibility accessibility = Accessibility.NotApplicable,
            TypeKind typeKind = TypeKind.Class,
            string projectName = null,
            bool isNewFile = false,
            string existingFilename = null,
            ImmutableArray<string> newFileFolderContainers = default,
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
            using var workspace = TestWorkspace.IsWorkspaceElement(initial)
                ? EditorTestWorkspace.Create(initial, composition: s_composition)
                : languageName == LanguageNames.CSharp
                  ? EditorTestWorkspace.CreateCSharp(initial, composition: s_composition)
                  : EditorTestWorkspace.CreateVisualBasic(initial, composition: s_composition);

            var testOptions = new TestParameters();
            var (diagnostics, actions, _) = await GetDiagnosticAndFixesAsync(workspace, testOptions);

            var testState = new GenerateTypeTestState(workspace, projectToBeModified: projectName, typeName, existingFilename);

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

            var generateTypeDiagFixes = diagnostics.SingleOrDefault(df => GenerateTypeTestState.FixIds.Contains(df.Id));

            if (isMissing)
            {
                Assert.Empty(actions);
                return;
            }

            var fixActions = MassageActions(actions);
            Assert.False(fixActions.IsDefault);

            // Since the dialog option is always fed as the last CodeAction
            var index = fixActions.Count() - 1;
            var action = fixActions.ElementAt(index);

            Assert.Equal(action.Title, FeaturesResources.Generate_new_type);
            var operations = await action.GetOperationsAsync(
                workspace.CurrentSolution, CodeAnalysisProgress.None, CancellationToken.None);
            Tuple<Solution, Solution> oldSolutionAndNewSolution = null;

            if (!isNewFile)
            {
                oldSolutionAndNewSolution = await TestOperationsAsync(
                    testState.Workspace, expected, operations,
                    conflictSpans: ImmutableArray<TextSpan>.Empty,
                    renameSpans: ImmutableArray<TextSpan>.Empty,
                    warningSpans: ImmutableArray<TextSpan>.Empty,
                    navigationSpans: ImmutableArray<TextSpan>.Empty,
                    expectedChangedDocumentId: testState.ExistingDocument.Id);
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
                    newFileName);
            }

            if (checkIfUsingsIncluded)
            {
                Assert.NotNull(expectedTextWithUsings);
                await TestOperationsAsync(testState.Workspace, expectedTextWithUsings, operations,
                    conflictSpans: ImmutableArray<TextSpan>.Empty,
                    renameSpans: ImmutableArray<TextSpan>.Empty,
                    warningSpans: ImmutableArray<TextSpan>.Empty,
                    navigationSpans: ImmutableArray<TextSpan>.Empty,
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
                var appliedChanges = await ApplyOperationsAndGetSolutionAsync(testState.Workspace, operations);
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
