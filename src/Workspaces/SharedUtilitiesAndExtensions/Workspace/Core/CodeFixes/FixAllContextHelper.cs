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
                        // For the 'ContainingMember' and 'ContainingType' scopes, we might be invoked in a couple of different ways:
                        //  1. Using the fully populated 'FixAllContext', which has already computed, non-empty 'fixAllSpans' for
                        //     the member or type declaration to fix in the given 'document'. For this scenario, we return the
                        //     diagnostics for the given fixAllSpans in the document for this scenario.
                        //  2. Using the original 'FixAllContext' which has the 'triggerSpan' and 'document' for the original code fix, but
                        //     the 'fixAllSpans' have not yet been computed. For this scenario, we use the 'IFixAllSpanMappingService' to
                        //     map the triggerSpan and fixAllScope to documents and spans to fix for the containing member or
                        //     type declaration (and its partials), and then return the diagnostics for each of the documents and fixAllSpans.

                        if (!fixAllSpans.IsEmpty)
                        {
                            var documentsAndSpans = SpecializedCollections.SingletonEnumerable(KeyValuePair.Create(document, fixAllSpans));
                            return await GetSpanDiagnosticsAsync(fixAllContext, documentsAndSpans).ConfigureAwait(false);
                        }
                        else if (triggerSpan.HasValue &&
                            document.GetLanguageService<IFixAllSpanMappingService>() is IFixAllSpanMappingService spanMappingService)
                        {
                            var documentsAndSpans = await spanMappingService.GetFixAllSpansAsync(document,
                                triggerSpan.Value, fixAllContext.Scope, fixAllContext.CancellationToken).ConfigureAwait(false);
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
                        if (!builder.TryGetValue(document, out var arrayBuilder))
                        {
                            arrayBuilder = ArrayBuilder<Diagnostic>.GetInstance();
                            builder.Add(document, arrayBuilder);
                        }

                        arrayBuilder.AddRange(documentDiagnostics);
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
                FixAllScope.ContainingMember => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_Containing_member, diagnosticId),
                FixAllScope.ContainingType => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_Containing_type, diagnosticId),
                _ => throw ExceptionUtilities.UnexpectedValue(fixAllScope),
            };
        }
    }
}
