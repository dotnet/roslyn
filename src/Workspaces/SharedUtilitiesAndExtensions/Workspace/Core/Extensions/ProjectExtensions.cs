// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ProjectExtensions
    {
        public static TLanguageService? GetLanguageService<TLanguageService>(this Project? project) where TLanguageService : class, ILanguageService
            => project?.GetExtendedLanguageServices().GetService<TLanguageService>();

        public static TLanguageService GetRequiredLanguageService<TLanguageService>(this Project project) where TLanguageService : class, ILanguageService
            => project.GetExtendedLanguageServices().GetRequiredService<TLanguageService>();

#pragma warning disable RS0030 // Do not used banned API 'Project.LanguageServices', use 'GetExtendedLanguageServices' instead - allow in this helper.
        /// <summary>
        /// Gets extended host language services, which includes language services from <see cref="Project.LanguageServices"/>.
        /// </summary>
        public static HostLanguageServices GetExtendedLanguageServices(this Project project)
            => project.Solution.Workspace.Services.GetExtendedLanguageServices(project.Language);
#pragma warning restore RS0030 // Do not used banned APIs

        public static async Task<VersionStamp> GetVersionAsync(this Project project, CancellationToken cancellationToken)
        {
            var version = project.Version;
            var latestVersion = await project.GetLatestDocumentVersionAsync(cancellationToken).ConfigureAwait(false);

            return version.GetNewerVersion(latestVersion);
        }

        public static string? TryGetAnalyzerConfigPathForProjectConfiguration(this Project project)
            => TryGetAnalyzerConfigPathForProjectOrDiagnosticConfiguration(project, diagnostic: null);

        public static string? TryGetAnalyzerConfigPathForDiagnosticConfiguration(this Project project, Diagnostic diagnostic)
        {
            Debug.Assert(diagnostic != null);
            return TryGetAnalyzerConfigPathForProjectOrDiagnosticConfiguration(project, diagnostic);
        }

        private static string? TryGetAnalyzerConfigPathForProjectOrDiagnosticConfiguration(Project project, Diagnostic? diagnostic)
        {
            if (project.AnalyzerConfigDocuments.Any())
            {
                var diagnosticFilePath = PathUtilities.GetDirectoryName(diagnostic?.Location.SourceTree?.FilePath ?? project.FilePath);
                if (!PathUtilities.IsAbsolute(diagnosticFilePath))
                {
                    return null;
                }

                // Currently, we use a simple heuristic to find existing .editorconfig file.
                // We start from the directory of the source file where the diagnostic was reported and walk up
                // the directory tree to find an .editorconfig file.
                // In future, we might change this algorithm, or allow end users to customize it based on options.

                var bestPath = string.Empty;
                AnalyzerConfigDocument? bestAnalyzerConfigDocument = null;
                foreach (var analyzerConfigDocument in project.AnalyzerConfigDocuments)
                {
                    // Analyzer config documents always have full paths, so GetDirectoryName will not return null.
                    var analyzerConfigDirectory = PathUtilities.GetDirectoryName(analyzerConfigDocument.FilePath)!;
                    if (diagnosticFilePath.StartsWith(analyzerConfigDirectory) &&
                        analyzerConfigDirectory.Length > bestPath.Length)
                    {
                        bestPath = analyzerConfigDirectory;
                        bestAnalyzerConfigDocument = analyzerConfigDocument;
                    }
                }

                if (bestAnalyzerConfigDocument != null)
                {
                    return bestAnalyzerConfigDocument.FilePath;
                }
            }

            // Did not find any existing .editorconfig, so create one at root of the solution, if one exists.
            // If project is not part of a solution, then use project path.
            var solutionOrProjectFilePath = project.Solution?.FilePath ?? project.FilePath;
            if (!PathUtilities.IsAbsolute(solutionOrProjectFilePath))
            {
                return null;
            }

            var solutionOrProjectDirectoryPath = PathUtilities.GetDirectoryName(solutionOrProjectFilePath);
            // Suppression should be removed or addressed https://github.com/dotnet/roslyn/issues/41636
            return PathUtilities.CombineAbsoluteAndRelativePaths(solutionOrProjectDirectoryPath!, ".editorconfig");
        }

        public static AnalyzerConfigDocument? TryGetExistingAnalyzerConfigDocumentAtPath(this Project project, string analyzerConfigPath)
        {
            Debug.Assert(analyzerConfigPath != null);
            Debug.Assert(PathUtilities.IsAbsolute(analyzerConfigPath));

            return project.AnalyzerConfigDocuments.FirstOrDefault(d => d.FilePath == analyzerConfigPath);
        }

        public static AnalyzerConfigDocument? GetOrCreateAnalyzerConfigDocument(this Project project, string analyzerConfigPath)
        {
            var existingAnalyzerConfigDocument = project.TryGetExistingAnalyzerConfigDocumentAtPath(analyzerConfigPath);
            if (existingAnalyzerConfigDocument != null)
            {
                return existingAnalyzerConfigDocument;
            }

            var id = DocumentId.CreateNewId(project.Id);
            var documentInfo = DocumentInfo.Create(id, ".editorconfig", filePath: analyzerConfigPath);
            var newSolution = project.Solution.AddAnalyzerConfigDocuments(ImmutableArray.Create(documentInfo));
            return newSolution.GetProject(project.Id)?.GetAnalyzerConfigDocument(id);
        }

        public static async Task<Compilation> GetRequiredCompilationAsync(this Project project, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation == null)
            {
                throw new InvalidOperationException(string.Format(WorkspaceExtensionsResources.Compilation_is_required_to_accomplish_the_task_but_is_not_supported_by_project_0, project.Name));
            }

            return compilation;
        }
    }
}
