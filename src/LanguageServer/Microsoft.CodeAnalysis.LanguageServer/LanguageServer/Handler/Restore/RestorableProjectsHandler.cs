// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Handler that allows the client to retrieve a set of restorable projects.
/// Used to populate a list of projects that can be restored.
/// </summary>
[ExportCSharpVisualBasicStatelessLspService(typeof(RestorableProjectsHandler)), Shared]
[Method(MethodName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RestorableProjectsHandler(ProjectTargetFrameworkManager projectTargetFrameworkManager) : ILspServiceRequestHandler<string[]>
{
    internal const string MethodName = "workspace/_roslyn_restorableProjects";

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public async Task<string[]> HandleRequestAsync(RequestContext context, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(context.Solution);

        // We use a sorted set here for the following reasons
        //   1.  Ensures the client gets a consistent ordering in the picker (especially useful for integration tests).
        //   2.  Removes projects with duplicate file paths (for example multi-targeted projects).  They all get restored
        //       together by file path.
        var projects = new SortedSet<string>();
        foreach (var project in context.Solution.Projects)
        {
            // To restore via the dotnet CLI, we must have a file path and it must be a .NET core project.
            if (project.FilePath != null && projectTargetFrameworkManager.IsDotnetCoreProject(project.Id))
            {
                projects.Add(project.FilePath);
            }
        }

        return projects.ToArray();
    }
}
