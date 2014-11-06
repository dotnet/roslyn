// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Driver to execute diagnostic analyzers for a given compilation.
    /// It uses a <see cref="AsyncQueue{TElement}"/> of <see cref="CompilationEvent"/>s to drive its analysis.
    /// </summary>
    public abstract class AnalyzerDriver : IDisposable
    {
        private static readonly ConditionalWeakTable<Compilation, SuppressMessageAttributeState> suppressMessageStateByCompilation = new ConditionalWeakTable<Compilation, SuppressMessageAttributeState>();

        private const string DiagnosticId = "AnalyzerDriver";
        private readonly Action<Diagnostic> addDiagnostic;
        private Compilation compilation;
        internal readonly Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException;
        private ImmutableArray<Task> workers;
        private ImmutableArray<Task> syntaxAnalyzers;

        internal HostCompilationStartAnalysisScope compilationAnalysisScope;

        // TODO: should these be made lazy?
        private ImmutableArray<ImmutableArray<SymbolAnalyzerAction>> declarationAnalyzerActionsByKind;
        private static readonly Task EmptyTask = Task.FromResult(false);

        private readonly Task initialWorker;
        protected AnalyzerOptions analyzerOptions;

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
        /// Create an <see cref="AnalyzerDriver"/> and attach it to the given compilation. 
        /// </summary>
        /// <param name="compilation">The compilation to which the new driver should be attached.</param>
        /// <param name="analyzers">The set of analyzers to include in the analysis.</param>
        /// <param name="options">Options that are passed to analyzers.</param>
        /// <param name="newCompilation">The new compilation with the analyzer driver attached.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to abort analysis.</param>
        /// <returns>A newly created analyzer driver</returns>
        /// <remarks>
        /// Note that since a compilation is immutable, the act of creating a driver and attaching it produces
        /// a new compilation. Any further actions on the compilation should use the new compilation.
        /// </remarks>
        public static AnalyzerDriver Create(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, out Compilation newCompilation, CancellationToken cancellationToken)
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

            options = options ?? AnalyzerOptions.Empty;
            AnalyzerDriver analyzerDriver = compilation.AnalyzerForLanguage(analyzers, options, cancellationToken);
            newCompilation = compilation.WithEventQueue(analyzerDriver.CompilationEventQueue);
            return analyzerDriver;
        }

        /// <summary>
        /// Create an analyzer driver.
        /// </summary>
        /// <param name="analyzers">The set of analyzers to include in the analysis</param>
        /// <param name="options">Options that are passed to analyzers</param>
        /// <param name="cancellationToken">a cancellation token that can be used to abort analysis</param>
        /// <param name="continueOnAnalyzerException">Delegate which is invoked when an analyzer throws an exception.
        /// If a non-null delegate is provided and it returns true, then the exception is handled and converted into a diagnostic and driver continues with other analyzers.
        /// Otherwise if it returns false, then the exception is not handled by the driver.
        /// If null, then the driver always handles the exception.
        /// </param>
        protected AnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, CancellationToken cancellationToken, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException = null)
        {
            this.CompilationEventQueue = new AsyncQueue<CompilationEvent>(cancellationToken);
            this.DiagnosticQueue = new AsyncQueue<Diagnostic>(cancellationToken);
            this.addDiagnostic = GetDiagnosticSinkWithSuppression();
            this.analyzerOptions = options;

            Func<Exception, DiagnosticAnalyzer, bool> defaultExceptionHandler = (exception, analyzer) => true;
            this.continueOnAnalyzerException = continueOnAnalyzerException ?? defaultExceptionHandler;

            // start the first task to drain the event queue. The first compilation event is to be handled before
            // any other ones, so we cannot have more than one event processing task until the first event has been handled.
            initialWorker = Task.Run(async () =>
            {
                try
                {
                    await InitialWorkerAsync(analyzers, this.continueOnAnalyzerException, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // If creation is cancelled we had better not use the driver any longer
                    this.Dispose();
                }
            });
        }

        /// <summary>
        /// Returns all diagnostics computed by the analyzers since the last time this was invoked.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync()
        {
            var allDiagnostics = DiagnosticBag.GetInstance();
            if (CompilationEventQueue.IsCompleted)
            {
                await DiagnosticQueue.WhenCompletedAsync.ConfigureAwait(false);
            }

            Diagnostic d;
            while (DiagnosticQueue.TryDequeue(out d))
            {
                allDiagnostics.Add(d);
            }

            if (compilation != null)
            {
                var filteredDiagnostics = DiagnosticBag.GetInstance();
                compilation.FilterAndAppendAndFreeDiagnostics(filteredDiagnostics, ref allDiagnostics);
                return filteredDiagnostics.ToReadOnlyAndFree();
            }
            else
            {
                return allDiagnostics.ToReadOnlyAndFree();
            }
        }

        /// <summary>
        /// Return a task that completes when the driver is done producing diagnostics.
        /// </summary>
        public async Task WhenCompletedAsync()
        {
            await Task.WhenAll(SpecializedCollections.SingletonEnumerable(CompilationEventQueue.WhenCompletedAsync)
                .Concat(workers))
                .ConfigureAwait(false);
        }

        private async Task InitialWorkerAsync(ImmutableArray<DiagnosticAnalyzer> initialAnalyzers, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException, CancellationToken cancellationToken)
        {
            // Pull out the first event, which should be the "start compilation" event.
            var firstEvent = await CompilationEventQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);
            var startCompilation = firstEvent as CompilationStartedEvent;
            if (startCompilation == null)
            {
                // The queue contents are ill formed, as they do not start with a CompilationStarted event.
                // Throwing an exception here won't do much good, as there is nothing higher on the call stack.
                // So we instead complete the queue so that the caller does not enqueue further data.
                CompilationEventQueue.Complete();
                DiagnosticQueue.Complete();
                CompilationEvent drainedEvent;
                while (CompilationEventQueue.TryDequeue(out drainedEvent)) { }
                Debug.Assert(false, "First event must be CompilationStartedEvent, not " + firstEvent.GetType().Name);
            }

            var compilation = startCompilation.Compilation;
            Interlocked.CompareExchange(ref this.compilation, compilation, null);

            // Compute the set of effective actions based on suppression, and running the initial analyzers
            var sessionAnalysisScope = GetSessionAnalysisScope(initialAnalyzers, compilation.Options, addDiagnostic, continueOnAnalyzerException, cancellationToken);
            Interlocked.CompareExchange(ref this.compilationAnalysisScope, GetCompilationAnalysisScope(sessionAnalysisScope, compilation, analyzerOptions, addDiagnostic, continueOnAnalyzerException, cancellationToken), null);
            ImmutableInterlocked.InterlockedInitialize(ref this.declarationAnalyzerActionsByKind, MakeDeclarationAnalyzersByKind());

            // Invoke the syntax tree analyzers
            // TODO: How can the caller restrict this to one or a set of trees, or a span in a tree, rather than all trees in the compilation?
            var syntaxAnalyzers = ArrayBuilder<Task>.GetInstance();
            foreach (var tree in compilation.SyntaxTrees)
            {
                foreach (var a in this.compilationAnalysisScope.SyntaxTreeActions)
                {
                    var runningAsynchronously = Task.Run(() =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        // Catch Exception from executing the action
                        ExecuteAndCatchIfThrows(a.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                        {
                            var context = new SyntaxTreeAnalysisContext(tree, analyzerOptions, addDiagnostic, cancellationToken);
                            a.Action(context);
                        });
                    });

                    syntaxAnalyzers.Add(runningAsynchronously);
                }
            }

            ImmutableInterlocked.InterlockedInitialize(ref this.syntaxAnalyzers, syntaxAnalyzers.ToImmutableAndFree());

            // start some tasks to drain the event queue
            cancellationToken.ThrowIfCancellationRequested();
            const int nTasks = 1;
            var workers = ArrayBuilder<Task>.GetInstance();
            for (int i = 0; i < nTasks; i++)
            {
                workers.Add(Task.Run(async () => await ProcessCompilationEventsAsync(cancellationToken).ConfigureAwait(false), cancellationToken));
            }

            ImmutableInterlocked.InterlockedInitialize(ref this.workers, workers.ToImmutableAndFree());
        }

        private ImmutableArray<ImmutableArray<SymbolAnalyzerAction>> MakeDeclarationAnalyzersByKind()
        {
            var analyzersByKind = new List<ArrayBuilder<SymbolAnalyzerAction>>();
            foreach (var analyzer in this.compilationAnalysisScope.SymbolActions)
            {
                // Catch exceptions from analyzer.Kinds.
                ExecuteAndCatchIfThrows(analyzer.Analyzer, addDiagnostic, continueOnAnalyzerException, CancellationToken.None, () =>
                {
                    var kinds = analyzer.Kinds;
                    foreach (var k in kinds.Distinct())
                    {
                        if ((int)k > 100) continue; // protect against vicious analyzers
                        while ((int)k >= analyzersByKind.Count)
                        {
                            analyzersByKind.Add(ArrayBuilder<SymbolAnalyzerAction>.GetInstance());
                        }

                        analyzersByKind[(int)k].Add(analyzer);
                    }
                });
            }

            return analyzersByKind.Select(a => a.ToImmutableAndFree()).ToImmutableArray();
        }

        private async Task ProcessCompilationEventsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await ProcessEventsAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // when the queue is Completed the awaiting tasks get cancelled.
                // In that case we just return from this task
            }
        }

        private async Task ProcessEventsAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    var e = await CompilationEventQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);
                    await ProcessEventAsync(e, cancellationToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    // when the task is cancelled we stop processing events.
                    // the caller catches this.
                    throw;
                }
                catch (OperationCanceledException)
                {
                    // when just a single operation is cancelled, we continue processing events.
                    // TODO: what is the desired behavior in this case?
                }
                catch (Exception ex)
                {
                    var desc = new DiagnosticDescriptor(DiagnosticId,
                          CodeAnalysisResources.CompilerAnalyzerFailure,
                          "diagnostic analyzer worker threw an exception " + ex,
                          category: Diagnostic.CompilerDiagnosticCategory,
                          defaultSeverity: DiagnosticSeverity.Error,
                          isEnabledByDefault: true,
                          customTags: WellKnownDiagnosticTags.AnalyzerException);
                    addDiagnostic(Diagnostic.Create(desc, Location.None));
                }
            }
        }

        private async Task ProcessEventAsync(CompilationEvent e, CancellationToken cancellationToken)
        {
            var symbolEvent = e as SymbolDeclaredCompilationEvent;
            if (symbolEvent != null)
            {
                await ProcessSymbolDeclared(symbolEvent, cancellationToken).ConfigureAwait(false);
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

            throw new InvalidOperationException("Unexpected compilation event of type " + e.GetType().Name);
        }

        private Task ProcessSymbolDeclared(SymbolDeclaredCompilationEvent symbolEvent, CancellationToken cancellationToken)
        {
            try
            {
                return AnalyzeSymbol(symbolEvent, cancellationToken);
            }
            finally
            {
                symbolEvent.FlushCache();
            }
        }

        private Task AnalyzeSymbol(SymbolDeclaredCompilationEvent symbolEvent, CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            var isGlobalNamespace = symbol.Kind == SymbolKind.Namespace && ((INamespaceSymbol)symbol).IsGlobalNamespace;

            // Skip implicitly declared symbols.
            // For global namespace, we don't want to execute symbol actions, but do want to execute syntax actions for global syntax nodes.
            if (symbol.IsImplicitlyDeclared && !isGlobalNamespace)
            {
                return EmptyTask;
            }

            Action<Diagnostic> addDiagnosticForSymbol = GetDiagnosticSinkWithSuppression(symbol);
            var tasks = ArrayBuilder<Task>.GetInstance();

            // Invoke symbol analyzers only for source symbols.
            var declaringSyntaxRefs = symbol.DeclaringSyntaxReferences;
            if (!isGlobalNamespace && (int)symbol.Kind < declarationAnalyzerActionsByKind.Length && declaringSyntaxRefs.Any(s => s.SyntaxTree != null))
            {
                foreach (var da in declarationAnalyzerActionsByKind[(int)symbol.Kind])
                {
                    // TODO: is the overhead of creating tasks here too high compared to the cost of running them sequentially?
                    tasks.Add(Task.Run(() =>
                    {
                        // Catch Exception from analyzing the symbol
                        ExecuteAndCatchIfThrows(da.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var symbolContext = new SymbolAnalysisContext(symbol, compilation, this.analyzerOptions, addDiagnosticForSymbol, cancellationToken);
                            da.Action(symbolContext);
                        });
                    }));
                }
            }

            foreach (var decl in declaringSyntaxRefs)
            {
                tasks.Add(AnalyzeDeclaringReferenceAsync(symbolEvent, decl, addDiagnostic, cancellationToken));
            }

            return Task.WhenAll(tasks.ToArrayAndFree());
        }

        protected abstract Task AnalyzeDeclaringReferenceAsync(SymbolDeclaredCompilationEvent symbolEvent, SyntaxReference decl, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken);

        private Task ProcessCompilationUnitCompleted(CompilationUnitCompletedEvent completedEvent, CancellationToken cancellationToken)
        {
            // When the compiler is finished with a compilation unit, we can run user diagnostics which
            // might want to ask the compiler for all the diagnostics in the source file, for example
            // to get information about unnecessary usings.

            try
            {
                var tasks = ArrayBuilder<Task>.GetInstance();
                var semanticModel = completedEvent.SemanticModel;
                foreach (var da in this.compilationAnalysisScope.SemanticModelActions)
                {
                    // TODO: is the overhead of creating tasks here too high compared to the cost of running them sequentially?
                    tasks.Add(Task.Run(() =>
                    {
                        // Catch Exception from da.Action
                        ExecuteAndCatchIfThrows(da.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var semanticModelContext = new SemanticModelAnalysisContext(semanticModel, this.analyzerOptions, addDiagnostic, cancellationToken);
                            da.Action(semanticModelContext);
                        });
                    }));
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
            var tasks = ArrayBuilder<Task>.GetInstance();
            foreach (var da in this.compilationAnalysisScope.CompilationEndActions)
            {
                // TODO: is the overhead of creating tasks here too high compared to the cost of running them sequentially?
                tasks.Add(Task.Run(() =>
                {
                    // Catch Exception from da.Action
                    ExecuteAndCatchIfThrows(da.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var compilationContext = new CompilationEndAnalysisContext(compilation, this.analyzerOptions, addDiagnostic, cancellationToken);
                        da.Action(compilationContext);
                    });
                }));
            }

            await Task.WhenAll(tasks.Concat(this.syntaxAnalyzers)).ConfigureAwait(false);
            DiagnosticQueue.Complete();
        }

        internal protected Action<Diagnostic> GetDiagnosticSinkWithSuppression(ISymbol symbolOpt = null)
        {
            return diagnostic =>
            {
                var d = compilation.FilterDiagnostic(diagnostic);
                if (d != null)
                {
                    var suppressMessageState = suppressMessageStateByCompilation.GetValue(compilation, (c) => new SuppressMessageAttributeState(c));
                    if (!suppressMessageState.IsDiagnosticSuppressed(d, symbolOpt: symbolOpt))
                    {
                        DiagnosticQueue.Enqueue(d);
                    }
                }
            };
        }

        /// <summary>
        /// Given a set of compiler or <see cref="DiagnosticAnalyzer"/> generated <paramref name="diagnostics"/>, returns the effective diagnostics after applying the below filters:
        /// 1) <see cref="CompilationOptions.SpecificDiagnosticOptions"/> specified for the given <paramref name="compilation"/>.
        /// 2) <see cref="CompilationOptions.GeneralDiagnosticOption"/> specified for the given <paramref name="compilation"/>.
        /// 3) Diagnostic suppression through applied <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/>.
        /// 4) Pragma directives for the given <paramref name="compilation"/>.
        /// </summary>
        public static IEnumerable<Diagnostic> GetEffectiveDiagnostics(IEnumerable<Diagnostic> diagnostics, Compilation compilation)
        {
            if (diagnostics == null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            var suppressMessageState = suppressMessageStateByCompilation.GetValue(compilation, (c) => new SuppressMessageAttributeState(c));
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic != null)
                {
                    var effectiveDiagnostic = compilation.FilterDiagnostic(diagnostic);
                    if (effectiveDiagnostic != null && !suppressMessageState.IsDiagnosticSuppressed(effectiveDiagnostic))
                    {
                        yield return effectiveDiagnostic;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// <paramref name="continueOnAnalyzerException"/> says whether the caller would like the exception thrown by the analyzers to be handled or not. If true - Handles ; False - Not handled.
        /// </summary>
        public static bool IsDiagnosticAnalyzerSuppressed(DiagnosticAnalyzer analyzer, CompilationOptions options, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (analyzer == null)
            {
                throw new ArgumentNullException(nameof(analyzer));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (continueOnAnalyzerException == null)
            {
                throw new ArgumentNullException(nameof(continueOnAnalyzerException));
            }

            Action<Diagnostic> dummy = _ => { };
            return IsDiagnosticAnalyzerSuppressed(analyzer, options, dummy, continueOnAnalyzerException, CancellationToken.None);
        }

        private static HostSessionStartAnalysisScope GetSessionAnalysisScope(
            IEnumerable<DiagnosticAnalyzer> analyzers,
            CompilationOptions compilationOptions,
            Func<DiagnosticAnalyzer, CompilationOptions, Action<Diagnostic>, Func<Exception, DiagnosticAnalyzer, bool>, CancellationToken, bool> isAnalyzerSuppressed,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            HostSessionStartAnalysisScope sessionScope = new HostSessionStartAnalysisScope();

            foreach (DiagnosticAnalyzer analyzer in analyzers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!isAnalyzerSuppressed(analyzer, compilationOptions, addDiagnostic, continueOnAnalyzerException, cancellationToken))
                {
                    ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                    {
                        // The Initialize method should be run asynchronously in case it is not well behaved, e.g. does not terminate.
                        analyzer.Initialize(new AnalysisContext(new AnalyzerSessionStartAnalysisScope(analyzer, sessionScope)));
                    });
                }
            }

            return sessionScope;
        }

        private static HostSessionStartAnalysisScope GetSessionAnalysisScope(IEnumerable<DiagnosticAnalyzer> analyzers, CompilationOptions compilationOptions, Action<Diagnostic> addDiagnostic, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException, CancellationToken cancellationToken)
        {
            return GetSessionAnalysisScope(analyzers, compilationOptions, IsDiagnosticAnalyzerSuppressed, addDiagnostic, continueOnAnalyzerException, cancellationToken);
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

        internal protected static void VerifyArguments(
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

        internal protected static void VerifyArguments(
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

        /// <summary>
        /// Executes the <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> for the given analyzer and returns the set of registered sessions.
        /// </summary>
        /// <param name="analyzer">Analyzer to get session wide analyzer actions.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// Note that this API doesn't execute any <see cref="CompilationStartAnalyzerAction"/> registered by the Initialize invocation.
        /// Use <see cref="ExecuteCompilationStartActions(AnalyzerActions, DiagnosticAnalyzer, Compilation, AnalyzerOptions, Action{Diagnostic}, Func{Exception, DiagnosticAnalyzer, bool}, CancellationToken)"/> API
        /// to get execute these actions to get the per-compilation analyzer actions.
        /// </remarks>
        public static AnalyzerActions GetSessionAnalyzerActions(
            DiagnosticAnalyzer analyzer, 
            Action<Diagnostic> addDiagnostic, 
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException, 
            CancellationToken cancellationToken)
        {
            VerifyArguments(analyzer, addDiagnostic, continueOnAnalyzerException);
            
            HostSessionStartAnalysisScope sessionScope = new HostSessionStartAnalysisScope();

            ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
            {
                // The Initialize method should be run asynchronously in case it is not well behaved, e.g. does not terminate.
                analyzer.Initialize(new AnalysisContext(new AnalyzerSessionStartAnalysisScope(analyzer, sessionScope)));
            });

            return sessionScope.GetAnalyzerActions(analyzer);
        }

        /// <summary>
        /// Executes the compilation start actions and returns the per-compilation analyzer actions added by these actions.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose compilation start actions are to be executed.</param>
        /// <param name="analyzer">Analyzer on which compilation start actions have to be executed.</param>
        /// <param name="compilation">Compilation to be used in the analysis.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static AnalyzerActions ExecuteCompilationStartActions(
            AnalyzerActions actions,
            DiagnosticAnalyzer analyzer,
            Compilation compilation, 
            AnalyzerOptions analyzerOptions, 
            Action<Diagnostic> addDiagnostic, 
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException, 
            CancellationToken cancellationToken)
        {
            VerifyArguments(compilation, actions, analyzerOptions, analyzer, addDiagnostic, continueOnAnalyzerException);

            HostCompilationStartAnalysisScope compilationScope = new HostCompilationStartAnalysisScope(new HostSessionStartAnalysisScope());

            foreach (var startAction in actions.CompilationStartActions)
            {
                if (startAction.Analyzer == analyzer)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                    {
                        startAction.Action(new CompilationStartAnalysisContext(new AnalyzerCompilationStartAnalysisScope(analyzer, compilationScope), compilation, analyzerOptions, cancellationToken));
                    });
                }
            }

            return compilationScope.GetAnalyzerActions(analyzer);
        }

        /// <summary>
        /// Executes the compilation end actions.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose compilation end actions are to be executed.</param>
        /// <param name="compilation">Compilation to be used in the analysis.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static void ExecuteCompilationEndActions(
            AnalyzerActions actions,
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            VerifyArguments(compilation, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);

            HostCompilationStartAnalysisScope compilationScope = new HostCompilationStartAnalysisScope(new HostSessionStartAnalysisScope());

            foreach (var endAction in actions.CompilationEndActions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ExecuteAndCatchIfThrows(endAction.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                {
                    var context = new CompilationEndAnalysisContext(compilation, analyzerOptions, addDiagnostic, cancellationToken);
                    endAction.Action(context);
                });
            }
        }

        /// <summary>
        /// Executes the symbol actions on the given symbols.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose symbol actions are to be executed.</param>
        /// <param name="symbols">Symbols to be analyzed.</param>
        /// <param name="compilation">Compilation to be used in the analysis.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static void ExecuteSymbolActions(
            AnalyzerActions actions,
            IEnumerable<ISymbol> symbols,
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            VerifyArguments(symbols, compilation, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);

            foreach (var symbol in symbols)
            {
                foreach (var symbolAction in actions.SymbolActions)
                {
                    var action = symbolAction.Action;
                    var kinds = symbolAction.Kinds;
                    if (kinds.Contains(symbol.Kind))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var symbolContext = new SymbolAnalysisContext(symbol, compilation, analyzerOptions, addDiagnostic, cancellationToken);

                        // Catch Exception from action.
                        ExecuteAndCatchIfThrows(symbolAction.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () => action(symbolContext));
                    }
                }
            }
        }

        /// <summary>
        /// Executes the semantic model actions on the given semantic model.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose semantic model actions are to be executed.</param>
        /// <param name="semanticModel">Semantic model to analyze.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static void ExecuteSemanticModelActions(
            AnalyzerActions actions,
            SemanticModel semanticModel,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            VerifyArguments(semanticModel, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);

            foreach (var semanticModelAction in actions.SemanticModelActions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Catch Exception from action.
                ExecuteAndCatchIfThrows(semanticModelAction.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                {
                    var context = new SemanticModelAnalysisContext(semanticModel, analyzerOptions, addDiagnostic, cancellationToken);
                    semanticModelAction.Action(context);
                });
            }
        }

        /// <summary>
        /// Executes the syntax tree actions on the given tree.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose syntax tree actions are to be executed.</param>
        /// <param name="syntaxTree">Syntax tree to analyze.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static void ExecuteSyntaxTreeActions(
            AnalyzerActions actions,
            SyntaxTree syntaxTree,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            VerifyArguments(syntaxTree, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);

            foreach (var syntaxTreeAction in actions.SyntaxTreeActions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Catch Exception from action.
                ExecuteAndCatchIfThrows(syntaxTreeAction.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                {
                    var context = new SyntaxTreeAnalysisContext(syntaxTree, analyzerOptions, addDiagnostic, cancellationToken);
                    syntaxTreeAction.Action(context);
                });
            }
        }

        private static HostCompilationStartAnalysisScope GetCompilationAnalysisScope(HostSessionStartAnalysisScope session, Compilation compilation, AnalyzerOptions analyzerOptions, Action<Diagnostic> addDiagnostic, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException, CancellationToken cancellationToken)
        {
            HostCompilationStartAnalysisScope compilationScope = new HostCompilationStartAnalysisScope(session);

            foreach (CompilationStartAnalyzerAction startAction in session.CompilationStartActions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ExecuteAndCatchIfThrows(startAction.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                {
                    startAction.Action(new CompilationStartAnalysisContext(new AnalyzerCompilationStartAnalysisScope(startAction.Analyzer, compilationScope), compilation, analyzerOptions, cancellationToken));
                });
            }

            return compilationScope;
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// </summary>
        private static bool IsDiagnosticAnalyzerSuppressed(
            DiagnosticAnalyzer analyzer,
            CompilationOptions options,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            var supportedDiagnostics = ImmutableArray<DiagnosticDescriptor>.Empty;

            // Catch Exception from analyzer.SupportedDiagnostics
            ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () => { supportedDiagnostics = analyzer.SupportedDiagnostics; });

            var diagnosticOptions = options.SpecificDiagnosticOptions;

            foreach (var diag in supportedDiagnostics)
            {
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

        protected static void ExecuteAndCatchIfThrows(DiagnosticAnalyzer analyzer, Action<Diagnostic> addDiagnostic, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException, CancellationToken cancellationToken, Action analyze)
        {
            try
            {
                analyze();
            }
            catch (OperationCanceledException oce) if (continueOnAnalyzerException(oce, analyzer))
            {
                if (oce.CancellationToken != cancellationToken)
                {
                    // Create a info diagnostic saying that the analyzer failed
                    addDiagnostic(GetAnalyzerDiagnostic(analyzer, oce));
                }
            }
            catch (Exception e) if (continueOnAnalyzerException(e, analyzer))
            {
                // Create a info diagnostic saying that the analyzer failed
                addDiagnostic(GetAnalyzerDiagnostic(analyzer, e));
            }
        }

        protected static Diagnostic GetAnalyzerDiagnostic(DiagnosticAnalyzer analyzer, Exception e)
        {
            return Diagnostic.Create(GetDiagnosticDescriptor(analyzer.GetType().ToString(), e.Message), Location.None);
        }

        private static DiagnosticDescriptor GetDiagnosticDescriptor(string analyzerName, string message)
        {
            return new DiagnosticDescriptor(DiagnosticId,
                CodeAnalysisResources.CompilerAnalyzerFailure,
                string.Format(CodeAnalysisResources.CompilerAnalyzerThrows, analyzerName, message),
                category: Diagnostic.CompilerDiagnosticCategory,
                defaultSeverity: DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                customTags: WellKnownDiagnosticTags.AnalyzerException);
        }

        public void Dispose()
        {
            CompilationEventQueue.Complete();
            DiagnosticQueue.Complete();
        }
    }

    /// <summary>
    /// Driver to execute diagnostic analyzers for a given compilation.
    /// It uses a <see cref="AsyncQueue{TElement}"/> of <see cref="CompilationEvent"/>s to drive its analysis.
    /// </summary>
    public class AnalyzerDriver<TLanguageKindEnum> : AnalyzerDriver where TLanguageKindEnum : struct
    {
        private Func<SyntaxNode, TLanguageKindEnum> GetKind;
        private ImmutableDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> lazyNodeActionsByKind = null;

        /// <summary>
        /// Create an analyzer driver.
        /// </summary>
        /// <param name="analyzers">The set of analyzers to include in the analysis</param>
        /// <param name="getKind">A delegate that returns the language-specific kind for a given syntax node</param>
        /// <param name="options">Options that are passed to analyzers</param>
        /// <param name="cancellationToken">a cancellation token that can be used to abort analysis</param>
        /// <param name="continueOnAnalyzerException">Delegate which is invoked when an analyzer throws an exception.
        /// If a non-null delegate is provided and it returns true, then the exception is handled and converted into a diagnostic and driver continues with other analyzers.
        /// Otherwise if it returns false, then the exception is not handled by the driver.
        /// If null, then the driver always handles the exception.
        /// </param>
        internal AnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, Func<SyntaxNode, TLanguageKindEnum> getKind, AnalyzerOptions options, CancellationToken cancellationToken, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException = null) : base(analyzers, options, cancellationToken, continueOnAnalyzerException)
        {
            GetKind = getKind;
        }

        private ImmutableDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> NodeActionsByKind
        {
            get
            {
                if (lazyNodeActionsByKind == null)
                {
                    var nodeActions = this.compilationAnalysisScope.GetSyntaxNodeActions<TLanguageKindEnum>();
                    ImmutableDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> analyzersByKind;
                    if (nodeActions.Any())
                    {
                        var addDiagnostic = GetDiagnosticSinkWithSuppression();
                        var pooledAnalyzerActionsByKind = PooledDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.GetInstance();
                        GetNodeActionsByKind(nodeActions, pooledAnalyzerActionsByKind, addDiagnostic);
                        analyzersByKind = pooledAnalyzerActionsByKind.ToImmutableDictionary();
                        pooledAnalyzerActionsByKind.Free();
                    }
                    else
                    {
                        analyzersByKind = ImmutableDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.Empty;
                    }

                    lazyNodeActionsByKind = analyzersByKind;
                }

                return lazyNodeActionsByKind;
            }
        }

        private static bool CanHaveExecutableCodeBlock(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                case SymbolKind.Event:
                case SymbolKind.Property:
                case SymbolKind.NamedType:
                    return true;

                case SymbolKind.Field:
                    Debug.Assert(((IFieldSymbol)symbol).AssociatedSymbol == null);
                    return true;

                default:
                    return false;
            }
        }

        protected override async Task AnalyzeDeclaringReferenceAsync(SymbolDeclaredCompilationEvent symbolEvent, SyntaxReference decl, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            SemanticModel semanticModel = symbolEvent.SemanticModel(decl);
            var declaringReferenceSyntax = await decl.GetSyntaxAsync().ConfigureAwait(false);
            var syntax = semanticModel.GetTopmostNodeForDiagnosticAnalysis(symbol, declaringReferenceSyntax);

            var statelessNodeActionsByKind = this.NodeActionsByKind;
            var executeSyntaxNodeActions = statelessNodeActionsByKind.Any();
            var executeCodeBlockActions = CanHaveExecutableCodeBlock(symbol) && (this.compilationAnalysisScope.HasCodeBlockStartActions<TLanguageKindEnum>() || this.compilationAnalysisScope.HasCodeBlockEndActions<TLanguageKindEnum>());

            if (executeSyntaxNodeActions || executeCodeBlockActions)
            {
                // We only care about the top level symbol declaration and its immediate member declarations.
                int? levelsToCompute = 2;

                var declarationsInNode = semanticModel.GetDeclarationsInNode(syntax, getSymbol: syntax != declaringReferenceSyntax, cancellationToken: cancellationToken, levelsToCompute: levelsToCompute);

                // Execute stateless syntax node actions.
                if (executeSyntaxNodeActions)
                {
                    ExecuteStatelessNodeActions(statelessNodeActionsByKind, syntax, symbol, declarationsInNode, semanticModel,
                        reportDiagnostic, this.continueOnAnalyzerException, this.analyzerOptions, this.GetKind, cancellationToken);
                }

                // Execute code block actions.
                if (executeCodeBlockActions)
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
                        ExecuteCodeBlockActions(this.compilationAnalysisScope,
                            syntax, symbol, executableCodeBlocks, this.analyzerOptions,
                            semanticModel, reportDiagnostic, this.continueOnAnalyzerException, this.GetKind, cancellationToken);
                    }
                }
            }
        }

        private static void ExecuteStatelessNodeActions(
            IDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> actionsByKind,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            IEnumerable<DeclarationInfo> declarationsInNode,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            AnalyzerOptions analyzerOptions,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
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

                    descendantDeclsToSkip = descendantDeclsToSkip ?? new HashSet<SyntaxNode>();
                    descendantDeclsToSkip.Add(declInNode.DeclaredNode);
                }

                first = false;
            }

            var nodesToAnalyze = descendantDeclsToSkip == null ?
                declaredNode.DescendantNodesAndSelf(descendIntoTrivia: true) :
                declaredNode.DescendantNodesAndSelf(n => !descendantDeclsToSkip.Contains(n), descendIntoTrivia: true).Except(descendantDeclsToSkip);

            ExecuteSyntaxNodeActions(nodesToAnalyze, actionsByKind, semanticModel,
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

        /// <summary>
        /// Executes the syntax node actions on the given syntax nodes.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose code block start actions and end actions are to be executed.</param>
        /// <param name="nodes">Syntax nodes to be analyzed.</param>
        /// <param name="semanticModel">SemanticModel to be used in the analysis.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="getKind">Delegate to compute language specific syntax kind for a syntax node.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static void ExecuteSyntaxNodeActions(
            AnalyzerActions actions,
            IEnumerable<SyntaxNode> nodes,
            SemanticModel semanticModel,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            CancellationToken cancellationToken)
        {
            VerifyArguments(nodes, getKind, semanticModel, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);

            var syntaxNodeActions = actions.GetSyntaxNodeActions<TLanguageKindEnum>();

            foreach (var syntaxNodeAction in syntaxNodeActions)
            {
                var action = syntaxNodeAction.Action;
                var kinds = syntaxNodeAction.Kinds;

                foreach (var node in nodes)
                {
                    if (kinds.Contains(getKind(node)))
                    {
                        ExecuteSyntaxNodeAction(action, node, syntaxNodeAction.Analyzer, semanticModel, analyzerOptions, addDiagnostic, continueOnAnalyzerException, cancellationToken);
                    }
                }
            }
        }

        private static void ExecuteSyntaxNodeAction(
            Action<SyntaxNodeAnalysisContext> syntaxNodeAction,
            SyntaxNode node,
            DiagnosticAnalyzer analyzer,
            SemanticModel semanticModel,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            var syntaxNodeContext = new SyntaxNodeAnalysisContext(node, semanticModel, analyzerOptions, addDiagnostic, cancellationToken);
            
            // Catch Exception from action.
            ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () => syntaxNodeAction(syntaxNodeContext));
        }

        /// <summary>
        /// Executes the given code block actions on all the executable code blocks for each declaration info in <paramref name="declarationsInNode"/>.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose code block start actions and end actions are to be executed.</param>
        /// <param name="declarationsInNode">Declarations to be analyzed.</param>
        /// <param name="semanticModel">SemanticModel to be shared amongst all actions.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from any action should be handled or not.</param>
        /// <param name="getKind">Delegate to compute language specific syntax kind for a syntax node.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static void ExecuteCodeBlockActions(
            AnalyzerActions actions,
            IEnumerable<DeclarationInfo> declarationsInNode,
            SemanticModel semanticModel,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            CancellationToken cancellationToken)
        {
            VerifyArguments(declarationsInNode, getKind, semanticModel, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);

            var codeBlockStartActions = actions.GetCodeBlockStartActions<TLanguageKindEnum>();
            var codeBlockEndActions = actions.GetCodeBlockEndActions<TLanguageKindEnum>();

            if (!codeBlockStartActions.Any() && !codeBlockEndActions.Any())
            {
                return;
            }

            foreach (var declInfo in declarationsInNode)
            {
                var declaredNode = declInfo.DeclaredNode;
                var declaredSymbol = declInfo.DeclaredSymbol;
                var executableCodeBlocks = declInfo.ExecutableCodeBlocks;

                if (declaredSymbol != null && declInfo.ExecutableCodeBlocks.Any())
                {
                    ExecuteCodeBlockActions(codeBlockStartActions, codeBlockEndActions, declaredNode, declaredSymbol,
                        executableCodeBlocks, analyzerOptions, semanticModel, addDiagnostic, continueOnAnalyzerException, getKind, cancellationToken);
                }
            }
        }

        private static void ExecuteCodeBlockActions(
            IEnumerable<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> codeBlockStartActions,
            IEnumerable<CodeBlockEndAnalyzerAction<TLanguageKindEnum>> codeBlockEndActions,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            ImmutableArray<SyntaxNode> executableCodeBlocks,
            AnalyzerOptions analyzerOptions,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            CancellationToken cancellationToken)
        {
            Debug.Assert(declaredNode != null);
            Debug.Assert(declaredSymbol != null);
            Debug.Assert(CanHaveExecutableCodeBlock(declaredSymbol));
            Debug.Assert(codeBlockStartActions.Any() || codeBlockEndActions.Any());
            Debug.Assert(executableCodeBlocks.Any());

            // Compute the sets of code block end and stateful syntax node actions.
            var endedActions = PooledHashSet<CodeBlockEndAnalyzerAction<TLanguageKindEnum>>.GetInstance();
            var executableNodeActions = ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>.GetInstance();

            // Include the stateless code block actions.
            endedActions.AddAll(codeBlockEndActions);

            // Include the stateful actions.
            foreach (var da in codeBlockStartActions)
            {
                // Catch Exception from the start action.
                ExecuteAndCatchIfThrows(da.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                {
                    HostCodeBlockStartAnalysisScope<TLanguageKindEnum> codeBlockScope = new HostCodeBlockStartAnalysisScope<TLanguageKindEnum>();
                    AnalyzerCodeBlockStartAnalysisScope<TLanguageKindEnum> analyzerBlockScope = new AnalyzerCodeBlockStartAnalysisScope<TLanguageKindEnum>(da.Analyzer, codeBlockScope);
                    CodeBlockStartAnalysisContext<TLanguageKindEnum> blockStartContext = new CodeBlockStartAnalysisContext<TLanguageKindEnum>(analyzerBlockScope, declaredNode, declaredSymbol, semanticModel, analyzerOptions, cancellationToken);
                    da.Action(blockStartContext);
                    endedActions.AddAll(codeBlockScope.CodeBlockEndActions);
                    executableNodeActions.AddRange(codeBlockScope.SyntaxNodeActions);
                });
            }

            // Execute stateful executable node analyzers, if any.
            if (executableNodeActions.Any())
            {
                var executableNodeActionsByKind = PooledDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.GetInstance();
                GetNodeActionsByKind(executableNodeActions, executableNodeActionsByKind, addDiagnostic);

                var nodesToAnalyze = executableCodeBlocks.SelectMany(cb => cb.DescendantNodesAndSelf());
                ExecuteSyntaxNodeActions(nodesToAnalyze, executableNodeActionsByKind, semanticModel,
                    analyzerOptions, addDiagnostic, continueOnAnalyzerException, getKind, cancellationToken);

                foreach (var b in executableNodeActionsByKind.Values)
                {
                    b.Free();
                }

                executableNodeActionsByKind.Free();
            }

            // Execute code block end actions.
            foreach (var a in endedActions)
            {
                // Catch Exception from a.OnCodeBlockEnded
                ExecuteAndCatchIfThrows(a.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () => a.Action(new CodeBlockEndAnalysisContext(declaredNode, declaredSymbol, semanticModel, analyzerOptions, addDiagnostic, cancellationToken)));
            }

            endedActions.Free();
            executableNodeActions.Free();
        }

        private static void ExecuteCodeBlockActions(
            HostCompilationStartAnalysisScope compilationScope,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            ImmutableArray<SyntaxNode> executableCodeBlocks,
            AnalyzerOptions analyzerOptions,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            CancellationToken cancellationToken)
        {
            ExecuteCodeBlockActions(
                compilationScope.GetCodeBlockStartActions<TLanguageKindEnum>(),
                compilationScope.GetCodeBlockEndActions<TLanguageKindEnum>(),
                declaredNode,
                declaredSymbol,
                executableCodeBlocks,
                analyzerOptions,
                semanticModel,
                addDiagnostic,
                continueOnAnalyzerException,
                getKind,
                cancellationToken);
        }

        private static void GetNodeActionsByKind(
            IEnumerable<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> nodeActions,
            PooledDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            Action<Diagnostic> addDiagnostic)
        {
            Debug.Assert(nodeActions != null && nodeActions.Any());
            Debug.Assert(nodeActionsByKind != null && !nodeActionsByKind.Any());

            foreach (var nodeAction in nodeActions)
            {
                // Catch Exception from  nodeAnalyzer.Kinds
                try
                {
                    foreach (var kind in nodeAction.Kinds)
                    {
                        ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> actionsForKind;
                        if (!nodeActionsByKind.TryGetValue(kind, out actionsForKind))
                        {
                            nodeActionsByKind.Add(kind, actionsForKind = ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>.GetInstance());
                        }

                        actionsForKind.Add(nodeAction);
                    }
                }
                catch (Exception e)
                {
                    // Create a diagnostic saying that the analyzer failed.
                    addDiagnostic(GetAnalyzerDiagnostic(nodeAction.Analyzer, e));
                }
            }
        }

        private static void ExecuteSyntaxNodeActions(
            IEnumerable<SyntaxNode> nodesToAnalyze,
            IDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            SemanticModel model,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnException,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            CancellationToken cancellationToken)
        {
            Debug.Assert(nodeActionsByKind != null);
            Debug.Assert(nodeActionsByKind.Any());

            foreach (var child in nodesToAnalyze)
            {
                ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> actionsForKind;
                if (nodeActionsByKind.TryGetValue(getKind(child), out actionsForKind))
                {
                    foreach (var action in actionsForKind)
                    {
                        ExecuteSyntaxNodeAction(action.Action, child, action.Analyzer, model, analyzerOptions, addDiagnostic, continueOnException, cancellationToken);
                    }
                }
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            foreach (var kvp in this.NodeActionsByKind)
            {
                kvp.Value.Free();
            }
        }
    }
}
