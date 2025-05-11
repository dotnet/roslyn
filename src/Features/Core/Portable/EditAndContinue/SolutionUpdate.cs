// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal readonly struct SolutionUpdate(
    ModuleUpdates moduleUpdates,
    ImmutableArray<ProjectId> projectsToStale,
    ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> nonRemappableRegions,
    ImmutableArray<ProjectBaseline> projectBaselines,
    ImmutableArray<ProjectDiagnostics> diagnostics,
    ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> documentsWithRudeEdits,
    Diagnostic? syntaxError)
{
    public readonly ModuleUpdates ModuleUpdates = moduleUpdates;
    public readonly ImmutableArray<ProjectId> ProjectsToStale = projectsToStale;
    public readonly ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> NonRemappableRegions = nonRemappableRegions;
    public readonly ImmutableArray<ProjectBaseline> ProjectBaselines = projectBaselines;
    public readonly ImmutableArray<ProjectDiagnostics> Diagnostics = diagnostics;
    public readonly ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> DocumentsWithRudeEdits = documentsWithRudeEdits;
    public readonly Diagnostic? SyntaxError = syntaxError;

    public static SolutionUpdate Empty(
        ImmutableArray<ProjectDiagnostics> diagnostics,
        ImmutableArray<(DocumentId, ImmutableArray<RudeEditDiagnostic>)> documentsWithRudeEdits,
        Diagnostic? syntaxError,
        ModuleUpdateStatus status)
        => new(
            new(status, Updates: []),
            projectsToStale: [],
            nonRemappableRegions: [],
            projectBaselines: [],
            diagnostics,
            documentsWithRudeEdits,
            syntaxError);

    internal void Log(TraceLog log, UpdateId updateId)
    {
        log.Write($"Solution update {updateId} status: {ModuleUpdates.Status}");

        foreach (var moduleUpdate in ModuleUpdates.Updates)
        {
            log.Write("Module update: " +
                $"capabilities=[{string.Join(",", moduleUpdate.RequiredCapabilities)}], " +
                $"types=[{string.Join(",", moduleUpdate.UpdatedTypes.Select(token => token.ToString("X8")))}], " +
                $"methods=[{string.Join(",", moduleUpdate.UpdatedMethods.Select(token => token.ToString("X8")))}]");
        }

        foreach (var projectDiagnostics in Diagnostics)
        {
            foreach (var diagnostic in projectDiagnostics.Diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    log.Write($"Project {projectDiagnostics.ProjectId.DebugName} update error: {diagnostic}", LogMessageSeverity.Error);
                }
            }
        }

        foreach (var documentWithRudeEdits in DocumentsWithRudeEdits)
        {
            foreach (var rudeEdit in documentWithRudeEdits.Diagnostics)
            {
                log.Write($"Document {documentWithRudeEdits.DocumentId.DebugName} rude edit: {rudeEdit.Kind} {rudeEdit.SyntaxKind}", LogMessageSeverity.Error);
            }
        }
    }
}
