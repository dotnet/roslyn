// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalyzerManager
    {
        private sealed class AnalyzerExecutionContext
        {
            /// <summary>
            /// Cached mapping of localizable strings in this descriptor to any exceptions thrown while obtaining them.
            /// </summary>
            private static ImmutableDictionary<LocalizableString, Exception?> s_localizableStringToException = ImmutableDictionary<LocalizableString, Exception?>.Empty.WithComparers(Roslyn.Utilities.ReferenceEqualityComparer.Instance);

            private readonly DiagnosticAnalyzer _analyzer;
            private readonly object _gate;

            /// <summary>
            /// Map from (symbol, analyzer) to count of its member symbols whose symbol declared events are not yet processed.
            /// </summary>
            private Dictionary<ISymbol, HashSet<ISymbol>?>? _lazyPendingMemberSymbolsMap;

            /// <summary>
            /// Symbol declared events for symbols with pending symbol end analysis for given analyzer.
            /// </summary>
            private Dictionary<ISymbol, (ImmutableArray<SymbolEndAnalyzerAction>, SymbolDeclaredCompilationEvent)>? _lazyPendingSymbolEndActionsMap;

            /// <summary>
            /// Task to compute HostSessionStartAnalysisScope for session wide analyzer actions, i.e. AnalyzerActions registered by analyzer's Initialize method.
            /// These are run only once per every analyzer. 
            /// </summary>
            private Task<HostSessionStartAnalysisScope>? _lazySessionScopeTask;

            /// <summary>
            /// Task to compute HostCompilationStartAnalysisScope for per-compilation analyzer actions, i.e. AnalyzerActions registered by analyzer's CompilationStartActions.
            /// </summary>
            private Task<HostCompilationStartAnalysisScope>? _lazyCompilationScopeTask;

            /// <summary>
            /// Task to compute HostSymbolStartAnalysisScope for per-symbol analyzer actions, i.e. AnalyzerActions registered by analyzer's SymbolStartActions.
            /// </summary>
            private Dictionary<ISymbol, Task<HostSymbolStartAnalysisScope>>? _lazySymbolScopeTasks;

            /// <summary>
            /// Supported diagnostic descriptors for diagnostic analyzer, if any.
            /// </summary>
            private ImmutableArray<DiagnosticDescriptor> _lazyDiagnosticDescriptors;

            /// <summary>
            /// Supported suppression descriptors for diagnostic suppressor, if any.
            /// </summary>
            private ImmutableArray<SuppressionDescriptor> _lazySuppressionDescriptors;

            public AnalyzerExecutionContext(DiagnosticAnalyzer analyzer)
            {
                _analyzer = analyzer;
                _gate = new object();
            }

            [PerformanceSensitive(
                "https://github.com/dotnet/roslyn/issues/26778",
                AllowCaptures = false)]
            public Task<HostSessionStartAnalysisScope> GetSessionAnalysisScopeAsync(AnalyzerExecutor analyzerExecutor, CancellationToken cancellationToken)
            {
                lock (_gate)
                {
                    Task<HostSessionStartAnalysisScope> task;
                    if (_lazySessionScopeTask != null)
                    {
                        return _lazySessionScopeTask;
                    }

                    task = getSessionAnalysisScopeTaskSlowAsync(this, analyzerExecutor, cancellationToken);
                    _lazySessionScopeTask = task;
                    return task;

                    static Task<HostSessionStartAnalysisScope> getSessionAnalysisScopeTaskSlowAsync(AnalyzerExecutionContext context, AnalyzerExecutor executor, CancellationToken cancellationToken)
                    {
                        return Task.Run(() =>
                        {
                            var sessionScope = new HostSessionStartAnalysisScope();
                            executor.ExecuteInitializeMethod(context._analyzer, sessionScope, executor.SeverityFilter, cancellationToken);
                            return sessionScope;
                        }, cancellationToken);
                    }
                }
            }

            public void ClearSessionScopeTask()
            {
                lock (_gate)
                {
                    _lazySessionScopeTask = null;
                }
            }

            public Task<HostCompilationStartAnalysisScope> GetCompilationAnalysisScopeAsync(
                HostSessionStartAnalysisScope sessionScope,
                AnalyzerExecutor analyzerExecutor,
                CancellationToken cancellationToken)
            {
                lock (_gate)
                {
                    if (_lazyCompilationScopeTask == null)
                    {
                        _lazyCompilationScopeTask = Task.Run(() =>
                        {
                            var compilationAnalysisScope = new HostCompilationStartAnalysisScope(sessionScope);
                            analyzerExecutor.ExecuteCompilationStartActions(sessionScope.GetAnalyzerActions(_analyzer).CompilationStartActions, compilationAnalysisScope, cancellationToken);
                            return compilationAnalysisScope;
                        }, cancellationToken);
                    }

                    return _lazyCompilationScopeTask;
                }
            }

            public void ClearCompilationScopeTask()
            {
                lock (_gate)
                {
                    _lazyCompilationScopeTask = null;
                }
            }

            public Task<HostSymbolStartAnalysisScope> GetSymbolAnalysisScopeAsync(
                ISymbol symbol,
                bool isGeneratedCodeSymbol,
                SyntaxTree? filterTree,
                TextSpan? filterSpan,
                ImmutableArray<SymbolStartAnalyzerAction> symbolStartActions,
                AnalyzerExecutor analyzerExecutor,
                CancellationToken cancellationToken)
            {
                lock (_gate)
                {
                    _lazySymbolScopeTasks ??= new Dictionary<ISymbol, Task<HostSymbolStartAnalysisScope>>();
                    if (!_lazySymbolScopeTasks.TryGetValue(symbol, out var symbolScopeTask))
                    {
                        symbolScopeTask = Task.Run(() => getSymbolAnalysisScopeCore(), cancellationToken);
                        _lazySymbolScopeTasks.Add(symbol, symbolScopeTask);
                    }

                    return symbolScopeTask;

                    HostSymbolStartAnalysisScope getSymbolAnalysisScopeCore()
                    {
                        var symbolAnalysisScope = new HostSymbolStartAnalysisScope();
                        analyzerExecutor.ExecuteSymbolStartActions(symbol, _analyzer, symbolStartActions, symbolAnalysisScope, isGeneratedCodeSymbol, filterTree, filterSpan, cancellationToken);

                        var symbolEndActions = symbolAnalysisScope.GetAnalyzerActions(_analyzer);
                        if (symbolEndActions.SymbolEndActionsCount > 0)
                        {
                            var dependentSymbols = getDependentSymbols();
                            lock (_gate)
                            {
                                _lazyPendingMemberSymbolsMap ??= new Dictionary<ISymbol, HashSet<ISymbol>?>();

                                // Guard against entry added from another thread.
                                VerifyNewEntryForPendingMemberSymbolsMap(symbol, dependentSymbols);
                                _lazyPendingMemberSymbolsMap[symbol] = dependentSymbols;
                            }
                        }

                        return symbolAnalysisScope;
                    }
                }

                HashSet<ISymbol>? getDependentSymbols()
                {
                    HashSet<ISymbol>? memberSet = null;
                    switch (symbol.Kind)
                    {
                        case SymbolKind.NamedType:
                            processMembers(((INamedTypeSymbol)symbol).GetMembers());
                            break;

                        case SymbolKind.Namespace:
                            processMembers(((INamespaceSymbol)symbol).GetMembers());
                            break;
                    }

                    return memberSet;

                    void processMembers(IEnumerable<ISymbol> members)
                    {
                        foreach (var member in members)
                        {
                            if (!member.IsImplicitlyDeclared && member.IsInSource())
                            {
                                memberSet ??= new HashSet<ISymbol>();
                                memberSet.Add(member);

                                // Ensure that we include symbols for both parts of partial methods.
                                // https://github.com/dotnet/roslyn/issues/73772: also cascade to partial property implementation part
                                if (member is IMethodSymbol method &&
                                    !(method.PartialImplementationPart is null))
                                {
                                    memberSet.Add(method.PartialImplementationPart);
                                }
                            }

                            if (member is INamedTypeSymbol typeMember)
                            {
                                processMembers(typeMember.GetMembers());
                            }
                        }
                    }
                }
            }

            [Conditional("DEBUG")]
            private void VerifyNewEntryForPendingMemberSymbolsMap(ISymbol symbol, HashSet<ISymbol>? dependentSymbols)
            {
                Debug.Assert(_lazyPendingMemberSymbolsMap != null, $"{nameof(_lazyPendingMemberSymbolsMap)} was expected to be a non-null value.");

                if (_lazyPendingMemberSymbolsMap.TryGetValue(symbol, out var existingDependentSymbols))
                {
                    if (existingDependentSymbols == null)
                    {
                        Debug.Assert(dependentSymbols == null, $"{nameof(dependentSymbols)} was expected to be null.");
                    }
                    else
                    {
                        Debug.Assert(dependentSymbols != null, $"{nameof(dependentSymbols)} was expected to be a non-null value.");
                        Debug.Assert(existingDependentSymbols.IsSubsetOf(dependentSymbols), $"{nameof(existingDependentSymbols)} was expected to be a subset of {nameof(dependentSymbols)}");
                    }
                }
            }

            public void ClearSymbolScopeTask(ISymbol symbol)
            {
                lock (_gate)
                {
                    _lazySymbolScopeTasks?.Remove(symbol);
                }
            }

            public ImmutableArray<DiagnosticDescriptor> GetOrComputeDiagnosticDescriptors(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor, CancellationToken cancellationToken)
                => GetOrComputeDescriptors(ref _lazyDiagnosticDescriptors, ComputeDiagnosticDescriptors_NoLock, analyzer, analyzerExecutor, _gate, cancellationToken);

            public ImmutableArray<SuppressionDescriptor> GetOrComputeSuppressionDescriptors(DiagnosticSuppressor suppressor, AnalyzerExecutor analyzerExecutor, CancellationToken cancellationToken)
                => GetOrComputeDescriptors(ref _lazySuppressionDescriptors, ComputeSuppressionDescriptors_NoLock, suppressor, analyzerExecutor, _gate, cancellationToken);

            private static ImmutableArray<TDescriptor> GetOrComputeDescriptors<TDescriptor>(
                ref ImmutableArray<TDescriptor> lazyDescriptors,
                Func<DiagnosticAnalyzer, AnalyzerExecutor, CancellationToken, ImmutableArray<TDescriptor>> computeDescriptorsNoLock,
                DiagnosticAnalyzer analyzer,
                AnalyzerExecutor analyzerExecutor,
                object gate,
                CancellationToken cancellationToken)
            {
                if (!lazyDescriptors.IsDefault)
                {
                    return lazyDescriptors;
                }

                lock (gate)
                {
                    // We re-check if lazyDescriptors is default inside the lock statement
                    // to ensure that we don't invoke 'computeDescriptorsNoLock' multiple times.
                    // 'computeDescriptorsNoLock' makes analyzer callbacks and these can throw
                    // exceptions, leading to AD0001 diagnostics and duplicate callbacks can
                    // lead to duplicate AD0001 diagnostics.
                    if (lazyDescriptors.IsDefault)
                    {
                        lazyDescriptors = computeDescriptorsNoLock(analyzer, analyzerExecutor, cancellationToken);
                    }

                    return lazyDescriptors;
                }
            }

            /// <summary>
            /// Compute <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> and exception handler for the given <paramref name="analyzer"/>.
            /// </summary>
            private static ImmutableArray<DiagnosticDescriptor> ComputeDiagnosticDescriptors_NoLock(
                DiagnosticAnalyzer analyzer,
                AnalyzerExecutor analyzerExecutor,
                CancellationToken cancellationToken)
            {
                var supportedDiagnostics = ImmutableArray<DiagnosticDescriptor>.Empty;

                // Catch Exception from analyzer.SupportedDiagnostics
                analyzerExecutor.ExecuteAndCatchIfThrows(
                    analyzer,
                    analyze: _ =>
                    {
                        var supportedDiagnosticsLocal = analyzer.SupportedDiagnostics;
                        if (!supportedDiagnosticsLocal.IsDefaultOrEmpty)
                        {
                            foreach (var descriptor in supportedDiagnosticsLocal)
                            {
                                if (descriptor == null)
                                {
                                    // Disallow null descriptors.
                                    throw new ArgumentException(string.Format(CodeAnalysisResources.SupportedDiagnosticsHasNullDescriptor, analyzer.ToString()), nameof(DiagnosticAnalyzer.SupportedDiagnostics));
                                }
                            }

                            supportedDiagnostics = supportedDiagnosticsLocal;
                        }
                    },
                    argument: (object?)null,
                    contextInfo: null,
                    cancellationToken: cancellationToken);

                // Force evaluate and report exception diagnostics from LocalizableString.ToString().
                var onAnalyzerException = analyzerExecutor.OnAnalyzerException;
                if (onAnalyzerException != null)
                {
                    foreach (var descriptor in supportedDiagnostics)
                    {
                        // Compute the localizable strings once, caching any exceptions produced doing that. This helps
                        // avoid an excessive amount of string allocations loading resources.
                        forceLocalizableStringExceptions(descriptor.Title);
                        forceLocalizableStringExceptions(descriptor.MessageFormat);
                        forceLocalizableStringExceptions(descriptor.Description);
                    }
                }

                return supportedDiagnostics;

                void forceLocalizableStringExceptions(LocalizableString localizableString)
                {
                    var exception = getAndCacheToStringException(localizableString);
                    if (exception != null)
                    {
                        var diagnostic = AnalyzerExecutor.CreateAnalyzerExceptionDiagnostic(analyzer, exception);
                        onAnalyzerException(exception, analyzer, diagnostic, cancellationToken);
                    }
                }

                static Exception? getAndCacheToStringException(LocalizableString localizableString)
                {
                    if (!localizableString.CanThrowExceptions)
                        return null;

                    return ImmutableInterlocked.GetOrAdd(ref s_localizableStringToException, localizableString, computeException);

                    static Exception? computeException(LocalizableString localizableString)
                    {
                        Exception? localException = null;
                        EventHandler<Exception> handler = (_, ex) => localException = ex;

                        localizableString.OnException += handler;
                        localizableString.ToString();
                        localizableString.OnException -= handler;

                        return localException;
                    }
                }
            }

            private static ImmutableArray<SuppressionDescriptor> ComputeSuppressionDescriptors_NoLock(
                DiagnosticAnalyzer analyzer,
                AnalyzerExecutor analyzerExecutor,
                CancellationToken cancellationToken)
            {
                var descriptors = ImmutableArray<SuppressionDescriptor>.Empty;

                if (analyzer is DiagnosticSuppressor suppressor)
                {
                    // Catch Exception from suppressor.SupportedSuppressions
                    analyzerExecutor.ExecuteAndCatchIfThrows(
                        analyzer,
                        analyze: _ =>
                        {
                            var descriptorsLocal = suppressor.SupportedSuppressions;
                            if (!descriptorsLocal.IsDefaultOrEmpty)
                            {
                                foreach (var descriptor in descriptorsLocal)
                                {
                                    if (descriptor == null)
                                    {
                                        // Disallow null descriptors.
                                        throw new ArgumentException(string.Format(CodeAnalysisResources.SupportedSuppressionsHasNullDescriptor, analyzer.ToString()), nameof(DiagnosticSuppressor.SupportedSuppressions));
                                    }
                                }

                                descriptors = descriptorsLocal;
                            }
                        },
                        argument: (object?)null,
                        contextInfo: null,
                        cancellationToken: cancellationToken);
                }

                return descriptors;
            }

            public bool TryProcessCompletedMemberAndGetPendingSymbolEndActionsForContainer(
                ISymbol containingSymbol,
                ISymbol processedMemberSymbol,
                out (ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions, SymbolDeclaredCompilationEvent symbolDeclaredEvent) containerEndActionsAndEvent)
            {
                containerEndActionsAndEvent = default;
                lock (_gate)
                {
                    if (_lazyPendingMemberSymbolsMap == null ||
                        !_lazyPendingMemberSymbolsMap.TryGetValue(containingSymbol, out var pendingMemberSymbols))
                    {
                        return false;
                    }

                    Debug.Assert(pendingMemberSymbols != null);

                    var removed = pendingMemberSymbols.Remove(processedMemberSymbol);

                    if (pendingMemberSymbols.Count > 0 ||
                        _lazyPendingSymbolEndActionsMap == null ||
                        !_lazyPendingSymbolEndActionsMap.TryGetValue(containingSymbol, out containerEndActionsAndEvent))
                    {
                        return false;
                    }

                    _lazyPendingSymbolEndActionsMap.Remove(containingSymbol);
                    return true;
                }
            }

            public bool TryStartExecuteSymbolEndActions(ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions, SymbolDeclaredCompilationEvent symbolDeclaredEvent)
            {
                Debug.Assert(!symbolEndActions.IsEmpty);

                var symbol = symbolDeclaredEvent.Symbol;
                lock (_gate)
                {
                    Debug.Assert(_lazyPendingMemberSymbolsMap != null);

                    if (_lazyPendingMemberSymbolsMap.TryGetValue(symbol, out var pendingMemberSymbols) &&
                        pendingMemberSymbols?.Count > 0)
                    {
                        // At least one member is not complete, so mark the event for later processing of symbol end actions.
                        MarkSymbolEndAnalysisPending_NoLock(symbol, symbolEndActions, symbolDeclaredEvent);
                        return false;
                    }

                    // Try remove the pending event in case it was marked pending by another thread when members were not yet complete.
                    _lazyPendingSymbolEndActionsMap?.Remove(symbol);
                    return true;
                }
            }

            public void MarkSymbolEndAnalysisComplete(ISymbol symbol)
            {
                lock (_gate)
                {
                    _lazyPendingMemberSymbolsMap?.Remove(symbol);
                }
            }

            public void MarkSymbolEndAnalysisPending(ISymbol symbol, ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions, SymbolDeclaredCompilationEvent symbolDeclaredEvent)
            {
                lock (_gate)
                {
                    MarkSymbolEndAnalysisPending_NoLock(symbol, symbolEndActions, symbolDeclaredEvent);
                }
            }

            private void MarkSymbolEndAnalysisPending_NoLock(ISymbol symbol, ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions, SymbolDeclaredCompilationEvent symbolDeclaredEvent)
            {
                _lazyPendingSymbolEndActionsMap ??= new Dictionary<ISymbol, (ImmutableArray<SymbolEndAnalyzerAction>, SymbolDeclaredCompilationEvent)>();
                _lazyPendingSymbolEndActionsMap[symbol] = (symbolEndActions, symbolDeclaredEvent);
            }

            [Conditional("DEBUG")]
            public void VerifyAllSymbolEndActionsExecuted()
            {
                lock (_gate)
                {
                    Debug.Assert(_lazyPendingMemberSymbolsMap == null || _lazyPendingMemberSymbolsMap.Count == 0);
                    Debug.Assert(_lazyPendingSymbolEndActionsMap == null || _lazyPendingSymbolEndActionsMap.Count == 0);
                }
            }
        }
    }
}
