// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ProjectExtensions
    {
        public static bool IsFromPrimaryBranch(this Project project)
        {
            return project.Solution.BranchId == project.Solution.Workspace.PrimaryBranchId;
        }

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

        public static async Task<VersionStamp> GetVersionAsync(this Project project, CancellationToken cancellationToken)
        {
            var version = project.Version;
            var latestVersion = await project.GetLatestDocumentVersionAsync(cancellationToken).ConfigureAwait(false);

            return version.GetNewerVersion(latestVersion);
        }

        public static Glyph GetGlyph(this Project project)
        {
            // TODO: Get the glyph from the hierarchy
            return project.Language == LanguageNames.CSharp      ? Glyph.CSharpProject :
                   project.Language == LanguageNames.VisualBasic ? Glyph.BasicProject :
                                                                   Glyph.Assembly;
        }
    }
}