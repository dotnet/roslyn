// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

    public bool TryGetDocumentId(IVsHierarchyItem hierarchyItem, string? targetFrameworkMoniker, [NotNullWhen(true)] out DocumentId? documentId)
    {
        documentId = null;

        var identity = hierarchyItem.HierarchyIdentity;
        var solution = _workspace.CurrentSolution;
        if (!TryGetProjectId(solution, hierarchyItem.Parent, targetFrameworkMoniker, out var projectId))
            return false;

        var hierarchy = identity.Hierarchy;
        var itemId = identity.ItemID;

        if (!hierarchy.TryGetCanonicalName(itemId, out var itemName))
            return false;

        var documentIds = solution.GetDocumentIdsWithFilePath(itemName);
        documentId = documentIds.FirstOrDefault(static (d, projectId) => d.ProjectId == projectId, projectId);

        return documentId != null;
    }

    public bool TryGetProjectId(IVsHierarchyItem? hierarchyItem, string? targetFrameworkMoniker, [NotNullWhen(true)] out ProjectId? projectId)
        => TryGetProjectId(_workspace.CurrentSolution, hierarchyItem, targetFrameworkMoniker, out projectId);

    private bool TryGetProjectId(
        Solution solution, IVsHierarchyItem? hierarchyItem, string? targetFrameworkMoniker, [NotNullWhen(true)] out ProjectId? projectId)
    {
        projectId = null;

        if (hierarchyItem is null)
            return false;

        // A project node is represented in two different hierarchies: the solution's IVsHierarchy (where it is a leaf node)
        // and the project's own IVsHierarchy (where it is the root node). The IVsHierarchyItem joins them together for the
        // purpose of creating the tree displayed in Solution Explorer. The project's hierarchy is what is passed from the
        // project system to the language service, so that's the one the one to query here. To do that we need to get
        // the "nested" hierarchy from the IVsHierarchyItem.
        var nestedHierarchy = hierarchyItem.HierarchyIdentity.NestedHierarchy;

        using var candidateProjects = TemporaryArray<ProjectId>.Empty;

        // First filter the projects by matching up properties on the input hierarchy against properties on each
        // project's hierarchy.
        foreach (var currentId in solution.ProjectIds)
        {
            var hierarchy = _workspace.GetHierarchy(currentId);
            if (hierarchy == nestedHierarchy)
            {
                var project = solution.GetRequiredProject(currentId);

                // We're about to access various properties of the IVsHierarchy associated with the project.
                // The properties supported and the interpretation of their values varies from one project system
                // to another. This code is designed with C# and VB in mind, so we need to filter out everything
                // else.
                if (project.Language is LanguageNames.CSharp or LanguageNames.VisualBasic)
                    candidateProjects.Add(currentId);
            }
        }

        // If we only have one candidate then no further checks are required.
        if (candidateProjects.Count == 1)
        {
            projectId = candidateProjects[0];
            return projectId != null;
        }

        // For CPS projects, we may have a string we extracted from a $TFM-prefixed capability; compare that to the string we're given
        // from CPS to see if this matches.
        if (targetFrameworkMoniker != null)
        {
            var matchingProjectId = candidateProjects.FirstOrDefault(
                static (id, tuple) => tuple._workspace.TryGetDependencyNodeTargetIdentifier(id) == tuple.targetFrameworkMoniker,
                (_workspace, targetFrameworkMoniker));

            if (matchingProjectId != null)
            {
                projectId = matchingProjectId;
                return projectId != null;
            }
        }

        // If we have multiple candidates then we might be dealing with Web Application Projects. In this case
        // there will be one main project plus one project for each open aspx/cshtml/vbhtml file, all with
        // identical properties on their hierarchies. We can find the main project by taking the first project
        // without a ContainedDocument.
        foreach (var candidateProjectId in candidateProjects)
        {
            var candidateProject = solution.GetRequiredProject(candidateProjectId);
            if (!candidateProject.DocumentIds.Any(id => ContainedDocument.TryGetContainedDocument(id) != null))
            {
                projectId = candidateProject.Id;
                return projectId != null;
            }
        }

        return false;
    }
}
