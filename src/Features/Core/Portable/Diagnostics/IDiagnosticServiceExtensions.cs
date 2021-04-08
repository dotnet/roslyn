﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class IDiagnosticServiceExtensions
    {
        public static ValueTask<ImmutableArray<DiagnosticData>> GetPullDiagnosticsAsync(this IDiagnosticService service, DiagnosticBucket bucket, bool includeSuppressedDiagnostics, Option2<DiagnosticMode> diagnosticMode, CancellationToken cancellationToken)
            => service.GetPullDiagnosticsAsync(bucket.Workspace, bucket.ProjectId, bucket.DocumentId, bucket.Id, includeSuppressedDiagnostics, diagnosticMode, cancellationToken);

        public static ValueTask<ImmutableArray<DiagnosticData>> GetPushDiagnosticsAsync(this IDiagnosticService service, DiagnosticBucket bucket, bool includeSuppressedDiagnostics, Option2<DiagnosticMode> diagnosticMode, CancellationToken cancellationToken)
            => service.GetPushDiagnosticsAsync(bucket.Workspace, bucket.ProjectId, bucket.DocumentId, bucket.Id, includeSuppressedDiagnostics, diagnosticMode, cancellationToken);

        public static ValueTask<ImmutableArray<DiagnosticData>> GetPushDiagnosticsAsync(
            this IDiagnosticService service,
            Document document,
            bool includeSuppressedDiagnostics,
            Option2<DiagnosticMode> diagnosticMode,
            CancellationToken cancellationToken)
        {
            return GetDiagnosticsAsync(service, document, includeSuppressedDiagnostics, forPullDiagnostics: false, diagnosticMode, cancellationToken);
        }

        public static ValueTask<ImmutableArray<DiagnosticData>> GetPullDiagnosticsAsync(
            this IDiagnosticService service,
            Document document,
            bool includeSuppressedDiagnostics,
            Option2<DiagnosticMode> diagnosticMode,
            CancellationToken cancellationToken)
        {
            return GetDiagnosticsAsync(service, document, includeSuppressedDiagnostics, forPullDiagnostics: true, diagnosticMode, cancellationToken);
        }

        public static async ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            this IDiagnosticService service,
            Document document,
            bool includeSuppressedDiagnostics,
            bool forPullDiagnostics,
            Option2<DiagnosticMode> diagnosticMode,
            CancellationToken cancellationToken)
        {
            var project = document.Project;
            var workspace = project.Solution.Workspace;

            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var result);

            var buckets = forPullDiagnostics
                ? service.GetPullDiagnosticBuckets(workspace, project.Id, document.Id, diagnosticMode, cancellationToken)
                : service.GetPushDiagnosticBuckets(workspace, project.Id, document.Id, diagnosticMode, cancellationToken);

            foreach (var bucket in buckets)
            {
                Contract.ThrowIfFalse(workspace.Equals(bucket.Workspace));
                Contract.ThrowIfFalse(document.Id.Equals(bucket.DocumentId));

                var diagnostics = forPullDiagnostics
                    ? await service.GetPullDiagnosticsAsync(bucket, includeSuppressedDiagnostics, diagnosticMode, cancellationToken).ConfigureAwait(false)
                    : await service.GetPushDiagnosticsAsync(bucket, includeSuppressedDiagnostics, diagnosticMode, cancellationToken).ConfigureAwait(false);
                result.AddRange(diagnostics);
            }

            return result.ToImmutable();
        }
    }
}
