// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct EmitSolutionUpdateResults
    {
        [DataContract]
        internal readonly struct Data
        {
            [DataMember]
            public required ModuleUpdates ModuleUpdates { get; init; }

            [DataMember]
            public required ImmutableArray<DiagnosticData> Diagnostics { get; init; }

            [DataMember]
            public required ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> RudeEdits { get; init; }

            [DataMember]
            public required DiagnosticData? SyntaxError { get; init; }
        }

        public static readonly EmitSolutionUpdateResults Empty = new()
        {
            ModuleUpdates = new ModuleUpdates(ModuleUpdateStatus.None, ImmutableArray<ManagedHotReloadUpdate>.Empty),
            Diagnostics = ImmutableArray<ProjectDiagnostics>.Empty,
            RudeEdits = ImmutableArray<(DocumentId, ImmutableArray<RudeEditDiagnostic>)>.Empty,
            SyntaxError = null
        };

        public required ModuleUpdates ModuleUpdates { get; init; }
        public required ImmutableArray<ProjectDiagnostics> Diagnostics { get; init; }
        public required ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> RudeEdits { get; init; }
        public required Diagnostic? SyntaxError { get; init; }

        public Data Dehydrate(Solution solution)
            => new()
            {
                ModuleUpdates = ModuleUpdates,
                Diagnostics = Diagnostics.ToDiagnosticData(solution),
                RudeEdits = RudeEdits,
                SyntaxError = GetSyntaxErrorData(solution)
            };

        public DiagnosticData? GetSyntaxErrorData(Solution solution)
        {
            if (SyntaxError == null)
            {
                return null;
            }

            Debug.Assert(SyntaxError.Location.SourceTree != null);
            return DiagnosticData.Create(SyntaxError, solution.GetRequiredDocument(SyntaxError.Location.SourceTree));
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
            ImmutableArray<DiagnosticData> diagnosticData,
            ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> rudeEdits,
            DiagnosticData? syntaxError,
            ModuleUpdateStatus updateStatus,
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

                builder.Add(data.ToHotReloadDiagnostic(updateStatus));

                // only report first error
                break;
            }

            if (syntaxError != null)
            {
                Debug.Assert(syntaxError.DataLocation != null);
                Debug.Assert(syntaxError.Message != null);

                var fileSpan = syntaxError.DataLocation.MappedFileSpan;

                builder.Add(new ManagedHotReloadDiagnostic(
                    syntaxError.Id,
                    syntaxError.Message,
                    ManagedHotReloadDiagnosticSeverity.Error,
                    fileSpan.Path,
                    fileSpan.Span.ToSourceSpan()));
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
                        DiagnosticSeverity.Error => ManagedHotReloadDiagnosticSeverity.RestartRequired,
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
