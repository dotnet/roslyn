// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class IDiagnosticServiceExtensions
    {
        public static ValueTask<ImmutableArray<DiagnosticData>> GetPullDiagnosticsAsync(this IDiagnosticService service, DiagnosticBucket bucket, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
            => service.GetPullDiagnosticsAsync(bucket.Workspace, bucket.ProjectId, bucket.DocumentId, bucket.Id, includeSuppressedDiagnostics, cancellationToken);

        public static ValueTask<ImmutableArray<DiagnosticData>> GetPullDiagnosticsAsync(
            this IDiagnosticService service,
            Document document,
            bool includeSuppressedDiagnostics,
            CancellationToken cancellationToken)
        {
            return service.GetPullDiagnosticsAsync(document.Project.Solution.Workspace, document.Project.Id, document.Id, id: null, includeSuppressedDiagnostics, cancellationToken);
        }
    }
}
