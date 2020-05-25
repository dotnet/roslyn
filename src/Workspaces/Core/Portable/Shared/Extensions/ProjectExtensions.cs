// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
    }
}
