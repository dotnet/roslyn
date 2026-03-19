// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

[ExportWorkspaceService(typeof(IHierarchyItemToProjectIdMap), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class HierarchyItemToProjectIdMap(VisualStudioWorkspaceImpl workspace) : IHierarchyItemToProjectIdMap
{
    private readonly VisualStudioWorkspaceImpl _workspace = workspace;

    public bool TryGetProject(IVsHierarchyItem? hierarchyItem, string? targetFrameworkMoniker, [NotNullWhen(true)] out Project? project)
        => TryGetProject(_workspace.CurrentSolution, hierarchyItem, targetFrameworkMoniker, out project);

    private bool TryGetProject(
        Solution solution, IVsHierarchyItem? hierarchyItem, string? targetFrameworkMoniker, [NotNullWhen(true)] out Project? project)
    {
        project = null;

        if (hierarchyItem is null)
            return false;

        // A project node is represented in two different hierarchies: the solution's IVsHierarchy (where it is a leaf node)
        // and the project's own IVsHierarchy (where it is the root node). The IVsHierarchyItem joins them together for the
        // purpose of creating the tree displayed in Solution Explorer. The project's hierarchy is what is passed from the
        // project system to the language service, so that's the one the one to query here. To do that we need to get
        // the "nested" hierarchy from the IVsHierarchyItem.
        var nestedHierarchy = hierarchyItem.HierarchyIdentity.NestedHierarchy;

        using var candidateProjects = TemporaryArray<Project>.Empty;

        // First filter the projects by matching up properties on the input hierarchy against properties on each
        // project's hierarchy.
        foreach (var currentId in solution.ProjectIds)
        {
            var hierarchy = _workspace.GetHierarchy(currentId);
            if (hierarchy == nestedHierarchy)
            {
                var currentProject = solution.GetProject(currentId);

                // We're about to access various properties of the IVsHierarchy associated with the project.
                // The properties supported and the interpretation of their values varies from one project system
                // to another. This code is designed with C# and VB in mind, so we need to filter out everything
                // else.
                if (currentProject?.Language is LanguageNames.CSharp or LanguageNames.VisualBasic)
                    candidateProjects.Add(currentProject);
            }
        }

        // If we only have one candidate then no further checks are required.
        if (candidateProjects.Count == 1)
        {
            project = candidateProjects[0];
            return project != null;
        }

        // For CPS projects, we may have a string we extracted from a $TFM-prefixed capability; compare that to the string we're given
        // from CPS to see if this matches.
        if (targetFrameworkMoniker != null)
        {
            var matchingProject = candidateProjects.FirstOrDefault(
                static (project, tuple) => tuple._workspace.TryGetDependencyNodeTargetIdentifier(project.Id) == tuple.targetFrameworkMoniker,
                (_workspace, targetFrameworkMoniker));

            if (matchingProject != null)
            {
                project = matchingProject;
                return project != null;
            }
        }

        // If we have multiple candidates then we might be dealing with Web Application Projects. In this case
        // there will be one main project plus one project for each open aspx/cshtml/vbhtml file, all with
        // identical properties on their hierarchies. We can find the main project by taking the first project
        // without a ContainedDocument.
        foreach (var candidateProject in candidateProjects)
        {
            if (!candidateProject.DocumentIds.Any(id => ContainedDocument.TryGetContainedDocument(id) != null))
            {
                project = candidateProject;
                return project != null;
            }
        }

        return false;
    }
}
