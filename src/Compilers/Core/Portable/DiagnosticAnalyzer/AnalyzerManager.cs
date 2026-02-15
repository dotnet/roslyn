// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Manages properties of analyzers (such as registered actions, supported diagnostics) for analyzer host's lifetime
    /// and executes the callbacks into the analyzers.
    /// 
    /// It ensures the following for the lifetime of analyzer host:
    /// 1) <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> is invoked only once per-analyzer.
    /// 2) <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> is invoked only once per-analyzer.
    /// 3) <see cref="CompilationStartAnalyzerAction"/> registered during Initialize are invoked only once per-compilation per-analyzer and analyzer options.
    /// </summary>
    internal partial class AnalyzerManager
    {
        // This cache stores the analyzer execution context per-analyzer (i.e. registered actions, supported descriptors, etc.).
        // Not created as ImmutableDictionary for perf considerations, but should be treated as immutable
        private readonly Dictionary<DiagnosticAnalyzer, AnalyzerExecutionContext> _analyzerExecutionContextMap;

        public AnalyzerManager(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            _analyzerExecutionContextMap = CreateAnalyzerExecutionContextMap(analyzers);
        }

        public AnalyzerManager(DiagnosticAnalyzer analyzer)
        {
            _analyzerExecutionContextMap = CreateAnalyzerExecutionContextMap(SpecializedCollections.SingletonEnumerable(analyzer));
        }

        private Dictionary<DiagnosticAnalyzer, AnalyzerExecutionContext> CreateAnalyzerExecutionContextMap(IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            var analyzerExecutionContextMap = new Dictionary<DiagnosticAnalyzer, AnalyzerExecutionContext>();
            foreach (var analyzer in analyzers)
            {
                analyzerExecutionContextMap.Add(analyzer, new AnalyzerExecutionContext(analyzer));
            }

            return analyzerExecutionContextMap;
        }

        private AnalyzerExecutionContext GetAnalyzerExecutionContext(DiagnosticAnalyzer analyzer) => _analyzerExecutionContextMap[analyzer];

        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/issues/26778",
            OftenCompletesSynchronously = true)]
        private async ValueTask<HostCompilationStartAnalysisScope> GetCompilationAnalysisScopeAsync(
            HostSessionStartAnalysisScope sessionScope,
            AnalyzerExecutor analyzerExecutor,
            CancellationToken cancellationToken)
        {
            var analyzerExecutionContext = GetAnalyzerExecutionContext(sessionScope.Analyzer);
            return await GetCompilationAnalysisScopeCoreAsync(sessionScope, analyzerExecutor, analyzerExecutionContext, cancellationToken).ConfigureAwait(false);
        }

        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/issues/26778",
            OftenCompletesSynchronously = true)]
        private async ValueTask<HostCompilationStartAnalysisScope> GetCompilationAnalysisScopeCoreAsync(
            HostSessionStartAnalysisScope sessionScope,
            AnalyzerExecutor analyzerExecutor,
            AnalyzerExecutionContext analyzerExecutionContext,
            CancellationToken cancellationToken)
        {
            try
            {
                return await analyzerExecutionContext.GetCompilationAnalysisScopeAsync(sessionScope, analyzerExecutor, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Task to compute the scope was cancelled.
                // Clear the compilation scope for analyzer, so we can attempt a retry.
                analyzerExecutionContext.ClearCompilationScopeTask();

                cancellationToken.ThrowIfCancellationRequested();
                return await GetCompilationAnalysisScopeCoreAsync(sessionScope, analyzerExecutor, analyzerExecutionContext, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<HostSymbolStartAnalysisScope> GetSymbolAnalysisScopeAsync(
            ISymbol symbol,
            bool isGeneratedCodeSymbol,
            SyntaxTree? filterTree,
            TextSpan? filterSpan,
            DiagnosticAnalyzer analyzer,
            ImmutableArray<SymbolStartAnalyzerAction> symbolStartActions,
            AnalyzerExecutor analyzerExecutor,
            CancellationToken cancellationToken)
        {
            var analyzerExecutionContext = GetAnalyzerExecutionContext(analyzer);
            return await GetSymbolAnalysisScopeCoreAsync(symbol, isGeneratedCodeSymbol, filterTree, filterSpan, symbolStartActions, analyzerExecutor, analyzerExecutionContext, cancellationToken).ConfigureAwait(false);
        }

        private async Task<HostSymbolStartAnalysisScope> GetSymbolAnalysisScopeCoreAsync(
            ISymbol symbol,
            bool isGeneratedCodeSymbol,
            SyntaxTree? filterTree,
            TextSpan? filterSpan,
            ImmutableArray<SymbolStartAnalyzerAction> symbolStartActions,
            AnalyzerExecutor analyzerExecutor,
            AnalyzerExecutionContext analyzerExecutionContext,
            CancellationToken cancellationToken)
        {
            try
            {
                return await analyzerExecutionContext.GetSymbolAnalysisScopeAsync(symbol, isGeneratedCodeSymbol, filterTree, filterSpan, symbolStartActions, analyzerExecutor, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Task to compute the scope was cancelled.
                // Clear the symbol scope for analyzer, so we can attempt a retry.
                analyzerExecutionContext.ClearSymbolScopeTask(symbol);

                cancellationToken.ThrowIfCancellationRequested();
                return await GetSymbolAnalysisScopeCoreAsync(symbol, isGeneratedCodeSymbol, filterTree, filterSpan, symbolStartActions, analyzerExecutor, analyzerExecutionContext, cancellationToken).ConfigureAwait(false);
            }
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        private async ValueTask<HostSessionStartAnalysisScope> GetSessionAnalysisScopeAsync(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor,
            CancellationToken cancellationToken)
        {
            var analyzerExecutionContext = GetAnalyzerExecutionContext(analyzer);
            return await GetSessionAnalysisScopeCoreAsync(analyzerExecutor, analyzerExecutionContext, cancellationToken).ConfigureAwait(false);
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        private async ValueTask<HostSessionStartAnalysisScope> GetSessionAnalysisScopeCoreAsync(
            AnalyzerExecutor analyzerExecutor,
            AnalyzerExecutionContext analyzerExecutionContext,
            CancellationToken cancellationToken)
        {
            try
            {
                var task = analyzerExecutionContext.GetSessionAnalysisScopeAsync(analyzerExecutor, cancellationToken);
                return await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Task to compute the scope was cancelled.
                // Clear the entry in scope map for analyzer, so we can attempt a retry.
                analyzerExecutionContext.ClearSessionScopeTask();

                cancellationToken.ThrowIfCancellationRequested();
                return await GetSessionAnalysisScopeCoreAsync(analyzerExecutor, analyzerExecutionContext, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get all the analyzer actions to execute for the given analyzer against a given compilation.
        /// The returned actions include the actions registered during <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> method as well as
        /// the actions registered during <see cref="CompilationStartAnalyzerAction"/> for the given compilation.
        /// </summary>
        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        public async ValueTask<AnalyzerActions> GetAnalyzerActionsAsync(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor, CancellationToken cancellationToken)
        {
            var sessionScope = await GetSessionAnalysisScopeAsync(analyzer, analyzerExecutor, cancellationToken).ConfigureAwait(false);
            if (sessionScope.GetAnalyzerActions().CompilationStartActionsCount > 0 && analyzerExecutor.Compilation != null)
            {
                var compilationScope = await GetCompilationAnalysisScopeAsync(sessionScope, analyzerExecutor, cancellationToken).ConfigureAwait(false);
                return compilationScope.GetAnalyzerActions();
            }

            return sessionScope.GetAnalyzerActions();
        }

        /// <summary>
        /// Get the per-symbol analyzer actions to be executed by the given analyzer.
        /// These are the actions registered during the various RegisterSymbolStartAction method invocations for the given symbol on different analysis contexts.
        /// </summary>
        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        public async ValueTask<AnalyzerActions> GetPerSymbolAnalyzerActionsAsync(
            ISymbol symbol,
            bool isGeneratedCodeSymbol,
            SyntaxTree? filterTree,
            TextSpan? filterSpan,
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor,
            CancellationToken cancellationToken)
        {
            var analyzerActions = await GetAnalyzerActionsAsync(analyzer, analyzerExecutor, cancellationToken).ConfigureAwait(false);
            if (analyzerActions.SymbolStartActionsCount > 0)
            {
                var filteredSymbolStartActions = getFilteredActionsByKind(analyzerActions.SymbolStartActions);
                if (filteredSymbolStartActions.Length > 0)
                {
                    var symbolScope = await GetSymbolAnalysisScopeAsync(symbol, isGeneratedCodeSymbol, filterTree, filterSpan, analyzer, filteredSymbolStartActions, analyzerExecutor, cancellationToken).ConfigureAwait(false);
                    return symbolScope.GetAnalyzerActions();
                }
            }

            return AnalyzerActions.Empty;

            ImmutableArray<SymbolStartAnalyzerAction> getFilteredActionsByKind(ImmutableArray<SymbolStartAnalyzerAction> symbolStartActions)
            {
                ArrayBuilder<SymbolStartAnalyzerAction>? filteredActionsBuilderOpt = null;
                for (int i = 0; i < symbolStartActions.Length; i++)
                {
                    var symbolStartAction = symbolStartActions[i];
                    if (symbolStartAction.Kind != symbol.Kind)
                    {
                        if (filteredActionsBuilderOpt == null)
                        {
                            filteredActionsBuilderOpt = ArrayBuilder<SymbolStartAnalyzerAction>.GetInstance();
                            filteredActionsBuilderOpt.AddRange(symbolStartActions, i);
                        }
                    }
                    else if (filteredActionsBuilderOpt != null)
                    {
                        filteredActionsBuilderOpt.Add(symbolStartAction);
                    }
                }

                return filteredActionsBuilderOpt != null ? filteredActionsBuilderOpt.ToImmutableAndFree() : symbolStartActions;
            }
        }
        /// <summary>
        /// Returns true if the given analyzer has enabled concurrent execution by invoking <see cref="AnalysisContext.EnableConcurrentExecution"/>.
        /// </summary>
        public async Task<bool> IsConcurrentAnalyzerAsync(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor, CancellationToken cancellationToken)
        {
            var sessionScope = await GetSessionAnalysisScopeAsync(analyzer, analyzerExecutor, cancellationToken).ConfigureAwait(false);
            return sessionScope.IsConcurrentAnalyzer();
        }

        /// <summary>
        /// Returns <see cref="GeneratedCodeAnalysisFlags"/> for the given analyzer.
        /// If an analyzer hasn't configured generated code analysis, returns <see cref="AnalyzerDriver.DefaultGeneratedCodeAnalysisFlags"/>.
        /// </summary>
        public async Task<GeneratedCodeAnalysisFlags> GetGeneratedCodeAnalysisFlagsAsync(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor, CancellationToken cancellationToken)
        {
            var sessionScope = await GetSessionAnalysisScopeAsync(analyzer, analyzerExecutor, cancellationToken).ConfigureAwait(false);
            return sessionScope.GetGeneratedCodeAnalysisFlags();
        }

        /// <summary>
        /// Return <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> of given <paramref name="analyzer"/>.
        /// </summary>
        public ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnosticDescriptors(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor,
            CancellationToken cancellationToken)
        {
            var analyzerExecutionContext = GetAnalyzerExecutionContext(analyzer);
            return analyzerExecutionContext.GetOrComputeDiagnosticDescriptors(analyzer, analyzerExecutor, cancellationToken);
        }

        /// <summary>
        /// Return <see cref="DiagnosticSuppressor.SupportedSuppressions"/> of given <paramref name="suppressor"/>.
        /// </summary>
        public ImmutableArray<SuppressionDescriptor> GetSupportedSuppressionDescriptors(
            DiagnosticSuppressor suppressor,
            AnalyzerExecutor analyzerExecutor,
            CancellationToken cancellationToken)
        {
            var analyzerExecutionContext = GetAnalyzerExecutionContext(suppressor);
            return analyzerExecutionContext.GetOrComputeSuppressionDescriptors(suppressor, analyzerExecutor, cancellationToken);
        }

        internal bool IsSupportedDiagnostic(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer, AnalyzerExecutor analyzerExecutor, CancellationToken cancellationToken)
        {
            // Avoid realizing all the descriptors for all compiler diagnostics by assuming that compiler analyzer doesn't report unsupported diagnostics.
            if (isCompilerAnalyzer(analyzer))
            {
                return true;
            }

            // Get all the supported diagnostics and scan them linearly to see if the reported diagnostic is supported by the analyzer.
            // The linear scan is okay, given that this runs only if a diagnostic is being reported and a given analyzer is quite unlikely to have hundreds of thousands of supported diagnostics.
            var supportedDescriptors = GetSupportedDiagnosticDescriptors(analyzer, analyzerExecutor, cancellationToken);
            foreach (var descriptor in supportedDescriptors)
            {
                if (descriptor.Id.Equals(diagnostic.Id, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// </summary>
        internal bool IsDiagnosticAnalyzerSuppressed(
            DiagnosticAnalyzer analyzer,
            CompilationOptions options,
            Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer,
            AnalyzerExecutor analyzerExecutor,
            AnalysisScope analysisScope,
            SeverityFilter severityFilter,
            CancellationToken cancellationToken)
        {
            Func<DiagnosticAnalyzer, ImmutableArray<DiagnosticDescriptor>> getSupportedDiagnosticDescriptors =
                analyzer => GetSupportedDiagnosticDescriptors(analyzer, analyzerExecutor, cancellationToken);
            Func<DiagnosticSuppressor, ImmutableArray<SuppressionDescriptor>> getSupportedSuppressionDescriptors =
                suppressor => GetSupportedSuppressionDescriptors(suppressor, analyzerExecutor, cancellationToken);

            return IsDiagnosticAnalyzerSuppressed(analyzer, options, isCompilerAnalyzer, severityFilter,
                isEnabledWithAnalyzerConfigOptions, getSupportedDiagnosticDescriptors, getSupportedSuppressionDescriptors, cancellationToken);

            bool isEnabledWithAnalyzerConfigOptions(DiagnosticDescriptor descriptor)
            {
                if (analyzerExecutor.Compilation.Options.SyntaxTreeOptionsProvider is { } treeOptions)
                {
                    foreach (var tree in analysisScope.SyntaxTrees)
                    {
                        // Check if diagnostic is enabled by SyntaxTree.DiagnosticOptions or Bulk configuration from AnalyzerConfigOptions.
                        if (treeOptions.TryGetDiagnosticValue(tree, descriptor.Id, cancellationToken, out var configuredValue) ||
                            analyzerExecutor.AnalyzerOptions.TryGetSeverityFromBulkConfiguration(tree, analyzerExecutor.Compilation, descriptor, cancellationToken, out configuredValue))
                        {
                            if (configuredValue != ReportDiagnostic.Suppress && !severityFilter.Contains(configuredValue))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options or filtered by severity.
        /// <para>
        /// An analyzer is considered suppressed if ALL of its supported diagnostics meet one of the following conditions:
        /// <list type="bullet">
        /// <item>The diagnostic is explicitly configured as suppressed (e.g., via /nowarn, .editorconfig, or ruleset)</item>
        /// <item>The diagnostic is disabled by default and not explicitly enabled</item>
        /// <item>The diagnostic's effective severity is in the <paramref name="severityFilter"/> 
        /// (e.g., on command line builds where Hidden and Info diagnostics are filtered out)</item>
        /// </list>
        /// </para>
        /// <para>
        /// Exceptions:
        /// <list type="bullet">
        /// <item>Compiler analyzers are never suppressed</item>
        /// <item>Diagnostics marked as NotConfigurable and enabled by default cannot be suppressed</item>
        /// <item>Diagnostics marked as CustomSeverityConfigurable cannot be suppressed by the compiler</item>
        /// </list>
        /// </para>
        /// </summary>
        internal static bool IsDiagnosticAnalyzerSuppressed(
            DiagnosticAnalyzer analyzer,
            CompilationOptions options,
            Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer,
            SeverityFilter severityFilter,
            Func<DiagnosticDescriptor, bool> isEnabledWithAnalyzerConfigOptions,
            Func<DiagnosticAnalyzer, ImmutableArray<DiagnosticDescriptor>> getSupportedDiagnosticDescriptors,
            Func<DiagnosticSuppressor, ImmutableArray<SuppressionDescriptor>> getSupportedSuppressionDescriptors,
            CancellationToken cancellationToken)
        {
            if (isCompilerAnalyzer(analyzer))
            {
                // Compiler analyzer must always be executed for compiler errors, which cannot be suppressed or filtered.
                return false;
            }

            var supportedDiagnostics = getSupportedDiagnosticDescriptors(analyzer);
            var diagnosticOptions = options.SpecificDiagnosticOptions;

            foreach (var diag in supportedDiagnostics)
            {
                if (diag.IsNotConfigurable())
                {
                    if (diag.IsEnabledByDefault)
                    {
                        // Diagnostic descriptor is not configurable, so the diagnostics created through it cannot be suppressed.
                        return false;
                    }
                    else
                    {
                        // NotConfigurable disabled diagnostic can be ignored as it is never reported.
                        continue;
                    }
                }
                else if (diag.IsCustomSeverityConfigurable())
                {
                    // Analyzer supports custom ways for configuring diagnostic severity that may not be understood by the compiler.
                    // We always consider such analyzers to be non-suppressed. Analyzer is responsible for bailing out early if
                    // it has been suppressed by some custom configuration.
                    return false;
                }

                // Is this diagnostic suppressed by default (as written by the rule author)
                var isSuppressed = !diag.IsEnabledByDefault;

                // Global editorconfig settings overrides the analyzer author
                // Compilation wide user settings (diagnosticOptions) from ruleset/nowarn/warnaserror overrides the analyzer author and global editorconfig settings.
                // Note that "/warnaserror-:DiagnosticId" adds a diagnostic option with value 'ReportDiagnostic.Default',
                // which should not alter 'isSuppressed'.
                if ((diagnosticOptions.TryGetValue(diag.Id, out var severity) && severity != ReportDiagnostic.Default) ||
                    (options.SyntaxTreeOptionsProvider is object && options.SyntaxTreeOptionsProvider.TryGetGlobalDiagnosticValue(diag.Id, cancellationToken, out severity)))
                {
                    isSuppressed = severity == ReportDiagnostic.Suppress;
                }
                else
                {
                    severity = isSuppressed ? ReportDiagnostic.Suppress : DiagnosticDescriptor.MapSeverityToReport(diag.DefaultSeverity);
                }

                // Is this diagnostic suppressed due to its severity being filtered out?
                // For example, on command line builds, Hidden and Info diagnostics are filtered out and not shown in build output,
                // so analyzers that only produce such diagnostics can be skipped entirely for better performance.
                if (severityFilter.Contains(severity))
                {
                    isSuppressed = true;
                }

                // Editorconfig user settings override compilation wide settings.
                if (isSuppressed &&
                    isEnabledWithAnalyzerConfigOptions(diag))
                {
                    isSuppressed = false;
                }

                if (!isSuppressed)
                {
                    return false;
                }
            }

            if (analyzer is DiagnosticSuppressor suppressor)
            {
                foreach (var suppressionDescriptor in getSupportedSuppressionDescriptors(suppressor))
                {
                    if (!suppressionDescriptor.IsDisabled(options))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal static bool HasCompilerOrNotConfigurableTagOrCustomConfigurableTag(ImmutableArray<string> customTags)
        {
            foreach (var customTag in customTags)
            {
                if (customTag is WellKnownDiagnosticTags.Compiler or WellKnownDiagnosticTags.NotConfigurable or WellKnownDiagnosticTags.CustomSeverityConfigurable)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasNotConfigurableTag(ImmutableArray<string> customTags)
            => HasCustomTag(customTags, WellKnownDiagnosticTags.NotConfigurable);

        internal static bool HasCustomSeverityConfigurableTag(ImmutableArray<string> customTags)
            => HasCustomTag(customTags, WellKnownDiagnosticTags.CustomSeverityConfigurable);

        private static bool HasCustomTag(ImmutableArray<string> customTags, string tagToFind)
        {
            foreach (var customTag in customTags)
            {
                if (customTag == tagToFind)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryProcessCompletedMemberAndGetPendingSymbolEndActionsForContainer(
            ISymbol containingSymbol,
            ISymbol processedMemberSymbol,
            DiagnosticAnalyzer analyzer,
            out (ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions, SymbolDeclaredCompilationEvent symbolDeclaredEvent) containerEndActionsAndEvent)
        {
            return GetAnalyzerExecutionContext(analyzer).TryProcessCompletedMemberAndGetPendingSymbolEndActionsForContainer(containingSymbol, processedMemberSymbol, out containerEndActionsAndEvent);
        }

        public bool TryStartExecuteSymbolEndActions(ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions, DiagnosticAnalyzer analyzer, SymbolDeclaredCompilationEvent symbolDeclaredEvent)
        {
            return GetAnalyzerExecutionContext(analyzer).TryStartExecuteSymbolEndActions(symbolEndActions, symbolDeclaredEvent);
        }

        public void MarkSymbolEndAnalysisPending(
            ISymbol symbol,
            DiagnosticAnalyzer analyzer,
            ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions,
            SymbolDeclaredCompilationEvent symbolDeclaredEvent)
        {
            GetAnalyzerExecutionContext(analyzer).MarkSymbolEndAnalysisPending(symbol, symbolEndActions, symbolDeclaredEvent);
        }

        public void MarkSymbolEndAnalysisComplete(ISymbol symbol, DiagnosticAnalyzer analyzer)
        {
            GetAnalyzerExecutionContext(analyzer).MarkSymbolEndAnalysisComplete(symbol);
        }

        [Conditional("DEBUG")]
        public void VerifyAllSymbolEndActionsExecuted()
        {
            foreach (var analyzerExecutionContext in _analyzerExecutionContextMap.Values)
            {
                analyzerExecutionContext.VerifyAllSymbolEndActionsExecuted();
            }
        }
    }
}
