// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild;

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

    public ProjectFile LoadProject(string path, string projectContent, ProjectBuildManager buildManager)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        // We expect MSBuild to consume this stream with a utf-8 encoding.
        // This is because we expect the stream we create to not include a BOM nor an an encoding declaration a la `<?xml encoding="..."?>`.
        // In this scenario, the XML standard requires XML processors to consume the document with a UTF-8 encoding.
        // https://www.w3.org/TR/xml/#d0e4623
        // Theoretically we could also enforce that 'projectContent' does not contain an encoding declaration with non-UTF-8 encoding.
        // But it seems like a very unlikely scenario to actually get into--this is not something people generally put on real project files.
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(projectContent));
        var (project, log) = buildManager.LoadProject(path, stream);

        return this.CreateProjectFile(project, buildManager, log);
    }
}
