// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes;

internal static partial class FixAllContextHelper
{
    public static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
        FixAllContext fixAllContext)
    {
        var cancellationToken = fixAllContext.CancellationToken;

        var allDiagnostics = ImmutableArray<Diagnostic>.Empty;

        var document = fixAllContext.Document;
        var project = fixAllContext.Project;

        var progressTracker = fixAllContext.Progress;

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
#if WORKSPACE
                    var diagnosticSpan = fixAllContext.State.DiagnosticSpan;
                    if (diagnosticSpan.HasValue &&
                        document.GetLanguageService<IFixAllSpanMappingService>() is { } spanMappingService)
                    {
                        var documentsAndSpans = await spanMappingService.GetFixAllSpansAsync(document,
                            diagnosticSpan.Value, fixAllContext.Scope, fixAllContext.CancellationToken).ConfigureAwait(false);
                        return await GetSpanDiagnosticsAsync(fixAllContext, documentsAndSpans).ConfigureAwait(false);
                    }
#else
                    Debug.Fail("FixAllScope.ContainingMember and FixAllScope.ContainingType are not supported in CodeStyle layer");
#endif
                }

                break;

            case FixAllScope.Project:
                allDiagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                break;

            case FixAllScope.Solution:
                {
                    var projectsToFix = project.Solution.Projects
                        .WhereAsArray(p => p.Language == project.Language);

                    // Update the progress dialog with the count of projects to actually fix. We'll update the progress
                    // bar as we get all the documents in AddDocumentDiagnosticsAsync.

                    progressTracker.AddItems(projectsToFix.Length);

                    allDiagnostics = await ProducerConsumer<ImmutableArray<Diagnostic>>.RunParallelAsync(
                        source: projectsToFix,
                        produceItems: static async (projectToFix, callback, args, cancellationToken) =>
                        {
                            var (fixAllContext, progressTracker) = args;
                            using var _ = progressTracker.ItemCompletedScope();
                            callback(await fixAllContext.GetAllDiagnosticsAsync(projectToFix).ConfigureAwait(false));
                        },
                        consumeItems: static async (results, _1, cancellationToken) =>
                        {
                            using var _2 = ArrayBuilder<Diagnostic>.GetInstance(out var builder);

                            await foreach (var diagnostics in results.ConfigureAwait(false))
                                builder.AddRange(diagnostics);

                            return builder.ToImmutableAndClear();
                        },
                        args: (fixAllContext, progressTracker),
                        cancellationToken).ConfigureAwait(false);
                }

                break;
        }

        if (allDiagnostics.IsEmpty)
        {
            return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty;
        }

        return await GetDocumentDiagnosticsToFixAsync(
            fixAllContext.Solution, allDiagnostics, fixAllContext.CancellationToken).ConfigureAwait(false);

#if WORKSPACE
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
#endif
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
                builder.Add(document, [.. diagnosticsForDocument]);
            }
        }

        return builder.ToImmutable();
    }
}
