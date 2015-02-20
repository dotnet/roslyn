// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
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
    /// 3) <see cref="CompilationStartAnalyzerAction"/> registered during Initialize are invoked only once per-analyzer per-compilation.
    /// </summary>
    internal partial class AnalyzerManager
    {
        /// <summary>
        /// Gets the default instance of the AnalyzerManager for the lifetime of the analyzer host process.
        /// </summary>
        public static readonly AnalyzerManager Instance = new AnalyzerManager();

        // This map stores the tasks to compute HostSessionStartAnalysisScope for session wide analyzer actions, i.e. AnalyzerActions registered by analyzer's Initialize method.
        // These are run only once per every analyzer.
        private readonly ConditionalWeakTable<DiagnosticAnalyzer, Task<HostSessionStartAnalysisScope>> _sessionScopeMap =
            new ConditionalWeakTable<DiagnosticAnalyzer, Task<HostSessionStartAnalysisScope>>();

        // This map stores the tasks to compute HostCompilationStartAnalysisScope for per-compilation analyzer actions, i.e. AnalyzerActions registered by analyzer's CompilationStartActions.
        // Compilation start actions will get executed once per-each compilation as user might want to return different set of custom actions for each compilation.
        private readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<DiagnosticAnalyzer, Task<HostCompilationStartAnalysisScope>>> _compilationScopeMap =
            new ConditionalWeakTable<Compilation, ConcurrentDictionary<DiagnosticAnalyzer, Task<HostCompilationStartAnalysisScope>>>();

        /// <summary>
        /// Cache descriptors for each diagnostic analyzer. We do this since <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> is
        /// a property rather than metadata. We expect it to be cheap and immutable, but we can't force them to be so, we cache them
        /// and ask only once.
        /// </summary>
        private readonly ConditionalWeakTable<DiagnosticAnalyzer, IReadOnlyList<DiagnosticDescriptor>> _descriptorCache =
            new ConditionalWeakTable<DiagnosticAnalyzer, IReadOnlyList<DiagnosticDescriptor>>();
        
        private Task<HostCompilationStartAnalysisScope> GetCompilationAnalysisScopeCoreAsync(
            DiagnosticAnalyzer analyzer,
            HostSessionStartAnalysisScope sessionScope,
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            Func<DiagnosticAnalyzer, Task<HostCompilationStartAnalysisScope>> getTask = a =>
            {
                return Task.Run(() =>
                {
                    var compilationAnalysisScope = new HostCompilationStartAnalysisScope(sessionScope);
                    ExecuteCompilationStartActions(sessionScope.CompilationStartActions, compilationAnalysisScope, compilation,
                        analyzerOptions, continueOnAnalyzerException, cancellationToken);
                    return compilationAnalysisScope;
                }, cancellationToken);
            };

            var compilationActionsMap = _compilationScopeMap.GetOrCreateValue(compilation);
            return compilationActionsMap.GetOrAdd(analyzer, getTask);
        }

        private async Task<HostCompilationStartAnalysisScope> GetCompilationAnalysisScopeAsync(
            DiagnosticAnalyzer analyzer,
            HostSessionStartAnalysisScope sessionScope,
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            try
            {
                return await GetCompilationAnalysisScopeCoreAsync(analyzer, sessionScope,
                    compilation, analyzerOptions, continueOnAnalyzerException, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Task to compute the scope was cancelled.
                // Clear the entry in scope map for analyzer, so we can attempt a retry.
                var compilationActionsMap = _compilationScopeMap.GetOrCreateValue(compilation);
                Task<HostCompilationStartAnalysisScope> cancelledTask;
                compilationActionsMap.TryRemove(analyzer, out cancelledTask);

                cancellationToken.ThrowIfCancellationRequested();
                return await GetCompilationAnalysisScopeAsync(analyzer, sessionScope,
                    compilation, analyzerOptions, continueOnAnalyzerException, cancellationToken).ConfigureAwait(false);

            }
        }

        private Task<HostSessionStartAnalysisScope> GetSessionAnalysisScopeCoreAsync(
            DiagnosticAnalyzer analyzer,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            Func<DiagnosticAnalyzer, Task<HostSessionStartAnalysisScope>> getTask = a =>
            {
                return Task.Run(() =>
                {
                    var sessionScope = new HostSessionStartAnalysisScope();
                    ExecuteInitializeMethod(a, sessionScope, continueOnAnalyzerException, cancellationToken);
                    return sessionScope;
                }, cancellationToken);
            };

            var callback = new ConditionalWeakTable<DiagnosticAnalyzer, Task<HostSessionStartAnalysisScope>>.CreateValueCallback(getTask);
            return _sessionScopeMap.GetValue(analyzer, callback);
        }

        private async Task<HostSessionStartAnalysisScope> GetSessionAnalysisScopeAsync(
            DiagnosticAnalyzer analyzer,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            try
            {
                return await GetSessionAnalysisScopeCoreAsync(analyzer, continueOnAnalyzerException, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Task to compute the scope was cancelled.
                // Clear the entry in scope map for analyzer, so we can attempt a retry.
                _sessionScopeMap.Remove(analyzer);

                cancellationToken.ThrowIfCancellationRequested();
                return await GetSessionAnalysisScopeAsync(analyzer, continueOnAnalyzerException, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get all the analyzer actions to execute for the given analyzer against a given compilation.
        /// The returned actions include the actions registered during <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> method as well as
        /// the actions registered during <see cref="CompilationStartAnalyzerAction"/> for the given compilation.
        /// </summary>
        public async Task<AnalyzerActions> GetAnalyzerActionsAsync(
            DiagnosticAnalyzer analyzer,
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            var sessionScope = await GetSessionAnalysisScopeAsync(analyzer, continueOnAnalyzerException, cancellationToken).ConfigureAwait(false);
            if (sessionScope.CompilationStartActions.Length > 0 && compilation != null)
            {
                var compilationScope = await GetCompilationAnalysisScopeAsync(analyzer, sessionScope,
                    compilation, analyzerOptions, continueOnAnalyzerException, cancellationToken).ConfigureAwait(false);
                return compilationScope.GetAnalyzerActions(analyzer);
            }

            return sessionScope.GetAnalyzerActions(analyzer);
        }

        /// <summary>
        /// Return <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> of given <paramref name="analyzer"/>.
        /// </summary>
        public ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnosticDescriptors(
            DiagnosticAnalyzer analyzer,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            var descriptors = _descriptorCache.GetValue(analyzer, key =>
            {
                var supportedDiagnostics = ImmutableArray<DiagnosticDescriptor>.Empty;

                // Catch Exception from analyzer.SupportedDiagnostics
                ExecuteAndCatchIfThrows(analyzer, continueOnAnalyzerException, () => { supportedDiagnostics = analyzer.SupportedDiagnostics; }, cancellationToken);

                return supportedDiagnostics;
            });

            return (ImmutableArray<DiagnosticDescriptor>)descriptors;
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// </summary>
        internal bool IsDiagnosticAnalyzerSuppressed(
            DiagnosticAnalyzer analyzer,
            CompilationOptions options,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer,
            CancellationToken cancellationToken)
        {
            if (isCompilerAnalyzer(analyzer))
            {
                // Compiler analyzer must always be executed for compiler errors, which cannot be suppressed or filtered.
                return false;
            }

            var supportedDiagnostics = GetSupportedDiagnosticDescriptors(analyzer, continueOnAnalyzerException, cancellationToken);
            var diagnosticOptions = options.SpecificDiagnosticOptions;

            foreach (var diag in supportedDiagnostics)
            {
                if (diag.IsNotConfigurable())
                {
                    // If diagnostic descriptor is not configurable, then diagnostics created through it cannot be suppressed.
                    return false;
                }

                // Is this diagnostic suppressed by default (as written by the rule author)
                var isSuppressed = !diag.IsEnabledByDefault;

                // If the user said something about it, that overrides the author.
                if (diagnosticOptions.ContainsKey(diag.Id))
                {
                    isSuppressed = diagnosticOptions[diag.Id] == ReportDiagnostic.Suppress;
                }

                if (isSuppressed)
                {
                    continue;
                }
                else
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

        internal EventHandler<AnalyzerExceptionDiagnosticArgs> RegisterAnalyzerExceptionDiagnosticHandler(ImmutableArray<DiagnosticAnalyzer> analyzers, Func<Diagnostic, bool> addAnalyzerExceptionDiagnostic)
        {
            Action<object, AnalyzerExceptionDiagnosticArgs> onAnalyzerExceptionDiagnostic =
                (sender, args) => addAnalyzerExceptionDiagnostic(args.Diagnostic);
            return RegisterAnalyzerExceptionDiagnosticHandler(analyzers, onAnalyzerExceptionDiagnostic);
        }

        internal EventHandler<AnalyzerExceptionDiagnosticArgs> RegisterAnalyzerExceptionDiagnosticHandler(ImmutableArray<DiagnosticAnalyzer> analyzers, Action<object, AnalyzerExceptionDiagnosticArgs> onAnayzerExceptionDiagnostic)
        {
            EventHandler<AnalyzerExceptionDiagnosticArgs> handler = (sender, args) =>
            {
                if (analyzers.Contains(args.FaultedAnalyzer))
                {
                    onAnayzerExceptionDiagnostic(sender, args);
                }
            };

            _analyzerExceptionDiagnostic += handler;
            return handler;
        }

        internal EventHandler<AnalyzerExceptionDiagnosticArgs> RegisterAnalyzerExceptionDiagnosticHandler(DiagnosticAnalyzer analyzer, Func<Diagnostic, bool> addAnalyzerExceptionDiagnostic)
        {
            Action<object, AnalyzerExceptionDiagnosticArgs> onAnalyzerExceptionDiagnostic =
                (sender, args) => addAnalyzerExceptionDiagnostic(args.Diagnostic);
            return RegisterAnalyzerExceptionDiagnosticHandler(analyzer, onAnalyzerExceptionDiagnostic);
        }

        internal EventHandler<AnalyzerExceptionDiagnosticArgs> RegisterAnalyzerExceptionDiagnosticHandler(DiagnosticAnalyzer analyzer, Action<object, AnalyzerExceptionDiagnosticArgs> onAnayzerExceptionDiagnostic)
        {
            EventHandler<AnalyzerExceptionDiagnosticArgs> handler = (sender, args) =>
            {
                if (analyzer == args.FaultedAnalyzer)
                {
                    onAnayzerExceptionDiagnostic(sender, args);
                }
            };

            _analyzerExceptionDiagnostic += handler;
            return handler;
        }

        internal void UnregisterAnalyzerExceptionDiagnosticHandler(EventHandler<AnalyzerExceptionDiagnosticArgs> handler)
        {
            _analyzerExceptionDiagnostic -= handler;
        }
    }
}
