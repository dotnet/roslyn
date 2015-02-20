// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

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

        private readonly Action<Diagnostic> _addDiagnostic;
        private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
        private readonly CancellationTokenRegistration _queueRegistration;
        protected readonly AnalyzerManager analyzerManager;
        protected readonly AnalyzerOptions analyzerOptions;
        internal readonly Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException;

        // Lazy fields initialized in Initialize() API
        private Compilation _compilation;
        internal AnalyzerActions analyzerActions;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ImmutableArray<SymbolAnalyzerAction>>> _symbolActionsByKind;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<SemanticModelAnalyzerAction>> _semanticModelActionsMap;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CompilationEndAnalyzerAction>> _compilationEndActionsMap;

        /// <summary>
        /// Primary driver task which processes all <see cref="CompilationEventQueue"/> events, runs analyzer actions and signals completion of <see cref="DiagnosticQueue"/> at the end.
        /// </summary>
        private Task _primaryTask;

        /// <summary>
        /// The compilation queue to create the compilation with via WithEventQueue.
        /// </summary>
        public AsyncQueue<CompilationEvent> CompilationEventQueue
        {
            get; private set;
        }

        /// <summary>
        /// An async queue that is fed the diagnostics as they are computed.
        /// </summary>
        public AsyncQueue<Diagnostic> DiagnosticQueue
        {
            get; private set;
        }

        /// <summary>
        /// Initializes the compilation for the analyzer driver.
        /// It also computes and initializes <see cref="analyzerActions"/> and <see cref="_symbolActionsByKind"/>.
        /// Finally, it initializes and starts the <see cref="_primaryTask"/> for the driver.
        /// </summary>
        /// <remarks>
        /// NOTE: This method must only be invoked from <see cref="AnalyzerDriver.Create(Compilation, ImmutableArray{DiagnosticAnalyzer}, AnalyzerOptions, AnalyzerManager, out Compilation, CancellationToken)"/>.
        /// </remarks>
        private void Initialize(Compilation comp, CancellationToken cancellationToken)
        {
            try
            {
                Debug.Assert(_compilation == null);
                Debug.Assert(comp.EventQueue == this.CompilationEventQueue);

                _compilation = comp;

                // Compute the set of effective actions based on suppression, and running the initial analyzers
                var analyzerActionsTask = GetAnalyzerActionsAsync(_analyzers, analyzerManager, comp, analyzerOptions, continueOnAnalyzerException, cancellationToken);
                var initializeTask = analyzerActionsTask.ContinueWith(t =>
                {
                    this.analyzerActions = t.Result;
                    _symbolActionsByKind = MakeSymbolActionsByKind();
                    _semanticModelActionsMap = MakeSemanticModelActionsByAnalyzer();
                    _compilationEndActionsMap = MakeCompilationEndActionsByAnalyzer();
                }, cancellationToken);

                // create the primary driver task.
                cancellationToken.ThrowIfCancellationRequested();
                _primaryTask = Task.Run(async () =>
                    {
                        await initializeTask.ConfigureAwait(false);
                        await ProcessCompilationEventsAsync(cancellationToken).ConfigureAwait(false);
                        await ExecuteSyntaxTreeActions(cancellationToken).ConfigureAwait(false);
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
                        foreach (var syntaxTreeAction in analyzerAndActions)
                        {
                            // Catch Exception from executing the action
                            analyzerManager.ExecuteAndCatchIfThrows(syntaxTreeAction.Analyzer, continueOnAnalyzerException, () =>
                            {
                                var context = new SyntaxTreeAnalysisContext(tree, analyzerOptions, _addDiagnostic, cancellationToken);
                                syntaxTreeAction.Action(context);
                            }, cancellationToken);
                        }
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
        /// <param name="newCompilation">The new compilation with the analyzer driver attached.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to abort analysis.</param>
        /// <returns>A newly created analyzer driver</returns>
        /// <remarks>
        /// Note that since a compilation is immutable, the act of creating a driver and attaching it produces
        /// a new compilation. Any further actions on the compilation should use the new compilation.
        /// </remarks>
        public static AnalyzerDriver Create(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, AnalyzerManager analyzerManager, out Compilation newCompilation, CancellationToken cancellationToken)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (analyzers.IsDefaultOrEmpty)
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentCannotBeEmpty, nameof(analyzers));
            }

            if (analyzers.Any(a => a == null))
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentElementCannotBeNull, nameof(analyzers));
            }

            return Create(compilation, analyzers, options, analyzerManager, out newCompilation, continueOnAnalyzerException: null, cancellationToken: cancellationToken);
        }

        // internal for testing purposes
        internal static AnalyzerDriver Create(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, AnalyzerManager analyzerManager, out Compilation newCompilation, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException, CancellationToken cancellationToken)
        {
            options = options ?? AnalyzerOptions.Empty;
            AnalyzerDriver analyzerDriver = compilation.AnalyzerForLanguage(analyzers, options, analyzerManager, continueOnAnalyzerException, cancellationToken);
            newCompilation = compilation.WithEventQueue(analyzerDriver.CompilationEventQueue);
            analyzerDriver.Initialize(newCompilation, cancellationToken);
            return analyzerDriver;
        }

        /// <summary>
        /// Create an analyzer driver.
        /// </summary>
        /// <param name="analyzers">The set of analyzers to include in the analysis</param>
        /// <param name="options">Options that are passed to analyzers</param>
        /// <param name="analyzerManager">AnalyzerManager to manage analyzers for analyzer host's lifetime.</param>
        /// <param name="cancellationToken">a cancellation token that can be used to abort analysis</param>
        /// <param name="continueOnAnalyzerException">Delegate which is invoked when an analyzer throws an exception.
        /// If a non-null delegate is provided and it returns true, then the exception is handled and converted into a diagnostic and driver continues with other analyzers.
        /// Otherwise if it returns false, then the exception is not handled by the driver.
        /// If null, then the driver always handles the exception.
        /// </param>
        protected AnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, AnalyzerManager analyzerManager, CancellationToken cancellationToken, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException = null)
        {
            _analyzers = analyzers;
            this.analyzerOptions = options;
            this.analyzerManager = analyzerManager;

            this.CompilationEventQueue = new AsyncQueue<CompilationEvent>();
            this.DiagnosticQueue = new AsyncQueue<Diagnostic>();
            _addDiagnostic = GetDiagnosticSinkWithSuppression();
            _queueRegistration = cancellationToken.Register(() =>
            {
                this.CompilationEventQueue.TryComplete();
                this.DiagnosticQueue.TryComplete();
            });

            Func<Exception, DiagnosticAnalyzer, bool> defaultExceptionHandler = (exception, analyzer) => true;
            this.continueOnAnalyzerException = continueOnAnalyzerException ?? defaultExceptionHandler;
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

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CompilationEndAnalyzerAction>> MakeCompilationEndActionsByAnalyzer()
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<CompilationEndAnalyzerAction>>();
            var actionsByAnalyzers = this.analyzerActions.CompilationEndActions.GroupBy(action => action.Analyzer);
            foreach (var analyzerAndActions in actionsByAnalyzers)
            {
                builder.Add(analyzerAndActions.Key, analyzerAndActions.ToImmutableArray());
            }

            return builder.ToImmutable();
        }

        private async Task ProcessCompilationEventsAsync(CancellationToken cancellationToken)
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

                try
                {
                    await ProcessEventAsync(e, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // when just a single operation is cancelled, we continue processing events.
                    // TODO: what is the desired behavior in this case?
                }
            }
        }

        private async Task ProcessEventAsync(CompilationEvent e, CancellationToken cancellationToken)
        {
            var symbolEvent = e as SymbolDeclaredCompilationEvent;
            if (symbolEvent != null)
            {
                await ProcessSymbolDeclaredAsync(symbolEvent, cancellationToken).ConfigureAwait(false);
                return;
            }

            var completedEvent = e as CompilationUnitCompletedEvent;
            if (completedEvent != null)
            {
                await ProcessCompilationUnitCompleted(completedEvent, cancellationToken).ConfigureAwait(false);
                return;
            }

            var endEvent = e as CompilationCompletedEvent;
            if (endEvent != null)
            {
                await ProcessCompilationCompletedAsync(endEvent, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (e is CompilationStartedEvent)
            {
                // Ignore CompilationStartedEvent.
                return;
            }

            throw new InvalidOperationException("Unexpected compilation event of type " + e.GetType().Name);
        }

        private async Task ProcessSymbolDeclaredAsync(SymbolDeclaredCompilationEvent symbolEvent, CancellationToken cancellationToken)
        {
            // Create a task per-analyzer to execute analyzer actions.
            // We execute analyzers in parallel, but for a given analyzer we execute actions sequentially.
            var tasksMap = PooledDictionary<DiagnosticAnalyzer, Task>.GetInstance();

            try
            {
                var symbol = symbolEvent.Symbol;

                // Skip symbol actions for implicitly declared symbols.
                if (!symbol.IsImplicitlyDeclared)
                {
                    AddTasksForExecutingSymbolActions(symbolEvent, tasksMap, cancellationToken);
                }

                // Skip syntax actions for implicitly declared symbols, except for implicitly declared global namespace symbols.
                if (!symbol.IsImplicitlyDeclared ||
                    (symbol.Kind == SymbolKind.Namespace && ((INamespaceSymbol)symbol).IsGlobalNamespace))
                {
                    AddTasksForExecutingDeclaringReferenceActions(symbolEvent, tasksMap, _addDiagnostic, cancellationToken);
                }

                // Execute all analyzer actions.
                await Task.WhenAll(tasksMap.Values).ConfigureAwait(false);
            }
            finally
            {
                tasksMap.Free();
                symbolEvent.FlushCache();
            }
        }

        private void AddTasksForExecutingSymbolActions(SymbolDeclaredCompilationEvent symbolEvent, IDictionary<DiagnosticAnalyzer, Task> taskMap, CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            Action<Diagnostic> addDiagnosticForSymbol = GetDiagnosticSinkWithSuppression(symbol);

            foreach (var analyzerAndActions in _symbolActionsByKind)
            {
                var analyzer = analyzerAndActions.Key;
                var actionsByKind = analyzerAndActions.Value;

                Action executeSymbolActionsForAnalyzer = () =>
                        ExecuteSymbolActionsForAnalyzer(symbol, analyzer, actionsByKind, addDiagnosticForSymbol, cancellationToken);

                AddAnalyzerActionsExecutor(taskMap, analyzer, executeSymbolActionsForAnalyzer, cancellationToken);
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
                foreach (var da in actionsByKind[(int)symbol.Kind])
                {
                    Debug.Assert(da.Analyzer == analyzer);

                    // Catch Exception from analyzing the symbol
                    analyzerManager.ExecuteAndCatchIfThrows(da.Analyzer, continueOnAnalyzerException, () =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var symbolContext = new SymbolAnalysisContext(symbol, _compilation, this.analyzerOptions, addDiagnosticForSymbol, cancellationToken);
                        da.Action(symbolContext);
                    }, cancellationToken);
                }
            }
        }

        protected static void AddAnalyzerActionsExecutor(IDictionary<DiagnosticAnalyzer, Task> map, DiagnosticAnalyzer analyzer, Action executeAnalyzerActions, CancellationToken cancellationToken)
        {
            Task currentTask;
            if (!map.TryGetValue(analyzer, out currentTask))
            {
                map[analyzer] = Task.Run(executeAnalyzerActions, cancellationToken);
            }
            else
            {
                map[analyzer] = currentTask.ContinueWith(_ => executeAnalyzerActions(), cancellationToken);
            }
        }

        protected abstract void AddTasksForExecutingDeclaringReferenceActions(SymbolDeclaredCompilationEvent symbolEvent, IDictionary<DiagnosticAnalyzer, Task> taskMap, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken);

        private Task ProcessCompilationUnitCompleted(CompilationUnitCompletedEvent completedEvent, CancellationToken cancellationToken)
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
                        foreach (var semanticModelAction in analyzerAndActions.Value)
                        {
                            Debug.Assert(semanticModelAction.Analyzer == analyzerAndActions.Key);

                            // Catch Exception from semanticModelAction
                            analyzerManager.ExecuteAndCatchIfThrows(semanticModelAction.Analyzer, continueOnAnalyzerException, () =>
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    var semanticModelContext = new SemanticModelAnalysisContext(semanticModel, this.analyzerOptions, _addDiagnostic, cancellationToken);
                                    semanticModelAction.Action(semanticModelContext);
                                }, cancellationToken);
                        }
                    }, cancellationToken);

                    tasks.Add(task);
                }

                return Task.WhenAll(tasks.ToArrayAndFree());
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
                // Execute analyzers in parallel.
                var tasks = ArrayBuilder<Task>.GetInstance();
                foreach (var analyzerAndActions in _compilationEndActionsMap)
                {
                    var task = Task.Run(() =>
                    {
                        // Execute actions for a given analyzer sequentially.
                        foreach (var endAction in analyzerAndActions.Value)
                        {
                            Debug.Assert(endAction.Analyzer == analyzerAndActions.Key);

                            // Catch Exception from endAction
                            analyzerManager.ExecuteAndCatchIfThrows(endAction.Analyzer, continueOnAnalyzerException, () =>
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    var compilationContext = new CompilationEndAnalysisContext(_compilation, this.analyzerOptions, _addDiagnostic, cancellationToken);
                                    endAction.Action(compilationContext);
                                }, cancellationToken);
                        }
                    }, cancellationToken);

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks.ToArrayAndFree()).ConfigureAwait(false);
            }
            finally
            {
                endEvent.FlushCache();
            }
        }

        internal protected Action<Diagnostic> GetDiagnosticSinkWithSuppression(ISymbol symbolOpt = null)
        {
            return diagnostic =>
            {
                var d = _compilation.FilterDiagnostic(diagnostic);
                if (d != null)
                {
                    var suppressMessageState = SuppressMessageStateByCompilation.GetValue(_compilation, (c) => new SuppressMessageAttributeState(c));
                    if (!suppressMessageState.IsDiagnosticSuppressed(d, symbolOpt: symbolOpt))
                    {
                        DiagnosticQueue.Enqueue(d);
                    }
                }
            };
        }

        private static void VerifyArguments(
            IEnumerable<ISymbol> symbols,
            Compilation compilation,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (symbols.Any(s => s == null))
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentElementCannotBeNull, nameof(symbols));
            }

            VerifyArguments(compilation, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        private static void VerifyArguments(
            Compilation compilation,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            VerifyArguments(actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        internal static void VerifyArguments(
            SemanticModel semanticModel,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (semanticModel == null)
            {
                throw new ArgumentNullException(nameof(semanticModel));
            }

            VerifyArguments(actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        private static void VerifyArguments(
            SyntaxTree syntaxTree,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            VerifyArguments(actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        private static void VerifyArguments(
            Compilation compilation,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            DiagnosticAnalyzer analyzer,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (analyzer == null)
            {
                throw new ArgumentNullException(nameof(analyzer));
            }

            VerifyArguments(actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        internal static void VerifyArguments(
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            VerifyArguments(analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        internal protected static void VerifyArguments(
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (analyzerOptions == null)
            {
                throw new ArgumentNullException(nameof(analyzerOptions));
            }

            if (addDiagnostic == null)
            {
                throw new ArgumentNullException(nameof(addDiagnostic));
            }

            if (continueOnAnalyzerException == null)
            {
                throw new ArgumentNullException(nameof(continueOnAnalyzerException));
            }
        }

        internal protected static void VerifyArguments(
            DiagnosticAnalyzer analyzer,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (analyzer == null)
            {
                throw new ArgumentNullException(nameof(analyzer));
            }

            if (addDiagnostic == null)
            {
                throw new ArgumentNullException(nameof(addDiagnostic));
            }

            if (continueOnAnalyzerException == null)
            {
                throw new ArgumentNullException(nameof(continueOnAnalyzerException));
            }
        }

        private static Task<AnalyzerActions> GetAnalyzerActionsAsync(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalyzerManager analyzerManager,
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException, 
            CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                AnalyzerActions allAnalyzerActions = new AnalyzerActions();
                foreach (var analyzer in analyzers)
                {
                    if (!IsDiagnosticAnalyzerSuppressed(analyzer, analyzerManager, compilation.Options, continueOnAnalyzerException, cancellationToken))
                    {
                        var analyzerActions = await analyzerManager.GetAnalyzerActionsAsync(analyzer,
                            compilation, analyzerOptions, continueOnAnalyzerException, cancellationToken).ConfigureAwait(false);
                        allAnalyzerActions = allAnalyzerActions.Append(analyzerActions);
                    }
                }

                return allAnalyzerActions;
            }, cancellationToken);
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// </summary>
        internal static bool IsDiagnosticAnalyzerSuppressed(
            DiagnosticAnalyzer analyzer,
            AnalyzerManager analyzerManager,
            CompilationOptions options,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            return analyzerManager.IsDiagnosticAnalyzerSuppressed(analyzer, options, continueOnAnalyzerException, IsCompilerAnalyzer, cancellationToken);
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
        private Func<SyntaxNode, TLanguageKindEnum> _getKind;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>> _lazyNodeActionsByKind = null;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>> _lazyCodeBlockStartActionsByAnalyzer = null;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockEndAnalyzerAction>> _lazyCodeBlockEndActionsByAnalyzer = null;

        /// <summary>
        /// Create an analyzer driver.
        /// </summary>
        /// <param name="analyzers">The set of analyzers to include in the analysis</param>
        /// <param name="getKind">A delegate that returns the language-specific kind for a given syntax node</param>
        /// <param name="options">Options that are passed to analyzers</param>
        /// <param name="analyzerManager">AnalyzerManager to manage analyzers for the lifetime of analyzer host.</param>
        /// <param name="continueOnAnalyzerException">Delegate which is invoked when an analyzer throws an exception.
        /// If a non-null delegate is provided and it returns true, then the exception is handled and converted into a diagnostic and driver continues with other analyzers.
        /// Otherwise if it returns false, then the exception is not handled by the driver.
        /// If null, then the driver always handles the exception.
        /// </param>
        /// <param name="cancellationToken">a cancellation token that can be used to abort analysis</param>
        internal AnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, Func<SyntaxNode, TLanguageKindEnum> getKind, AnalyzerOptions options, AnalyzerManager analyzerManager, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException, CancellationToken cancellationToken) : base(analyzers, options, analyzerManager, cancellationToken, continueOnAnalyzerException)
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
                        var addDiagnostic = GetDiagnosticSinkWithSuppression();
                        var nodeActionsByAnalyzers = nodeActions.GroupBy(a => a.Analyzer);
                        var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>>();
                        foreach (var analzerAndActions in nodeActionsByAnalyzers)
                        {
                            ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> actionsByKind;
                            if (analzerAndActions.Any())
                            {
                                actionsByKind = AnalyzerManager.GetNodeActionsByKind(analzerAndActions, addDiagnostic);
                            }
                            else
                            {
                                actionsByKind = ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.Empty;
                            }

                            builder.Add(analzerAndActions.Key, actionsByKind);
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

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>> CodeBlockStartActionsByAnalyer
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

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockEndAnalyzerAction>> CodeBlockEndActionsByAnalyer
        {
            get
            {
                if (_lazyCodeBlockEndActionsByAnalyzer == null)
                {
                    ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockEndAnalyzerAction>> codeBlockEndActionsByAnalyzer;
                    var codeBlockEndActions = this.analyzerActions.CodeBlockEndActions;
                    if (codeBlockEndActions.Any())
                    {
                        var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<CodeBlockEndAnalyzerAction>>();
                        var actionsByAnalyzer = codeBlockEndActions.GroupBy(action => action.Analyzer);
                        foreach (var analyzerAndActions in actionsByAnalyzer)
                        {
                            builder.Add(analyzerAndActions.Key, analyzerAndActions.ToImmutableArrayOrEmpty());
                        }

                        codeBlockEndActionsByAnalyzer = builder.ToImmutable();
                    }
                    else
                    {
                        codeBlockEndActionsByAnalyzer = ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockEndAnalyzerAction>>.Empty;
                    }

                    Interlocked.CompareExchange(ref _lazyCodeBlockEndActionsByAnalyzer, codeBlockEndActionsByAnalyzer, null);
                }

                return _lazyCodeBlockEndActionsByAnalyzer;
            }
        }

        protected override void AddTasksForExecutingDeclaringReferenceActions(
            SymbolDeclaredCompilationEvent symbolEvent,
            IDictionary<DiagnosticAnalyzer, Task> taskMap,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            var executeSyntaxNodeActions = this.NodeActionsByKind.Any();
            var executeCodeBlockActions = AnalyzerManager.CanHaveExecutableCodeBlock(symbol) && (this.CodeBlockStartActionsByAnalyer.Any() || this.CodeBlockEndActionsByAnalyer.Any());

            if (executeSyntaxNodeActions || executeCodeBlockActions)
            {
                foreach (var decl in symbol.DeclaringSyntaxReferences)
                {
                    AddTasksForExecutingDeclaringReferenceActions(decl, symbolEvent, taskMap, reportDiagnostic,
                        executeSyntaxNodeActions, executeCodeBlockActions, cancellationToken);
                }
            }
        }

        private void AddTasksForExecutingDeclaringReferenceActions(
            SyntaxReference decl,
            SymbolDeclaredCompilationEvent symbolEvent,
            IDictionary<DiagnosticAnalyzer, Task> taskMap,
            Action<Diagnostic> reportDiagnostic,
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
                foreach (var analyzerAndActions in this.NodeActionsByKind)
                {
                    Action executeStatelessNodeActions = () =>
                        ExecuteStatelessNodeActions(analyzerAndActions.Value, syntax, symbol, declarationsInNode, semanticModel,
                        reportDiagnostic, this.continueOnAnalyzerException, this.analyzerOptions, _getKind, analyzerManager, cancellationToken);

                    AddAnalyzerActionsExecutor(taskMap, analyzerAndActions.Key, executeStatelessNodeActions, cancellationToken);
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
                    foreach (var analyzerAndActions in this.CodeBlockStartActionsByAnalyer)
                    {
                        Action executeCodeBlockActions = () =>
                        {
                            var codeBlockStartActions = analyzerAndActions.Value;
                            var codeBlockEndActions = this.CodeBlockEndActionsByAnalyer.ContainsKey(analyzerAndActions.Key) ?
                                this.CodeBlockEndActionsByAnalyer[analyzerAndActions.Key] :
                                ImmutableArray<CodeBlockEndAnalyzerAction>.Empty;

                            analyzerManager.ExecuteCodeBlockActions(codeBlockStartActions, codeBlockEndActions,
                                syntax, symbol, executableCodeBlocks, analyzerOptions,
                                semanticModel, reportDiagnostic, this.continueOnAnalyzerException, _getKind, cancellationToken);
                        };

                        AddAnalyzerActionsExecutor(taskMap, analyzerAndActions.Key, executeCodeBlockActions, cancellationToken);
                    }

                    // Execute code block end actions for analyzers which don't have corresponding code block start actions.
                    foreach (var analyzerAndActions in this.CodeBlockEndActionsByAnalyer)
                    {
                        // skip analyzers executed above.
                        if (!CodeBlockStartActionsByAnalyer.ContainsKey(analyzerAndActions.Key))
                        {
                            Action executeCodeBlockActions = () =>
                            {
                                var codeBlockStartActions = ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>.Empty;
                                var codeBlockEndActions = analyzerAndActions.Value;

                                analyzerManager.ExecuteCodeBlockActions(codeBlockStartActions, codeBlockEndActions,
                                    syntax, symbol, executableCodeBlocks, analyzerOptions,
                            semanticModel, reportDiagnostic, this.continueOnAnalyzerException, _getKind, cancellationToken);
                            };

                            AddAnalyzerActionsExecutor(taskMap, analyzerAndActions.Key, executeCodeBlockActions, cancellationToken);
                        }
                    }
                }
            }
        }

        private static void ExecuteStatelessNodeActions(
            IDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> actionsByKind,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            IEnumerable<DeclarationInfo> declarationsInNode,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            AnalyzerOptions analyzerOptions,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            AnalyzerManager analyzerManager,
            CancellationToken cancellationToken)
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
                    // Adjust syntax node for analysis to be just the field (except for the first field so that we don't skip nodes common to all fields).
                    if (declInNode.DeclaredSymbol == declaredSymbol)
                    {
                        if (!first)
                        {
                            declaredNode = declInNode.DeclaredNode;
                        }

                        continue;
                    }

                    // Compute the topmost node representing the syntax declaration for the member that needs to be skipped.
                    var declarationNodeToSkip = declInNode.DeclaredNode;
                    var declaredSymbolOfDeclInNode = declInNode.DeclaredSymbol ?? semanticModel.GetDeclaredSymbol(declInNode.DeclaredNode, cancellationToken);
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
                declaredNode.DescendantNodesAndSelf(n => !descendantDeclsToSkip.Contains(n), descendIntoTrivia: true).Except(descendantDeclsToSkip);

            analyzerManager.ExecuteSyntaxNodeActions(nodesToAnalyze, actionsByKind, semanticModel,
                analyzerOptions, addDiagnostic, continueOnAnalyzerException, getKind, cancellationToken);
        }

        private static void VerifyArguments(
            IEnumerable<DeclarationInfo> declarationsInNode,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            SemanticModel semanticModel,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (declarationsInNode == null)
            {
                throw new ArgumentNullException(nameof(declarationsInNode));
            }

            VerifyArguments(getKind, semanticModel, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        private static void VerifyArguments(
            IEnumerable<SyntaxNode> nodes,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            SemanticModel semanticModel,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (nodes.Any(n => n == null))
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentElementCannotBeNull, nameof(nodes));
            }

            VerifyArguments(getKind, semanticModel, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        private static void VerifyArguments(
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            SemanticModel semanticModel,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (getKind == null)
            {
                throw new ArgumentNullException(nameof(getKind));
            }

            VerifyArguments(semanticModel, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }
    }

    internal static class AnalyzerDriverResources
    {
        internal static string AnalyzerFailure => CodeAnalysisResources.CompilerAnalyzerFailure;
        internal static string AnalyzerThrows => CodeAnalysisResources.CompilerAnalyzerThrows;
        internal static string ArgumentElementCannotBeNull => CodeAnalysisResources.ArgumentElementCannotBeNull;
        internal static string ArgumentCannotBeEmpty => CodeAnalysisResources.ArgumentCannotBeEmpty;
    }
}
