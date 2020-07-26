// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ProjectExtensions
    {
        public static bool IsFromPrimaryBranch(this Project project)
            => project.Solution.BranchId == project.Solution.Workspace.PrimaryBranchId;

        public static async Task<bool> IsForkedProjectWithSemanticChangesAsync(this Project project, CancellationToken cancellationToken)
        {
            if (project.IsFromPrimaryBranch())
            {
                return false;
            }

            var currentProject = project.Solution.Workspace.CurrentSolution.GetProject(project.Id);
            if (currentProject == null)
            {
                return true;
            }

            var semanticVersion = await project.GetSemanticVersionAsync(cancellationToken).ConfigureAwait(false);
            var currentSemanticVersion = await currentProject.GetSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

            return !semanticVersion.Equals(currentSemanticVersion);
        }

        internal static Project WithSolutionOptions(this Project project, OptionSet options)
            => project.Solution.WithOptions(options).GetProject(project.Id)!;

        public static TextDocument? GetTextDocument(this Project project, DocumentId? documentId)
            => project.Solution.GetTextDocument(documentId);

        internal static DocumentId? GetDocumentForExternalLocation(this Project project, Location location)
        {
            Debug.Assert(location.Kind == LocationKind.ExternalFile);
            return project.GetDocumentIdWithFilePath(location.GetLineSpan().Path);
        }

        internal static DocumentId? GetDocumentForFile(this Project project, AdditionalText additionalText)
            => project.GetDocumentIdWithFilePath(additionalText.Path);

        private static DocumentId? GetDocumentIdWithFilePath(this Project project, string filePath)
            => project.Solution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault(id => id.ProjectId == project.Id);

        public static Document GetRequiredDocument(this Project project, DocumentId documentId)
        {
            var document = project.GetDocument(documentId);
            if (document == null)
            {
                throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);
            }

            return document;
        }

        public static TextDocument GetRequiredTextDocument(this Project project, DocumentId documentId)
        {
            var document = project.GetTextDocument(documentId);
            if (document == null)
            {
                throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);
            }

            return document;
        }
    }
}
