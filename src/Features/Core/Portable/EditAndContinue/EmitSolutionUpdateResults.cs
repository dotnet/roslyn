﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct EmitSolutionUpdateResults
    {
        [DataContract]
        internal readonly struct Data
        {
            [DataMember(Order = 0)]
            public readonly ManagedModuleUpdates ModuleUpdates;

            [DataMember(Order = 1)]
            public readonly ImmutableArray<DiagnosticData> Diagnostics;

            [DataMember(Order = 2)]
            public readonly ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> RudeEdits;

            public Data(
                ManagedModuleUpdates moduleUpdates,
                ImmutableArray<DiagnosticData> diagnostics,
                ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> rudeEdits)
            {
                ModuleUpdates = moduleUpdates;
                Diagnostics = diagnostics;
                RudeEdits = rudeEdits;
            }
        }

        public static readonly EmitSolutionUpdateResults Empty =
            new(new ManagedModuleUpdates(ManagedModuleUpdateStatus.None, ImmutableArray<ManagedModuleUpdate>.Empty),
                ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostic)>.Empty,
                ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)>.Empty);

        public readonly ManagedModuleUpdates ModuleUpdates;
        public readonly ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostics)> Diagnostics;
        public readonly ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> RudeEdits;

        public EmitSolutionUpdateResults(
            ManagedModuleUpdates moduleUpdates,
            ImmutableArray<(ProjectId ProjectId, ImmutableArray<Diagnostic> Diagnostic)> diagnostics,
            ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> documentsWithRudeEdits)
        {
            ModuleUpdates = moduleUpdates;
            Diagnostics = diagnostics;
            RudeEdits = documentsWithRudeEdits;
        }

        public Data Dehydrate(Solution solution)
            => new(ModuleUpdates, GetDiagnosticData(solution), RudeEdits);

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
            foreach (var (documentId, documentRudeEdits) in RudeEdits)
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

        internal static async ValueTask<ImmutableArray<ManagedHotReloadDiagnostic>> GetHotReloadDiagnosticsAsync(
            Solution solution,
            ImmutableArray<DiagnosticData> diagnosticData, ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> rudeEdits,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<ManagedHotReloadDiagnostic>.GetInstance(out var builder);

            // Add the first compiler emit error. Do not report warnings - they do not block applying the edit.
            // It's unnecessary to report more then one error since all the diagnostics are already reported in the Error List
            // and this is just messaging to the agent.

            foreach (var data in diagnosticData)
            {
                if (data.Severity != DiagnosticSeverity.Error)
                {
                    continue;
                }

                var fileSpan = data.DataLocation?.GetFileLinePositionSpan();

                builder.Add(new ManagedHotReloadDiagnostic(
                    data.Id,
                    data.Message ?? FeaturesResources.Unknown_error_occurred,
                    ManagedHotReloadDiagnosticSeverity.Error,
                    fileSpan?.Path ?? "",
                    fileSpan?.Span.ToSourceSpan() ?? default));

                // only report first error
                break;
            }

            // Report all rude edits.

            foreach (var (documentId, diagnostics) in rudeEdits)
            {
                var document = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                foreach (var diagnostic in diagnostics)
                {
                    var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(diagnostic.Kind);

                    var severity = descriptor.DefaultSeverity switch
                    {
                        DiagnosticSeverity.Error => ManagedHotReloadDiagnosticSeverity.Error,
                        DiagnosticSeverity.Warning => ManagedHotReloadDiagnosticSeverity.Warning,
                        _ => throw ExceptionUtilities.UnexpectedValue(descriptor.DefaultSeverity)
                    };

                    var fileSpan = tree.GetMappedLineSpan(diagnostic.Span, cancellationToken);

                    builder.Add(new ManagedHotReloadDiagnostic(
                        descriptor.Id,
                        string.Format(descriptor.MessageFormat.ToString(CultureInfo.CurrentUICulture), diagnostic.Arguments),
                        severity,
                        fileSpan.Path ?? "",
                        fileSpan.Span.ToSourceSpan()));
                }
            }

            return builder.ToImmutable();
        }
    }
}
