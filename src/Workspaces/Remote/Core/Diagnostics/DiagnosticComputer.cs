// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    internal class DiagnosticComputer
    {
        public async Task<string> GetDiagnosticsAsync(SolutionSnapshotId solutionSnapshotId, ProjectId projectId, CancellationToken cancellationToken)
        {
            var compilation = await RoslynServices.CompilationService.GetCompilationAsync(solutionSnapshotId, projectId, cancellationToken).ConfigureAwait(false);

            // TODO: from here to down is just test code
            var diagnostics = compilation.GetDiagnostics(cancellationToken);
            return string.Join("|", diagnostics.Select(d => d.GetMessage()));
        }
    }
}
