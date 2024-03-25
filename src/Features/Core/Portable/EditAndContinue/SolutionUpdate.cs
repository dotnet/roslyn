// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal readonly struct SolutionUpdate(
    ModuleUpdates moduleUpdates,
    ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> nonRemappableRegions,
    ImmutableArray<ProjectBaseline> projectBaselines,
    ImmutableArray<ProjectDiagnostics> diagnostics,
    ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> documentsWithRudeEdits,
    Diagnostic? syntaxError)
{
    public readonly ModuleUpdates ModuleUpdates = moduleUpdates;
    public readonly ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> NonRemappableRegions = nonRemappableRegions;
    public readonly ImmutableArray<ProjectBaseline> ProjectBaselines = projectBaselines;
    public readonly ImmutableArray<ProjectDiagnostics> Diagnostics = diagnostics;
    public readonly ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> DocumentsWithRudeEdits = documentsWithRudeEdits;
    public readonly Diagnostic? SyntaxError = syntaxError;

    public static SolutionUpdate Blocked(
        ImmutableArray<ProjectDiagnostics> diagnostics,
        ImmutableArray<(DocumentId, ImmutableArray<RudeEditDiagnostic>)> documentsWithRudeEdits,
        Diagnostic? syntaxError,
        bool hasEmitErrors)
        => new(
            new(syntaxError != null || hasEmitErrors ? ModuleUpdateStatus.Blocked : ModuleUpdateStatus.RestartRequired, []),
            ImmutableArray<(Guid, ImmutableArray<(ManagedModuleMethodId, NonRemappableRegion)>)>.Empty,
            [],
            diagnostics,
            documentsWithRudeEdits,
            syntaxError);

    internal void Log(TraceLog log, UpdateId updateId)
    {
        log.Write("Solution update {0}.{1} status: {2}", updateId.SessionId.Ordinal, updateId.Ordinal, ModuleUpdates.Status);

        foreach (var moduleUpdate in ModuleUpdates.Updates)
        {
            log.Write("Module update: capabilities=[{0}], types=[{1}], methods=[{2}]",
                moduleUpdate.RequiredCapabilities,
                moduleUpdate.UpdatedTypes,
                moduleUpdate.UpdatedMethods);
        }

        foreach (var projectDiagnostics in Diagnostics)
        {
            foreach (var diagnostic in projectDiagnostics.Diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                {
                    log.Write("Project {0} update error: {1}", projectDiagnostics.ProjectId, diagnostic);
                }
            }
        }

        foreach (var documentWithRudeEdits in DocumentsWithRudeEdits)
        {
            foreach (var rudeEdit in documentWithRudeEdits.Diagnostics)
            {
                log.Write("Document {0} rude edit: {1} {2}", documentWithRudeEdits.DocumentId, rudeEdit.Kind, rudeEdit.SyntaxKind);
            }
        }
    }
}
