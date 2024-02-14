// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Copilot;

[ExportWorkspaceService(typeof(ICopilotCodeAnalysisService)), Shared]
internal sealed class VisualStudioCopilotCodeAnalysisService : ICopilotCodeAnalysisService
{
    private readonly CopilotDiagnosticUpdateSource _copilotDiagnosticUpdateSource;
    private readonly IDiagnosticsRefresher _diagnosticsRefresher;
    private readonly IGlobalOptionService _globalOptions;
    private readonly ConditionalWeakTable<Document, ConcurrentDictionary<string, (ImmutableArray<Diagnostic> Diagnostics, bool IsCompleteResult)>> _diagnosticsCache = new();
    private ISettingsManager? _settingsManager;
    private bool _executingAnalysis;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioCopilotCodeAnalysisService(
        CopilotDiagnosticUpdateSource copilotDiagnosticUpdateSource,
        IDiagnosticsRefresher diagnosticsRefresher,
        IGlobalOptionService globalOptions,
        IThreadingContext threadingContext,
        IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManagerService)
    {
        _copilotDiagnosticUpdateSource = copilotDiagnosticUpdateSource;
        _diagnosticsRefresher = diagnosticsRefresher;
        _globalOptions = globalOptions;
        InitializeAsync(settingsManagerService, threadingContext.DisposalToken).Forget();
    }

    private async Task InitializeAsync(IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManagerService, CancellationToken cancellationToken)
    {
        _settingsManager = await settingsManagerService.GetValueAsync(cancellationToken).ConfigureAwait(false);
    }

    public bool IsRefineOptionEnabled(Document document)
        => IsCopilotOptionEnabled(document, "EnableCSharpRefineQuickActionSuggestion");

    public bool IsCodeAnalysisOptionEnabled(Document document)
        => IsCopilotOptionEnabled(document, "EnableCSharpCodeAnalysis");

    private bool IsCopilotOptionEnabled(Document document, string optionName)
    {
        // The bool setting is persisted as 0=None, 1=True, 2=False, so it needs to be retrieved as an int.
        if (_settingsManager?.TryGetValue($"Microsoft.VisualStudio.Conversations.{optionName}", out int isEnabled) != GetValueResult.Success)
            return false;

        return isEnabled == 1 && document.GetLanguageService<ICopilotAnalyzer>() is not null;
    }

    private static bool IsLspPullDiagnostics(IGlobalOptionService globalOptions)
        => globalOptions.IsLspPullDiagnostics(InternalDiagnosticsOptionsStorage.NormalDiagnosticMode);

    public Task<bool> IsAvailableAsync(Document document, CancellationToken cancellationToken)
    {
        var analyzer = document.GetLanguageService<ICopilotAnalyzer>();
        return analyzer is null
            ? Task.FromResult(false)
            : analyzer.IsAvailableAsync(cancellationToken);
    }

    public Task<ImmutableArray<string>> GetAvailablePromptTitlesAsync(Document document, CancellationToken cancellationToken)
    {
        if (!IsCodeAnalysisOptionEnabled(document))
            return Task.FromResult(ImmutableArray<string>.Empty);

        var analyzer = document.GetRequiredLanguageService<ICopilotAnalyzer>();
        return analyzer.GetAvailablePromptTitlesAsync(document, cancellationToken);
    }

    private async Task<bool> ShouldSkipAnalysisAsync(Document document, CancellationToken cancellationToken)
    {
        if (!IsCodeAnalysisOptionEnabled(document))
            return true;

        if (await document.IsGeneratedCodeAsync(cancellationToken).ConfigureAwait(false))
            return true;

        return false;
    }

    public async Task AnalyzeDocumentAsync(Document document, TextSpan? span, string promptTitle, CancellationToken cancellationToken)
    {
        if (_executingAnalysis)
            return;

        _executingAnalysis = true;

        try
        {
            if (await ShouldSkipAnalysisAsync(document, cancellationToken).ConfigureAwait(false))
                return;

            if (FullDocumentDiagnosticsCached(document, promptTitle))
                return;

            if (await IsAvailableAsync(document, cancellationToken).ConfigureAwait(false) is false)
                return;

            var isFullDocumentAnalysis = !span.HasValue;
            var analyzer = document.GetRequiredLanguageService<ICopilotAnalyzer>();
            var diagnostics = await analyzer.AnalyzeDocumentAsync(document, span, promptTitle, cancellationToken).ConfigureAwait(false);
            var isLspPullDiagnostics = IsLspPullDiagnostics(_globalOptions);

            cancellationToken.ThrowIfCancellationRequested();

            CacheAndRefreshDiagnosticsIfNeeded(document, promptTitle, diagnostics, isLspPullDiagnostics, isFullDocumentAnalysis);
        }
        finally
        {
            _executingAnalysis = false;
        }
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

    private void CacheAndRefreshDiagnosticsIfNeeded(Document document, string promptTitle, ImmutableArray<Diagnostic> diagnostics, bool isLspPullDiagnostics, bool isCompleteResult)
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

        if (isLspPullDiagnostics)
        {
            // For LSP pull diagnostics mode, request a diagnostic workspace refresh.
            // We will include the cached copilot diagnostics from this service as part of that pull request.
            _diagnosticsRefresher.RequestWorkspaceRefresh();
        }
        else
        {
            // For non-LSP pull diagnostics mode, update diagnostics using a special IDiagnosticUpdateSource
            var diagnosticData = diagnostics.SelectAsArray(d => DiagnosticData.Create(d, document));
            _copilotDiagnosticUpdateSource.ReportDiagnostics(document, promptTitle, diagnosticData);
        }
    }

    public async Task<ImmutableArray<Diagnostic>> GetCachedDocumentDiagnosticsAsync(Document document, ImmutableArray<string> promptTitles, CancellationToken cancellationToken)
    {
        if (await ShouldSkipAnalysisAsync(document, cancellationToken).ConfigureAwait(false))
            return [];

        var analyzer = document.GetRequiredLanguageService<ICopilotAnalyzer>();
        var isLspPullDiagnostics = IsLspPullDiagnostics(_globalOptions);
        using var _1 = ArrayBuilder<Diagnostic>.GetInstance(out var diagnostics);

        foreach (var promptTitle in promptTitles)
        {
            if (TryGetDiagnosticsFromCache(document, promptTitle, out var existingDiagnostics, out _))
            {
                diagnostics.AddRange(existingDiagnostics);
            }
            else
            {
                var cachedDiagnostics = await analyzer.GetCachedDiagnosticsAsync(document, promptTitle, cancellationToken).ConfigureAwait(false);
                diagnostics.AddRange(cachedDiagnostics);
                _ = Task.Run(() => CacheAndRefreshDiagnosticsIfNeeded(document, promptTitle, cachedDiagnostics, isLspPullDiagnostics, isCompleteResult: false), cancellationToken);
            }
        }

        return diagnostics.ToImmutable();
    }

    public Task StartRefinementSessionAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken)
    {
        if (!IsRefineOptionEnabled(oldDocument))
            return Task.CompletedTask;

        var analyzer = oldDocument.GetRequiredLanguageService<ICopilotAnalyzer>();
        return analyzer.StartRefinementSessionAsync(oldDocument, newDocument, primaryDiagnostic, cancellationToken);
    }
}
