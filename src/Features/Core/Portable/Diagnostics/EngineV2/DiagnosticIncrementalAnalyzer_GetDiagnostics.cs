// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        public override Task<ImmutableArray<DiagnosticData>> GetSpecificCachedDiagnosticsAsync(Solution solution, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetSpecificDiagnosticsAsync(solution, id, includeSuppressedDiagnostics, cancellationToken);
        }

        public override Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetDiagnosticsAsync(solution, projectId, documentId, includeSuppressedDiagnostics, cancellationToken);
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Solution solution, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (id is ValueTuple<DiagnosticIncrementalAnalyzer, DocumentId>)
            {
                var key = (ValueTuple<DiagnosticIncrementalAnalyzer, DocumentId>)id;
                return await GetDiagnosticsAsync(solution, key.Item2.ProjectId, key.Item2, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
            }

            if (id is ValueTuple<DiagnosticIncrementalAnalyzer, ProjectId>)
            {
                var key = (ValueTuple<DiagnosticIncrementalAnalyzer, ProjectId>)id;
                var diagnostics = await GetDiagnosticsAsync(solution, key.Item2, null, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
                return diagnostics.Where(d => d.DocumentId == null).ToImmutableArray();
            }

            return ImmutableArray<DiagnosticData>.Empty;
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (documentId != null)
            {
                var diagnostics = await GetProjectDiagnosticsAsync(solution.GetProject(projectId), includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
                return diagnostics.Where(d => d.DocumentId == documentId).ToImmutableArrayOrEmpty();
            }

            if (projectId != null)
            {
                return await GetProjectDiagnosticsAsync(solution.GetProject(projectId), includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
            }

            var builder = ImmutableArray.CreateBuilder<DiagnosticData>();
            foreach (var project in solution.Projects)
            {
                builder.AddRange(await GetProjectDiagnosticsAsync(project, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false));
            }

            return builder.ToImmutable();
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, DocumentId documentId = null, ImmutableHashSet<string> diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var diagnostics = await GetDiagnosticsAsync(solution, projectId, documentId, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
            return diagnostics.Where(d => diagnosticIds.Contains(d.Id)).ToImmutableArrayOrEmpty();
        }

        public override async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(Solution solution, ProjectId projectId = null, ImmutableHashSet<string> diagnosticIds = null, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var diagnostics = await GetDiagnosticsForIdsAsync(solution, projectId, null, diagnosticIds, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
            return diagnostics.Where(d => d.DocumentId == null).ToImmutableArray();
        }
    }
}