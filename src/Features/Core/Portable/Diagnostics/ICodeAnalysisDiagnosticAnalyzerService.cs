// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Service to compute and fetch analyzer diagnostics from explicit invocation of code analysis on a project or a solution.
/// </summary>
internal interface ICodeAnalysisDiagnosticAnalyzerService : IWorkspaceService
{
    void Clear();

    /// <summary>
    /// Runs all the applicable analyzers on the given project.
    /// </summary>
    ValueTask RunAnalysisAsync(Project project, CancellationToken cancellationToken);

    /// <summary>
    /// Returns true if <see cref="RunAnalysisAsync(Project, CancellationToken)"/> was invoked on either the current or
    /// a prior snapshot of the project or containing solution for the given <paramref name="projectId"/>. This method
    /// will keep returning true for a given project ID once any given snapshot of the project has been analyzed. This
    /// changes once the solution is closed/reloaded, at which point all the projects are returned back to not analyzed
    /// state and this method will return false.
    /// </summary>
    bool HasProjectBeenAnalyzed(ProjectId projectId);

    /// <summary>
    /// Returns analyzer diagnostics reported on the given <paramref name="documentId"/>> from the last <see
    /// cref="RunAnalysisAsync(Project, CancellationToken)"/> invocation on the containing project or solution. The
    /// caller is expected to check <see cref="HasProjectBeenAnalyzed(ProjectId)"/> prior to calling this method.
    /// </summary>
    /// <remarks>
    /// Note that the returned diagnostics may not be from the latest document snapshot.
    /// </remarks>
    ImmutableArray<DiagnosticData> GetLastComputedDocumentDiagnostics(DocumentId documentId);

    /// <summary>
    /// Returns analyzer diagnostics without any document location reported on the given <paramref name="projectId"/>>
    /// from the last <see cref="RunAnalysisAsync(Project, CancellationToken)"/> invocation on the given project or
    /// solution. The caller is expected to check <see cref="HasProjectBeenAnalyzed(ProjectId)"/> prior to calling this
    /// method.
    /// </summary>
    /// <remarks>
    /// Note that the returned diagnostics may not be from the latest project snapshot.
    /// </remarks>
    ImmutableArray<DiagnosticData> GetLastComputedProjectDiagnostics(ProjectId projectId);
}
