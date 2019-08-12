// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
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
        private readonly ImmutableDictionary<DiagnosticAnalyzer, AnalyzerExecutionContext> _analyzerExecutionContextMap;

        public AnalyzerManager(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            _analyzerExecutionContextMap = CreateAnalyzerExecutionContextMap(analyzers);
        }

        public AnalyzerManager(DiagnosticAnalyzer analyzer)
        {
            _analyzerExecutionContextMap = CreateAnalyzerExecutionContextMap(SpecializedCollections.SingletonEnumerable(analyzer));
        }

        private ImmutableDictionary<DiagnosticAnalyzer, AnalyzerExecutionContext> CreateAnalyzerExecutionContextMap(IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, AnalyzerExecutionContext>();
            foreach (var analyzer in analyzers)
            {
                builder.Add(analyzer, new AnalyzerExecutionContext(analyzer));
            }

            return builder.ToImmutable();
        }

        private AnalyzerExecutionContext GetAnalyzerExecutionContext(DiagnosticAnalyzer analyzer) => _analyzerExecutionContextMap[analyzer];

        private async Task<HostCompilationStartAnalysisScope> GetCompilationAnalysisScopeAsync(
            DiagnosticAnalyzer analyzer,
            HostSessionStartAnalysisScope sessionScope,
            AnalyzerExecutor analyzerExecutor)
        {
            var analyzerExecutionContext = GetAnalyzerExecutionContext(analyzer);
            return await GetCompilationAnalysisScopeCoreAsync(sessionScope, analyzerExecutor, analyzerExecutionContext).ConfigureAwait(false);
        }

        private async Task<HostCompilationStartAnalysisScope> GetCompilationAnalysisScopeCoreAsync(
            HostSessionStartAnalysisScope sessionScope,
            AnalyzerExecutor analyzerExecutor,
            AnalyzerExecutionContext analyzerExecutionContext)
        {
            try
            {
                return await analyzerExecutionContext.GetCompilationAnalysisScopeAsync(sessionScope, analyzerExecutor).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Task to compute the scope was cancelled.
                // Clear the compilation scope for analyzer, so we can attempt a retry.
                analyzerExecutionContext.ClearCompilationScopeTask();

                analyzerExecutor.CancellationToken.ThrowIfCancellationRequested();
                return await GetCompilationAnalysisScopeCoreAsync(sessionScope, analyzerExecutor, analyzerExecutionContext).ConfigureAwait(false);
            }
        }

        private async Task<HostSymbolStartAnalysisScope> GetSymbolAnalysisScopeAsync(
            ISymbol symbol,
            DiagnosticAnalyzer analyzer,
            ImmutableArray<SymbolStartAnalyzerAction> symbolStartActions,
            AnalyzerExecutor analyzerExecutor)
        {
            var analyzerExecutionContext = GetAnalyzerExecutionContext(analyzer);
            return await GetSymbolAnalysisScopeCoreAsync(symbol, symbolStartActions, analyzerExecutor, analyzerExecutionContext).ConfigureAwait(false);
        }

        private async Task<HostSymbolStartAnalysisScope> GetSymbolAnalysisScopeCoreAsync(
            ISymbol symbol,
            ImmutableArray<SymbolStartAnalyzerAction> symbolStartActions,
            AnalyzerExecutor analyzerExecutor,
            AnalyzerExecutionContext analyzerExecutionContext)
        {
            try
            {
                return await analyzerExecutionContext.GetSymbolAnalysisScopeAsync(symbol, symbolStartActions, analyzerExecutor).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Task to compute the scope was cancelled.
                // Clear the symbol scope for analyzer, so we can attempt a retry.
                analyzerExecutionContext.ClearSymbolScopeTask(symbol);

                analyzerExecutor.CancellationToken.ThrowIfCancellationRequested();
                return await GetSymbolAnalysisScopeCoreAsync(symbol, symbolStartActions, analyzerExecutor, analyzerExecutionContext).ConfigureAwait(false);
            }
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        private async ValueTask<HostSessionStartAnalysisScope> GetSessionAnalysisScopeAsync(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor)
        {
            var analyzerExecutionContext = GetAnalyzerExecutionContext(analyzer);
            return await GetSessionAnalysisScopeCoreAsync(analyzerExecutor, analyzerExecutionContext).ConfigureAwait(false);
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        private async ValueTask<HostSessionStartAnalysisScope> GetSessionAnalysisScopeCoreAsync(
            AnalyzerExecutor analyzerExecutor,
            AnalyzerExecutionContext analyzerExecutionContext)
        {
            try
            {
                var task = analyzerExecutionContext.GetSessionAnalysisScopeTask(analyzerExecutor);
                return await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Task to compute the scope was cancelled.
                // Clear the entry in scope map for analyzer, so we can attempt a retry.
                analyzerExecutionContext.ClearSessionScopeTask();

                analyzerExecutor.CancellationToken.ThrowIfCancellationRequested();
                return await GetSessionAnalysisScopeCoreAsync(analyzerExecutor, analyzerExecutionContext).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get all the analyzer actions to execute for the given analyzer against a given compilation.
        /// The returned actions include the actions registered during <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> method as well as
        /// the actions registered during <see cref="CompilationStartAnalyzerAction"/> for the given compilation.
        /// </summary>
        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        public async ValueTask<AnalyzerActions> GetAnalyzerActionsAsync(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor)
        {
            var sessionScope = await GetSessionAnalysisScopeAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
            if (sessionScope.GetAnalyzerActions(analyzer).CompilationStartActions.Length > 0 && analyzerExecutor.Compilation != null)
            {
                var compilationScope = await GetCompilationAnalysisScopeAsync(analyzer, sessionScope, analyzerExecutor).ConfigureAwait(false);
                return compilationScope.GetAnalyzerActions(analyzer);
            }

            return sessionScope.GetAnalyzerActions(analyzer);
        }

        /// <summary>
        /// Get the per-symbol analyzer actions to be executed by the given analyzer.
        /// These are the actions registered during the various RegisterSymbolStartAction method invocations for the given symbol on different analysis contexts.
        /// </summary>
        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        public async ValueTask<AnalyzerActions> GetPerSymbolAnalyzerActionsAsync(ISymbol symbol, DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor)
        {
            var analyzerActions = await GetAnalyzerActionsAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
            if (analyzerActions.SymbolStartActionsCount > 0)
            {
                var filteredSymbolStartActions = getFilteredActionsByKind(analyzerActions.SymbolStartActions);
                if (filteredSymbolStartActions.Length > 0)
                {
                    var symbolScope = await GetSymbolAnalysisScopeAsync(symbol, analyzer, filteredSymbolStartActions, analyzerExecutor).ConfigureAwait(false);
                    return symbolScope.GetAnalyzerActions(analyzer);
                }
            }

            return null;

            ImmutableArray<SymbolStartAnalyzerAction> getFilteredActionsByKind(ImmutableArray<SymbolStartAnalyzerAction> symbolStartActions)
            {
                ArrayBuilder<SymbolStartAnalyzerAction> filteredActionsBuilderOpt = null;
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
        public async Task<bool> IsConcurrentAnalyzerAsync(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor)
        {
            var sessionScope = await GetSessionAnalysisScopeAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
            return sessionScope.IsConcurrentAnalyzer(analyzer);
        }

        /// <summary>
        /// Returns <see cref="GeneratedCodeAnalysisFlags"/> for the given analyzer.
        /// If an analyzer hasn't configured generated code analysis, returns <see cref="AnalyzerDriver.DefaultGeneratedCodeAnalysisFlags"/>.
        /// </summary>
        public async Task<GeneratedCodeAnalysisFlags> GetGeneratedCodeAnalysisFlagsAsync(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor)
        {
            var sessionScope = await GetSessionAnalysisScopeAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
            return sessionScope.GetGeneratedCodeAnalysisFlags(analyzer);
        }

        private static void ForceLocalizableStringExceptions(LocalizableString localizableString, EventHandler<Exception> handler)
        {
            if (localizableString.CanThrowExceptions)
            {
                localizableString.OnException += handler;
                localizableString.ToString();
                localizableString.OnException -= handler;
            }
        }

        /// <summary>
        /// Return <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> of given <paramref name="analyzer"/>.
        /// </summary>
        public ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnosticDescriptors(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor)
        {
            var analyzerExecutionContext = GetAnalyzerExecutionContext(analyzer);
            return analyzerExecutionContext.GetOrComputeDiagnosticDescriptors(analyzer, analyzerExecutor);
        }

        /// <summary>
        /// Return <see cref="DiagnosticSuppressor.SupportedSuppressions"/> of given <paramref name="suppressor"/>.
        /// </summary>
        public ImmutableArray<SuppressionDescriptor> GetSupportedSuppressionDescriptors(
            DiagnosticSuppressor suppressor,
            AnalyzerExecutor analyzerExecutor)
        {
            var analyzerExecutionContext = GetAnalyzerExecutionContext(suppressor);
            return analyzerExecutionContext.GetOrComputeSuppressionDescriptors(suppressor, analyzerExecutor);
        }

        internal bool IsSupportedDiagnostic(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer, AnalyzerExecutor analyzerExecutor)
        {
            // Avoid realizing all the descriptors for all compiler diagnostics by assuming that compiler analyzer doesn't report unsupported diagnostics.
            if (isCompilerAnalyzer(analyzer))
            {
                return true;
            }

            // Get all the supported diagnostics and scan them linearly to see if the reported diagnostic is supported by the analyzer.
            // The linear scan is okay, given that this runs only if a diagnostic is being reported and a given analyzer is quite unlikely to have hundreds of thousands of supported diagnostics.
            var supportedDescriptors = GetSupportedDiagnosticDescriptors(analyzer, analyzerExecutor);
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
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options or compilation wide analyzer config options.
        /// </summary>
        internal bool IsDiagnosticAnalyzerSuppressed(
            DiagnosticAnalyzer analyzer,
            CompilationOptions options,
            Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer,
            AnalyzerExecutor analyzerExecutor)
        {
            if (isCompilerAnalyzer(analyzer))
            {
                // Compiler analyzer must always be executed for compiler errors, which cannot be suppressed or filtered.
                return false;
            }

            var supportedDiagnostics = GetSupportedDiagnosticDescriptors(analyzer, analyzerExecutor);
            var diagnosticOptions = options.SpecificDiagnosticOptions;

            foreach (var diag in supportedDiagnostics)
            {
                if (HasNotConfigurableTag(diag.CustomTags))
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

                // Is this diagnostic suppressed by default (as written by the rule author)
                var isSuppressed = !diag.IsEnabledByDefault;

                // Diagnostic configuration via compilation options (ruleset or /nowarn) or via compilation wide analyzer config options overrides the author.
                if (diagnosticOptions.TryGetValue(diag.Id, out var severity))
                {
                    isSuppressed = severity == ReportDiagnostic.Suppress;
                }
                else if (analyzerExecutor.Compilation != null &&
                    isConfiguredWithAnalyzerConfigOptions(diag, analyzerExecutor.Compilation, out var hasCompilationWideSuppression))
                {
                    isSuppressed = hasCompilationWideSuppression;
                }

                if (!isSuppressed)
                {
                    return false;
                }
            }

            if (analyzer is DiagnosticSuppressor suppressor)
            {
                foreach (var suppressionDescriptor in GetSupportedSuppressionDescriptors(suppressor, analyzerExecutor))
                {
                    if (!suppressionDescriptor.IsDisabled(options))
                    {
                        return false;
                    }
                }
            }

            return true;

            static bool isConfiguredWithAnalyzerConfigOptions(DiagnosticDescriptor diagnosticDescriptor, Compilation compilation, out bool hasCompilationWideSuppression)
            {
                hasCompilationWideSuppression = false;
                if (compilation.SyntaxTrees.IsEmpty())
                {
                    return false;
                }

                var hasConfigurationEntry = false;
                var hasTreeWithoutConfiguration = false;
                foreach (var tree in compilation.SyntaxTrees)
                {
                    if (!tree.DiagnosticOptions.TryGetValue(diagnosticDescriptor.Id, out var configuredValue))
                    {
                        hasTreeWithoutConfiguration = true;
                    }
                    else
                    {
                        hasConfigurationEntry = true;
                        if (configuredValue != ReportDiagnostic.Suppress)
                        {
                            hasCompilationWideSuppression = false;
                            return true;
                        }
                    }
                }

                hasCompilationWideSuppression = !hasTreeWithoutConfiguration;
                return hasConfigurationEntry;
            }
        }

        internal static bool HasNotConfigurableTag(IEnumerable<string> customTags)
        {
            foreach (var customTag in customTags)
            {
                if (customTag == WellKnownDiagnosticTags.NotConfigurable)
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
