// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct SolutionUpdate
    {
        public readonly ModuleUpdates ModuleUpdates;
        public readonly ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> NonRemappableRegions;
        public readonly ImmutableArray<ProjectBaseline> ProjectBaselines;
        public readonly ImmutableArray<ProjectDiagnostics> Diagnostics;
        public readonly ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> DocumentsWithRudeEdits;
        public readonly Diagnostic? SyntaxError;

        public SolutionUpdate(
            ModuleUpdates moduleUpdates,
            ImmutableArray<(Guid ModuleId, ImmutableArray<(ManagedModuleMethodId Method, NonRemappableRegion Region)>)> nonRemappableRegions,
            ImmutableArray<ProjectBaseline> projectBaselines,
            ImmutableArray<ProjectDiagnostics> diagnostics,
            ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> documentsWithRudeEdits,
            Diagnostic? syntaxError)
        {
            ModuleUpdates = moduleUpdates;
            NonRemappableRegions = nonRemappableRegions;
            ProjectBaselines = projectBaselines;
            Diagnostics = diagnostics;
            DocumentsWithRudeEdits = documentsWithRudeEdits;
            SyntaxError = syntaxError;
        }

        public static SolutionUpdate Blocked(
            ImmutableArray<ProjectDiagnostics> diagnostics,
            ImmutableArray<(DocumentId, ImmutableArray<RudeEditDiagnostic>)> documentsWithRudeEdits,
            Diagnostic? syntaxError,
            bool hasEmitErrors)
            => new(
                new(syntaxError != null || hasEmitErrors ? ModuleUpdateStatus.Blocked : ModuleUpdateStatus.RestartRequired, ImmutableArray<ManagedHotReloadUpdate>.Empty),
                ImmutableArray<(Guid, ImmutableArray<(ManagedModuleMethodId, NonRemappableRegion)>)>.Empty,
                ImmutableArray<ProjectBaseline>.Empty,
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

            if (Diagnostics.Length > 0)
            {
                var firstProjectDiagnostic = Diagnostics[0];

                log.Write("Solution update diagnostics: #{0} [{1}: {2}, ...]",
                    Diagnostics.Length,
                    firstProjectDiagnostic.ProjectId,
                    firstProjectDiagnostic.Diagnostics[0]);
            }

            if (DocumentsWithRudeEdits.Length > 0)
            {
                var firstDocumentWithRudeEdits = DocumentsWithRudeEdits[0];

                log.Write("Solution update documents with rude edits: #{0} [{1}: {2}, ...]",
                    DocumentsWithRudeEdits.Length,
                    firstDocumentWithRudeEdits.DocumentId,
                    firstDocumentWithRudeEdits.Diagnostics[0].Kind);
            }
        }
    }
}
