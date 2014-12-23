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
        private readonly CancellationTokenRegistration queueRegistration;
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
        /// Returns all diagnostics computed by the analyzers for the compilation.
        /// </summary>
        /// <param name="compilation">The compilation to run analysis against</param>
        /// <param name="analyzers">The set of analyzers to include in the analysis</param>
        /// <param name="cancellationToken">A cancellation token that can be used to abort analysis.</param>
        public static Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(
            Compilation compilation, 
            ImmutableArray<DiagnosticAnalyzer> analyzers, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetAnalyzerDiagnosticsAsync(compilation, analyzers, null, cancellationToken);
        }

        /// <summary>
        /// Returns all diagnostics computed by the analyzers for the compilation.
        /// </summary>
        /// <param name="compilation">The compilation to run analysis against</param>
        /// <param name="analyzers">The set of analyzers to include in the analysis</param>
        /// <param name="options">Options that are passed to analyzers</param>
        /// <param name="cancellationToken">A cancellation token that can be used to abort analysis.</param>
        public static Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(
            Compilation compilation, 
            ImmutableArray<DiagnosticAnalyzer> analyzers, 
            AnalyzerOptions options, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            options = options ?? AnalyzerOptions.Empty;
            Compilation newCompilation = null;
            var analyzerDriver = AnalyzerDriver.Create(compilation, analyzers, options, out newCompilation, cancellationToken);

            // We need to generate compiler events in order for the event queue to be populated and the analyzer driver to return diagnostics.
            // So we'll call GetDiagnostics which will generate all events except for those on emit.
            newCompilation.GetDiagnostics(cancellationToken);

            return analyzerDriver.GetDiagnosticsAsync();
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
            this.CompilationEventQueue = new AsyncQueue<CompilationEvent>();
            this.DiagnosticQueue = new AsyncQueue<Diagnostic>();
            this.addDiagnostic = GetDiagnosticSinkWithSuppression();
            this.analyzerOptions = options;
            this.queueRegistration = cancellationToken.Register(() =>
            {
                this.CompilationEventQueue.TryComplete();
                this.DiagnosticQueue.TryComplete();
            });

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
                        AnalyzerDriverHelper.ExecuteAndCatchIfThrows(a.Analyzer, addDiagnostic, continueOnAnalyzerException, () =>
                        {
                            var context = new SyntaxTreeAnalysisContext(tree, analyzerOptions, addDiagnostic, cancellationToken);
                            a.Action(context);
                        }, cancellationToken);
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
            }

            return analyzersByKind.Select(a => a.ToImmutableAndFree()).ToImmutableArray();
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
                        AnalyzerDriverHelper.ExecuteAndCatchIfThrows(da.Analyzer, addDiagnostic, continueOnAnalyzerException, () =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var symbolContext = new SymbolAnalysisContext(symbol, compilation, this.analyzerOptions, addDiagnosticForSymbol, cancellationToken);
                            da.Action(symbolContext);
                        }, cancellationToken);
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
                        AnalyzerDriverHelper.ExecuteAndCatchIfThrows(da.Analyzer, addDiagnostic, continueOnAnalyzerException, () =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var semanticModelContext = new SemanticModelAnalysisContext(semanticModel, this.analyzerOptions, addDiagnostic, cancellationToken);
                            da.Action(semanticModelContext);
                        }, cancellationToken);
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
                    AnalyzerDriverHelper.ExecuteAndCatchIfThrows(da.Analyzer, addDiagnostic, continueOnAnalyzerException, () =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var compilationContext = new CompilationEndAnalysisContext(compilation, this.analyzerOptions, addDiagnostic, cancellationToken);
                        da.Action(compilationContext);
                    }, cancellationToken);
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
                    AnalyzerDriverHelper.ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnAnalyzerException, () =>
                    {
                        // The Initialize method should be run asynchronously in case it is not well behaved, e.g. does not terminate.
                        analyzer.Initialize(new AnalyzerAnalysisContext(analyzer, sessionScope));
                    }, cancellationToken);
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

        private static HostCompilationStartAnalysisScope GetCompilationAnalysisScope(HostSessionStartAnalysisScope session, Compilation compilation, AnalyzerOptions analyzerOptions, Action<Diagnostic> addDiagnostic, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException, CancellationToken cancellationToken)
        {
            HostCompilationStartAnalysisScope compilationScope = new HostCompilationStartAnalysisScope(session);

            foreach (CompilationStartAnalyzerAction startAction in session.CompilationStartActions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AnalyzerDriverHelper.ExecuteAndCatchIfThrows(startAction.Analyzer, addDiagnostic, continueOnAnalyzerException, () =>
                {
                    startAction.Action(new AnalyzerCompilationStartAnalysisContext(startAction.Analyzer, compilationScope, compilation, analyzerOptions, cancellationToken));
                }, cancellationToken);
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
            if (analyzer is CompilerDiagnosticAnalyzer)
            {
                // Compiler analyzer must always be executed for compiler errors, which cannot be suppressed or filtered.
                return false;
            }

            var supportedDiagnostics = ImmutableArray<DiagnosticDescriptor>.Empty;

            // Catch Exception from analyzer.SupportedDiagnostics
            AnalyzerDriverHelper.ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnAnalyzerException, () => { supportedDiagnostics = analyzer.SupportedDiagnostics; }, cancellationToken);

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

        public void Dispose()
        {
            this.CompilationEventQueue.TryComplete();
            this.DiagnosticQueue.TryComplete();
            this.queueRegistration.Dispose();
        }
    }

    /// <summary>
    /// Driver to execute diagnostic analyzers for a given compilation.
    /// It uses a <see cref="AsyncQueue{TElement}"/> of <see cref="CompilationEvent"/>s to drive its analysis.
    /// </summary>
    internal class AnalyzerDriver<TLanguageKindEnum> : AnalyzerDriver where TLanguageKindEnum : struct
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
                        AnalyzerDriverHelper.GetNodeActionsByKind(nodeActions, pooledAnalyzerActionsByKind, addDiagnostic);
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

        protected override async Task AnalyzeDeclaringReferenceAsync(SymbolDeclaredCompilationEvent symbolEvent, SyntaxReference decl, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            SemanticModel semanticModel = symbolEvent.SemanticModel(decl);
            var declaringReferenceSyntax = await decl.GetSyntaxAsync().ConfigureAwait(false);
            var syntax = semanticModel.GetTopmostNodeForDiagnosticAnalysis(symbol, declaringReferenceSyntax);

            var statelessNodeActionsByKind = this.NodeActionsByKind;
            var executeSyntaxNodeActions = statelessNodeActionsByKind.Any();
            var executeCodeBlockActions = AnalyzerDriverHelper.CanHaveExecutableCodeBlock(symbol) && (this.compilationAnalysisScope.HasCodeBlockStartActions<TLanguageKindEnum>() || this.compilationAnalysisScope.HasCodeBlockEndActions<TLanguageKindEnum>());

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

            AnalyzerDriverHelper.ExecuteSyntaxNodeActions(nodesToAnalyze, actionsByKind, semanticModel,
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
            AnalyzerDriverHelper.ExecuteCodeBlockActions<TLanguageKindEnum>(
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

        public new void Dispose()
        {
            base.Dispose();
            foreach (var kvp in this.NodeActionsByKind)
            {
                kvp.Value.Free();
            }
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
