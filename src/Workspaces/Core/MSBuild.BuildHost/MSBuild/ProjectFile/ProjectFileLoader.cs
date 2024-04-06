// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild.Build;
using Microsoft.CodeAnalysis.MSBuild.Logging;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal abstract class ProjectFileLoader
    {
        public abstract string Language { get; }

        protected abstract ProjectFile CreateProjectFile(MSB.Evaluation.Project? project, ProjectBuildManager buildManager, DiagnosticLog log);

        public async Task<ProjectFile> LoadProjectFileAsync(string path, ProjectBuildManager buildManager, CancellationToken cancellationToken)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            // load project file async
            var (project, log) = await buildManager.LoadProjectAsync(path, cancellationToken).ConfigureAwait(false);

            return this.CreateProjectFile(project, buildManager, log);
        }
    }
}
