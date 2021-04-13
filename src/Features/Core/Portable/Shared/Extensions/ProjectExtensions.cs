// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ProjectExtensions
    {
        public static Glyph GetGlyph(this Project project)
        {
            // TODO: Get the glyph from the hierarchy
            return project.Language == LanguageNames.CSharp ? Glyph.CSharpProject :
                   project.Language == LanguageNames.VisualBasic ? Glyph.BasicProject :
                                                                   Glyph.Assembly;
        }

        public static string GetNameWithAllFlavors(this Project project, ImmutableArray<ProjectId> allProjectIds)
        {
            // If there aren't any additional matches in other projects, we don't need to merge anything.
            if (allProjectIds.Length > 0)
            {
                var (firstProjectName, _) = project.State.NameAndFlavor;
                if (firstProjectName != null)
                {
                    // First get the simple project name and flavor for the actual project we got a hit in.  If we can't
                    // figure this out, we can't create a merged name.
                    using var _ = ArrayBuilder<string>.GetInstance(out var flavors);
                    project.GetAllFlavors(allProjectIds, flavors);

                    if (flavors.Count > 1)
                        return $"{firstProjectName} ({string.Join(", ", flavors)})";
                }
            }

            // Couldn't compute a merged project name (or only had one project).  Just return the name of hte project itself.
            return project.Name;
        }

        public static void GetAllFlavors(
            this Project project, ImmutableArray<ProjectId> allProjectIds, ArrayBuilder<string> flavors)
        {
            var solution = project.Solution;

            var (firstProjectName, firstProjectFlavor) = project.State.NameAndFlavor;
            if (firstProjectName != null)
            {
                flavors.Add(firstProjectFlavor!);

                // Now, do the same for the other projects where we had a match. As above, if we can't figure out the
                // simple name/flavor, or if the simple project name doesn't match the simple project name we started
                // with then we can't merge these.
                foreach (var projectId in allProjectIds)
                {
                    var otherProject = solution.GetRequiredProject(projectId);
                    var (projectName, projectFlavor) = otherProject.State.NameAndFlavor;
                    if (projectName == firstProjectName)
                        flavors.Add(projectFlavor!);
                }

                flavors.RemoveDuplicates();
                flavors.Sort();
            }
        }
    }
}
