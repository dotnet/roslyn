// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Service to compute and fetch analyzer diagnostics from explicit invocation of code analysis on a project or a solution.
    /// </summary>
    internal interface ICodeAnalysisDiagnosticAnalyzerService : IWorkspaceService
    {
        /// <summary>
        /// Runs all the applicable analyzers on the given project or entire solution if <paramref name="projectId"/> is null.
        /// </summary>
        Task RunAnalysisAsync(Solution solution, Action<Project> onProjectAnalyzed, ProjectId? projectId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns true if <see cref="RunAnalysisAsync(Solution, Action{Project}, ProjectId?, CancellationToken)"/> was invoked
        /// on either the current or a prior snapshot of the project or containing solution for the given <paramref name="projectId"/>.
        /// </summary>
        bool HasProjectBeenAnalyzed(ProjectId projectId);

        /// <summary>
        /// Returns analyzer diagnostics reported on the given <paramref name="documentId"/>> from the last
        /// <see cref="RunAnalysisAsync(Solution, Action{Project}, ProjectId?, CancellationToken)"/> invocation on the containing project or solution.
        /// The caller is expected to check <see cref="HasProjectBeenAnalyzed(ProjectId)"/> prior to calling this method.
        /// </summary>
        /// <remarks>
        /// Note that the returned diagnostics may not be from the latest document snapshot.
        /// </remarks>
        Task<ImmutableArray<DiagnosticData>> GetDocumentDiagnosticsAsync(DocumentId documentId, Workspace workspace, CancellationToken cancellationToken);

        /// <summary>
        /// Returns analyzer diagnostics without any document location reported on the given <paramref name="projectId"/>> from the last
        /// <see cref="RunAnalysisAsync(Solution, Action{Project}, ProjectId?, CancellationToken)"/> invocation on the given project or solution.
        /// The caller is expected to check <see cref="HasProjectBeenAnalyzed(ProjectId)"/> prior to calling this method.
        /// </summary>
        /// <remarks>
        /// Note that the returned diagnostics may not be from the latest project snapshot.
        /// </remarks>
        Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsAsync(ProjectId projectId, Workspace workspace, CancellationToken cancellationToken);
    }
}
