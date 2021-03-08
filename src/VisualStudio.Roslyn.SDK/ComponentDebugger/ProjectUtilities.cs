// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ProjectSystem;

namespace Roslyn.ComponentDebugger
{
    public static class ProjectUtilities
    {
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
