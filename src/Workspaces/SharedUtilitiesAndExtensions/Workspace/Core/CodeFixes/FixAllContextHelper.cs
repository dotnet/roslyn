// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal static class FixAllContextExtensions
    {
        public static IProgressTracker GetProgressTracker(this FixAllContext context)
        {
#if CODE_STYLE
            return NoOpProgressTracker.Instance;
#else
            return context.ProgressTracker;
#endif
        }
    }

    internal static class FixAllContextHelper
    {
        public static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
            FixAllContext fixAllContext, TextSpan? triggerSpan, ImmutableArray<TextSpan> fixAllSpans)
        {
            var cancellationToken = fixAllContext.CancellationToken;

            var allDiagnostics = ImmutableArray<Diagnostic>.Empty;

            var document = fixAllContext.Document;
            var project = fixAllContext.Project;

            var progressTracker = fixAllContext.GetProgressTracker();

            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    if (document != null && !await document.IsGeneratedCodeAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var documentDiagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                        return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty.SetItem(document, documentDiagnostics);
                    }

                    break;

                case FixAllScope.ContainingMember or FixAllScope.ContainingType:
                    if (document != null && !await document.IsGeneratedCodeAsync(cancellationToken).ConfigureAwait(false))
                    {
                        if (!fixAllSpans.IsEmpty)
                        {
                            // We are processing a span-based FixAllContext created for a partial declaration.
                            var documentDiagnostics = ImmutableArray<Diagnostic>.Empty;
                            foreach (var fixAllSpan in fixAllSpans)
                            {
                                documentDiagnostics = documentDiagnostics.AddRange(await fixAllContext.GetDocumentSpanDiagnosticsAsync(
                                    document, fixAllSpan).ConfigureAwait(false));
                            }

                            return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty.SetItem(document, documentDiagnostics);
                        }
                        else if (triggerSpan.HasValue &&
                            document.GetLanguageService<IFixAllSpanMappingService>() is IFixAllSpanMappingService spanMappingService)
                        {
                            // We need to compute diagnostics for each of the containing member/type and its partial declarations
                            // using the trigger span.
                            var documentsAndSpans = await spanMappingService.GetFixAllSpansAsync(document,
                                triggerSpan.Value, fixAllContext.Scope, fixAllContext.CancellationToken).ConfigureAwait(false);
                            var unused = PooledDictionary<Document, ImmutableArray<Diagnostic>>.GetInstance(out var builder);
                            foreach (var (documentToFix, spans) in documentsAndSpans)
                            {
                                foreach (var span in spans)
                                {
                                    var documentDiagnostics = await fixAllContext.GetDocumentSpanDiagnosticsAsync(documentToFix, span).ConfigureAwait(false);
                                    if (builder.TryGetValue(documentToFix, out var existingDiagnostics))
                                    {
                                        documentDiagnostics = existingDiagnostics.AddRange(documentDiagnostics);
                                    }

                                    builder[documentToFix] = documentDiagnostics;
                                }
                            }

                            return builder.ToImmutableDictionary();
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
        }

        private static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
            Solution solution,
            ImmutableArray<Diagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            var builder = ImmutableDictionary.CreateBuilder<Document, ImmutableArray<Diagnostic>>();
            foreach (var (document, diagnosticsForDocument) in diagnostics.GroupBy(d => solution.GetDocument(d.Location.SourceTree)))
            {
                if (document is null)
                    continue;

                cancellationToken.ThrowIfCancellationRequested();
                if (!await document.IsGeneratedCodeAsync(cancellationToken).ConfigureAwait(false))
                {
                    builder.Add(document, diagnosticsForDocument.ToImmutableArray());
                }
            }

            return builder.ToImmutable();
        }

        public static string GetDefaultFixAllTitle(FixAllContext fixAllContext)
            => GetDefaultFixAllTitle(fixAllContext.Scope, fixAllContext.DiagnosticIds, fixAllContext.Document, fixAllContext.Project);

        public static string GetDefaultFixAllTitle(
            FixAllScope fixAllScope,
            ImmutableHashSet<string> diagnosticIds,
            Document? triggerDocument,
            Project triggerProject)
        {
            var diagnosticId = diagnosticIds.First();

            return fixAllScope switch
            {
                FixAllScope.Custom => string.Format(WorkspaceExtensionsResources.Fix_all_0, diagnosticId),
                FixAllScope.Document => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_1, diagnosticId, triggerDocument!.Name),
                FixAllScope.Project => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_1, diagnosticId, triggerProject.Name),
                FixAllScope.Solution => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_Solution, diagnosticId),
                FixAllScope.ContainingMember => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_Containing_Member, diagnosticId),
                FixAllScope.ContainingType => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_Containing_Type, diagnosticId),
                _ => throw ExceptionUtilities.UnexpectedValue(fixAllScope),
            };
        }
    }
}
