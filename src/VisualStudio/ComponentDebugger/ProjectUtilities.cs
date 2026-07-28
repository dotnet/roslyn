// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Utilities;

namespace Roslyn.ComponentDebugger
{
    public static class ProjectUtilities
    {
        public static async Task<ImmutableArray<UnconfiguredProject>> GetComponentReferencingProjectsAsync(this UnconfiguredProject unconfiguredProject)
        {
            var targetProjects = ArrayBuilder<UnconfiguredProject>.GetInstance();

            // get the output assembly for this project
            var projectArgs = await unconfiguredProject.GetCompilationArgumentsAsync().ConfigureAwait(false);
            var targetArg = projectArgs.LastOrDefault(a => a.StartsWith("/out:", StringComparison.OrdinalIgnoreCase));
            var target = Path.GetFileName(targetArg);

            var projectService = unconfiguredProject.Services.ProjectService;
            foreach (var targetProjectUnconfigured in projectService.LoadedUnconfiguredProjects)
            {
                // check if the args contain the project as an analyzer ref
                foreach (var arg in await targetProjectUnconfigured.GetCompilationArgumentsAsync().ConfigureAwait(false))
                {
                    if (arg.StartsWith("/analyzer:", StringComparison.OrdinalIgnoreCase)
                        && arg.EndsWith(target, StringComparison.OrdinalIgnoreCase))
                    {
                        targetProjects.Add(targetProjectUnconfigured);
                    }
                }
            }
            return targetProjects.ToImmutableAndFree();
        }

        public static Task<ImmutableArray<string>> GetCompilationArgumentsAsync(this UnconfiguredProject project)
        {
            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var dataSource = project.Services.ExportProvider.GetExportedValueOrDefault<CommandLineArgumentsDataSource>();
            if (dataSource is object)
            {
                return dataSource.GetArgsAsync();
            }

            return Task.FromResult(ImmutableArray<string>.Empty);
        }
    }
}
