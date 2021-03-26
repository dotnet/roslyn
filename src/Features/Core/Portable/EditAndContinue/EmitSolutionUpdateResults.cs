// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct EmitSolutionUpdateResults
    {
        public static readonly EmitSolutionUpdateResults Empty =
            new(new ManagedModuleUpdates(ManagedModuleUpdateStatus.None, ImmutableArray<ManagedModuleUpdate>.Empty),
                ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostic)>.Empty,
                ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)>.Empty);

        public readonly ManagedModuleUpdates ModuleUpdates;
        public readonly ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostic)> Diagnostics;
        public readonly ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> DocumentsWithRudeEdits;

        public EmitSolutionUpdateResults(
            ManagedModuleUpdates moduleUpdates,
            ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostic)> diagnostics,
            ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> documentsWithRudeEdits)
        {
            ModuleUpdates = moduleUpdates;
            Diagnostics = diagnostics;
            DocumentsWithRudeEdits = documentsWithRudeEdits;
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

        public async Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync(Solution solution, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var diagnostics);

            // add rude edits:
            foreach (var (documentId, documentRudeEdits) in DocumentsWithRudeEdits)
            {
                var document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfNull(document);

                var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                foreach (var documentRudeEdit in documentRudeEdits)
                {
                    diagnostics.Add(documentRudeEdit.ToDiagnostic(tree));
                }
            }

            // add emit diagnostics:
            foreach (var (_, projectEmitDiagnostics) in Diagnostics)
            {
                diagnostics.AddRange(projectEmitDiagnostics);
            }

            return diagnostics.ToImmutable();
        }
    }
}
