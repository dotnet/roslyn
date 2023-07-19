// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal static partial class FixAllContextHelper
    {
        public static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
            FixAllContext fixAllContext)
        {
            var cancellationToken = fixAllContext.CancellationToken;

            var allDiagnostics = ImmutableArray<Diagnostic>.Empty;

            var document = fixAllContext.Document;
            var project = fixAllContext.Project;

            var progressTracker = fixAllContext.GetProgressTracker();

            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    // Note: We avoid fixing diagnostics in generated code.
                    if (document != null && !await document.IsGeneratedCodeAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var documentDiagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                        return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty.SetItem(document, documentDiagnostics);
                    }

                    break;

                case FixAllScope.ContainingMember or FixAllScope.ContainingType:
                    // Note: We avoid fixing diagnostics in generated code.
                    if (document != null && !await document.IsGeneratedCodeAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var diagnosticSpan = fixAllContext.State.DiagnosticSpan;
                        if (diagnosticSpan.HasValue &&
                            document.GetLanguageService<IFixAllSpanMappingService>() is { } spanMappingService)
                        {
                            var documentsAndSpans = await spanMappingService.GetFixAllSpansAsync(document,
                                diagnosticSpan.Value, fixAllContext.Scope, fixAllContext.CancellationToken).ConfigureAwait(false);
                            return await GetSpanDiagnosticsAsync(fixAllContext, documentsAndSpans).ConfigureAwait(false);
                        }
                    }

                    break;

                case FixAllScope.Project:
                    allDiagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                    break;

                case FixAllScope.Solution:
                    var projectsToFix = project.Solution.Projects
                        .Where(p => p.Language == project.Language)
                        .ToImmutableArray();

                    // Update the progress dialog with the count of projects to actually fix. We'll update the progress
                    // bar as we get all the documents in AddDocumentDiagnosticsAsync.

                    progressTracker.AddItems(projectsToFix.Length);

                    var diagnostics = new ConcurrentDictionary<ProjectId, ImmutableArray<Diagnostic>>();
                    using (var _ = ArrayBuilder<Task>.GetInstance(projectsToFix.Length, out var tasks))
                    {
                        foreach (var projectToFix in projectsToFix)
                            tasks.Add(Task.Run(async () => await AddDocumentDiagnosticsAsync(diagnostics, projectToFix).ConfigureAwait(false), cancellationToken));

                        await Task.WhenAll(tasks).ConfigureAwait(false);
                        allDiagnostics = allDiagnostics.AddRange(diagnostics.SelectMany(i => i.Value));
                    }

                    break;
            }

            if (allDiagnostics.IsEmpty)
            {
                return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty;
            }

            return await GetDocumentDiagnosticsToFixAsync(
                fixAllContext.Solution, allDiagnostics, fixAllContext.CancellationToken).ConfigureAwait(false);

            async Task AddDocumentDiagnosticsAsync(ConcurrentDictionary<ProjectId, ImmutableArray<Diagnostic>> diagnostics, Project projectToFix)
            {
                try
                {
                    var projectDiagnostics = await fixAllContext.GetAllDiagnosticsAsync(projectToFix).ConfigureAwait(false);
                    diagnostics.TryAdd(projectToFix.Id, projectDiagnostics);
                }
                finally
                {
                    progressTracker.ItemCompleted();
                }
            }

            static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetSpanDiagnosticsAsync(
                FixAllContext fixAllContext,
                IEnumerable<KeyValuePair<Document, ImmutableArray<TextSpan>>> documentsAndSpans)
            {
                var builder = PooledDictionary<Document, ArrayBuilder<Diagnostic>>.GetInstance();
                foreach (var (document, spans) in documentsAndSpans)
                {
                    foreach (var span in spans)
                    {
                        var documentDiagnostics = await fixAllContext.GetDocumentSpanDiagnosticsAsync(document, span).ConfigureAwait(false);
                        builder.MultiAddRange(document, documentDiagnostics);
                    }
                }

                return builder.ToImmutableMultiDictionaryAndFree();
            }
        }

        private static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
            Solution solution,
            ImmutableArray<Diagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            var builder = ImmutableDictionary.CreateBuilder<Document, ImmutableArray<Diagnostic>>();

            // NOTE: We use 'GetTextDocumentForLocation' extension to ensure we also handle external location diagnostics in non-C#/VB languages.
            foreach (var (textDocument, diagnosticsForDocument) in diagnostics.GroupBy(d => solution.GetTextDocumentForLocation(d.Location)))
            {
                if (textDocument is not Document document)
                    continue;

                cancellationToken.ThrowIfCancellationRequested();
                if (!await document.IsGeneratedCodeAsync(cancellationToken).ConfigureAwait(false))
                {
                    builder.Add(document, diagnosticsForDocument.ToImmutableArray());
                }
            }

            return builder.ToImmutable();
        }
    }
}
