// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorLogger;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    using DiagnosticId = String;
    using LanguageKind = String;

    [Export(typeof(ICodeFixService)), Shared]
    internal partial class CodeFixService : ICodeFixService
    {
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly ImmutableArray<Lazy<CodeFixProvider, CodeChangeProviderMetadata>> _fixers;
        private readonly ImmutableDictionary<string, ImmutableArray<Lazy<CodeFixProvider, CodeChangeProviderMetadata>>> _fixersPerLanguageMap;

        private readonly ConditionalWeakTable<IReadOnlyList<AnalyzerReference>, ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>> _projectFixersMap = new();

        // Shared by project fixers and workspace fixers.
        private readonly ImmutableDictionary<LanguageKind, Lazy<ImmutableArray<IConfigurationFixProvider>>> _configurationProvidersMap;
        private readonly ImmutableArray<Lazy<IErrorLoggerService>> _errorLoggers;

        private ImmutableDictionary<LanguageKind, Lazy<ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>>>? _lazyWorkspaceFixersMap;
        private ImmutableDictionary<LanguageKind, Lazy<ImmutableDictionary<CodeFixProvider, int>>>? _lazyFixerPriorityMap;

        private ImmutableDictionary<CodeFixProvider, ImmutableArray<DiagnosticId>> _fixerToFixableIdsMap = ImmutableDictionary<CodeFixProvider, ImmutableArray<DiagnosticId>>.Empty;
        private ImmutableDictionary<object, FixAllProviderInfo?> _fixAllProviderMap = ImmutableDictionary<object, FixAllProviderInfo?>.Empty;
        private ImmutableDictionary<CodeFixProvider, CodeChangeProviderMetadata?> _fixerToMetadataMap = ImmutableDictionary<CodeFixProvider, CodeChangeProviderMetadata?>.Empty;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CodeFixService(
            IDiagnosticAnalyzerService diagnosticAnalyzerService,
            [ImportMany] IEnumerable<Lazy<IErrorLoggerService>> loggers,
            [ImportMany] IEnumerable<Lazy<CodeFixProvider, CodeChangeProviderMetadata>> fixers,
            [ImportMany] IEnumerable<Lazy<IConfigurationFixProvider, CodeChangeProviderMetadata>> configurationProviders)
        {
            _diagnosticService = diagnosticAnalyzerService;
            _errorLoggers = loggers.ToImmutableArray();

            _fixers = fixers.ToImmutableArray();
            _fixersPerLanguageMap = _fixers.ToPerLanguageMapWithMultipleLanguages();

            _configurationProvidersMap = GetConfigurationProvidersPerLanguageMap(configurationProviders);
        }

        private Func<string, bool>? GetShouldIncludeDiagnosticPredicate(
            TextDocument document,
            ICodeActionRequestPriorityProvider priorityProvider)
        {
            // For Normal or Low priority, we only need to execute analyzers which can report at least one fixable
            // diagnostic that can have a non-suppression/configuration fix.
            //
            // For CodeActionPriorityRequest.High, we only run compiler analyzer, which always has fixable diagnostics,
            // so we can return a null predicate here to include all diagnostics.

            if (!(priorityProvider.Priority is CodeActionRequestPriority.Default or CodeActionRequestPriority.Low))
                return null;

            var hasWorkspaceFixers = TryGetWorkspaceFixersMap(document, out var workspaceFixersMap);
            var projectFixersMap = GetProjectFixers(document);

            return id =>
            {
                if (hasWorkspaceFixers && workspaceFixersMap!.ContainsKey(id))
                    return true;

                return projectFixersMap.ContainsKey(id);
            };
        }

        public async Task<CodeFixCollection?> GetMostSevereFixAsync(
            TextDocument document, TextSpan range, ICodeActionRequestPriorityProvider priorityProvider, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            using var _ = TelemetryLogging.LogBlockTimeAggregated(FunctionId.CodeFix_Summary, $"Pri{priorityProvider.Priority.GetPriorityInt()}.{nameof(GetMostSevereFixAsync)}");

            ImmutableArray<DiagnosticData> allDiagnostics;

            using (TelemetryLogging.LogBlockTimeAggregated(FunctionId.CodeFix_Summary, $"Pri{priorityProvider.Priority.GetPriorityInt()}.{nameof(GetMostSevereFixAsync)}.{nameof(_diagnosticService.GetDiagnosticsForSpanAsync)}"))
            {
                allDiagnostics = await _diagnosticService.GetDiagnosticsForSpanAsync(
                    document, range, GetShouldIncludeDiagnosticPredicate(document, priorityProvider),
                    includeCompilerDiagnostics: true, includeSuppressedDiagnostics: false, priorityProvider, DiagnosticKind.All, isExplicit: false, cancellationToken).ConfigureAwait(false);
            }

            var copilotDiagnostics = await GetCopilotDiagnosticsAsync(document, range, priorityProvider.Priority, cancellationToken).ConfigureAwait(false);
            allDiagnostics = allDiagnostics.AddRange(copilotDiagnostics);

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var spanToDiagnostics = ConvertToMap(text, allDiagnostics);

            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = linkedTokenSource.Token;

            var spanToErrorDiagnostics = new SortedDictionary<TextSpan, List<DiagnosticData>>();
            var spanToOtherDiagnostics = new SortedDictionary<TextSpan, List<DiagnosticData>>();

            foreach (var (span, diagnostics) in spanToDiagnostics)
            {
                foreach (var diagnostic in diagnostics)
                {
                    var preferredMap = diagnostic.Severity == DiagnosticSeverity.Error
                        ? spanToErrorDiagnostics
                        : spanToOtherDiagnostics;

                    preferredMap.MultiAdd(span, diagnostic);
                }
            }

            var errorFixTask = GetFirstFixAsync(spanToErrorDiagnostics, cancellationToken);
            var otherFixTask = GetFirstFixAsync(spanToOtherDiagnostics, linkedToken);

            // If the error diagnostics task happens to complete with a non-null result before
            // the other diagnostics task, we can cancel the other task.
            var collection = await errorFixTask.ConfigureAwait(false) ??
                             await otherFixTask.ConfigureAwait(false);
            linkedTokenSource.Cancel();

            return collection;

            async Task<CodeFixCollection?> GetFirstFixAsync(
                SortedDictionary<TextSpan, List<DiagnosticData>> spanToDiagnostics,
                CancellationToken cancellationToken)
            {
                // Ensure we yield here so the caller can continue on.
                await TaskScheduler.Default.SwitchTo(alwaysYield: true);

                await foreach (var collection in StreamFixesAsync(
                    document, spanToDiagnostics, fixAllForInSpan: false,
                    priorityProvider, fallbackOptions, cancellationToken).ConfigureAwait(false))
                {
                    // Stop at the result error we see.
                    return collection;
                }

                return null;
            }
        }

        public async IAsyncEnumerable<CodeFixCollection> StreamFixesAsync(
            TextDocument document,
            TextSpan range,
            ICodeActionRequestPriorityProvider priorityProvider,
            CodeActionOptionsProvider fallbackOptions,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var _ = TelemetryLogging.LogBlockTimeAggregated(FunctionId.CodeFix_Summary, $"Pri{priorityProvider.Priority.GetPriorityInt()}");

            // We only need to compute suppression/configuration fixes when request priority is
            // 'CodeActionPriorityRequest.Lowest' or no priority was provided at all (so all providers should run).
            var includeSuppressionFixes = priorityProvider.Priority is null or CodeActionRequestPriority.Lowest;

            // REVIEW: this is the first and simplest design. basically, when ctrl+. is pressed, it asks diagnostic
            // service to give back current diagnostics for the given span, and it will use that to get fixes.
            // internally diagnostic service will either return cached information (if it is up-to-date) or
            // synchronously do the work at the spot.
            //
            // this design's weakness is that each side don't have enough information to narrow down works to do. it
            // will most likely always do more works than needed. sometimes way more than it is needed. (compilation)

            // We mark requests to GetDiagnosticsForSpanAsync as 'isExplicit = true' to indicate
            // user-invoked diagnostic requests, for example, user invoked Ctrl + Dot operation for lightbulb.
            ImmutableArray<DiagnosticData> diagnostics;

            using (TelemetryLogging.LogBlockTimeAggregated(FunctionId.CodeFix_Summary, $"Pri{priorityProvider.Priority.GetPriorityInt()}.{nameof(_diagnosticService.GetDiagnosticsForSpanAsync)}"))
            {
                diagnostics = await _diagnosticService.GetDiagnosticsForSpanAsync(
                    document, range, GetShouldIncludeDiagnosticPredicate(document, priorityProvider),
                    includeCompilerDiagnostics: true, includeSuppressedDiagnostics: includeSuppressionFixes, priorityProvider,
                    DiagnosticKind.All, isExplicit: true, cancellationToken).ConfigureAwait(false);
            }

            var copilotDiagnostics = await GetCopilotDiagnosticsAsync(document, range, priorityProvider.Priority, cancellationToken).ConfigureAwait(false);
            diagnostics = diagnostics.AddRange(copilotDiagnostics);

            if (diagnostics.IsEmpty)
                yield break;

            if (!diagnostics.IsEmpty)
            {
                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                var spanToDiagnostics = ConvertToMap(text, diagnostics);

                // 'CodeActionRequestPriority.Lowest' is used when the client only wants suppression/configuration fixes.
                if (priorityProvider.Priority != CodeActionRequestPriority.Lowest)
                {
                    await foreach (var collection in StreamFixesAsync(
                        document, spanToDiagnostics, fixAllForInSpan: false,
                        priorityProvider, fallbackOptions, cancellationToken).ConfigureAwait(false))
                    {
                        yield return collection;
                    }
                }
            }

            // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict CodeFixes in Interactive
            if (document.Project.Solution.WorkspaceKind != WorkspaceKind.Interactive && includeSuppressionFixes)
            {
                // For build-only diagnostics, we support configuration/suppression fixes.
                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                var spanToDiagnostics = ConvertToMap(text, diagnostics);

                // Ensure that we do not register duplicate configuration fixes.
                using var _2 = PooledHashSet<string>.GetInstance(out var registeredConfigurationFixTitles);
                foreach (var (span, diagnosticList) in spanToDiagnostics)
                {
                    await foreach (var codeFixCollection in StreamConfigurationFixesAsync(
                        document, span, diagnosticList, registeredConfigurationFixTitles, fallbackOptions, cancellationToken).ConfigureAwait(false))
                    {
                        yield return codeFixCollection;
                    }
                }
            }
        }

        private static async Task<ImmutableArray<DiagnosticData>> GetCopilotDiagnosticsAsync(
            TextDocument document,
            TextSpan range,
            CodeActionRequestPriority? priority,
            CancellationToken cancellationToken)
        {
            if (priority is null or CodeActionRequestPriority.Low)
                return await document.GetCachedCopilotDiagnosticsAsync(range, cancellationToken).ConfigureAwait(false);

            return [];
        }

        private static SortedDictionary<TextSpan, List<DiagnosticData>> ConvertToMap(
            SourceText text, ImmutableArray<DiagnosticData> diagnostics)
        {
            // group diagnostics by their diagnostics span
            //
            // invariant: later code gathers & runs CodeFixProviders for diagnostics with one identical diagnostics span
            // (that gets set later as CodeFixCollection's TextSpan) order diagnostics by span.
            var spanToDiagnostics = new SortedDictionary<TextSpan, List<DiagnosticData>>();
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.IsSuppressed)
                    continue;

                // TODO: Is it correct to use UnmappedFileSpan here?
                spanToDiagnostics.MultiAdd(diagnostic.DataLocation.UnmappedFileSpan.GetClampedTextSpan(text), diagnostic);
            }

            // Order diagnostics by DiagnosticId so the fixes are in a deterministic order.
            foreach (var (_, diagnosticList) in spanToDiagnostics)
                diagnosticList.Sort(static (d1, d2) => DiagnosticId.CompareOrdinal(d1.Id, d2.Id));

            return spanToDiagnostics;
        }

        public Task<CodeFixCollection?> GetDocumentFixAllForIdInSpanAsync(
            TextDocument document, TextSpan range, string diagnosticId, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
            => GetDocumentFixAllForIdInSpanAsync(document, range, diagnosticId, DiagnosticSeverity.Hidden, fallbackOptions, cancellationToken);

        public async Task<CodeFixCollection?> GetDocumentFixAllForIdInSpanAsync(
            TextDocument document, TextSpan range, string diagnosticId, DiagnosticSeverity minimumSeverity, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _ = TelemetryLogging.LogBlockTimeAggregated(FunctionId.CodeFix_Summary, $"{nameof(GetDocumentFixAllForIdInSpanAsync)}");
            ImmutableArray<DiagnosticData> diagnostics;

            using (TelemetryLogging.LogBlockTimeAggregated(FunctionId.CodeFix_Summary, $"{nameof(GetDocumentFixAllForIdInSpanAsync)}.{nameof(_diagnosticService.GetDiagnosticsForSpanAsync)}"))
            {
                diagnostics = await _diagnosticService.GetDiagnosticsForSpanAsync(
                    document, range, diagnosticId, includeSuppressedDiagnostics: false, priorityProvider: new DefaultCodeActionRequestPriorityProvider(),
                    DiagnosticKind.All, isExplicit: false, cancellationToken).ConfigureAwait(false);
            }

            diagnostics = diagnostics.WhereAsArray(d => d.Severity.IsMoreSevereThanOrEqualTo(minimumSeverity));
            if (!diagnostics.Any())
                return null;

            using var resultDisposer = ArrayBuilder<CodeFixCollection>.GetInstance(out var result);
            var spanToDiagnostics = new SortedDictionary<TextSpan, List<DiagnosticData>>
            {
                { range, diagnostics.ToList() },
            };

            await foreach (var collection in StreamFixesAsync(
                document, spanToDiagnostics, fixAllForInSpan: true, new DefaultCodeActionRequestPriorityProvider(),
                fallbackOptions, cancellationToken).ConfigureAwait(false))
            {
                if (collection.FixAllState is not null && collection.SupportedScopes.Contains(FixAllScope.Document))
                {
                    // TODO: Just get the first fix for now until we have a way to config user's preferred fix
                    // https://github.com/dotnet/roslyn/issues/27066
                    return collection;
                }
            }

            return null;
        }

        public Task<TDocument> ApplyCodeFixesForSpecificDiagnosticIdAsync<TDocument>(TDocument document, string diagnosticId, IProgress<CodeAnalysisProgress> progressTracker, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken) where TDocument : TextDocument
            => ApplyCodeFixesForSpecificDiagnosticIdAsync(document, diagnosticId, DiagnosticSeverity.Hidden, progressTracker, fallbackOptions, cancellationToken);

        public async Task<TDocument> ApplyCodeFixesForSpecificDiagnosticIdAsync<TDocument>(
            TDocument document,
            string diagnosticId,
            DiagnosticSeverity severity,
            IProgress<CodeAnalysisProgress> progressTracker,
            CodeActionOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
            where TDocument : TextDocument
        {
            cancellationToken.ThrowIfCancellationRequested();

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var textSpan = new TextSpan(0, text.Length);

            var fixCollection = await GetDocumentFixAllForIdInSpanAsync(
                document, textSpan, diagnosticId, severity, fallbackOptions, cancellationToken).ConfigureAwait(false);
            if (fixCollection == null)
            {
                return document;
            }

            var fixAllService = document.Project.Solution.Services.GetRequiredService<IFixAllGetFixesService>();

            var solution = await fixAllService.GetFixAllChangedSolutionAsync(
                new FixAllContext(fixCollection.FixAllState, progressTracker, cancellationToken)).ConfigureAwait(false);
            Contract.ThrowIfNull(solution);

            return (TDocument)(solution.GetTextDocument(document.Id) ?? throw new NotSupportedException(FeaturesResources.Removal_of_document_not_supported));
        }

        private bool TryGetWorkspaceFixersMap(TextDocument document, [NotNullWhen(true)] out ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>? fixerMap)
        {
            if (_lazyWorkspaceFixersMap == null)
            {
                var workspaceFixersMap = GetFixerPerLanguageMap(document.Project.Solution.Services);
                Interlocked.CompareExchange(ref _lazyWorkspaceFixersMap, workspaceFixersMap, null);
            }

            if (!_lazyWorkspaceFixersMap.TryGetValue(document.Project.Language, out var lazyFixerMap))
            {
                fixerMap = ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>.Empty;
                return false;
            }

            using var _ = PooledDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>.GetInstance(out var builder);
            foreach (var (id, fixers) in lazyFixerMap.Value)
            {
                var filteredFixers = ProjectCodeFixProvider.FilterExtensions(document, fixers, GetExtensionInfo);
                if (!filteredFixers.IsEmpty)
                    builder.Add(id, filteredFixers);
            }

            fixerMap = builder.ToImmutableDictionary();
            return fixerMap.Count > 0;
        }

        private bool TryGetWorkspaceFixersPriorityMap(TextDocument document, [NotNullWhen(true)] out Lazy<ImmutableDictionary<CodeFixProvider, int>>? fixersPriorityMap)
        {
            if (_lazyFixerPriorityMap == null)
            {
                var fixersPriorityByLanguageMap = GetFixerPriorityPerLanguageMap(document.Project.Solution.Services);
                Interlocked.CompareExchange(ref _lazyFixerPriorityMap, fixersPriorityByLanguageMap, null);
            }

            return _lazyFixerPriorityMap.TryGetValue(document.Project.Language, out fixersPriorityMap);
        }

        private bool TryGetWorkspaceFixer(
            Lazy<CodeFixProvider, CodeChangeProviderMetadata> lazyFixer,
            SolutionServices services,
            bool logExceptionWithInfoBar,
            [NotNullWhen(returnValue: true)] out CodeFixProvider? fixer)
        {
            try
            {
                fixer = lazyFixer.Value;
                return true;
            }
            catch (Exception ex)
            {
                // Gracefully handle exceptions in creating fixer instance.
                // Log exception and show info bar, if needed.
                if (logExceptionWithInfoBar)
                {
                    var errorReportingService = services.GetRequiredService<IErrorReportingService>();
                    var message = lazyFixer.Metadata.Name != null
                        ? string.Format(FeaturesResources.Error_creating_instance_of_CodeFixProvider_0, lazyFixer.Metadata.Name)
                        : FeaturesResources.Error_creating_instance_of_CodeFixProvider;

                    errorReportingService.ShowGlobalErrorInfo(
                        message,
                        TelemetryFeatureName.CodeFixProvider,
                        ex,
                        new InfoBarUI(
                            WorkspacesResources.Show_Stack_Trace,
                            InfoBarUI.UIKind.HyperLink,
                            () => errorReportingService.ShowDetailedErrorInfo(ex), closeAfterAction: true));

                    foreach (var errorLogger in _errorLoggers)
                    {
                        errorLogger.Value.LogException(this, ex);
                    }
                }

                fixer = null;
                return false;
            }
        }

        private async IAsyncEnumerable<CodeFixCollection> StreamFixesAsync(
            TextDocument document,
            SortedDictionary<TextSpan, List<DiagnosticData>> spanToDiagnostics,
            bool fixAllForInSpan,
            ICodeActionRequestPriorityProvider priorityProvider,
            CodeActionOptionsProvider fallbackOptions,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var hasAnySharedFixer = TryGetWorkspaceFixersMap(document, out var fixerMap);

            var projectFixersMap = GetProjectFixers(document);
            var hasAnyProjectFixer = projectFixersMap.Any();

            if (!hasAnySharedFixer && !hasAnyProjectFixer)
                yield break;

            // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict CodeFixes in Interactive
            var isInteractive = document.Project.Solution.WorkspaceKind == WorkspaceKind.Interactive;

            // gather CodeFixProviders for all distinct diagnostics found for current span
            using var _1 = PooledDictionary<CodeFixProvider, List<(TextSpan range, List<DiagnosticData> diagnostics)>>.GetInstance(out var fixerToRangesAndDiagnostics);
            using var _2 = PooledHashSet<CodeFixProvider>.GetInstance(out var currentFixers);

            foreach (var (range, diagnostics) in spanToDiagnostics)
            {
                currentFixers.Clear();

                foreach (var diagnosticId in diagnostics.Select(d => d.Id).Distinct())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Prioritize NuGet based project code fixers over VSIX based workspace code fixers.
                    if (hasAnyProjectFixer && projectFixersMap.TryGetValue(diagnosticId, out var projectFixers))
                    {
                        Debug.Assert(!isInteractive);
                        AddAllFixers(projectFixers, range, diagnostics, currentFixers, fixerToRangesAndDiagnostics);
                    }

                    if (hasAnySharedFixer && fixerMap!.TryGetValue(diagnosticId, out var workspaceFixers))
                    {
                        if (isInteractive)
                        {
                            AddAllFixers(workspaceFixers.WhereAsArray(IsInteractiveCodeFixProvider), range, diagnostics, currentFixers, fixerToRangesAndDiagnostics);
                        }
                        else
                        {
                            AddAllFixers(workspaceFixers, range, diagnostics, currentFixers, fixerToRangesAndDiagnostics);
                        }
                    }
                }
            }

            if (fixerToRangesAndDiagnostics.Count == 0)
                yield break;

            // Now, sort the fixers so that the ones that are ordered before others get their chance to run first.
            var allFixers = fixerToRangesAndDiagnostics.Keys.ToImmutableArray();
            if (TryGetWorkspaceFixersPriorityMap(document, out var fixersForLanguage))
                allFixers = allFixers.Sort(new FixerComparer(allFixers, fixersForLanguage.Value));

            var extensionManager = document.Project.Solution.Services.GetService<IExtensionManager>();

            // Run each CodeFixProvider to gather individual CodeFixes for reported diagnostics.
            // Ensure that no diagnostic has registered code actions from different code fix providers with same equivalance key.
            // This prevents duplicate registered code actions from NuGet and VSIX code fix providers.
            // See https://github.com/dotnet/roslyn/issues/18818 for details.
            var uniqueDiagosticToEquivalenceKeysMap = new Dictionary<Diagnostic, PooledHashSet<string?>>();

            // NOTE: For backward compatibility, we allow multiple registered code actions from the same code fix provider
            // to have the same equivalence key. See https://github.com/dotnet/roslyn/issues/44553 for details.
            // To ensure this, we track the fixer that first registered a code action to fix a diagnostic with a specific equivalence key.
            var diagnosticAndEquivalenceKeyToFixersMap = new Dictionary<(Diagnostic diagnostic, string? equivalenceKey), CodeFixProvider>();

            try
            {
                foreach (var fixer in allFixers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!priorityProvider.MatchesPriority(fixer))
                        continue;

                    foreach (var (span, diagnostics) in fixerToRangesAndDiagnostics[fixer])
                    {
                        // Log an individual telemetry event for slow codefix computations to
                        // allow targeted trace notifications for further investigation. 500 ms seemed like
                        // a good value so as to not be too noisy, but if fired, indicates a potential
                        // area requiring investigation.
                        const int CodeFixTelemetryDelay = 500;

                        var fixerName = fixer.GetType().Name;
                        var logMessage = KeyValueLogMessage.Create(m =>
                        {
                            m[TelemetryLogging.KeyName] = fixerName;
                            m[TelemetryLogging.KeyLanguageName] = document.Project.Language;
                        });

                        using var _ = TelemetryLogging.LogBlockTime(FunctionId.CodeFix_Delay, logMessage, CodeFixTelemetryDelay);

                        var codeFixCollection = await TryGetFixesOrConfigurationsAsync(
                            document, span, diagnostics, fixAllForInSpan, fixer,
                            hasFix: d => this.GetFixableDiagnosticIds(fixer, extensionManager).Contains(d.Id),
                            getFixes: dxs =>
                            {
                                var fixerMetadata = TryGetMetadata(fixer);

                                using (RoslynEventSource.LogInformationalBlock(FunctionId.CodeFixes_GetCodeFixesAsync, fixerName, cancellationToken))
                                {
                                    if (fixAllForInSpan)
                                    {
                                        var primaryDiagnostic = dxs.First();
                                        return GetCodeFixesAsync(document, primaryDiagnostic.Location.SourceSpan, fixer, fixerMetadata, fallbackOptions,
                                            [primaryDiagnostic], uniqueDiagosticToEquivalenceKeysMap,
                                            diagnosticAndEquivalenceKeyToFixersMap, cancellationToken);
                                    }
                                    else
                                    {
                                        return GetCodeFixesAsync(document, span, fixer, fixerMetadata, fallbackOptions, dxs,
                                            uniqueDiagosticToEquivalenceKeysMap, diagnosticAndEquivalenceKeyToFixersMap, cancellationToken);
                                    }
                                }
                            },
                            fallbackOptions,
                            cancellationToken).ConfigureAwait(false);

                        if (codeFixCollection != null)
                        {
                            yield return codeFixCollection;

                            // Just need the first result if we are doing fix all in span
                            if (fixAllForInSpan)
                                yield break;
                        }
                    }
                }
            }
            finally
            {
                foreach (var pooledSet in uniqueDiagosticToEquivalenceKeysMap.Values)
                {
                    pooledSet.Free();
                }
            }

            yield break;

            static void AddAllFixers(
                ImmutableArray<CodeFixProvider> fixers,
                TextSpan range,
                List<DiagnosticData> diagnostics,
                PooledHashSet<CodeFixProvider> currentFixers,
                PooledDictionary<CodeFixProvider, List<(TextSpan range, List<DiagnosticData> diagnostics)>> fixerToRangesAndDiagnostics)
            {
                foreach (var fixer in fixers)
                {
                    if (currentFixers.Add(fixer))
                        fixerToRangesAndDiagnostics.MultiAdd(fixer, (range, diagnostics));
                }
            }
        }

        private CodeChangeProviderMetadata? TryGetMetadata(CodeFixProvider fixer)
        {
            return ImmutableInterlocked.GetOrAdd(
                ref _fixerToMetadataMap,
                fixer,
                static (fixer, fixers) =>
                {
                    foreach (var lazy in fixers)
                    {
                        if (lazy.IsValueCreated && lazy.Value == fixer)
                            return lazy.Metadata;
                    }

                    // Note: it feels very strange that we could ever not find a fixer in our list.  However, this
                    // occurs in testing scenarios.  I'm not sure if the tests represent a bogus potential input, or if
                    // this is something that can actually occur in practice and we want to keep working.
                    return null;
                },
                _fixers);
        }

        private static async Task<ImmutableArray<CodeFix>> GetCodeFixesAsync(
            TextDocument document, TextSpan span, CodeFixProvider fixer, CodeChangeProviderMetadata? fixerMetadata, CodeActionOptionsProvider fallbackOptions,
            ImmutableArray<Diagnostic> diagnostics,
            Dictionary<Diagnostic, PooledHashSet<string?>> uniqueDiagosticToEquivalenceKeysMap,
            Dictionary<(Diagnostic diagnostic, string? equivalenceKey), CodeFixProvider> diagnosticAndEquivalenceKeyToFixersMap,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var fixesDisposer = ArrayBuilder<CodeFix>.GetInstance(out var fixes);
            var context = new CodeFixContext(document, span, diagnostics,
                // TODO: Can we share code between similar lambdas that we pass to this API in BatchFixAllProvider.cs, CodeFixService.cs and CodeRefactoringService.cs?
                (action, applicableDiagnostics) =>
                {
                    // Serialize access for thread safety - we don't know what thread the fix provider will call this delegate from.
                    lock (fixes)
                    {
                        // Filter out applicable diagnostics which already have a registered code action with same equivalence key.
                        applicableDiagnostics = FilterApplicableDiagnostics(applicableDiagnostics, action.EquivalenceKey,
                            fixer, uniqueDiagosticToEquivalenceKeysMap, diagnosticAndEquivalenceKeyToFixersMap);

                        if (!applicableDiagnostics.IsEmpty)
                        {
                            // Add the CodeFix Provider Name to the parent CodeAction's CustomTags.
                            // Always add a name even in cases of 3rd party fixers that do not export
                            // name metadata.
                            action.AddCustomTagAndTelemetryInfo(fixerMetadata, fixer);

                            fixes.Add(new CodeFix(document.Project, action, applicableDiagnostics));
                        }
                    }
                },
                fallbackOptions,
                cancellationToken);

            var task = fixer.RegisterCodeFixesAsync(context) ?? Task.CompletedTask;
            await task.ConfigureAwait(false);
            return fixes.ToImmutableAndClear();

            static ImmutableArray<Diagnostic> FilterApplicableDiagnostics(
                ImmutableArray<Diagnostic> applicableDiagnostics,
                string? equivalenceKey,
                CodeFixProvider fixer,
                Dictionary<Diagnostic, PooledHashSet<string?>> uniqueDiagosticToEquivalenceKeysMap,
                Dictionary<(Diagnostic diagnostic, string? equivalenceKey), CodeFixProvider> diagnosticAndEquivalenceKeyToFixersMap)
            {
                using var disposer = ArrayBuilder<Diagnostic>.GetInstance(out var newApplicableDiagnostics);
                foreach (var diagnostic in applicableDiagnostics)
                {
                    if (!uniqueDiagosticToEquivalenceKeysMap.TryGetValue(diagnostic, out var equivalenceKeys))
                    {
                        // First code action registered to fix this diagnostic with any equivalenceKey.
                        // Record the equivalence key and the fixer that registered this action.
                        equivalenceKeys = PooledHashSet<string?>.GetInstance();
                        equivalenceKeys.Add(equivalenceKey);
                        uniqueDiagosticToEquivalenceKeysMap[diagnostic] = equivalenceKeys;
                        diagnosticAndEquivalenceKeyToFixersMap.Add((diagnostic, equivalenceKey), fixer);
                    }
                    else if (equivalenceKeys.Add(equivalenceKey))
                    {
                        // First code action registered to fix this diagnostic with the given equivalenceKey.
                        // Record the the fixer that registered this action.
                        diagnosticAndEquivalenceKeyToFixersMap.Add((diagnostic, equivalenceKey), fixer);
                    }
                    else if (diagnosticAndEquivalenceKeyToFixersMap[(diagnostic, equivalenceKey)] != fixer)
                    {
                        // Diagnostic already has a registered code action with same equivalence key from a different fixer.
                        // Note that we allow same fixer to register multiple such code actions with the same equivalence key
                        // for backward compatibility. See https://github.com/dotnet/roslyn/issues/44553 for details.
                        continue;
                    }

                    newApplicableDiagnostics.Add(diagnostic);
                }

                return newApplicableDiagnostics.Count == applicableDiagnostics.Length
                    ? applicableDiagnostics
                    : newApplicableDiagnostics.ToImmutable();
            }
        }

        private async IAsyncEnumerable<CodeFixCollection> StreamConfigurationFixesAsync(
            TextDocument document,
            TextSpan diagnosticsSpan,
            IEnumerable<DiagnosticData> diagnostics,
            PooledHashSet<string> registeredConfigurationFixTitles,
            CodeActionOptionsProvider fallbackOptions,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_configurationProvidersMap.TryGetValue(document.Project.Language, out var lazyConfigurationProviders) ||
                lazyConfigurationProviders.Value == null)
            {
                yield break;
            }

            // append CodeFixCollection for each CodeFixProvider
            foreach (var provider in lazyConfigurationProviders.Value)
            {
                using (RoslynEventSource.LogInformationalBlock(FunctionId.CodeFixes_GetCodeFixesAsync, provider, cancellationToken))
                {
                    var codeFixCollection = await TryGetFixesOrConfigurationsAsync(
                        document, diagnosticsSpan, diagnostics, fixAllForInSpan: false, provider,
                        hasFix: d => provider.IsFixableDiagnostic(d),
                        getFixes: async dxs =>
                        {
                            var fixes = await provider.GetFixesAsync(document, diagnosticsSpan, dxs, fallbackOptions, cancellationToken).ConfigureAwait(false);
                            return fixes.WhereAsArray(f => registeredConfigurationFixTitles.Add(f.Action.Title));
                        },
                        fallbackOptions,
                        cancellationToken).ConfigureAwait(false);
                    if (codeFixCollection != null)
                        yield return codeFixCollection;
                }
            }
        }

        private async Task<CodeFixCollection?> TryGetFixesOrConfigurationsAsync<TCodeFixProvider>(
            TextDocument textDocument,
            TextSpan fixesSpan,
            IEnumerable<DiagnosticData> diagnosticsWithSameSpan,
            bool fixAllForInSpan,
            TCodeFixProvider fixer,
            Func<Diagnostic, bool> hasFix,
            Func<ImmutableArray<Diagnostic>, Task<ImmutableArray<CodeFix>>> getFixes,
            CodeActionOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
            where TCodeFixProvider : notnull
        {
            cancellationToken.ThrowIfCancellationRequested();

            var allDiagnostics =
                await diagnosticsWithSameSpan.OrderByDescending(d => d.Severity)
                                             .ToDiagnosticsAsync(textDocument.Project, cancellationToken).ConfigureAwait(false);
            var diagnostics = allDiagnostics.WhereAsArray(hasFix);
            if (diagnostics.Length <= 0)
            {
                // this can happen for suppression case where all diagnostics can't be suppressed
                return null;
            }

            var extensionManager = textDocument.Project.Solution.Services.GetRequiredService<IExtensionManager>();
            var fixes = await extensionManager.PerformFunctionAsync(fixer,
                _ => getFixes(diagnostics),
                defaultValue: [], cancellationToken).ConfigureAwait(false);

            if (fixes.IsDefaultOrEmpty)
                return null;

            // If the fix provider supports fix all occurrences, then get the corresponding FixAllProviderInfo and fix all context.
            var fixAllProviderInfo = extensionManager.PerformFunction(
                fixer, () => ImmutableInterlocked.GetOrAdd(ref _fixAllProviderMap, fixer, FixAllProviderInfo.Create), defaultValue: null);

            FixAllState? fixAllState = null;
            var supportedScopes = ImmutableArray<FixAllScope>.Empty;
            if (fixAllProviderInfo != null && textDocument is Document document)
            {
                var diagnosticIds = diagnostics.Where(fixAllProviderInfo.CanBeFixed)
                                               .Select(d => d.Id)
                                               .ToImmutableHashSet();

                var diagnosticProvider = fixAllForInSpan
                    ? new FixAllPredefinedDiagnosticProvider(allDiagnostics)
                    : (FixAllContext.DiagnosticProvider)new FixAllDiagnosticProvider(_diagnosticService, diagnosticIds);

                var codeFixProvider = (fixer as CodeFixProvider) ?? new WrapperCodeFixProvider((IConfigurationFixProvider)fixer, diagnostics.Select(d => d.Id));

                fixAllState = new FixAllState(
                    (FixAllProvider)fixAllProviderInfo.FixAllProvider,
                    fixesSpan,
                    document,
                    document.Project,
                    codeFixProvider,
                    FixAllScope.Document,
                    fixes[0].Action.EquivalenceKey,
                    diagnosticIds,
                    diagnosticProvider,
                    fallbackOptions);

                supportedScopes = fixAllProviderInfo.SupportedScopes;
            }

            return new CodeFixCollection(
                fixer, fixesSpan, fixes, fixAllState,
                supportedScopes, diagnostics.First());
        }

        /// <summary> Looks explicitly for an <see cref="AbstractSuppressionCodeFixProvider"/>.</summary>
        public CodeFixProvider? GetSuppressionFixer(string language, IEnumerable<string> diagnosticIds)
        {
            if (!_configurationProvidersMap.TryGetValue(language, out var lazyConfigurationProviders) ||
                lazyConfigurationProviders.Value.IsDefault)
            {
                return null;
            }

            // Explicitly looks for an AbstractSuppressionCodeFixProvider
            var fixer = lazyConfigurationProviders.Value.OfType<AbstractSuppressionCodeFixProvider>().FirstOrDefault();
            if (fixer == null)
            {
                return null;
            }

            return new WrapperCodeFixProvider(fixer, diagnosticIds);
        }

        private bool IsInteractiveCodeFixProvider(CodeFixProvider provider)
        {
            // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict CodeFixes in Interactive
            return provider is FullyQualify.AbstractFullyQualifyCodeFixProvider or
                   AddImport.AbstractAddImportCodeFixProvider;
        }

        private ImmutableArray<DiagnosticId> GetFixableDiagnosticIds(CodeFixProvider fixer, IExtensionManager? extensionManager)
        {
            // If we are passed a null extension manager it means we do not have access to a document so there is nothing to
            // show the user.  In this case we will log any exceptions that occur, but the user will not see them.
            if (extensionManager != null)
            {
                return extensionManager.PerformFunction(
                    fixer,
                    () => ImmutableInterlocked.GetOrAdd(ref _fixerToFixableIdsMap, fixer, f => GetAndTestFixableDiagnosticIds(f)),
                    defaultValue: []);
            }

            try
            {
                return ImmutableInterlocked.GetOrAdd(ref _fixerToFixableIdsMap, fixer, f => GetAndTestFixableDiagnosticIds(f));
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                foreach (var logger in _errorLoggers)
                {
                    logger.Value.LogException(fixer, e);
                }

                return [];
            }
        }

        private static ImmutableArray<string> GetAndTestFixableDiagnosticIds(CodeFixProvider codeFixProvider)
        {
            var ids = codeFixProvider.FixableDiagnosticIds;
            if (ids.IsDefault)
            {
                throw new InvalidOperationException(
                    string.Format(
                        WorkspacesResources._0_returned_an_uninitialized_ImmutableArray,
                        codeFixProvider.GetType().Name + "." + nameof(CodeFixProvider.FixableDiagnosticIds)));
            }

            return ids;
        }

        private ImmutableDictionary<LanguageKind, Lazy<ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>>> GetFixerPerLanguageMap(
            SolutionServices services)
        {
            var fixerMap = ImmutableDictionary.Create<LanguageKind, Lazy<ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>>>();
            var extensionManager = services.GetService<IExtensionManager>();
            foreach (var (diagnosticId, lazyFixers) in _fixersPerLanguageMap)
            {
                var lazyMap = new Lazy<ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>>(() =>
                {
                    using var _ = PooledDictionary<DiagnosticId, ArrayBuilder<CodeFixProvider>>.GetInstance(out var mutableMap);

                    foreach (var lazyFixer in lazyFixers)
                    {
                        if (!TryGetWorkspaceFixer(lazyFixer, services, logExceptionWithInfoBar: true, out var fixer))
                        {
                            continue;
                        }

                        foreach (var id in this.GetFixableDiagnosticIds(fixer, extensionManager))
                        {
                            if (string.IsNullOrWhiteSpace(id))
                            {
                                continue;
                            }

                            mutableMap.MultiAdd(id, fixer);
                        }
                    }

                    return mutableMap.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableAndFree());
                }, isThreadSafe: true);

                fixerMap = fixerMap.Add(diagnosticId, lazyMap);
            }

            return fixerMap;
        }

        private static ImmutableDictionary<LanguageKind, Lazy<ImmutableArray<IConfigurationFixProvider>>> GetConfigurationProvidersPerLanguageMap(
            IEnumerable<Lazy<IConfigurationFixProvider, CodeChangeProviderMetadata>> configurationProviders)
        {
            var configurationProvidersPerLanguageMap = configurationProviders.ToPerLanguageMapWithMultipleLanguages();

            var configurationFixerMap = ImmutableDictionary.CreateBuilder<LanguageKind, Lazy<ImmutableArray<IConfigurationFixProvider>>>();
            foreach (var (diagnosticId, lazyFixers) in configurationProvidersPerLanguageMap)
            {
                var lazyConfigurationFixers = new Lazy<ImmutableArray<IConfigurationFixProvider>>(() => GetConfigurationFixProviders(lazyFixers));
                configurationFixerMap.Add(diagnosticId, lazyConfigurationFixers);
            }

            return configurationFixerMap.ToImmutable();

            static ImmutableArray<IConfigurationFixProvider> GetConfigurationFixProviders(ImmutableArray<Lazy<IConfigurationFixProvider, CodeChangeProviderMetadata>> languageKindAndFixers)
            {
                var orderedLanguageKindAndFixers = ExtensionOrderer.Order(languageKindAndFixers);
                var builder = new FixedSizeArrayBuilder<IConfigurationFixProvider>(orderedLanguageKindAndFixers.Count);
                foreach (var languageKindAndFixersValue in orderedLanguageKindAndFixers)
                    builder.Add(languageKindAndFixersValue.Value);

                return builder.MoveToImmutable();
            }
        }

        private ImmutableDictionary<LanguageKind, Lazy<ImmutableDictionary<CodeFixProvider, int>>> GetFixerPriorityPerLanguageMap(SolutionServices services)
        {
            var languageMap = ImmutableDictionary.CreateBuilder<LanguageKind, Lazy<ImmutableDictionary<CodeFixProvider, int>>>();
            foreach (var (diagnosticId, lazyFixers) in _fixersPerLanguageMap)
            {
                var lazyMap = new Lazy<ImmutableDictionary<CodeFixProvider, int>>(() =>
                {
                    var priorityMap = ImmutableDictionary.CreateBuilder<CodeFixProvider, int>();

                    var fixers = ExtensionOrderer.Order(lazyFixers);
                    for (var i = 0; i < fixers.Count; i++)
                    {
                        if (!TryGetWorkspaceFixer(fixers[i], services, logExceptionWithInfoBar: false, out var fixer))
                            continue;

                        priorityMap.Add(fixer, i);
                    }

                    return priorityMap.ToImmutable();
                }, isThreadSafe: true);

                languageMap.Add(diagnosticId, lazyMap);
            }

            return languageMap.ToImmutable();
        }

        private ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>> GetProjectFixers(TextDocument document)
        {
            // TODO (https://github.com/dotnet/roslyn/issues/4932): Don't restrict CodeFixes in Interactive
            return document.Project.Solution.WorkspaceKind == WorkspaceKind.Interactive
                ? ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>>.Empty
                : _projectFixersMap.GetValue(document.Project.AnalyzerReferences, _ => ComputeProjectFixers(document));
        }

        private ImmutableDictionary<DiagnosticId, ImmutableArray<CodeFixProvider>> ComputeProjectFixers(TextDocument document)
        {
            var extensionManager = document.Project.Solution.Services.GetService<IExtensionManager>();

            using var _ = PooledDictionary<DiagnosticId, ArrayBuilder<CodeFixProvider>>.GetInstance(out var builder);
            var codeFixProviders = ProjectCodeFixProvider.GetExtensions(document, GetExtensionInfo);
            foreach (var fixer in codeFixProviders)
            {
                var fixableIds = this.GetFixableDiagnosticIds(fixer, extensionManager);
                foreach (var id in fixableIds)
                {
                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    builder.MultiAdd(id, fixer);
                }
            }

            return builder.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableAndFree());
        }

        private static ProjectCodeFixProvider.ExtensionInfo GetExtensionInfo(ExportCodeFixProviderAttribute attribute)
        {
            var kinds = EnumArrayConverter.FromStringArray<TextDocumentKind>(attribute.DocumentKinds);

            return new(kinds, attribute.DocumentExtensions);
        }

        private sealed class FixerComparer : IComparer<CodeFixProvider>
        {
            private readonly Dictionary<CodeFixProvider, int> _fixerToIndex;
            private readonly ImmutableDictionary<CodeFixProvider, int> _priorityMap;

            public FixerComparer(
                ImmutableArray<CodeFixProvider> allFixers,
                ImmutableDictionary<CodeFixProvider, int> priorityMap)
            {
                _fixerToIndex = allFixers.Select((fixer, index) => (fixer, index)).ToDictionary(t => t.fixer, t => t.index);
                _priorityMap = priorityMap;
            }

            public int Compare([AllowNull] CodeFixProvider x, [AllowNull] CodeFixProvider y)
            {
                Contract.ThrowIfNull(x);
                Contract.ThrowIfNull(y);

                // If the fixers specify an explicit ordering between each other, then respect that.
                if (_priorityMap.TryGetValue(x, out var xOrder) &&
                    _priorityMap.TryGetValue(y, out var yOrder))
                {
                    var comparison = xOrder - yOrder;
                    if (comparison != 0)
                        return comparison;
                }

                // Otherwise, keep things in the same order that they were in the list (i.e. keep things stable).
                return _fixerToIndex[x] - _fixerToIndex[y];
            }
        }

        public TestAccessor GetTestAccessor()
            => new(this);

        public readonly struct TestAccessor
        {
            private readonly CodeFixService _codeFixService;

            public TestAccessor(CodeFixService codeFixService)
            {
                _codeFixService = codeFixService;
            }

            public ImmutableDictionary<LanguageKind, Lazy<ImmutableDictionary<CodeFixProvider, int>>> GetFixerPriorityPerLanguageMap(SolutionServices services)
                => _codeFixService.GetFixerPriorityPerLanguageMap(services);
        }
    }
}
