// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeActions.WorkspaceServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddMissingReference;

internal sealed class AddMissingReferenceCodeAction(Project project, string title, ProjectReference? projectReferenceToAdd, AssemblyIdentity missingAssemblyIdentity) : CodeAction
{
    private readonly Project _project = project;
    private readonly ProjectReference? _projectReferenceToAdd = projectReferenceToAdd;
    private readonly AssemblyIdentity _missingAssemblyIdentity = missingAssemblyIdentity;

    public override string Title { get; } = title;

    /// <summary>
    /// This code action only works by adding references.  As such, it requires a non document change (and is
    /// thus restricted in which hosts it can run).
    /// </summary>
    public override ImmutableArray<string> Tags => RequiresNonDocumentChangeTags;

    public static async Task<CodeAction> CreateAsync(Project project, AssemblyIdentity missingAssemblyIdentity, CancellationToken cancellationToken)
    {
        var dependencyGraph = project.Solution.GetProjectDependencyGraph();

        // We want to find a project that generates this assembly, if one so exists. We therefore 
        // search all projects that our project with an error depends on. We want to do this for
        // complicated and evil scenarios like this one:
        //
        //     C -> B -> A
        //
        //     A'
        //
        // Where, for some insane reason, A and A' are two projects that both emit an assembly 
        // by the same name. So imagine we are using a type in B from C, and we are missing a 
        // reference to A.dll. Both A and A' are candidates, but we know we can throw out A'
        // since whatever type from B we are using that's causing the error, we know that type 
        // isn't referencing A'. Put another way: this code action adds a reference, but should 
        // never change the transitive closure of project references that C has.
        //
        // Doing this filtering also means we get to check less projects (good), and ensures that
        // whatever project reference we end up adding won't add a circularity (also good.)
        foreach (var candidateProjectId in dependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(project.Id))
        {
            var candidateProject = project.Solution.GetRequiredProject(candidateProjectId);
            if (candidateProject.SupportsCompilation &&
                string.Equals(missingAssemblyIdentity.Name, candidateProject.AssemblyName, StringComparison.OrdinalIgnoreCase))
            {
                // The name matches, so let's see if the full identities are equal. 
                var compilation = await candidateProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                if (missingAssemblyIdentity.Equals(compilation.Assembly.Identity))
                {
                    // It matches, so just add a reference to this
                    return new AddMissingReferenceCodeAction(project,
                        string.Format(FeaturesResources.Add_project_reference_to_0, candidateProject.Name),
                        new ProjectReference(candidateProjectId), missingAssemblyIdentity);
                }
            }
        }

        // No matching project, so metadata reference
        var description = string.Format(FeaturesResources.Add_reference_to_0, missingAssemblyIdentity.GetDisplayName());
        return new AddMissingReferenceCodeAction(project, description, null, missingAssemblyIdentity);
    }

    protected override async Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(
        IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
    {
        // If we have a project reference to add, then add it
        if (_projectReferenceToAdd != null)
        {
            // note: no need to post process since we are just adding a project reference and not making any code changes.
            return ImmutableArray.Create<CodeActionOperation>(
                new ApplyChangesOperation(_project.AddProjectReference(_projectReferenceToAdd).Solution));
        }
        else
        {
            // We didn't have any project, so we need to try adding a metadata reference
            var factoryService = _project.Solution.Services.GetRequiredService<IAddMetadataReferenceCodeActionOperationFactoryWorkspaceService>();
            var operation = factoryService.CreateAddMetadataReferenceOperation(_project.Id, _missingAssemblyIdentity);
            return ImmutableArray.Create(operation);
        }
    }
}
