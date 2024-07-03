// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes;

/// <summary>
/// Context for "Fix all occurrences" code fixes provided by a <see cref="FixAllProvider"/>.
/// </summary>
public partial class FixAllContext
{
    /// <summary>
    /// Diagnostic provider to fetch document/project diagnostics to fix in a <see cref="FixAllContext"/>.
    /// </summary>
    public abstract class DiagnosticProvider
    {
        /// <summary>
        /// Gets all the diagnostics to fix in the given document in a <see cref="FixAllContext"/>.
        /// </summary>
        public abstract Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all the project-level diagnostics to fix, i.e. diagnostics with no source location, in the given project in a <see cref="FixAllContext"/>.
        /// </summary>
        public abstract Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all the diagnostics to fix in the given project in a <see cref="FixAllContext"/>.
        /// This includes both document-level diagnostics for all documents in the given project and project-level diagnostics, i.e. diagnostics with no source location, in the given project. 
        /// </summary>
        public abstract Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken);

        internal static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(FixAllContext fixAllContext)
        {
            var result = await GetDocumentDiagnosticsToFixWorkerAsync(fixAllContext).ConfigureAwait(false);

            // Filter out any documents that we don't have any diagnostics for.
            return result.Where(kvp => !kvp.Value.IsDefaultOrEmpty).ToImmutableDictionary();

            static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixWorkerAsync(FixAllContext fixAllContext)
            {
                if (fixAllContext.State.DiagnosticProvider is FixAllState.FixMultipleDiagnosticProvider fixMultipleDiagnosticProvider)
                {
                    return fixMultipleDiagnosticProvider.DocumentDiagnosticsMap;
                }

                using (Logger.LogBlock(
                        FunctionId.CodeFixes_FixAllOccurrencesComputation_Document_Diagnostics,
                        FixAllLogger.CreateCorrelationLogMessage(fixAllContext.State.CorrelationId),
                        fixAllContext.CancellationToken))
                {
                    return await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);
                }
            }
        }

        internal static async Task<ImmutableDictionary<Project, ImmutableArray<Diagnostic>>> GetProjectDiagnosticsToFixAsync(
            FixAllContext fixAllContext)
        {
            using (Logger.LogBlock(
                FunctionId.CodeFixes_FixAllOccurrencesComputation_Project_Diagnostics,
                FixAllLogger.CreateCorrelationLogMessage(fixAllContext.State.CorrelationId),
                fixAllContext.CancellationToken))
            {
                var project = fixAllContext.Project;
                if (project != null)
                {
                    switch (fixAllContext.Scope)
                    {
                        case FixAllScope.Project:
                            var diagnostics = await fixAllContext.GetProjectDiagnosticsAsync(project).ConfigureAwait(false);
                            return ImmutableDictionary.CreateRange([KeyValuePairUtil.Create(project, diagnostics)]);

                        case FixAllScope.Solution:
                            return await ProducerConsumer<(Project project, ImmutableArray<Diagnostic> diagnostics)>.RunParallelAsync(
                                source: project.Solution.Projects,
                                produceItems: static async (project, callback, fixAllContext, cancellationToken) =>
                                {
                                    var diagnostics = await fixAllContext.GetProjectDiagnosticsAsync(project).ConfigureAwait(false);
                                    callback((project, diagnostics));
                                },
                                consumeItems: static async (results, args, cancellationToken) =>
                                {
                                    var projectsAndDiagnostics = ImmutableDictionary.CreateBuilder<Project, ImmutableArray<Diagnostic>>();

                                    await foreach (var (project, diagnostics) in results)
                                    {
                                        if (diagnostics.Any())
                                            projectsAndDiagnostics.Add(project, diagnostics);
                                    }

                                    return projectsAndDiagnostics.ToImmutable();
                                },
                                args: fixAllContext,
                                fixAllContext.CancellationToken).ConfigureAwait(false);
                    }
                }

                return ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty;
            }
        }
    }
}
