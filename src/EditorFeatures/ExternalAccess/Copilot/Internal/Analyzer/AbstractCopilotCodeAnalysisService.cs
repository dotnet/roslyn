// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Analyzer;

/// <summary>
/// Copilot code analysis service that coordinates triggering Copilot code analysis
/// in the background for a given document.
/// This service caches the computed Copilot suggestion diagnostics by method body to ensure that
/// we do not perform duplicate analysis calls.
/// Additionally, it performs all the option checks and Copilot service availability checks
/// to determine if we should skip analysis or not.
/// </summary>
internal abstract class AbstractCopilotCodeAnalysisService(IDiagnosticsRefresher diagnosticsRefresher) : ICopilotCodeAnalysisService
{
    // The _diagnosticsCache is a cache for computed diagnostics via `AnalyzeDocumentAsync`.
    // Each document maps to a dictionary, which in tern maps a prompt title to a list of existing Diagnostics and a boolean flag.
    // The list of diagnostics represents the diagnostics computed for the document under the given prompt title,
    // the boolean flag indicates whether the diagnostics result is for the entire document.
    // This cache is used to avoid duplicate analysis calls by storing the computed diagnostics for each document and prompt title.
    private readonly ConditionalWeakTable<Document, ConcurrentDictionary<string, (ImmutableArray<Diagnostic> Diagnostics, bool IsCompleteResult)>> _diagnosticsCache = new();

