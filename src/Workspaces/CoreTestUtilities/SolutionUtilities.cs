// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public static class SolutionUtilities
{
    public static ProjectChanges GetSingleChangedProjectChanges(Solution oldSolution, Solution newSolution)
    {
        var solutionDifferences = newSolution.GetChanges(oldSolution);
        var projectChanges = solutionDifferences.GetProjectChanges();

        Assert.NotNull(projectChanges);
        Assert.NotEmpty(projectChanges);

        var projectId = projectChanges.Single().ProjectId;

        var oldProject = oldSolution.GetRequiredProject(projectId);
        var newProject = newSolution.GetRequiredProject(projectId);

        return newProject.GetChanges(oldProject);
    }

    private static IEnumerable<ProjectChanges> GetChangedProjectChanges(Solution oldSolution, Solution newSolution)
    {
        var solutionDifferences = newSolution.GetChanges(oldSolution);
        return solutionDifferences.GetProjectChanges().Select(n => n.NewProject.GetChanges(n.OldProject));
    }

    public static Document GetSingleChangedDocument(Solution oldSolution, Solution newSolution)
    {
        var projectDifferences = GetSingleChangedProjectChanges(oldSolution, newSolution);
        var documentId = projectDifferences.GetChangedDocuments().Single();

        return newSolution.GetDocument(documentId)!;
    }

    public static TextDocument GetSingleChangedAdditionalDocument(Solution oldSolution, Solution newSolution)
    {
        var projectDifferences = GetSingleChangedProjectChanges(oldSolution, newSolution);
        var documentId = projectDifferences.GetChangedAdditionalDocuments().Single();

        return newSolution.GetAdditionalDocument(documentId)!;
    }

    public static IEnumerable<DocumentId> GetChangedDocuments(Solution oldSolution, Solution newSolution)
    {
        var changedDocuments = new List<DocumentId>();
        var projectsDifference = GetChangedProjectChanges(oldSolution, newSolution);
        foreach (var projectDifference in projectsDifference)
        {
            changedDocuments.AddRange(projectDifference.GetChangedDocuments());
        }

        return changedDocuments;
    }

    public static Document GetSingleAddedDocument(Solution oldSolution, Solution newSolution)
    {
        var projectDifferences = GetSingleChangedProjectChanges(oldSolution, newSolution);
        var documentId = projectDifferences.GetAddedDocuments().Single();

        return newSolution.GetRequiredDocument(documentId);
    }

    public static IEnumerable<DocumentId> GetTextChangedDocuments(Solution oldSolution, Solution newSolution)
    {
        var changedDocuments = new List<DocumentId>();
        var projectsDifference = GetChangedProjectChanges(oldSolution, newSolution);
        foreach (var projectDifference in projectsDifference)
        {
            changedDocuments.AddRange(projectDifference.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true));
        }

        return changedDocuments;
    }

    public static IEnumerable<DocumentId> GetAddedDocuments(Solution oldSolution, Solution newSolution)
    {
        var addedDocuments = new List<DocumentId>();
        var projectsDifference = GetChangedProjectChanges(oldSolution, newSolution);
        foreach (var projectDifference in projectsDifference)
        {
            addedDocuments.AddRange(projectDifference.GetAddedDocuments());
        }

        return addedDocuments;
    }

    public static Tuple<Project, ProjectReference> GetSingleAddedProjectReference(Solution oldSolution, Solution newSolution)
    {
        var projectChanges = GetSingleChangedProjectChanges(oldSolution, newSolution);
        return Tuple.Create(projectChanges.NewProject, projectChanges.GetAddedProjectReferences().Single());
    }

    public static Project AddEmptyProject(Solution solution, string languageName = LanguageNames.CSharp, string name = "TestProject")
    {
        var id = ProjectId.CreateNewId();
        return solution.AddProject(
            ProjectInfo.Create(
                id,
                VersionStamp.Default,
                name: name,
                assemblyName: name,
                language: languageName)
                .WithCompilationOutputInfo(new CompilationOutputInfo(
                    assemblyPath: Path.Combine(TempRoot.Root, name),
                    generatedFilesOutputDirectory: null)))
            .GetRequiredProject(id);
    }
}
