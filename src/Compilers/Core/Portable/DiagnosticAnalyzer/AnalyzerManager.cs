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
        private readonly Dictionary<DiagnosticAnalyzer, Task<HostSessionStartAnalysisScope>> _sessionScopeMap =
            new Dictionary<DiagnosticAnalyzer, Task<HostSessionStartAnalysisScope>>(capacity: 5);

        // This map stores the tasks to compute HostCompilationStartAnalysisScope for per-compilation analyzer actions, i.e. AnalyzerActions registered by analyzer's CompilationStartActions.
        // Compilation start actions will get executed once per-each AnalyzerAndOptions as user might want to return different set of custom actions for each compilation/analyzer options.
        private readonly Dictionary<AnalyzerAndOptions, ConditionalWeakTable<Compilation, Task<HostCompilationStartAnalysisScope>>> _compilationScopeMap =
            new Dictionary<AnalyzerAndOptions, ConditionalWeakTable<Compilation, Task<HostCompilationStartAnalysisScope>>>(capacity: 5);

        /// <summary>
        /// Cache descriptors for each diagnostic analyzer. We do this since <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> is
        /// a property rather than metadata. We expect it to be cheap and immutable, but we can't force them to be so, we cache them
        /// and ask only once.
        /// </summary>
        private readonly Dictionary<DiagnosticAnalyzer, Tuple<ImmutableArray<DiagnosticDescriptor>, EventHandler<Exception>>> _descriptorMap =
            new Dictionary<DiagnosticAnalyzer, Tuple<ImmutableArray<DiagnosticDescriptor>, EventHandler<Exception>>>(capacity: 5);

        private ConditionalWeakTable<Compilation, Task<HostCompilationStartAnalysisScope>> GetOrCreateCompilationActionsCache(AnalyzerAndOptions analyzerAndOptions)
        {
            lock (_compilationScopeMap)
            {
                ConditionalWeakTable<Compilation, Task<HostCompilationStartAnalysisScope>> value;
                if (_compilationScopeMap.TryGetValue(analyzerAndOptions, out value))
                {
                    return value;
                }

                value = new ConditionalWeakTable<Compilation, Task<HostCompilationStartAnalysisScope>>();
                _compilationScopeMap.Add(analyzerAndOptions, value);
                return value;
            }
        }

        private void ClearCompilationScopeMap(AnalyzerAndOptions analyzerAndOptions, AnalyzerExecutor analyzerExecutor)
        {
            ConditionalWeakTable<Compilation, Task<HostCompilationStartAnalysisScope>> compilationActionsCache;
            lock (_compilationScopeMap)
            {
                if (_compilationScopeMap.TryGetValue(analyzerAndOptions, out compilationActionsCache))
                {
                    compilationActionsCache.Remove(analyzerExecutor.Compilation);
                }
            }
        }

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
            var compilationActionsCache = GetOrCreateCompilationActionsCache(analyzerAndOptions);
            return compilationActionsCache.GetValue(analyzerExecutor.Compilation, callback);
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
                ClearCompilationScopeMap(analyzerAndOptions, analyzerExecutor);

                analyzerExecutor.CancellationToken.ThrowIfCancellationRequested();
                return await GetCompilationAnalysisScopeAsync(analyzer, sessionScope, analyzerExecutor).ConfigureAwait(false);
            }
        }

        private Task<HostSessionStartAnalysisScope> GetSessionAnalysisScopeTask(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor)
        {
            lock (_sessionScopeMap)
            {
                return GetSessionAnalysisScopeTask_NoLock(analyzer, analyzerExecutor);
            }
        }

        private Task<HostSessionStartAnalysisScope> GetSessionAnalysisScopeTask_NoLock(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor)
        {
            Task<HostSessionStartAnalysisScope> task;
            if (_sessionScopeMap.TryGetValue(analyzer, out task))
            {
                return task;
            }

            task = Task.Run(() =>
            {
                var sessionScope = new HostSessionStartAnalysisScope();
                analyzerExecutor.ExecuteInitializeMethod(analyzer, sessionScope);
                return sessionScope;
            }, analyzerExecutor.CancellationToken);

            _sessionScopeMap.Add(analyzer, task);
            return task;
        }

        private async Task<HostSessionStartAnalysisScope> GetSessionAnalysisScopeAsync(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor)
        {
            try
            {
                var task = GetSessionAnalysisScopeTask(analyzer, analyzerExecutor);
                return await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Task to compute the scope was cancelled.
                // Clear the entry in scope map for analyzer, so we can attempt a retry.
                ClearSessionScopeMap(analyzer);

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
        /// If an analyzer hasn't configured generated code analysis, returns <see cref="AnalyzerDriver.DefaultGeneratedCodeAnalysisFlags"/>.
        /// </summary>
        public async Task<GeneratedCodeAnalysisFlags> GetGeneratedCodeAnalysisFlagsAsync(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor)
        {
            var sessionScope = await GetSessionAnalysisScopeAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
            return sessionScope.GetGeneratedCodeAnalysisFlags(analyzer);
        }

        /// <summary>
        /// Compute <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> and exception handler for the given <paramref name="analyzer"/>.
        /// </summary>
        private static Tuple<ImmutableArray<DiagnosticDescriptor>, EventHandler<Exception>> ComputeDescriptorsAndHandler(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor)
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
        }

        /// <summary>
        /// Return <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> of given <paramref name="analyzer"/>.
        /// </summary>
        public ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnosticDescriptors(
            DiagnosticAnalyzer analyzer,
            AnalyzerExecutor analyzerExecutor)
        {
            // Check if the value has already been computed and stored.
            Tuple<ImmutableArray<DiagnosticDescriptor>, EventHandler<Exception>> value;
            lock (_descriptorMap)
            {
                if (_descriptorMap.TryGetValue(analyzer, out value))
                {
                    return value.Item1;
                }
            }

            // Otherwise, compute the value.
            // We do so outside the lock statement as we are calling into user code, which may be a long running operation.
            value = ComputeDescriptorsAndHandler(analyzer, analyzerExecutor);

            lock (_descriptorMap)
            {
                // Check if another thread already stored the computed value.
                Tuple<ImmutableArray<DiagnosticDescriptor>, EventHandler<Exception>> storedValue;
                if (_descriptorMap.TryGetValue(analyzer, out storedValue))
                {
                    // If so, we return the stored value.
                    value = storedValue;
                }
                else
                {
                    // Otherwise, store the value computed here.
                    _descriptorMap.Add(analyzer, value);
                }
            }

            return value.Item1;
        }

        /// <summary>
        /// This method should be invoked when the analyzer host is disposing off the analyzers.
        /// It unregisters the exception handler hooked up to the descriptors' LocalizableString fields and subsequently removes the cached descriptors for the analyzers.
        /// </summary>
        internal void ClearAnalyzerState(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            if (!analyzers.IsDefaultOrEmpty)
            {
                ClearDescriptorState(analyzers);
                ClearAnalysisScopeState(analyzers);
            }
        }

        private void ClearDescriptorState(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            lock (_descriptorMap)
            {
                foreach (var analyzer in analyzers)
                {
                    // Host is disposing the analyzer instance, unsubscribe analyzer exception handlers.
                    Tuple<ImmutableArray<DiagnosticDescriptor>, EventHandler<Exception>> value;
                    if (_descriptorMap.TryGetValue(analyzer, out value))
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

                        _descriptorMap.Remove(analyzer);
                    }
                }
            }
        }

        private void ClearSessionScopeMap(DiagnosticAnalyzer analyzer)
        {
            lock (_sessionScopeMap)
            {
                _sessionScopeMap.Remove(analyzer);
            }
        }

        private void ClearAnalysisScopeState(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            // Clear session scope.
            lock (_sessionScopeMap)
            {
                ClearSessionScopeMap_NoLock(analyzers);
            }

            // Clear compilation scope.
            lock (_compilationScopeMap)
            {
                ClearCompilationScopeMap_NoLock(analyzers);
            }            
        }

        private void ClearSessionScopeMap_NoLock(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            foreach (var analyzer in analyzers)
            {
                _sessionScopeMap.Remove(analyzer);
            }
        }

        private void ClearCompilationScopeMap_NoLock(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            var keysToRemove = ArrayBuilder<AnalyzerAndOptions>.GetInstance();
            var analyzersSet = analyzers.ToImmutableHashSet();
            foreach (var analyzerAndOptions in _compilationScopeMap.Keys)
            {
                if (analyzersSet.Contains(analyzerAndOptions.Analyzer))
                {
                    keysToRemove.Add(analyzerAndOptions);
                }
            }

            foreach (var analyzerAndOptions in keysToRemove)
            {
                _compilationScopeMap.Remove(analyzerAndOptions);
            }

            keysToRemove.Free();
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
