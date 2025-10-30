// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal readonly record struct ProjectDiagnostics(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostics);

internal static partial class Extensions
{
    public static ImmutableArray<DiagnosticData> ToDiagnosticData(this ImmutableArray<ProjectDiagnostics> diagnostics, Solution solution)
    {
        using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var result);

        foreach (var (projectId, projectDiagnostics) in diagnostics)
        {
            var project = solution.GetRequiredProject(projectId);

            foreach (var diagnostic in projectDiagnostics)
            {
                var document = solution.GetDocument(diagnostic.Location.SourceTree);
                var data = document != null ? DiagnosticData.Create(diagnostic, document) : DiagnosticData.Create(diagnostic, project);
                result.Add(data);
            }
        }

        return result.ToImmutableAndClear();
    }
}
