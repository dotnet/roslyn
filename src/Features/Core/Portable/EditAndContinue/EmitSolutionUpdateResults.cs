// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct EmitSolutionUpdateResults
    {
        public static readonly EmitSolutionUpdateResults Empty =
            new(new ManagedModuleUpdates(ManagedModuleUpdateStatus.None, ImmutableArray<ManagedModuleUpdate>.Empty),
                ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostic)>.Empty);

        public readonly ManagedModuleUpdates ModuleUpdates;
        public readonly ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostic)> Diagnostics;

        public EmitSolutionUpdateResults(ManagedModuleUpdates moduleUpdates, ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostic)> diagnostics)
        {
            ModuleUpdates = moduleUpdates;
            Diagnostics = diagnostics;
        }

        public ImmutableArray<DiagnosticData> GetDiagnosticData(Solution solution)
        {
            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var result);

            foreach (var (projectId, diagnostics) in Diagnostics)
            {
                var project = solution.GetRequiredProject(projectId);

                foreach (var diagnostic in diagnostics)
                {
                    var document = solution.GetDocument(diagnostic.Location.SourceTree);
                    var data = (document != null) ? DiagnosticData.Create(diagnostic, document) : DiagnosticData.Create(diagnostic, project);
                    result.Add(data);
                }
            }

            return result.ToImmutable();
        }
    }
}
