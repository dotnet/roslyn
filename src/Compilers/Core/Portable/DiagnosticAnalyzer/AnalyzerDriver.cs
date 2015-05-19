﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Driver to execute diagnostic analyzers for a given compilation.
    /// It uses a <see cref="AsyncQueue{TElement}"/> of <see cref="CompilationEvent"/>s to drive its analysis.
    /// </summary>
    internal abstract class AnalyzerDriver : IDisposable
    {
        internal static readonly ConditionalWeakTable<Compilation, SuppressMessageAttributeState> SuppressMessageStateByCompilation = new ConditionalWeakTable<Compilation, SuppressMessageAttributeState>();

        // Protect against vicious analyzers that provide large values for SymbolKind.
        private const int MaxSymbolKind = 100;

        private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
        private readonly CancellationTokenRegistration _queueRegistration;
        protected readonly AnalyzerManager analyzerManager;
        
        // Lazy fields initialized in Initialize() API
        private Compilation _compilation;
        protected AnalyzerExecutor analyzerExecutor;
        internal AnalyzerActions analyzerActions;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ImmutableArray<SymbolAnalyzerAction>>> _symbolActionsByKind;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<SemanticModelAnalyzerAction>> _semanticModelActionsMap;
        // Compilation actions and compilation end actions have separate maps so that it is easy to
        // execute the compilation actions before the compilation end actions.
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>> _compilationActionsMap;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>> _compilationEndActionsMap;

        /// <summary>
        /// Primary driver task which processes all <see cref="CompilationEventQueue"/> events, runs analyzer actions and signals completion of <see cref="DiagnosticQueue"/> at the end.
        /// </summary>
        private Task _primaryTask;

        /// <summary>
        /// Number of worker tasks processing compilation events and executing analyzer actions.
        /// </summary>
        private readonly int _workerCount = Environment.ProcessorCount;

        /// <summary>
        /// The compilation queue to create the compilation with via WithEventQueue.
        /// </summary>
        public AsyncQueue<CompilationEvent> CompilationEventQueue { get; }

        /// <summary>
        /// An async queue that is fed the diagnostics as they are computed.
        /// </summary>
        public AsyncQueue<Diagnostic> DiagnosticQueue { get; }

        /// <summary>
        /// Initializes the compilation for the analyzer driver.
        /// It also computes and initializes <see cref="analyzerActions"/> and <see cref="_symbolActionsByKind"/>.
        /// Finally, it initializes and starts the <see cref="_primaryTask"/> for the driver.
        /// </summary>
        /// <remarks>
        /// NOTE: This method must only be invoked from <see cref="AnalyzerDriver.Create(Compilation, ImmutableArray{DiagnosticAnalyzer}, AnalyzerOptions, AnalyzerManager, Action{Diagnostic}, Boolean, out Compilation, CancellationToken)"/>.
        /// </remarks>
        private void Initialize(Compilation comp, AnalyzerExecutor analyzerExecutor, CancellationToken cancellationToken)
        {
            try
            {
                Debug.Assert(_compilation == null);
                Debug.Assert(comp.EventQueue == this.CompilationEventQueue);

                _compilation = comp;
                this.analyzerExecutor = analyzerExecutor;

                // Compute the set of effective actions based on suppression, and running the initial analyzers
                var analyzerActionsTask = GetAnalyzerActionsAsync(_analyzers, analyzerManager, analyzerExecutor);
                var initializeTask = analyzerActionsTask.ContinueWith(t =>
                {
                    this.analyzerActions = t.Result;
                    _symbolActionsByKind = MakeSymbolActionsByKind();
                    _semanticModelActionsMap = MakeSemanticModelActionsByAnalyzer();
                    _compilationActionsMap = MakeCompilationActionsByAnalyzer(this.analyzerActions.CompilationActions);
                    _compilationEndActionsMap = MakeCompilationActionsByAnalyzer(this.analyzerActions.CompilationEndActions);
                }, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);

                // create the primary driver task.
                cancellationToken.ThrowIfCancellationRequested();
                _primaryTask = Task.Run(async () =>
                    {
                        await initializeTask.ConfigureAwait(false);

                        await ProcessCompilationEventsAsync(cancellationToken).ConfigureAwait(false);
                    }, cancellationToken)
                    .ContinueWith(c => DiagnosticQueue.TryComplete(), cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            finally
            {
                if (_primaryTask == null)
                {
                    // Set primaryTask to be a cancelled task.
                    var tcs = new TaskCompletionSource<int>();
                    tcs.SetCanceled();
                    _primaryTask = tcs.Task;

                    // Try to set the DiagnosticQueue to be complete.
                    this.DiagnosticQueue.TryComplete();
                }
            }
        }

        private Task ExecuteSyntaxTreeActions(CancellationToken cancellationToken)
        {
            // Execute syntax tree analyzers in parallel.
            var tasks = ArrayBuilder<Task>.GetInstance();
            foreach (var tree in _compilation.SyntaxTrees)
            {
                var actionsByAnalyzers = this.analyzerActions.SyntaxTreeActions.GroupBy(action => action.Analyzer);
                foreach (var analyzerAndActions in actionsByAnalyzers)
                {
                    var task = Task.Run(() =>
                    {
                        // Execute actions for a given analyzer sequentially.
                        analyzerExecutor.ExecuteSyntaxTreeActions(analyzerAndActions.ToImmutableArray(), tree);
                    }, cancellationToken);

                    tasks.Add(task);
                }
            }

            return Task.WhenAll(tasks.ToArrayAndFree());
        }

        /// <summary>
        /// Create an <see cref="AnalyzerDriver"/> and attach it to the given compilation. 
        /// </summary>
        /// <param name="compilation">The compilation to which the new driver should be attached.</param>
        /// <param name="analyzers">The set of analyzers to include in the analysis.</param>
        /// <param name="options">Options that are passed to analyzers.</param>
        /// <param name="analyzerManager">AnalyzerManager to manage analyzers for the lifetime of analyzer host.</param>
        /// <param name="addExceptionDiagnostic">Delegate to add diagnostics generated for exceptions from third party analyzers.</param>
        /// <param name="reportAnalyzer">Report additional information related to analyzers, such as analyzer execution time.</param>
        /// <param name="newCompilation">The new compilation with the analyzer driver attached.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to abort analysis.</param>
        /// <returns>A newly created analyzer driver</returns>
        /// <remarks>
        /// Note that since a compilation is immutable, the act of creating a driver and attaching it produces
        /// a new compilation. Any further actions on the compilation should use the new compilation.
        /// </remarks>
        public static AnalyzerDriver Create(
            Compilation compilation,
            ImmutableArray<DiagnosticAnalyzer> analyzers, 
            AnalyzerOptions options, 
            AnalyzerManager analyzerManager, 
            Action<Diagnostic> addExceptionDiagnostic,
            bool reportAnalyzer,
            out Compilation newCompilation, 
            CancellationToken cancellationToken)
        {
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = 
                (ex, analyzer, diagnostic) => addExceptionDiagnostic?.Invoke(diagnostic);

            return Create(compilation, analyzers, options, analyzerManager, onAnalyzerException, reportAnalyzer, out newCompilation, cancellationToken: cancellationToken);
        }

        // internal for testing purposes
        internal static AnalyzerDriver Create(
            Compilation compilation,
            ImmutableArray<DiagnosticAnalyzer> analyzers, 
            AnalyzerOptions options, 
            AnalyzerManager analyzerManager,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            bool reportAnalyzer,
            out Compilation newCompilation, 
            CancellationToken cancellationToken)
        {
            options = options ?? AnalyzerOptions.Empty;
            AnalyzerDriver analyzerDriver = compilation.AnalyzerForLanguage(analyzers, analyzerManager, cancellationToken);
            newCompilation = compilation.WithEventQueue(analyzerDriver.CompilationEventQueue);

            var addDiagnostic = GetDiagnosticSinkWithSuppression(analyzerDriver.DiagnosticQueue.Enqueue, newCompilation);

            Action<Exception, DiagnosticAnalyzer, Diagnostic> newOnAnalyzerException;
            if (onAnalyzerException != null)
            {
                // Wrap onAnalyzerException to pass in filtered diagnostic.
                var comp = newCompilation;
                newOnAnalyzerException = (ex, analyzer, diagnostic) => 
                    onAnalyzerException(ex, analyzer, GetFilteredDiagnostic(diagnostic, comp));
            }
            else
            {
                // Add exception diagnostic to regular diagnostic bag.
                newOnAnalyzerException = (ex, analyzer, diagnostic) => addDiagnostic(diagnostic);
            }

            // Assume all analyzers are non-thread safe.
            var singleThreadedAnalyzerToGateMap = ImmutableDictionary.CreateRange(analyzers.Select(a => KeyValuePair.Create(a, new object())));
            
            if (reportAnalyzer)
            {
                // If we are reporting detailed analyzer performance numbers, then do a dummy invocation of Compilation.GetTypeByMetadataName API upfront.
                // This API seems to cause a severe hit for the first analyzer invoking it and hence introduces lot of noise in the computed analyzer execution times.
                var unused = newCompilation.GetTypeByMetadataName("System.Object");
            }

            var analyzerExecutor = AnalyzerExecutor.Create(newCompilation, options, addDiagnostic, newOnAnalyzerException, IsCompilerAnalyzer, analyzerManager, singleThreadedAnalyzerToGateMap, reportAnalyzer, cancellationToken);
            
            analyzerDriver.Initialize(newCompilation, analyzerExecutor, cancellationToken);

            return analyzerDriver;
        }

        /// <summary>
        /// Create an analyzer driver.
        /// </summary>
        /// <param name="analyzers">The set of analyzers to include in the analysis</param>
        /// <param name="analyzerManager">AnalyzerManager to manage analyzers for analyzer host's lifetime.</param>
        /// <param name="cancellationToken">a cancellation token that can be used to abort analysis</param>
        protected AnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerManager analyzerManager, CancellationToken cancellationToken)
        {
            _analyzers = analyzers;
            this.analyzerManager = analyzerManager;

            this.CompilationEventQueue = new AsyncQueue<CompilationEvent>();
            this.DiagnosticQueue = new AsyncQueue<Diagnostic>();
            _queueRegistration = cancellationToken.Register(() =>
            {
                this.CompilationEventQueue.TryComplete();
                this.DiagnosticQueue.TryComplete();
            });
        }

        /// <summary>
        /// Returns all diagnostics computed by the analyzers since the last time this was invoked.
        /// If <see cref="CompilationEventQueue"/> has been completed with all compilation events, then it waits for
        /// <see cref="WhenCompletedTask"/> task for the driver to finish processing all events and generate remaining analyzer diagnostics.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync()
        {
            var allDiagnostics = DiagnosticBag.GetInstance();
            if (CompilationEventQueue.IsCompleted)
            {
                await this.WhenCompletedTask.ConfigureAwait(false);
            }

            Diagnostic d;
            while (DiagnosticQueue.TryDequeue(out d))
            {
                allDiagnostics.Add(d);
            }

            var diagnostics = allDiagnostics.ToReadOnlyAndFree();

            // Verify that the diagnostics are already filtered.
            Debug.Assert(_compilation == null ||
                diagnostics.All(diag => _compilation.FilterDiagnostic(diag)?.Severity == diag.Severity));

            return diagnostics;
        }

        /// <summary>
        /// Return a task that completes when the driver is done producing diagnostics.
        /// </summary>
        public Task WhenCompletedTask => _primaryTask;

        internal ImmutableDictionary<DiagnosticAnalyzer, TimeSpan> AnalyzerExecutionTimes => analyzerExecutor.AnalyzerExecutionTimes;

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ImmutableArray<SymbolAnalyzerAction>>> MakeSymbolActionsByKind()
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<ImmutableArray<SymbolAnalyzerAction>>>();
            var actionsByAnalyzers = this.analyzerActions.SymbolActions.GroupBy(action => action.Analyzer);
            foreach (var analyzerAndActions in actionsByAnalyzers)
            {
                var actionsByKindBuilder = new List<ArrayBuilder<SymbolAnalyzerAction>>();
                foreach (var symbolAction in analyzerAndActions)
                {
                    var kinds = symbolAction.Kinds;
                    foreach (int kind in kinds.Distinct())
                    {
                        if (kind > MaxSymbolKind) continue; // protect against vicious analyzers
                        while (kind >= actionsByKindBuilder.Count)
                        {
                            actionsByKindBuilder.Add(ArrayBuilder<SymbolAnalyzerAction>.GetInstance());
                        }

                        actionsByKindBuilder[kind].Add(symbolAction);
                    }
                }

                var actionsByKind = actionsByKindBuilder.Select(a => a.ToImmutableAndFree()).ToImmutableArray();
                builder.Add(analyzerAndActions.Key, actionsByKind);
            }

            return builder.ToImmutable();
        }

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<SemanticModelAnalyzerAction>> MakeSemanticModelActionsByAnalyzer()
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<SemanticModelAnalyzerAction>>();
            var actionsByAnalyzers = this.analyzerActions.SemanticModelActions.GroupBy(action => action.Analyzer);
            foreach (var analyzerAndActions in actionsByAnalyzers)
            {
                builder.Add(analyzerAndActions.Key, analyzerAndActions.ToImmutableArray());
            }

            return builder.ToImmutable();
        }

        private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>> MakeCompilationActionsByAnalyzer(ImmutableArray<CompilationAnalyzerAction> compilationActions)
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>>();
            var actionsByAnalyzers = compilationActions.GroupBy(action => action.Analyzer);
            foreach (var analyzerAndActions in actionsByAnalyzers)
            {
                builder.Add(analyzerAndActions.Key, analyzerAndActions.ToImmutableArray());
            }

            return builder.ToImmutable();
        }

        private async Task ProcessCompilationEventsAsync(CancellationToken cancellationToken)
        {
            CompilationCompletedEvent completedEvent = null;

            // Kick off worker tasks to process all compilation events (except the compilation end event) in parallel.
            // Compilation end event must be processed after all other events.
            var workerTasks = new Task[_workerCount];
            for (int i = 0; i < _workerCount; i++)
            {
                workerTasks[i] = Task.Run(async () =>
                    {
                        var result = await ProcessCompilationEventsCoreAsync(cancellationToken).ConfigureAwait(false);
                        if (result != null)
                        {
                            completedEvent = result;
                        }
                    }, cancellationToken);
            }

            // Kick off tasks to execute syntax tree actions.
            var syntaxTreeActionsTask = ExecuteSyntaxTreeActions(cancellationToken);

            // Wait for all worker threads to complete processing events.
            await Task.WhenAll(workerTasks.Concat(syntaxTreeActionsTask)).ConfigureAwait(false);

            // Finally process the compilation completed event, if any.
            if (completedEvent != null)
            {
                await ProcessEventAsync(completedEvent, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<CompilationCompletedEvent> ProcessCompilationEventsCoreAsync(CancellationToken cancellationToken)
        {
            while (!CompilationEventQueue.IsCompleted || CompilationEventQueue.Count > 0)
            {
                CompilationEvent e;
                try
                {
                    e = await CompilationEventQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    // When the queue is completed with a pending DequeueAsync return then a 
                    // TaskCanceledException will be thrown.  This just signals the queue is 
                    // complete and we should finish processing it.
                    Debug.Assert(CompilationEventQueue.IsCompleted, "DequeueAsync should never throw unless the AsyncQueue<T> is completed.");
                    break;
                }

                if (e.Compilation != _compilation)
                {
                    Debug.Assert(false, "CompilationEvent with a different compilation then driver's compilation?");
                    continue;
                }

                // Don't process the compilation completed event as other worker threads might still be processing other compilation events.
                // The caller will wait for all workers to complete and finally process this event.
                var compilationCompletedEvent = e as CompilationCompletedEvent;
                if (compilationCompletedEvent != null)
                {
                    return compilationCompletedEvent;
                }

                await ProcessEventAsync(e, cancellationToken).ConfigureAwait(false);
            }

            // Another thread dequeued the compilation completed event, so we just return null.
            return null;
        }

        private async Task ProcessEventAsync(CompilationEvent e, CancellationToken cancellationToken)
        {
            try
            {
                var processEventTask = ProcessEventCoreAsync(e, cancellationToken);
                if (processEventTask != null)
                {
                    await processEventTask.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // when just a single operation is cancelled, we continue processing events.
                // TODO: what is the desired behavior in this case?
            }
        }

        private Task ProcessEventCoreAsync(CompilationEvent e, CancellationToken cancellationToken)
        {
            var symbolEvent = e as SymbolDeclaredCompilationEvent;
            if (symbolEvent != null)
            {
                return ProcessSymbolDeclaredAsync(symbolEvent, cancellationToken);
            }

            var completedEvent = e as CompilationUnitCompletedEvent;
            if (completedEvent != null)
            {
                return ProcessCompilationUnitCompletedAsync(completedEvent, cancellationToken);
            }

            var endEvent = e as CompilationCompletedEvent;
            if (endEvent != null)
            {
                return ProcessCompilationCompletedAsync(endEvent, cancellationToken);
            }

            if (e is CompilationStartedEvent)
            {
                // Ignore CompilationStartedEvent.
                return null;
            }

            throw new InvalidOperationException("Unexpected compilation event of type " + e.GetType().Name);
        }

        private async Task ProcessSymbolDeclaredAsync(SymbolDeclaredCompilationEvent symbolEvent, CancellationToken cancellationToken)
        {
            // Collect all the analyzer action executors grouped by analyzer.
            // NOTE: Right now we execute all the actions sequentially, but there is scope to fine tune this to execute certain actions in parallel.
            var actionsMap = PooledDictionary<DiagnosticAnalyzer, ArrayBuilder<Action>>.GetInstance();
            
            try
            {
                var symbol = symbolEvent.Symbol;

                // Skip symbol actions for implicitly declared symbols.
                if (!symbol.IsImplicitlyDeclared)
                {
                    AddTasksForExecutingSymbolActions(symbolEvent, actionsMap, cancellationToken);
                }

                // Skip syntax actions for implicitly declared symbols, except for implicitly declared global namespace symbols.
                if (!symbol.IsImplicitlyDeclared ||
                    (symbol.Kind == SymbolKind.Namespace && ((INamespaceSymbol)symbol).IsGlobalNamespace))
                {
                    AddTasksForExecutingDeclaringReferenceActions(symbolEvent, actionsMap, cancellationToken);
                }

                // Execute all analyzer actions.
                await Task.Run(() =>
                {
                    foreach (var builder in actionsMap.Values)
                    {
                        foreach (var action in builder)
                        {
                            action();
                        }

                        builder.Free();
                    };
                }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                actionsMap.Free();
                symbolEvent.FlushCache();
            }
        }

        private void AddTasksForExecutingSymbolActions(SymbolDeclaredCompilationEvent symbolEvent, IDictionary<DiagnosticAnalyzer, ArrayBuilder<Action>> actionsMap, CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            Action<Diagnostic> addDiagnosticForSymbol = GetDiagnosticSinkWithSuppression(DiagnosticQueue.Enqueue, _compilation, symbol);

            foreach (var analyzerAndActions in _symbolActionsByKind)
            {
                var analyzer = analyzerAndActions.Key;
                var actionsByKind = analyzerAndActions.Value;

                Action executeSymbolActionsForAnalyzer = () =>
                    ExecuteSymbolActionsForAnalyzer(symbol, analyzer, actionsByKind, addDiagnosticForSymbol,  cancellationToken);

                AddAnalyzerActionsExecutor(actionsMap, analyzer, executeSymbolActionsForAnalyzer);
            }
        }

        private void ExecuteSymbolActionsForAnalyzer(
            ISymbol symbol,
            DiagnosticAnalyzer analyzer,
            ImmutableArray<ImmutableArray<SymbolAnalyzerAction>> actionsByKind,
            Action<Diagnostic> addDiagnosticForSymbol,
            CancellationToken cancellationToken)
        {
            // Invoke symbol analyzers only for source symbols.
            var declaringSyntaxRefs = symbol.DeclaringSyntaxReferences;
            if ((int)symbol.Kind < actionsByKind.Length && declaringSyntaxRefs.Any(s => s.SyntaxTree != null))
            {
                analyzerExecutor.ExecuteSymbolActions(actionsByKind[(int)symbol.Kind], symbol, addDiagnosticForSymbol);
            }
        }

        protected static void AddAnalyzerActionsExecutor(IDictionary<DiagnosticAnalyzer, ArrayBuilder<Action>> map, DiagnosticAnalyzer analyzer, Action executeAnalyzerActions)
        {
            ArrayBuilder<Action> currentActions;
            if (!map.TryGetValue(analyzer, out currentActions))
            {
                currentActions = ArrayBuilder<Action>.GetInstance();
                map[analyzer] = currentActions;
            }

            currentActions.Add(executeAnalyzerActions);
        }

        protected abstract void AddTasksForExecutingDeclaringReferenceActions(SymbolDeclaredCompilationEvent symbolEvent, IDictionary<DiagnosticAnalyzer, ArrayBuilder<Action>> actionsMap, CancellationToken cancellationToken);

        private async Task ProcessCompilationUnitCompletedAsync(CompilationUnitCompletedEvent completedEvent, CancellationToken cancellationToken)
        {
            // When the compiler is finished with a compilation unit, we can run user diagnostics which
            // might want to ask the compiler for all the diagnostics in the source file, for example
            // to get information about unnecessary usings.

            try
            {
                // Execute analyzers in parallel.
                var tasks = ArrayBuilder<Task>.GetInstance();

                var semanticModel = completedEvent.SemanticModel;
                foreach (var analyzerAndActions in _semanticModelActionsMap)
                {
                    var task = Task.Run(() =>
                    {
                        // Execute actions for a given analyzer sequentially.
                        analyzerExecutor.ExecuteSemanticModelActions(analyzerAndActions.Value, semanticModel);
                    }, cancellationToken);

                    tasks.Add(task); 
                }

                await Task.WhenAll(tasks.ToArrayAndFree()).ConfigureAwait(false);
            }
            finally
            {
                completedEvent.FlushCache();
            }
        }

        private async Task ProcessCompilationCompletedAsync(CompilationCompletedEvent endEvent, CancellationToken cancellationToken)
        {
            try
            {
                await ExecuteCompilationActionsAsync(_compilationActionsMap, cancellationToken).ConfigureAwait(false);
                await ExecuteCompilationActionsAsync(_compilationEndActionsMap, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                endEvent.FlushCache();
            }
        }

        private Task ExecuteCompilationActionsAsync(ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>> compilationActionsMap, CancellationToken cancellationToken)
        {
            // Execute analyzers in parallel.
            var tasks = ArrayBuilder<Task>.GetInstance();
            foreach (var analyzerAndActions in compilationActionsMap)
            {
                var task = Task.Run(() =>
                {
                    // Execute actions for a given analyzer sequentially.
                    analyzerExecutor.ExecuteCompilationActions(analyzerAndActions.Value);
                }, cancellationToken);

                tasks.Add(task);
            }

            return Task.WhenAll(tasks.ToArrayAndFree());
        }

        internal static Action<Diagnostic> GetDiagnosticSinkWithSuppression(Action<Diagnostic> addDiagnosticCore, Compilation compilation, ISymbol symbolOpt = null)
        {
            return diagnostic =>
            {
                var filteredDiagnostic = GetFilteredDiagnostic(diagnostic, compilation, symbolOpt);
                if (filteredDiagnostic != null)
                {
                    addDiagnosticCore(filteredDiagnostic);
                }
            };
        }

        private static Diagnostic GetFilteredDiagnostic(Diagnostic diagnostic, Compilation compilation, ISymbol symbolOpt = null)
        {
            var filteredDiagnostic = compilation.FilterDiagnostic(diagnostic);
            if (filteredDiagnostic != null)
            {
                var suppressMessageState = SuppressMessageStateByCompilation.GetValue(compilation, (c) => new SuppressMessageAttributeState(c));
                if (suppressMessageState.IsDiagnosticSuppressed(filteredDiagnostic, symbolOpt: symbolOpt))
                {
                    return null;
                }
            }

            return filteredDiagnostic;
        }

        private static Task<AnalyzerActions> GetAnalyzerActionsAsync(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalyzerManager analyzerManager,
            AnalyzerExecutor analyzerExecutor)
        {
            return Task.Run(async () =>
            {
                AnalyzerActions allAnalyzerActions = new AnalyzerActions();
                foreach (var analyzer in analyzers)
                {
                    if (!IsDiagnosticAnalyzerSuppressed(analyzer, analyzerExecutor.Compilation.Options, analyzerManager, analyzerExecutor))
                    {
                        var analyzerActions = await analyzerManager.GetAnalyzerActionsAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
                        allAnalyzerActions = allAnalyzerActions.Append(analyzerActions);
                    }
                }

                return allAnalyzerActions;
            }, analyzerExecutor.CancellationToken);
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// </summary>
        internal static bool IsDiagnosticAnalyzerSuppressed(
            DiagnosticAnalyzer analyzer,
            CompilationOptions options,
            AnalyzerManager analyzerManager,
            AnalyzerExecutor analyzerExecutor)
        {
            return analyzerManager.IsDiagnosticAnalyzerSuppressed(analyzer, options, IsCompilerAnalyzer, analyzerExecutor);
        }

        private static bool IsCompilerAnalyzer(DiagnosticAnalyzer analyzer)
        {
            return analyzer is CompilerDiagnosticAnalyzer;
        }

        public void Dispose()
        {
            this.CompilationEventQueue.TryComplete();
            this.DiagnosticQueue.TryComplete();
            _queueRegistration.Dispose();
        }
    }

    /// <summary>
    /// Driver to execute diagnostic analyzers for a given compilation.
    /// It uses a <see cref="AsyncQueue{TElement}"/> of <see cref="CompilationEvent"/>s to drive its analysis.
    /// </summary>
    internal class AnalyzerDriver<TLanguageKindEnum> : AnalyzerDriver where TLanguageKindEnum : struct
    {
        private readonly Func<SyntaxNode, TLanguageKindEnum> _getKind;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>> _lazyNodeActionsByKind;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>> _lazyCodeBlockStartActionsByAnalyzer;
        // Code block actions and code block end actions are kept separate so that it is easy to
        // execute the code block actions before the code block end actions.
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> _lazyCodeBlockEndActionsByAnalyzer;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> _lazyCodeBlockActionsByAnalyzer;

        /// <summary>
        /// Create an analyzer driver.
        /// </summary>
        /// <param name="analyzers">The set of analyzers to include in the analysis</param>
        /// <param name="getKind">A delegate that returns the language-specific kind for a given syntax node</param>
        /// <param name="analyzerManager">AnalyzerManager to manage analyzers for the lifetime of analyzer host.</param>
        /// <param name="cancellationToken">a cancellation token that can be used to abort analysis</param>
        internal AnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, Func<SyntaxNode, TLanguageKindEnum> getKind, AnalyzerManager analyzerManager, CancellationToken cancellationToken) : base(analyzers, analyzerManager, cancellationToken)
        {
            _getKind = getKind;
        }

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>> NodeActionsByKind
        {
            get
            {
                if (_lazyNodeActionsByKind == null)
                {
                    var nodeActions = this.analyzerActions.GetSyntaxNodeActions<TLanguageKindEnum>();
                    ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>> analyzerActionsByKind;
                    if (nodeActions.Any())
                    {
                        var nodeActionsByAnalyzers = nodeActions.GroupBy(a => a.Analyzer);
                        var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>>();
                        foreach (var analyzerAndActions in nodeActionsByAnalyzers)
                        {
                            ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> actionsByKind;
                            if (analyzerAndActions.Any())
                            {
                                actionsByKind = AnalyzerExecutor.GetNodeActionsByKind(analyzerAndActions);
                            }
                            else
                            {
                                actionsByKind = ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.Empty;
                            }

                            builder.Add(analyzerAndActions.Key, actionsByKind);
                        }

                        analyzerActionsByKind = builder.ToImmutable();
                    }
                    else
                    {
                        analyzerActionsByKind = ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>>.Empty;
                    }

                    Interlocked.CompareExchange(ref _lazyNodeActionsByKind, analyzerActionsByKind, null);
                }

                return _lazyNodeActionsByKind;
            }
        }

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>> CodeBlockStartActionsByAnalyzer
        {
            get
            {
                if (_lazyCodeBlockStartActionsByAnalyzer == null)
                {
                    ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>> codeBlockStartActionsByAnalyzer;
                    var codeBlockStartActions = this.analyzerActions.GetCodeBlockStartActions<TLanguageKindEnum>();
                    if (codeBlockStartActions.Any())
                    {
                        var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>>();
                        var actionsByAnalyzer = codeBlockStartActions.GroupBy(action => action.Analyzer);
                        foreach (var analyzerAndActions in actionsByAnalyzer)
                        {
                            builder.Add(analyzerAndActions.Key, analyzerAndActions.ToImmutableArrayOrEmpty());
                        }

                        codeBlockStartActionsByAnalyzer = builder.ToImmutable();
                    }
                    else
                    {
                        codeBlockStartActionsByAnalyzer = ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>>.Empty;
                    }

                    Interlocked.CompareExchange(ref _lazyCodeBlockStartActionsByAnalyzer, codeBlockStartActionsByAnalyzer, null);
                }

                return _lazyCodeBlockStartActionsByAnalyzer;
            }
        }

        private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> GetCodeBlockActionsByAnalyzer(
            ref ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> lazyCodeBlockActionsByAnalyzer,
            ImmutableArray<CodeBlockAnalyzerAction> codeBlockActions)
        {
            if (lazyCodeBlockActionsByAnalyzer == null)
            {
                ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> codeBlockActionsByAnalyzer;
                if (!codeBlockActions.IsEmpty)
                {
                    var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>>();
                    var actionsByAnalyzer = codeBlockActions.GroupBy(action => action.Analyzer);
                    foreach (var analyzerAndActions in actionsByAnalyzer)
                    {
                        builder.Add(analyzerAndActions.Key, analyzerAndActions.ToImmutableArrayOrEmpty());
                    }

                    codeBlockActionsByAnalyzer = builder.ToImmutable();
                }
                else
                {
                    codeBlockActionsByAnalyzer = ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>>.Empty;
                }

                Interlocked.CompareExchange(ref lazyCodeBlockActionsByAnalyzer, codeBlockActionsByAnalyzer, null);
            }

            return lazyCodeBlockActionsByAnalyzer;
        }

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> CodeBlockEndActionsByAnalyzer
        {
            get { return GetCodeBlockActionsByAnalyzer(ref _lazyCodeBlockEndActionsByAnalyzer, this.analyzerActions.CodeBlockEndActions); }
        }

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> CodeBlockActionsByAnalyzer
        {
            get { return GetCodeBlockActionsByAnalyzer(ref _lazyCodeBlockActionsByAnalyzer, this.analyzerActions.CodeBlockActions); }
        }

        protected override void AddTasksForExecutingDeclaringReferenceActions(
            SymbolDeclaredCompilationEvent symbolEvent,
            IDictionary<DiagnosticAnalyzer, ArrayBuilder<Action>> actionsMap,
            CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            var executeSyntaxNodeActions = this.NodeActionsByKind.Any();
            var executeCodeBlockActions = AnalyzerExecutor.CanHaveExecutableCodeBlock(symbol) &&
                (!this.CodeBlockStartActionsByAnalyzer.IsEmpty || !this.CodeBlockEndActionsByAnalyzer.IsEmpty || !this.CodeBlockActionsByAnalyzer.IsEmpty);

            if (executeSyntaxNodeActions || executeCodeBlockActions)
            {
                foreach (var decl in symbol.DeclaringSyntaxReferences)
                {
                    AddTasksForExecutingDeclaringReferenceActions(decl, symbolEvent, actionsMap, executeSyntaxNodeActions, executeCodeBlockActions, cancellationToken);
                }
            }
        }

        private void AddTasksForExecutingDeclaringReferenceActions(
            SyntaxReference decl,
            SymbolDeclaredCompilationEvent symbolEvent,
            IDictionary<DiagnosticAnalyzer, ArrayBuilder<Action>> actionsMap,
            bool shouldExecuteSyntaxNodeActions,
            bool shouldExecuteCodeBlockActions,
            CancellationToken cancellationToken)
        {
            Debug.Assert(shouldExecuteSyntaxNodeActions || shouldExecuteCodeBlockActions);

            var symbol = symbolEvent.Symbol;
            SemanticModel semanticModel = symbolEvent.SemanticModel(decl);
            var declaringReferenceSyntax = decl.GetSyntax(cancellationToken);
            var syntax = semanticModel.GetTopmostNodeForDiagnosticAnalysis(symbol, declaringReferenceSyntax);

            // We only care about the top level symbol declaration and its immediate member declarations.
            int? levelsToCompute = 2;

            var declarationsInNode = semanticModel.GetDeclarationsInNode(syntax, getSymbol: syntax != declaringReferenceSyntax, cancellationToken: cancellationToken, levelsToCompute: levelsToCompute);

            // Execute stateless syntax node actions.
            if (shouldExecuteSyntaxNodeActions)
            {
                var nodesToAnalyze = GetSyntaxNodesToAnalyze(syntax, symbol, declarationsInNode, semanticModel, analyzerExecutor);

                foreach (var analyzerAndActions in this.NodeActionsByKind)
                {
                    Action executeStatelessNodeActions = () =>
                        analyzerExecutor.ExecuteSyntaxNodeActions(nodesToAnalyze, analyzerAndActions.Value, semanticModel, _getKind);

                    AddAnalyzerActionsExecutor(actionsMap, analyzerAndActions.Key, executeStatelessNodeActions);
                }
            }

            // Execute code block actions.
            if (shouldExecuteCodeBlockActions)
            {
                // Compute the executable code blocks of interest.
                var executableCodeBlocks = ImmutableArray<SyntaxNode>.Empty;
                foreach (var declInNode in declarationsInNode)
                {
                    if (declInNode.DeclaredNode == syntax || declInNode.DeclaredNode == declaringReferenceSyntax)
                    {
                        executableCodeBlocks = declInNode.ExecutableCodeBlocks;
                        break;
                    }
                }

                if (executableCodeBlocks.Any())
                {
                    foreach (var analyzerActions in GetCodeBlockActions())
                    {
                        Action executeCodeBlockActions = () =>
                        {
                            analyzerExecutor.ExecuteCodeBlockActions(analyzerActions.CodeBlockStartActions, analyzerActions.CodeBlockActions, analyzerActions.CodeBlockEndActions,
                                syntax, symbol, executableCodeBlocks, semanticModel, _getKind);
                        };

                        AddAnalyzerActionsExecutor(actionsMap, analyzerActions.Analyzer, executeCodeBlockActions);
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private struct CodeBlockAnalyzerActions
        {
            public DiagnosticAnalyzer Analyzer;
            public ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> CodeBlockStartActions;
            public ImmutableArray<CodeBlockAnalyzerAction> CodeBlockActions;
            public ImmutableArray<CodeBlockAnalyzerAction> CodeBlockEndActions;
        }

        private IEnumerable<CodeBlockAnalyzerActions> GetCodeBlockActions()
        {
            // Include analyzers with code block start actions.

            foreach (var analyzerAndActions in this.CodeBlockStartActionsByAnalyzer)
            {
                ImmutableArray<CodeBlockAnalyzerAction> codeBlockActions;
                if (!this.CodeBlockActionsByAnalyzer.TryGetValue(analyzerAndActions.Key, out codeBlockActions))
                {
                    codeBlockActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
                }

                ImmutableArray<CodeBlockAnalyzerAction> codeBlockEndActions;
                if (!this.CodeBlockEndActionsByAnalyzer.TryGetValue(analyzerAndActions.Key, out codeBlockEndActions))
                {
                    codeBlockEndActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
                }
                
                yield return
                    new CodeBlockAnalyzerActions
                    {
                        Analyzer = analyzerAndActions.Key,
                        CodeBlockStartActions = analyzerAndActions.Value,
                        CodeBlockActions = codeBlockActions,
                        CodeBlockEndActions = codeBlockEndActions
                    };
            }

            // Include analyzers with code block actions.

            foreach (var analyzerAndActions in this.CodeBlockActionsByAnalyzer)
            {
                // Skip analyzers included above.
                if (!CodeBlockStartActionsByAnalyzer.ContainsKey(analyzerAndActions.Key))
                {
                    ImmutableArray<CodeBlockAnalyzerAction> codeBlockEndActions;
                    if (!this.CodeBlockEndActionsByAnalyzer.TryGetValue(analyzerAndActions.Key, out codeBlockEndActions))
                    {
                        codeBlockEndActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
                    }
                    
                    yield return
                        new CodeBlockAnalyzerActions
                        {
                            Analyzer = analyzerAndActions.Key,
                            CodeBlockStartActions = ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>.Empty,
                            CodeBlockActions = analyzerAndActions.Value,
                            CodeBlockEndActions = codeBlockEndActions
                        };
                }
            }

            // Include analyzers with code block end actions.

            foreach (var analyzerAndActions in this.CodeBlockEndActionsByAnalyzer)
            {
                // Skip analyzers included above.
                if (!CodeBlockStartActionsByAnalyzer.ContainsKey(analyzerAndActions.Key) && !CodeBlockActionsByAnalyzer.ContainsKey(analyzerAndActions.Key))
                {
                    yield return
                        new CodeBlockAnalyzerActions
                        {
                            Analyzer = analyzerAndActions.Key,
                            CodeBlockStartActions = ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>.Empty,
                            CodeBlockActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty,
                            CodeBlockEndActions = analyzerAndActions.Value
                        };
                }
            }
        }


        private static ImmutableArray<SyntaxNode> GetSyntaxNodesToAnalyze(
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            IEnumerable<DeclarationInfo> declarationsInNode,
            SemanticModel semanticModel,
            AnalyzerExecutor analyzerExecutor)
        {
            // Eliminate syntax nodes for descendant member declarations within declarations.
            // There will be separate symbols declared for the members, hence we avoid duplicate syntax analysis by skipping these here.
            HashSet<SyntaxNode> descendantDeclsToSkip = null;
            bool first = true;
            foreach (var declInNode in declarationsInNode)
            {
                if (declInNode.DeclaredNode != declaredNode)
                {
                    // Might be a field declaration statement with multiple fields declared.
                    // If so, we execute syntax node analysis for entire field declaration (and its descendants)
                    // if we processing the first field and skip syntax actions for remaining fields in the declaration.
                    if (declInNode.DeclaredSymbol == declaredSymbol)
                    {
                        if (first)
                        {
                            break;
                        }

                        return ImmutableArray<SyntaxNode>.Empty;
                    }

                    // Compute the topmost node representing the syntax declaration for the member that needs to be skipped.
                    var declarationNodeToSkip = declInNode.DeclaredNode;
                    var declaredSymbolOfDeclInNode = declInNode.DeclaredSymbol ?? semanticModel.GetDeclaredSymbol(declInNode.DeclaredNode, analyzerExecutor.CancellationToken);
                    if (declaredSymbolOfDeclInNode != null)
                    {
                        declarationNodeToSkip = semanticModel.GetTopmostNodeForDiagnosticAnalysis(declaredSymbolOfDeclInNode, declInNode.DeclaredNode);
                    }

                    descendantDeclsToSkip = descendantDeclsToSkip ?? new HashSet<SyntaxNode>();
                    descendantDeclsToSkip.Add(declarationNodeToSkip);
                }

                first = false;
            }

            var nodesToAnalyze = descendantDeclsToSkip == null ?
                declaredNode.DescendantNodesAndSelf(descendIntoTrivia: true) :
                GetSyntaxNodesToAnalyze(declaredNode, descendantDeclsToSkip);
            return nodesToAnalyze.ToImmutableArray();
        }

        private static IEnumerable<SyntaxNode> GetSyntaxNodesToAnalyze(SyntaxNode declaredNode, HashSet<SyntaxNode> descendantDeclsToSkip)
        {
            Debug.Assert(declaredNode != null);
            Debug.Assert(descendantDeclsToSkip != null);

            foreach (var node in declaredNode.DescendantNodesAndSelf(n => !descendantDeclsToSkip.Contains(n), descendIntoTrivia: true))
            {
                if (!descendantDeclsToSkip.Contains(node))
                {
                    yield return node;
                }
            }
        }
    }

    internal static class AnalyzerDriverResources
    {
        internal static string AnalyzerFailure => CodeAnalysisResources.CompilerAnalyzerFailure;
        internal static string AnalyzerThrows => CodeAnalysisResources.CompilerAnalyzerThrows;
        internal static string AnalyzerThrowsDescription => CodeAnalysisResources.CompilerAnalyzerThrowsDescription;
        internal static string ArgumentElementCannotBeNull => CodeAnalysisResources.ArgumentElementCannotBeNull;
        internal static string ArgumentCannotBeEmpty => CodeAnalysisResources.ArgumentCannotBeEmpty;
        internal static string UnsupportedDiagnosticReported => CodeAnalysisResources.UnsupportedDiagnosticReported;
    }
}