    protected abstract Task<bool> IsAvailableCoreAsync(CancellationToken cancellationToken);
    protected abstract Task<ImmutableArray<string>> GetAvailablePromptTitlesCoreAsync(Document document, CancellationToken cancellationToken);
    protected abstract Task<ImmutableArray<Diagnostic>> AnalyzeDocumentCoreAsync(Document document, TextSpan? span, string promptTitle, CancellationToken cancellationToken);
    protected abstract Task<ImmutableArray<Diagnostic>> GetCachedDiagnosticsCoreAsync(Document document, string promptTitle, CancellationToken cancellationToken);
    protected abstract Task StartRefinementSessionCoreAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken);
    protected abstract Task<string> GetOnTheFlyDocsCoreAsync(string symbolSignature, ImmutableArray<string> declarationCode, string language, CancellationToken cancellationToken);
    protected abstract Task<bool> IsFileExcludedCoreAsync(string filePath, CancellationToken cancellationToken);

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        => IsAvailableCoreAsync(cancellationToken);

    public async Task<ImmutableArray<string>> GetAvailablePromptTitlesAsync(Document document, CancellationToken cancellationToken)
    {
        if (document.GetLanguageService<ICopilotOptionsService>() is not { } service)
            return [];

        if (!await service.IsCodeAnalysisOptionEnabledAsync().ConfigureAwait(false))
            return [];

        return await GetAvailablePromptTitlesCoreAsync(document, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> ShouldSkipAnalysisAsync(Document document, CancellationToken cancellationToken)
    {
        if (document.GetLanguageService<ICopilotOptionsService>() is not { } service)
            return true;

        if (!await service.IsCodeAnalysisOptionEnabledAsync().ConfigureAwait(false))
            return true;

        if (await document.IsGeneratedCodeAsync(cancellationToken).ConfigureAwait(false))
            return true;

        return false;
    }

    public async Task AnalyzeDocumentAsync(Document document, TextSpan? span, string promptTitle, CancellationToken cancellationToken)
    {
        if (await ShouldSkipAnalysisAsync(document, cancellationToken).ConfigureAwait(false))
            return;

        if (FullDocumentDiagnosticsCached(document, promptTitle))
            return;

        if (!await IsAvailableAsync(cancellationToken).ConfigureAwait(false))
            return;

        var isFullDocumentAnalysis = !span.HasValue;
        var diagnostics = await AnalyzeDocumentCoreAsync(document, span, promptTitle, cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        CacheAndRefreshDiagnosticsIfNeeded(document, promptTitle, diagnostics, isFullDocumentAnalysis);
    }

    private bool FullDocumentDiagnosticsCached(Document document, string promptTitle)
        => TryGetDiagnosticsFromCache(document, promptTitle, out _, out var isCompleteResult) && isCompleteResult;

    private bool TryGetDiagnosticsFromCache(Document document, string promptTitle, out ImmutableArray<Diagnostic> diagnostics, out bool isCompleteResult)
    {
        if (_diagnosticsCache.TryGetValue(document, out var existingDiagnosticsMap)
            && existingDiagnosticsMap.TryGetValue(promptTitle, out var value))
        {
            diagnostics = value.Diagnostics;
            isCompleteResult = value.IsCompleteResult;
            return true;
        }

        diagnostics = [];
        isCompleteResult = false;
        return false;
    }

    private void CacheAndRefreshDiagnosticsIfNeeded(Document document, string promptTitle, ImmutableArray<Diagnostic> diagnostics, bool isCompleteResult)
    {
        lock (_diagnosticsCache)
        {
            // Nothing to be updated if we have already cached complete diagnostic result.
            if (FullDocumentDiagnosticsCached(document, promptTitle))
                return;

            // No cancellation from here.
            var diagnosticsMap = _diagnosticsCache.GetOrCreateValue(document);
            diagnosticsMap[promptTitle] = (diagnostics, isCompleteResult);
        }

        // For LSP pull diagnostics, request a diagnostic workspace refresh.
        // We will include the cached copilot diagnostics from this service as part of that pull request.
        diagnosticsRefresher.RequestWorkspaceRefresh();
    }

    public async Task<ImmutableArray<Diagnostic>> GetCachedDocumentDiagnosticsAsync(Document document, TextSpan? span, ImmutableArray<string> promptTitles, CancellationToken cancellationToken)
    {
        if (await ShouldSkipAnalysisAsync(document, cancellationToken).ConfigureAwait(false))
            return [];

        using var _1 = ArrayBuilder<Diagnostic>.GetInstance(out var diagnostics);

        foreach (var promptTitle in promptTitles)
        {
            // First, we try to fetch the diagnostics from our local cache.
            // If we haven't cached the diagnostics locally, then we fetch the cached diagnostics
            // from the core copilot analyzer. Subsequently, we update our local cache to store
            // these diagnostics so that future diagnostic requests can be served quickly.
            // We also raise diagnostic refresh requests for all our diagnostic clients when
            // updating our local diagnostics cache.

            if (TryGetDiagnosticsFromCache(document, promptTitle, out var existingDiagnostics, out _))
            {
                diagnostics.AddRange(existingDiagnostics);
            }
            else
            {
                var cachedDiagnostics = await GetCachedDiagnosticsCoreAsync(document, promptTitle, cancellationToken).ConfigureAwait(false);
                diagnostics.AddRange(cachedDiagnostics);
                CacheAndRefreshDiagnosticsIfNeeded(document, promptTitle, cachedDiagnostics, isCompleteResult: false);
            }
        }

        if (span.HasValue)
            return await GetDiagnosticsIntersectWithSpanAsync(document, diagnostics, span.Value, cancellationToken).ConfigureAwait(false);

        return diagnostics.ToImmutable();
    }

    protected virtual Task<ImmutableArray<Diagnostic>> GetDiagnosticsIntersectWithSpanAsync(Document document, IReadOnlyList<Diagnostic> diagnostics, TextSpan span, CancellationToken cancellationToken)
    {
        return Task.FromResult(diagnostics.WhereAsArray((diagnostic, _) => diagnostic.Location.SourceSpan.IntersectsWith(span), state: (object)null));
    }

    public async Task StartRefinementSessionAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken)
    {
        if (oldDocument.GetLanguageService<ICopilotOptionsService>() is not { } service)
            return;

        if (await service.IsRefineOptionEnabledAsync().ConfigureAwait(false))
            await StartRefinementSessionCoreAsync(oldDocument, newDocument, primaryDiagnostic, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> GetOnTheFlyDocsAsync(string symbolSignature, ImmutableArray<string> declarationCode, string language, CancellationToken cancellationToken)
    {
        if (!await IsAvailableAsync(cancellationToken).ConfigureAwait(false))
            return string.Empty;

        return await GetOnTheFlyDocsCoreAsync(symbolSignature, declarationCode, language, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> IsFileExcludedAsync(string filePath, CancellationToken cancellationToken)
    {
        if (!await IsAvailableAsync(cancellationToken).ConfigureAwait(false))
            return false;

        return await IsFileExcludedCoreAsync(filePath, cancellationToken).ConfigureAwait(false);
    }
}
