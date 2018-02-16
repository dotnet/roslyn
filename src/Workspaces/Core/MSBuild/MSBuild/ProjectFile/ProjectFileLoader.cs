// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild.Build;
using Microsoft.CodeAnalysis.MSBuild.Logging;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal abstract class ProjectFileLoader : IProjectFileLoader
    {
        public abstract string Language { get; }

        protected abstract ProjectFile CreateProjectFile(MSB.Evaluation.Project project, DiagnosticLog log);

        public async Task<IProjectFile> LoadProjectFileAsync(string path, IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            // load project file async
            var (project, log) = await ProjectBuildManager.LoadProjectAsync(path, globalProperties, cancellationToken).ConfigureAwait(false);

            return this.CreateProjectFile(project, log);
        }

        public static async Task<string> GetOutputFilePathAsync(string path, IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
        {
            var (project, _) = await ProjectBuildManager.LoadProjectAsync(path, globalProperties, cancellationToken).ConfigureAwait(false);
            return project?.GetPropertyValue("TargetPath") ?? string.Empty;
        }

        public static IProjectFileLoader GetLoaderForProjectTypeGuid(Workspace workspace, Guid guid)
        {
            return workspace.Services.FindLanguageServices<IProjectFileLoader>(
                d => d.GetEnumerableMetadata<string>("ProjectTypeGuid").Any(g => guid == new Guid(g)))
                .FirstOrDefault();
        }

        public static IProjectFileLoader GetLoaderForProjectFileExtension(Workspace workspace, string extension)
        {
            return workspace.Services.FindLanguageServices<IProjectFileLoader>(
                d => d.GetEnumerableMetadata<string>("ProjectFileExtension").Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase)))
                .FirstOrDefault();
        }
    }
}
