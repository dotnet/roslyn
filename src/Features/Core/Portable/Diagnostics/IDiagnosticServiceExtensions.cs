// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class IDiagnosticServiceExtensions
    {
        public static ImmutableArray<DiagnosticData> GetDiagnostics(this IDiagnosticService service, Document document, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            var project = document.Project;
            var workspace = project.Solution.Workspace;

            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var result);

            foreach (var arg in service.GetDiagnosticsUpdatedEventArgs(workspace, project.Id, document.Id, cancellationToken))
            {
                Contract.ThrowIfFalse(workspace.Equals(arg.Workspace));
                Contract.ThrowIfFalse(document.Id.Equals(arg.DocumentId));

                var diagnostics = service.GetDiagnostics(arg.Workspace, arg.ProjectId, arg.DocumentId, arg.Id, includeSuppressedDiagnostics, cancellationToken);
                if (diagnostics != null)
                    result.AddRange(diagnostics);
            }

            return result.ToImmutable();
        }
    }
}
