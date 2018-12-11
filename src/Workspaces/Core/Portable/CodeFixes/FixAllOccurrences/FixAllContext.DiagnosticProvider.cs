// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
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
            internal virtual bool IsFixMultiple => false;

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

            internal async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
                FixAllContext fixAllContext)
            {
                var result = await GetDocumentDiagnosticsToFixWorkerAsync(fixAllContext).ConfigureAwait(false);

                // Filter out any documents that we don't have any diagnostics for.
                return result.Where(kvp => !kvp.Value.IsDefaultOrEmpty).ToImmutableDictionary();
            }

            internal virtual async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixWorkerAsync(
                FixAllContext fixAllContext)
            {
                using (Logger.LogBlock(
                    FunctionId.CodeFixes_FixAllOccurrencesComputation_Document_Diagnostics,
                    FixAllLogger.CreateCorrelationLogMessage(fixAllContext.State.CorrelationId),
                    fixAllContext.CancellationToken))
                {
                    return await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(
                        fixAllContext,
                        fixAllContext.ProgressTracker,
                        (document, cancellationToken) => document.IsGeneratedCode(cancellationToken)).ConfigureAwait(false);
                }
            }

            internal virtual async Task<ImmutableDictionary<Project, ImmutableArray<Diagnostic>>> GetProjectDiagnosticsToFixAsync(
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
                                var kvp = SpecializedCollections.SingletonEnumerable(KeyValuePairUtil.Create(project, diagnostics));
                                return ImmutableDictionary.CreateRange(kvp);

                            case FixAllScope.Solution:
                                var projectsAndDiagnostics = ImmutableDictionary.CreateBuilder<Project, ImmutableArray<Diagnostic>>();

                                var tasks = project.Solution.Projects.Select(async p => new
                                {
                                    Project = p,
                                    Diagnostics = await fixAllContext.GetProjectDiagnosticsAsync(p).ConfigureAwait(false)
                                }).ToArray();

                                await Task.WhenAll(tasks).ConfigureAwait(false);

                                foreach (var task in tasks)
                                {
                                    var projectAndDiagnostics = await task.ConfigureAwait(false);
                                    if (projectAndDiagnostics.Diagnostics.Any())
                                    {
                                        projectsAndDiagnostics[projectAndDiagnostics.Project] = projectAndDiagnostics.Diagnostics;
                                    }
                                }

                                return projectsAndDiagnostics.ToImmutable();
                        }
                    }

                    return ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty;
                }
            }
        }
    }
}
