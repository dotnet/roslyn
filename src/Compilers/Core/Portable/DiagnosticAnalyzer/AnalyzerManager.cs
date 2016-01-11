// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Manages properties of analyzers (such as registered actions, supported diagnostics) for analyzer host's lifetime
    /// and executes the callbacks into the analyzers.
    /// 
    /// It ensures the following for the lifetime of analyzer host:
    /// 1) <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> is invoked only once per-analyzer.
    /// 2) <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> is invoked only once per-analyzer.
    /// 3) <see cref="CompilationStartAnalyzerAction"/> registered during Initialize are invoked only once per-compilation per-<see cref="AnalyzerAndOptions"/>
    /// </summary>
    /// <remarks>
    /// TODO: Consider moving <see cref="_compilationScopeMap"/> and relevant APIs <see cref="GetCompilationAnalysisScopeAsync(DiagnosticAnalyzer, HostSessionStartAnalysisScope, AnalyzerExecutor)"/>
    /// out of the AnalyzerManager and into analyzer drivers.
    /// </remarks>
    internal partial class AnalyzerManager
    {
        /// <summary>
        /// Gets the default instance of the AnalyzerManager for the lifetime of the analyzer host process.
        /// </summary>
        public static readonly AnalyzerManager Instance = new AnalyzerManager();

        // This map stores the tasks to compute HostSessionStartAnalysisScope for session wide analyzer actions, i.e. AnalyzerActions registered by analyzer's Initialize method.
        // These are run only once per every analyzer.
        private readonly ConcurrentDictionary<DiagnosticAnalyzer, Task<HostSessionStartAnalysisScope>> _sessionScopeMap =
            new ConcurrentDictionary<DiagnosticAnalyzer, Task<HostSessionStartAnalysisScope>>(concurrencyLevel: 2, capacity: 5);

        // This map stores the tasks to compute HostCompilationStartAnalysisScope for per-compilation analyzer actions, i.e. AnalyzerActions registered by analyzer's CompilationStartActions.
        // Compilation start actions will get executed once per-each AnalyzerAndOptions as user might want to return different set of custom actions for each compilation/analyzer options.
        private readonly ConcurrentDictionary<AnalyzerAndOptions, ConditionalWeakTable<Compilation, Task<HostCompilationStartAnalysisScope>>> _compilationScopeMap =
            new ConcurrentDictionary<AnalyzerAndOptions, ConditionalWeakTable<Compilation, Task<HostCompilationStartAnalysisScope>>>(concurrencyLevel: 2, capacity: 5);

        /// <summary>
        /// Cache descriptors for each diagnostic analyzer. We do this since <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> is
        /// a property rather than metadata. We expect it to be cheap and immutable, but we can't force them to be so, we cache them
        /// and ask only once.
        /// </summary>
        private readonly ConcurrentDictionary<DiagnosticAnalyzer, Tuple<ImmutableArray<DiagnosticDescriptor>, EventHandler<Exception>>> _descriptorCache =
            new ConcurrentDictionary<DiagnosticAnalyzer, Tuple<ImmutableArray<DiagnosticDescriptor>, EventHandler<Exception>>>(concurrencyLevel: 2, capacity: 5);

        private Task<HostCompilationStartAnalysisScope> GetCompilationAnalysisScopeCoreAsync(
            AnalyzerAndOptions analyzerAndOptions,
            HostSessionStartAnalysisScope sessionScope,
            AnalyzerExecutor analyzerExecutor)
        {
            Func<Compilation, Task<HostCompilationStartAnalysisScope>> getTask = comp =>
            {
                return Task.Run(() =>
                {
                    var compilationAnalysisScope = new HostCompilationStartAnalysisScope(sessionScope);
                    analyzerExecutor.ExecuteCompilationStartActions(sessionScope.CompilationStartActions, compilationAnalysisScope);
                    return compilationAnalysisScope;
                }, analyzerExecutor.CancellationToken);
            };

            var callback = new ConditionalWeakTable<Compilation, Task<HostCompilationStartAnalysisScope>>.CreateValueCallback(getTask);
            var compilationActionsMap = _compilationScopeMap.GetOrAdd(analyzerAndOptions, new ConditionalWeakTable<Compilation, Task<HostCompilationStartAnalysisScope>>());
            return compilationActionsMap.GetValue(analyzerExecutor.Compilation, callback);
        }

        private async Task<HostCompilationStartAnalysisScope> GetCompilationAnalysisScopeAsync(
            DiagnosticAnalyzer analyzer,
            HostSessionStartAnalysisScope sessionScope,
            AnalyzerExecutor analyzerExecutor)
        {
            var analyzerAndOptions = new AnalyzerAndOptions(analyzer, analyzerExecutor.AnalyzerOptions);

            try
            {
                return await GetCompilationAnalysisScopeCoreAsync(analyzerAndOptions, sessionScope, analyzerExecutor).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Task to compute the scope was cancelled.
                // Clear the entry in scope map for analyzer, so we can attempt a retry.
                ConditionalWeakTable<Compilation, Task<HostCompilationStartAnalysisScope>> compilationActionsMap;
                if (_compilationScopeMap.TryGetValue(analyzerAndOptions, out compilationActionsMap))
                {
                    compilationActionsMap.Remove(analyzerExecutor.Compilation);
                }

                analyzerExecutor.CancellationToken.ThrowIfCancellationRequested();
                return await GetCompilationAnalysisScopeAsync(analyzer, sessionScope, analyzerExecutor).ConfigureAwait(false);
            }
        }

        private Task<HostSessionStartAnalysisScope> GetSessionAnalysisScopeCoreAsync(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor)
        {
            Func<DiagnosticAnalyzer, Task<HostSessionStartAnalysisScope>> getTask = a =>
            {
                return Task.Run(() =>
                {
                    var sessionScope = new HostSessionStartAnalysisScope();
                    analyzerExecutor.ExecuteInitializeMethod(a, sessionScope);
                    return sessionScope;
                }, analyzerExecutor.CancellationToken);
            };

            return _sessionScopeMap.GetOrAdd(analyzer, getTask);
        }

        private async Task<HostSessionStartAnalysisScope> GetSessionAnalysisScopeAsync(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor)
        {
            try
            {
                return await GetSessionAnalysisScopeCoreAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Task to compute the scope was cancelled.
                // Clear the entry in scope map for analyzer, so we can attempt a retry.
                Task<HostSessionStartAnalysisScope> cancelledTask;
                _sessionScopeMap.TryRemove(analyzer, out cancelledTask);

                analyzerExecutor.CancellationToken.ThrowIfCancellationRequested();
                return await GetSessionAnalysisScopeAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get all the analyzer actions to execute for the given analyzer against a given compilation.
        /// The returned actions include the actions registered during <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> method as well as
        /// the actions registered during <see cref="CompilationStartAnalyzerAction"/> for the given compilation.
        /// </summary>
        public async Task<AnalyzerActions> GetAnalyzerActionsAsync(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor)
        {
            var sessionScope = await GetSessionAnalysisScopeAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
            if (sessionScope.CompilationStartActions.Length > 0 && analyzerExecutor.Compilation != null)
            {
                var compilationScope = await GetCompilationAnalysisScopeAsync(analyzer, sessionScope, analyzerExecutor).ConfigureAwait(false);
                return compilationScope.GetAnalyzerActions(analyzer);
            }

            return sessionScope.GetAnalyzerActions(analyzer);
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
        /// If an analyzer hasn't configured generated code analysis, returns <see cref="GeneratedCodeAnalysisFlags.Default"/>.
        /// </summary>
        public async Task<GeneratedCodeAnalysisFlags> GetGeneratedCodeAnalysisFlagsAsync(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor)
        {
            var sessionScope = await GetSessionAnalysisScopeAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
            return sessionScope.GetGeneratedCodeAnalysisFlags(analyzer);                
        }

        /// <summary>
        /// Return <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> of given <paramref name="analyzer"/>.
        /// </summary>
        public ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnosticDescriptors(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor)
        {
            var descriptors = _descriptorCache.GetOrAdd(analyzer, key =>
            {
                var supportedDiagnostics = ImmutableArray<DiagnosticDescriptor>.Empty;

                // Catch Exception from analyzer.SupportedDiagnostics
                analyzerExecutor.ExecuteAndCatchIfThrows(analyzer, () =>
                    {
                        var supportedDiagnosticsLocal = analyzer.SupportedDiagnostics;
                        if (!supportedDiagnosticsLocal.IsDefaultOrEmpty)
                        {
                            supportedDiagnostics = supportedDiagnosticsLocal;
                        }
                    });

                EventHandler<Exception> handler = null;
                Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = analyzerExecutor.OnAnalyzerException;
                if (onAnalyzerException != null)
                {
                    handler = new EventHandler<Exception>((sender, ex) =>
                    {
                        var diagnostic = AnalyzerExecutor.CreateAnalyzerExceptionDiagnostic(analyzer, ex);
                        onAnalyzerException(ex, analyzer, diagnostic);
                    });

                    // Subscribe for exceptions from lazily evaluated localizable strings in the descriptors.
                    foreach (var descriptor in supportedDiagnostics)
                    {
                        descriptor.Title.OnException += handler;
                        descriptor.MessageFormat.OnException += handler;
                        descriptor.Description.OnException += handler;
                    }
                }

                return Tuple.Create(supportedDiagnostics, handler);
            });

            return descriptors.Item1;
        }

        /// <summary>
        /// This method should be invoked when the analyzer host is disposing off the analyzers.
        /// It unregisters the exception handler hooked up to the descriptors' LocalizableString fields and subsequently removes the cached descriptors for the analyzers.
        /// </summary>
        internal void ClearAnalyzerState(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            if (!analyzers.IsDefaultOrEmpty)
            {
                foreach (var analyzer in analyzers)
                {
                    ClearDescriptorState(analyzer);
                    ClearAnalysisScopeState(analyzer);
                }
            }
        }

        private void ClearDescriptorState(DiagnosticAnalyzer analyzer)
        {
            // Host is disposing the analyzer instance, unsubscribe analyzer exception handlers.
            Tuple<ImmutableArray<DiagnosticDescriptor>, EventHandler<Exception>> value;
            if (_descriptorCache.TryRemove(analyzer, out value))
            {
                var descriptors = value.Item1;
                var handler = value.Item2;
                if (handler != null)
                {
                    foreach (var descriptor in descriptors)
                    {
                        descriptor.Title.OnException -= handler;
                        descriptor.MessageFormat.OnException -= handler;
                        descriptor.Description.OnException -= handler;
                    }
                }
            }
        }

        private void ClearAnalysisScopeState(DiagnosticAnalyzer analyzer)
        {
            // Clear session scope.
            Task<HostSessionStartAnalysisScope> canceledTask;
            _sessionScopeMap.TryRemove(analyzer, out canceledTask);

            // Clear compilation scope.
            var keysToRemove = _compilationScopeMap.Keys.Where(analyzerAndOptions => analyzerAndOptions.Analyzer.Equals(analyzer)).ToImmutableArray();
            foreach (var analyzerAndOptions in keysToRemove)
            {
                ConditionalWeakTable<Compilation, Task<HostCompilationStartAnalysisScope>> map;
                _compilationScopeMap.TryRemove(analyzerAndOptions, out map);
            }
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
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
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

                // If the user said something about it, that overrides the author.
                if (diagnosticOptions.ContainsKey(diag.Id))
                {
                    isSuppressed = diagnosticOptions[diag.Id] == ReportDiagnostic.Suppress;
                }

                if (!isSuppressed)
                {
                    return false;
                }
            }

            return true;
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
    }
}
