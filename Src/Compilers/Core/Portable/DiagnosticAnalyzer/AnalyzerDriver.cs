// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics.Internal;
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

        protected HostCompilationStartAnalysisScope compilationAnalysisScope;
        // TODO: should these be made lazy?
        private ImmutableArray<ImmutableArray<SymbolAnalyzerAction>> declarationAnalyzerActionsByKind;

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
                        var syntaxTreeContext = new SyntaxTreeAnalysisContext(tree, analyzerOptions, addDiagnostic, cancellationToken);
                        // Catch Exception from executing the action
                        ExecuteAndCatchIfThrows(a.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () => { a.Action(syntaxTreeContext); });
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
                workers.Add(Task.Run(() => ProcessCompilationEventsAsync(cancellationToken)));
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
            Action<Diagnostic> addDiagnosticForSymbol = GetDiagnosticSinkWithSuppression(symbol);
            var tasks = ArrayBuilder<Task>.GetInstance();

            // Invoke symbol analyzers only for source symbols.
            var declaringSyntaxRefs = symbol.DeclaringSyntaxReferences;
            if ((int)symbol.Kind < declarationAnalyzerActionsByKind.Length && declaringSyntaxRefs.Any(s => s.SyntaxTree != null))
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
                throw new ArgumentNullException("diagnostics");
            }

            if (compilation == null)
            {
                throw new ArgumentNullException("compilation");
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
                throw new ArgumentNullException("analyzer");
            }

            if (options == null)
            {
                throw new ArgumentNullException("options");
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

        // ToDo: This method is public only to make it available to the IDE analyzer driver. Figure out how to make it internal.
        /// <summary>
        /// Create the initial analysis scope from a set of available analyzers.
        /// </summary>
        /// <param name="analyzers"></param>
        /// <returns></returns>
        public static HostSessionStartAnalysisScope GetSessionAnalysisScope(IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            return GetSessionAnalysisScope(analyzers, null, (analyzer, options, add, continueOn, cancellation) => false, (d) => { }, (exception, analyzer) => true, CancellationToken.None);
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
        private ImmutableDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> lazyNodeAnalyzersByKind = null;

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

        private ImmutableDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> NodeAnalyzersByKind
        {
            get
            {
                if (lazyNodeAnalyzersByKind == null)
                {
                    var nodeAnalyzers = this.compilationAnalysisScope.GetSyntaxNodeActions<TLanguageKindEnum>();
                    ImmutableDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> analyzersByKind;
                    if (nodeAnalyzers.Any())
                    {
                        var addDiagnostic = GetDiagnosticSinkWithSuppression();
                        var pooledAnalyzersByKind = PooledDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.GetInstance();
                        GetNodeAnalyzersByKind(nodeAnalyzers, pooledAnalyzersByKind, addDiagnostic);
                        analyzersByKind = pooledAnalyzersByKind.ToImmutableDictionary();
                        pooledAnalyzersByKind.Free();
                    }
                    else
                    {
                        analyzersByKind = ImmutableDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.Empty;
                    }

                    lazyNodeAnalyzersByKind = analyzersByKind;
                }

                return lazyNodeAnalyzersByKind;
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
                    // Check if this is not a compiler generated backing field.
                    return ((IFieldSymbol)symbol).AssociatedSymbol == null;

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

            var statelessNodeAnalyzersByKind = this.NodeAnalyzersByKind;
            var executeSyntaxNodeAnalyzers = statelessNodeAnalyzersByKind.Any();
            var executeCodeBlockAnalyzers = CanHaveExecutableCodeBlock(symbol) && (this.compilationAnalysisScope.HasCodeBlockStartActions<TLanguageKindEnum>() || this.compilationAnalysisScope.HasCodeBlockEndActions<TLanguageKindEnum>());

            if (executeSyntaxNodeAnalyzers || executeCodeBlockAnalyzers)
            {
                // We only care about the top level symbol declaration and its immediate member declarations.
                int? levelsToCompute = 2;

                var declarationsInNode = semanticModel.GetDeclarationsInNode(syntax, getSymbol: syntax != declaringReferenceSyntax, cancellationToken: cancellationToken, levelsToCompute: levelsToCompute);

                // Execute stateless syntax node analyzers.
                if (executeSyntaxNodeAnalyzers)
                {
                    ExecuteStatelessNodeAnalyzers(statelessNodeAnalyzersByKind, syntax, symbol, declarationsInNode, semanticModel,
                        reportDiagnostic, this.continueOnAnalyzerException, this.analyzerOptions, this.GetKind, cancellationToken);
                }

                // Execute code block analyzers.
                if (executeCodeBlockAnalyzers)
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
                        ExecuteCodeBlockAnalyzers(this.compilationAnalysisScope,
                            syntax, symbol, executableCodeBlocks, this.analyzerOptions,
                            semanticModel, reportDiagnostic, this.continueOnAnalyzerException, this.GetKind, cancellationToken);
                    }
                }
            }
        }

        private static void ExecuteStatelessNodeAnalyzers(
            IDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> analyzersByKind,
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

            ExecuteSyntaxNodeActions(nodesToAnalyze, analyzersByKind, semanticModel,
                analyzerOptions, addDiagnostic, continueOnAnalyzerException, getKind, cancellationToken);
        }

        /// <summary>
        /// Executes the given syntax node action on the given syntax node.
        /// </summary>
        /// <param name="syntaxNodeAction">Action to execute.</param>
        /// <param name="node">Syntax node to be analyzed.</param>
        /// <param name="semanticModel">SemanticModel to be used in the analysis.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static void ExecuteSyntaxNodeAction(
            SyntaxNodeAnalyzerAction<TLanguageKindEnum> syntaxNodeAction,
            SyntaxNode node,
            SemanticModel semanticModel,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            var syntaxNodeContext = new SyntaxNodeAnalysisContext(node, semanticModel, analyzerOptions, addDiagnostic, cancellationToken);
            // Catch Exception from action.
            ExecuteAndCatchIfThrows(syntaxNodeAction.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () => syntaxNodeAction.Action(syntaxNodeContext));
        }

        /// <summary>
        /// Executes the given code block actions on all the executable code blocks for each declaration info in <paramref name="declarationsInNode"/>.
        /// </summary>
        /// <param name="codeBlockStartedAnalyzers">Code block analyzer factories.</param>
        /// <param name="codeBlockEndedAnalyzers">Stateless code block analyzers.</param>
        /// <param name="declarationsInNode">Declarations to be analyzed.</param>
        /// <param name="semanticModel">SemanticModel to be shared amongst all actions.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from any action should be handled or not.</param>
        /// <param name="getKind">Delegate to compute language specific syntax kind for a syntax node.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="getAnalyzerKindsOfInterest">Optional delegate to return cached syntax kinds.
        /// If null, then this property is explicitly invoked by the driver to compute syntax kinds of interest.</param>
        public static void ExecuteCodeBlockActions(
            IEnumerable<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> codeBlockStartedAnalyzers,
            IEnumerable<CodeBlockEndAnalyzerAction<TLanguageKindEnum>> codeBlockEndedAnalyzers,
            IEnumerable<DeclarationInfo> declarationsInNode,
            SemanticModel semanticModel,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            CancellationToken cancellationToken,
            Func<SyntaxNodeAnalyzerAction<TLanguageKindEnum>, IEnumerable<TLanguageKindEnum>> getAnalyzerKindsOfInterest = null)
        {
            if (!codeBlockStartedAnalyzers.Any() && !codeBlockEndedAnalyzers.Any())
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
                    ExecuteCodeBlockAnalyzers(codeBlockStartedAnalyzers, codeBlockEndedAnalyzers, declaredNode, declaredSymbol,
                        executableCodeBlocks, analyzerOptions, semanticModel, addDiagnostic, continueOnAnalyzerException, getKind, cancellationToken, getAnalyzerKindsOfInterest);
                }
            }
        }

        private static void ExecuteCodeBlockAnalyzers(
            IEnumerable<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> codeBlockStartedAnalyzers,
            IEnumerable<CodeBlockEndAnalyzerAction<TLanguageKindEnum>> codeBlockEndedAnalyzers,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            ImmutableArray<SyntaxNode> executableCodeBlocks,
            AnalyzerOptions analyzerOptions,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            CancellationToken cancellationToken,
            Func<SyntaxNodeAnalyzerAction<TLanguageKindEnum>, IEnumerable<TLanguageKindEnum>> getAnalyzerKindsOfInterest = null)
        {
            Debug.Assert(declaredNode != null);
            Debug.Assert(declaredSymbol != null);
            Debug.Assert(CanHaveExecutableCodeBlock(declaredSymbol));
            Debug.Assert(codeBlockStartedAnalyzers.Any() || codeBlockEndedAnalyzers.Any());
            Debug.Assert(executableCodeBlocks.Any());

            // Compute the sets of code block end and stateful syntax node actions.
            var endedAnalyzers = PooledHashSet<CodeBlockEndAnalyzerAction<TLanguageKindEnum>>.GetInstance();
            var executableNodeAnalyzers = ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>.GetInstance();

            // Include the stateless code block actions.
            endedAnalyzers.AddAll(codeBlockEndedAnalyzers);

            // Include the stateful actions.
            foreach (var da in codeBlockStartedAnalyzers)
            {
                // Catch Exception from the start action.
                ExecuteAndCatchIfThrows(da.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                {
                    HostCodeBlockStartAnalysisScope<TLanguageKindEnum> codeBlockScope = new HostCodeBlockStartAnalysisScope<TLanguageKindEnum>();
                    AnalyzerCodeBlockStartAnalysisScope<TLanguageKindEnum> analyzerBlockScope = new AnalyzerCodeBlockStartAnalysisScope<TLanguageKindEnum>(da.Analyzer, codeBlockScope);
                    CodeBlockStartAnalysisContext<TLanguageKindEnum> blockStartContext = new CodeBlockStartAnalysisContext<TLanguageKindEnum>(analyzerBlockScope, declaredNode, declaredSymbol, semanticModel, analyzerOptions, cancellationToken);
                    da.Action(blockStartContext);
                    endedAnalyzers.AddAll(codeBlockScope.CodeBlockEndActions);
                    executableNodeAnalyzers.AddRange(codeBlockScope.SyntaxNodeActions);
                });
            }

            // Execute stateful executable node analyzers, if any.
            if (executableNodeAnalyzers.Any())
            {
                var executableNodeAnalyzersByKind = PooledDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.GetInstance();
                GetNodeAnalyzersByKind(executableNodeAnalyzers, executableNodeAnalyzersByKind, addDiagnostic, getAnalyzerKindsOfInterest);

                var nodesToAnalyze = executableCodeBlocks.SelectMany(cb => cb.DescendantNodesAndSelf());
                ExecuteSyntaxNodeActions(nodesToAnalyze, executableNodeAnalyzersByKind, semanticModel,
                    analyzerOptions, addDiagnostic, continueOnAnalyzerException, getKind, cancellationToken);

                foreach (var b in executableNodeAnalyzersByKind.Values)
                {
                    b.Free();
                }

                executableNodeAnalyzersByKind.Free();
            }

            // Execute code block end actions.
            foreach (var a in endedAnalyzers)
            {
                // Catch Exception from a.OnCodeBlockEnded
                ExecuteAndCatchIfThrows(a.Analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () => a.Action(new CodeBlockEndAnalysisContext(declaredNode, declaredSymbol, semanticModel, analyzerOptions, addDiagnostic, cancellationToken)));
            }

            endedAnalyzers.Free();
            executableNodeAnalyzers.Free();
        }

        private static void ExecuteCodeBlockAnalyzers(
            HostCompilationStartAnalysisScope compilationScope,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            ImmutableArray<SyntaxNode> executableCodeBlocks,
            AnalyzerOptions analyzerOptions,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            CancellationToken cancellationToken,
            Func<SyntaxNodeAnalyzerAction<TLanguageKindEnum>, IEnumerable<TLanguageKindEnum>> getAnalyzerKindsOfInterest = null)
        {
            ExecuteCodeBlockAnalyzers(
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
                cancellationToken,
                getAnalyzerKindsOfInterest);
        }

        private static void GetNodeAnalyzersByKind(
            IEnumerable<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> nodeAnalyzers,
            PooledDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeAnalyzersByKind,
            Action<Diagnostic> addDiagnostic,
            Func<SyntaxNodeAnalyzerAction<TLanguageKindEnum>, IEnumerable<TLanguageKindEnum>> getAnalyzerKindsOfInterest = null)
        {
            Debug.Assert(nodeAnalyzers != null && nodeAnalyzers.Any());
            Debug.Assert(nodeAnalyzersByKind != null && !nodeAnalyzersByKind.Any());

            foreach (var nodeAnalyzer in nodeAnalyzers)
            {
                // Catch Exception from  nodeAnalyzer.Kinds
                try
                {
                    var kindsOfInterest = getAnalyzerKindsOfInterest != null ?
                        getAnalyzerKindsOfInterest(nodeAnalyzer) :
                        nodeAnalyzer.Kinds;

                    foreach (var kind in kindsOfInterest)
                    {
                        ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> analyzersForKind;
                        if (!nodeAnalyzersByKind.TryGetValue(kind, out analyzersForKind))
                        {
                            nodeAnalyzersByKind.Add(kind, analyzersForKind = ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>.GetInstance());
                        }

                        analyzersForKind.Add(nodeAnalyzer);
                    }
                }
                catch (Exception e)
                {
                    // Create a diagnostic saying that the analyzer failed.
                    addDiagnostic(GetAnalyzerDiagnostic(nodeAnalyzer.Analyzer, e));
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
                    foreach (var analyzer in actionsForKind)
                    {
                        ExecuteSyntaxNodeAction(analyzer, child, model, analyzerOptions, addDiagnostic, continueOnException, cancellationToken);
                    }
                }
            }
        }

        public new void Dispose()
        {
            base.Dispose();
            foreach (var kvp in this.NodeAnalyzersByKind)
            {
                kvp.Value.Free();
            }
        }
    }
}
