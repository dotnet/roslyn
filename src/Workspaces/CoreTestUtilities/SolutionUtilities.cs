﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class SolutionUtilities
    {
        public static ProjectChanges GetSingleChangedProjectChanges(Solution oldSolution, Solution newSolution)
        {
            var solutionDifferences = newSolution.GetChanges(oldSolution);
            var projectId = solutionDifferences.GetProjectChanges().Single().ProjectId;

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

            return newSolution.GetDocument(documentId)!;
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
    }
}
