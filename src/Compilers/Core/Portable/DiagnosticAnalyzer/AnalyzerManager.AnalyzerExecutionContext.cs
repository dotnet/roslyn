// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalyzerManager
    {
        private sealed class AnalyzerExecutionContext
        {
            private readonly DiagnosticAnalyzer _analyzer;
            private readonly object _gate;

            /// <summary>
            /// Map from (symbol, analyzer) to count of its member symbols whose symbol declared events are not yet processed.
            /// </summary>
            private Dictionary<ISymbol, HashSet<ISymbol>> _lazyPendingMemberSymbolsMapOpt;

            /// <summary>
            /// Symbol declared events for symbols with pending symbol end analysis for given analyzer.
            /// </summary>
            private Dictionary<ISymbol, (ImmutableArray<SymbolEndAnalyzerAction>, SymbolDeclaredCompilationEvent)> _lazyPendingSymbolEndActionsOpt;

            public AnalyzerExecutionContext(DiagnosticAnalyzer analyzer)
            {
                _analyzer = analyzer;
                _gate = new object();
            }

            /// <summary>
            /// Task to compute HostSessionStartAnalysisScope for session wide analyzer actions, i.e. AnalyzerActions registered by analyzer's Initialize method.
            /// These are run only once per every analyzer. 
            /// </summary>
            private Task<HostSessionStartAnalysisScope> _lazySessionScopeTask;

            /// <summary>
            /// Task to compute HostCompilationStartAnalysisScope for per-compilation analyzer actions, i.e. AnalyzerActions registered by analyzer's CompilationStartActions.
            /// </summary>
            private Task<HostCompilationStartAnalysisScope> _lazyCompilationScopeTask;

            /// <summary>
            /// Task to compute HostSymbolStartAnalysisScope for per-symbol analyzer actions, i.e. AnalyzerActions registered by analyzer's SymbolStartActions.
            /// </summary>
            private Dictionary<ISymbol, Task<HostSymbolStartAnalysisScope>> _lazySymbolScopeTasks;

            /// <summary>
            /// Supported diagnostic descriptors for diagnostic analyzer, if any.
            /// </summary>
            private ImmutableArray<DiagnosticDescriptor> _lazyDiagnosticDescriptors = default(ImmutableArray<DiagnosticDescriptor>);

            /// <summary>
            /// Supported suppression descriptors for diagnostic suppressor, if any.
            /// </summary>
            private ImmutableArray<SuppressionDescriptor> _lazySuppressionDescriptors = default(ImmutableArray<SuppressionDescriptor>);

            public Task<HostSessionStartAnalysisScope> GetSessionAnalysisScopeTask(AnalyzerExecutor analyzerExecutor)
            {
                lock (_gate)
                {
                    Task<HostSessionStartAnalysisScope> task;
                    if (_lazySessionScopeTask != null)
                    {
                        return _lazySessionScopeTask;
                    }

                    task = Task.Run(() =>
                    {
                        var sessionScope = new HostSessionStartAnalysisScope();
                        analyzerExecutor.ExecuteInitializeMethod(_analyzer, sessionScope);
                        return sessionScope;
                    }, analyzerExecutor.CancellationToken);

                    _lazySessionScopeTask = task;
                    return task;
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
                AnalyzerExecutor analyzerExecutor)
            {
                lock (_gate)
                {
                    if (_lazyCompilationScopeTask == null)
                    {
                        _lazyCompilationScopeTask = Task.Run(() =>
                        {
                            var compilationAnalysisScope = new HostCompilationStartAnalysisScope(sessionScope);
                            analyzerExecutor.ExecuteCompilationStartActions(sessionScope.GetAnalyzerActions(_analyzer).CompilationStartActions, compilationAnalysisScope);
                            return compilationAnalysisScope;
                        }, analyzerExecutor.CancellationToken);
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
                ImmutableArray<SymbolStartAnalyzerAction> symbolStartActions,
                AnalyzerExecutor analyzerExecutor)
            {
                lock (_gate)
                {
                    _lazySymbolScopeTasks = _lazySymbolScopeTasks ?? new Dictionary<ISymbol, Task<HostSymbolStartAnalysisScope>>();
                    if (!_lazySymbolScopeTasks.TryGetValue(symbol, out var symbolScopeTask))
                    {
                        symbolScopeTask = Task.Run(() => getSymbolAnalysisScopeCore(), analyzerExecutor.CancellationToken);
                        _lazySymbolScopeTasks.Add(symbol, symbolScopeTask);
                    }

                    return symbolScopeTask;

                    HostSymbolStartAnalysisScope getSymbolAnalysisScopeCore()
                    {
                        var symbolAnalysisScope = new HostSymbolStartAnalysisScope();
                        analyzerExecutor.ExecuteSymbolStartActions(symbol, _analyzer, symbolStartActions, symbolAnalysisScope);

                        var symbolEndActions = symbolAnalysisScope.GetAnalyzerActions(_analyzer);
                        if (symbolEndActions.SymbolEndActionsCount > 0)
                        {
                            var dependentSymbols = getDependentSymbols();
                            lock (_gate)
                            {
                                _lazyPendingMemberSymbolsMapOpt = _lazyPendingMemberSymbolsMapOpt ?? new Dictionary<ISymbol, HashSet<ISymbol>>();

                                // Guard against entry added from another thread.
                                VerifyNewEntryForPendingMemberSymbolsMap(symbol, dependentSymbols);
                                _lazyPendingMemberSymbolsMapOpt[symbol] = dependentSymbols;
                            }
                        }

                        return symbolAnalysisScope;
                    }
                }

                HashSet<ISymbol> getDependentSymbols()
                {
                    HashSet<ISymbol> memberSet = null;
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
                                memberSet = memberSet ?? new HashSet<ISymbol>();
                                memberSet.Add(member);

                                // Ensure that we include symbols for both parts of partial methods.
                                if (member is IMethodSymbol method &&
                                    !(method.PartialImplementationPart is null))
                                {
                                    memberSet.Add(method.PartialImplementationPart);
                                }
                            }

                            if (member.Kind != symbol.Kind &&
                                member is INamedTypeSymbol typeMember)
                            {
                                processMembers(typeMember.GetMembers());
                            }
                        }
                    }
                }
            }

            [Conditional("DEBUG")]
            private void VerifyNewEntryForPendingMemberSymbolsMap(ISymbol symbol, HashSet<ISymbol> dependentSymbols)
            {
                if (_lazyPendingMemberSymbolsMapOpt.TryGetValue(symbol, out var existingDependentSymbols))
                {
                    if (existingDependentSymbols == null)
                    {
                        Debug.Assert(dependentSymbols == null);
                    }
                    else
                    {
                        Debug.Assert(dependentSymbols != null);
                        Debug.Assert(dependentSymbols.SetEquals(existingDependentSymbols));
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

            public ImmutableArray<DiagnosticDescriptor> GetOrComputeDiagnosticDescriptors(DiagnosticAnalyzer analyzer, AnalyzerExecutor analyzerExecutor)
                => GetOrComputeDescriptors(ref _lazyDiagnosticDescriptors, ComputeDiagnosticDescriptors, _gate, analyzer, analyzerExecutor);

            public ImmutableArray<SuppressionDescriptor> GetOrComputeSuppressionDescriptors(DiagnosticSuppressor suppressor, AnalyzerExecutor analyzerExecutor)
                => GetOrComputeDescriptors(ref _lazySuppressionDescriptors, ComputeSuppressionDescriptors, _gate, suppressor, analyzerExecutor);

            private static ImmutableArray<TDescriptor> GetOrComputeDescriptors<TDescriptor>(
                ref ImmutableArray<TDescriptor> lazyDescriptors,
                Func<DiagnosticAnalyzer, AnalyzerExecutor, ImmutableArray<TDescriptor>> computeDescriptors,
                object gate,
                DiagnosticAnalyzer analyzer,
                AnalyzerExecutor analyzerExecutor)
            {
                if (!lazyDescriptors.IsDefault)
                {
                    return lazyDescriptors;
                }

                // Otherwise, compute the value.
                // We do so outside the lock statement as we are calling into user code, which may be a long running operation.
                var descriptors = computeDescriptors(analyzer, analyzerExecutor);

                ImmutableInterlocked.InterlockedInitialize(ref lazyDescriptors, descriptors);
                return lazyDescriptors;
            }

            /// <summary>
            /// Compute <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/> and exception handler for the given <paramref name="analyzer"/>.
            /// </summary>
            private static ImmutableArray<DiagnosticDescriptor> ComputeDiagnosticDescriptors(
                DiagnosticAnalyzer analyzer,
                AnalyzerExecutor analyzerExecutor)
            {
                var supportedDiagnostics = ImmutableArray<DiagnosticDescriptor>.Empty;

                // Catch Exception from analyzer.SupportedDiagnostics
                analyzerExecutor.ExecuteAndCatchIfThrows(
                    analyzer,
                    _ =>
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
                    argument: default(object));

                // Force evaluate and report exception diagnostics from LocalizableString.ToString().
                Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = analyzerExecutor.OnAnalyzerException;
                if (onAnalyzerException != null)
                {
                    var handler = new EventHandler<Exception>((sender, ex) =>
                    {
                        var diagnostic = AnalyzerExecutor.CreateAnalyzerExceptionDiagnostic(analyzer, ex);
                        onAnalyzerException(ex, analyzer, diagnostic);
                    });

                    foreach (var descriptor in supportedDiagnostics)
                    {
                        ForceLocalizableStringExceptions(descriptor.Title, handler);
                        ForceLocalizableStringExceptions(descriptor.MessageFormat, handler);
                        ForceLocalizableStringExceptions(descriptor.Description, handler);
                    }
                }

                return supportedDiagnostics;
            }

            private static ImmutableArray<SuppressionDescriptor> ComputeSuppressionDescriptors(
                DiagnosticAnalyzer analyzer,
                AnalyzerExecutor analyzerExecutor)
            {
                var descriptors = ImmutableArray<SuppressionDescriptor>.Empty;

                if (analyzer is DiagnosticSuppressor suppressor)
                {
                    // Catch Exception from suppressor.SupportedSuppressions
                    analyzerExecutor.ExecuteAndCatchIfThrows(
                        analyzer,
                        _ =>
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
                        argument: default(object));
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
                    if (_lazyPendingMemberSymbolsMapOpt == null ||
                        !_lazyPendingMemberSymbolsMapOpt.TryGetValue(containingSymbol, out var pendingMemberSymbols))
                    {
                        return false;
                    }

                    var removed = pendingMemberSymbols.Remove(processedMemberSymbol);

                    if (pendingMemberSymbols.Count > 0 ||
                        _lazyPendingSymbolEndActionsOpt == null ||
                        !_lazyPendingSymbolEndActionsOpt.TryGetValue(containingSymbol, out containerEndActionsAndEvent))
                    {
                        return false;
                    }

                    _lazyPendingSymbolEndActionsOpt.Remove(containingSymbol);
                    return true;
                }
            }

            public bool TryStartExecuteSymbolEndActions(ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions, SymbolDeclaredCompilationEvent symbolDeclaredEvent)
            {
                Debug.Assert(!symbolEndActions.IsEmpty);

                var symbol = symbolDeclaredEvent.Symbol;
                lock (_gate)
                {
                    Debug.Assert(_lazyPendingMemberSymbolsMapOpt != null);

                    if (_lazyPendingMemberSymbolsMapOpt.TryGetValue(symbol, out var pendingMemberSymbols) &&
                        pendingMemberSymbols?.Count > 0)
                    {
                        // At least one member is not complete, so mark the event for later processing of symbol end actions.
                        MarkSymbolEndAnalysisPending_NoLock(symbol, symbolEndActions, symbolDeclaredEvent);
                        return false;
                    }

                    // Try remove the pending event in case it was marked pending by another thread when members were not yet complete.
                    _lazyPendingSymbolEndActionsOpt?.Remove(symbol);
                    return true;
                }
            }

            public void MarkSymbolEndAnalysisComplete(ISymbol symbol)
            {
                lock (_gate)
                {
                    _lazyPendingMemberSymbolsMapOpt?.Remove(symbol);
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
                _lazyPendingSymbolEndActionsOpt = _lazyPendingSymbolEndActionsOpt ?? new Dictionary<ISymbol, (ImmutableArray<SymbolEndAnalyzerAction>, SymbolDeclaredCompilationEvent)>();
                _lazyPendingSymbolEndActionsOpt[symbol] = (symbolEndActions, symbolDeclaredEvent);
            }

            [Conditional("DEBUG")]
            public void VerifyAllSymbolEndActionsExecuted()
            {
                lock (_gate)
                {
                    Debug.Assert(_lazyPendingMemberSymbolsMapOpt == null || _lazyPendingMemberSymbolsMapOpt.Count == 0);
                    Debug.Assert(_lazyPendingSymbolEndActionsOpt == null || _lazyPendingSymbolEndActionsOpt.Count == 0);
                }
            }
        }
    }
}
