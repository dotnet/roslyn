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
        internal Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException;
        private ImmutableArray<Task> workers;
        private ImmutableArray<Task> syntaxAnalyzers;

        // TODO: should these be made lazy?
        internal ImmutableArray<IDiagnosticAnalyzer> analyzers;
        private ImmutableArray<ICodeBlockNestedAnalyzerFactory> bodyAnalyzers;
        private ImmutableArray<ISemanticModelAnalyzer> semanticModelAnalyzers;
        private ImmutableArray<ImmutableArray<ISymbolAnalyzer>> declarationAnalyzersByKind; // indexed by symbol kind (of interest)
        internal ImmutableArray<ICodeBlockNestedAnalyzerFactory> codeBlockStartedAnalyzers;
        internal ImmutableArray<ICodeBlockAnalyzer> codeBlockEndedAnalyzers;
        private Task initialWorker;
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

        internal static Compilation AttachAnalyzerDriverToCompilation(Compilation compilation, ImmutableArray<IDiagnosticAnalyzer> analyzers, out AnalyzerDriver analyzerDriver3, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            analyzerDriver3 = compilation.AnalyzerForLanguage(analyzers, options, cancellationToken);
            return compilation.WithEventQueue(analyzerDriver3.CompilationEventQueue);
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
        protected AnalyzerDriver(ImmutableArray<IDiagnosticAnalyzer> analyzers, AnalyzerOptions options, CancellationToken cancellationToken, Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException = null)
        {
            this.CompilationEventQueue = new AsyncQueue<CompilationEvent>();
            this.DiagnosticQueue = new AsyncQueue<Diagnostic>();
            this.addDiagnostic = GetDiagnosticSinkWithSuppression();
            this.analyzerOptions = options;

            Func<Exception, IDiagnosticAnalyzer, bool> defaultExceptionHandler = (exception, analyzer) => true;
            this.continueOnAnalyzerException = continueOnAnalyzerException ?? defaultExceptionHandler;

            // start the first task to drain the event queue. The first compilation event is to be handled before
            // any other ones, so we cannot have more than one event processing task until the first event has been handled.
            initialWorker = Task.Run(async () =>
            {
                try
                {
                    await InitialWorkerAsync(analyzers, continueOnAnalyzerException, cancellationToken).ConfigureAwait(false);
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
                await DiagnosticQueue.WhenCompleted.ConfigureAwait(false);
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
            await Task.WhenAll(SpecializedCollections.SingletonEnumerable(CompilationEventQueue.WhenCompleted)
                .Concat(workers))
                .ConfigureAwait(false);
        }

        private async Task InitialWorkerAsync(ImmutableArray<IDiagnosticAnalyzer> initialAnalyzers, Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException, CancellationToken cancellationToken)
        {
            // Pull out the first event, which should be the "start compilation" event.
            var firstEvent = await CompilationEventQueue.DequeueAsync(/*cancellationToken*/).ConfigureAwait(false);
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

            // Compute the set of effective analyzers based on suppression, and running the initial analyzers
            var effectiveAnalyzers = GetEffectiveAnalyzers(initialAnalyzers, compilation, analyzerOptions, addDiagnostic, continueOnAnalyzerException, cancellationToken);
            
            ImmutableInterlocked.InterlockedInitialize(ref this.analyzers, effectiveAnalyzers);
            ImmutableInterlocked.InterlockedInitialize(ref declarationAnalyzersByKind, MakeDeclarationAnalyzersByKind());
            ImmutableInterlocked.InterlockedInitialize(ref bodyAnalyzers, effectiveAnalyzers.OfType<ICodeBlockNestedAnalyzerFactory>().ToImmutableArray());
            ImmutableInterlocked.InterlockedInitialize(ref semanticModelAnalyzers, effectiveAnalyzers.OfType<ISemanticModelAnalyzer>().ToImmutableArray());
            ImmutableInterlocked.InterlockedInitialize(ref codeBlockStartedAnalyzers, effectiveAnalyzers.OfType<ICodeBlockNestedAnalyzerFactory>().ToImmutableArray());
            ImmutableInterlocked.InterlockedInitialize(ref codeBlockEndedAnalyzers, effectiveAnalyzers.OfType<ICodeBlockAnalyzer>().ToImmutableArray());

            // Invoke the syntax tree analyzers
            // TODO: How can the caller restrict this to one or a set of trees, or a span in a tree, rather than all trees in the compilation?
            var syntaxAnalyzers = ArrayBuilder<Task>.GetInstance();
            foreach (var tree in compilation.SyntaxTrees)
            {
                foreach (var a in effectiveAnalyzers.OfType<ISyntaxTreeAnalyzer>())
                {
                    var runningAsynchronously = Task.Run(() =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        // Catch Exception from a.AnalyzeSyntaxTree
                        ExecuteAndCatchIfThrows(a, addDiagnostic, continueOnAnalyzerException, cancellationToken, () => { a.AnalyzeSyntaxTree(tree, addDiagnostic, analyzerOptions, cancellationToken); });
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

        private ImmutableArray<ImmutableArray<ISymbolAnalyzer>> MakeDeclarationAnalyzersByKind()
        {
            var analyzersByKind = new List<ArrayBuilder<ISymbolAnalyzer>>();
            foreach (var analyzer in analyzers.OfType<ISymbolAnalyzer>())
            {
                // catch exceptions from SymbolKindsOfInterest
                ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnAnalyzerException, default(CancellationToken), () =>
                {
                    var kinds = analyzer.SymbolKindsOfInterest;
                    foreach (var k in kinds.Distinct())
                    {
                        if ((int)k > 100) continue; // protect against vicious analyzers
                        while ((int)k >= analyzersByKind.Count)
                        {
                            analyzersByKind.Add(ArrayBuilder<ISymbolAnalyzer>.GetInstance());
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
                    var e = await CompilationEventQueue.DequeueAsync(/*cancellationToken*/).ConfigureAwait(false);
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
                          isEnabledByDefault: true);
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
            if ((int)symbol.Kind < declarationAnalyzersByKind.Length && declaringSyntaxRefs.Any(s => s.SyntaxTree != null))
            {
                foreach (var da in declarationAnalyzersByKind[(int)symbol.Kind])
                {
                    // TODO: is the overhead of creating tasks here too high compared to the cost of running them sequentially?
                    tasks.Add(Task.Run(() =>
                    {
                        // Catch Exception from da.AnalyzeSymbol
                        ExecuteAndCatchIfThrows(da, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            da.AnalyzeSymbol(symbol, compilation, addDiagnosticForSymbol, this.analyzerOptions, cancellationToken);
                        });
                    }));
                }
            }

            foreach (var decl in declaringSyntaxRefs)
            {
                tasks.Add(AnalyzeDeclaringReferenceAsync(symbolEvent, decl, addDiagnostic, cancellationToken));
            }

            return Task.WhenAll(tasks.ToImmutableAndFree());
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
                foreach (var da in semanticModelAnalyzers)
                {
                    // TODO: is the overhead of creating tasks here too high compared to the cost of running them sequentially?
                    tasks.Add(Task.Run(() =>
                    {
                        // Catch Exception from da.AnalyzeSemanticModel
                        ExecuteAndCatchIfThrows(da, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            da.AnalyzeSemanticModel(semanticModel, addDiagnostic, this.analyzerOptions, cancellationToken);
                        });
                    }));
                }

                return Task.WhenAll(tasks.ToImmutableAndFree());
            }
            finally
            {
                completedEvent.FlushCache();
            }
        }

        private async Task ProcessCompilationCompletedAsync(CompilationCompletedEvent endEvent, CancellationToken cancellationToken)
        {
            var tasks = ArrayBuilder<Task>.GetInstance();
            foreach (var da in analyzers.OfType<ICompilationAnalyzer>())
            {
                // TODO: is the overhead of creating tasks here too high compared to the cost of running them sequentially?
                tasks.Add(Task.Run(() =>
                {
                    // Catch Exception from da.OnCompilationEnded
                    ExecuteAndCatchIfThrows(da, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        da.AnalyzeCompilation(compilation, addDiagnostic, this.analyzerOptions, cancellationToken);
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
                    if (!suppressMessageState.IsDiagnosticSuppressed(d.Id, locationOpt: d.Location, symbolOpt: symbolOpt))
                    {
                        DiagnosticQueue.Enqueue(d);
                    }
                }
            };
        }

        /// <summary>
        /// Given a set of compiler or <see cref="IDiagnosticAnalyzer"/> generated <paramref name="diagnostics"/>, returns the effective diagnostics after applying the below filters:
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
                    if (effectiveDiagnostic != null && !suppressMessageState.IsDiagnosticSuppressed(effectiveDiagnostic.Id, effectiveDiagnostic.Location))
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
        public static bool IsDiagnosticAnalyzerSuppressed(IDiagnosticAnalyzer analyzer, CompilationOptions options, Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException)
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

        private static ImmutableArray<IDiagnosticAnalyzer> GetEffectiveAnalyzers(
            IEnumerable<IDiagnosticAnalyzer> analyzers, 
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            var effectiveAnalyzers = ImmutableArray.CreateBuilder<IDiagnosticAnalyzer>();
            foreach (var analyzer in analyzers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsDiagnosticAnalyzerSuppressed(analyzer, compilation.Options, addDiagnostic, continueOnAnalyzerException, cancellationToken))
                {
                    effectiveAnalyzers.Add(analyzer);
                    var startAnalyzer = analyzer as ICompilationNestedAnalyzerFactory;
                    if (startAnalyzer != null)
                    {
                        ExecuteAndCatchIfThrows(startAnalyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                        {
                            var compilationAnalyzer = startAnalyzer.CreateAnalyzerWithinCompilation(compilation, analyzerOptions, cancellationToken);
                            if (compilationAnalyzer != null) effectiveAnalyzers.Add(compilationAnalyzer);
                        });
                    }
                }
            }

            return effectiveAnalyzers.ToImmutable();
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// </summary>
        private static bool IsDiagnosticAnalyzerSuppressed(
            IDiagnosticAnalyzer analyzer, 
            CompilationOptions options, 
            Action<Diagnostic> addDiagnostic, 
            Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException, 
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

        protected static void ExecuteAndCatchIfThrows(IDiagnosticAnalyzer a, Action<Diagnostic> addDiagnostic, Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException, CancellationToken cancellationToken, Action analyze)
        {
            try
            {
                analyze();
            }
            catch (OperationCanceledException oce) if (continueOnAnalyzerException(oce, a))
            {
                if (oce.CancellationToken != cancellationToken)
                {
                    // Create a info diagnostic saying that the analyzer failed
                    addDiagnostic(GetAnalyzerDiagnostic(a, oce));
                }
            }
            catch (Exception e) if (continueOnAnalyzerException(e, a))
            {
                // Create a info diagnostic saying that the analyzer failed
                addDiagnostic(GetAnalyzerDiagnostic(a, e));
            }
        }

        internal static Diagnostic GetAnalyzerDiagnostic(IDiagnosticAnalyzer analyzer, Exception e)
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
                isEnabledByDefault: true);
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
    public class AnalyzerDriver<TSyntaxKind> : AnalyzerDriver
    {
        private Func<SyntaxNode, TSyntaxKind> GetKind;
        private ImmutableDictionary<TSyntaxKind, ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>> lazyNodeAnalyzersByKind = null;

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
        public AnalyzerDriver(ImmutableArray<IDiagnosticAnalyzer> analyzers, Func<SyntaxNode, TSyntaxKind> getKind, AnalyzerOptions options, CancellationToken cancellationToken, Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException = null) : base(analyzers, options, cancellationToken, continueOnAnalyzerException)
        {
            GetKind = getKind;
        }

        private ImmutableDictionary<TSyntaxKind, ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>> NodeAnalyzersByKind
        {
            get
            {
                if (lazyNodeAnalyzersByKind == null)
                {
                    var nodeAnalyzers = base.analyzers.OfType<ISyntaxNodeAnalyzer<TSyntaxKind>>();
                    ImmutableDictionary<TSyntaxKind, ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>> analyzersByKind;
                    if (nodeAnalyzers.Any())
                    {
                        var addDiagnostic = GetDiagnosticSinkWithSuppression();
                        analyzersByKind = GetNodeAnalyzersByKind(nodeAnalyzers, addDiagnostic).ToImmutableDictionary();
                    }
                    else
                    {
                        analyzersByKind = ImmutableDictionary<TSyntaxKind, ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>>.Empty;
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
                    return true;

                case SymbolKind.Field:
                    // Check if this is not a compiler generated backing field.
                    return ((IFieldSymbol)symbol).AssociatedSymbol == null;

                default:
                    return false;
            }
        }

        protected override async Task AnalyzeDeclaringReferenceAsync(SymbolDeclaredCompilationEvent symbolEvent, SyntaxReference decl, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            SemanticModel semanticModel = symbolEvent.SemanticModel(decl);
            var declaringReferenceSyntax = await decl.GetSyntaxAsync().ConfigureAwait(false);
            var syntax = semanticModel.GetTopmostNodeForDiagnosticAnalysis(symbol, declaringReferenceSyntax);

            var statelessNodeAnalyzersByKind = this.NodeAnalyzersByKind;
            var executeSyntaxNodeAnalyzers = statelessNodeAnalyzersByKind.Any();
            var executeCodeBlockAnalyzers = (this.codeBlockStartedAnalyzers.Any() || this.codeBlockEndedAnalyzers.Any()) &&
                CanHaveExecutableCodeBlock(symbol);

            if (executeSyntaxNodeAnalyzers || executeCodeBlockAnalyzers)
            {
                var declarationsInNode = semanticModel.GetDeclarationsInNode(syntax, getSymbol: syntax != declaringReferenceSyntax, cancellationToken: cancellationToken);

                // Execute stateless syntax node analyzers.
                if (executeSyntaxNodeAnalyzers)
                {
                    ExecuteStatelessNodeAnalyzers(statelessNodeAnalyzersByKind, syntax, symbol, declarationsInNode, semanticModel, 
                        addDiagnostic, this.continueOnAnalyzerException, this.analyzerOptions, this.GetKind, cancellationToken);
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

                    ExecuteCodeBlockAnalyzers(this.codeBlockStartedAnalyzers, this.codeBlockEndedAnalyzers,
                        syntax, symbol, executableCodeBlocks, this.analyzerOptions,
                        semanticModel, addDiagnostic, this.continueOnAnalyzerException, this.GetKind, cancellationToken);
                }
            }
        }

        private static void ExecuteStatelessNodeAnalyzers(
            IDictionary<TSyntaxKind, ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>> analyzersByKind,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            IEnumerable<DeclarationInfo> declarationsInNode,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException,
            AnalyzerOptions analyzerOptions,
            Func<SyntaxNode, TSyntaxKind> getKind,
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
                    if (declInNode.DeclaredSymbol == declaredSymbol && !first)
                    {
                        declaredNode = declInNode.DeclaredNode;
                        continue;
                    }

                    if (descendantDeclsToSkip == null)
                    {
                        descendantDeclsToSkip = new HashSet<SyntaxNode>();
                    }

                    descendantDeclsToSkip.Add(declInNode.DeclaredNode);
                }

                first = false;
            }

            var nodesToAnalyze = descendantDeclsToSkip == null ?
                declaredNode.DescendantNodesAndSelf(descendIntoTrivia: true) :
                declaredNode.DescendantNodesAndSelf(n => !descendantDeclsToSkip.Contains(n), descendIntoTrivia: true).Except(descendantDeclsToSkip);

            ExecuteSyntaxAnalyzers(nodesToAnalyze, analyzersByKind, semanticModel, 
                addDiagnostic, continueOnAnalyzerException, analyzerOptions, getKind, cancellationToken);
        }

        /// <summary>
        /// Executes the given code block analyzers on all the executable code blocks for each declaration info in <paramref name="declarationsInNode"/>.
        /// </summary>
        /// <param name="codeBlockStartedAnalyzers">Code block analyzer factories.</param>
        /// <param name="codeBlockEndedAnalyzers">Stateless code block analyzers.</param>
        /// <param name="declarationsInNode">Declarations to be analyzed.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="semanticModel">SemanticModel.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from any analyzer should be handled or not.</param>
        /// <param name="getKind">Delegate to compute language specific syntax kind for a syntax node.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="getAnalyzerKindsOfInterest">Optional delegate to return cached <see cref="ISyntaxNodeAnalyzer{TSyntaxKind}.SyntaxKindsOfInterest"/>.
        /// If null, then this property is explicitly invoked by the driver to compute syntax kinds of interest.</param>
        public static void ExecuteCodeBlockAnalyzers(
            IEnumerable<ICodeBlockNestedAnalyzerFactory> codeBlockStartedAnalyzers,
            IEnumerable<ICodeBlockAnalyzer> codeBlockEndedAnalyzers,
            IEnumerable<DeclarationInfo> declarationsInNode,
            AnalyzerOptions analyzerOptions,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException,
            Func<SyntaxNode, TSyntaxKind> getKind,
            CancellationToken cancellationToken,
            Func<ISyntaxNodeAnalyzer<TSyntaxKind>, IEnumerable<TSyntaxKind>> getAnalyzerKindsOfInterest = null)
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

                if (declaredSymbol != null && CanHaveExecutableCodeBlock(declaredSymbol))
                {
                    ExecuteCodeBlockAnalyzers(codeBlockStartedAnalyzers, codeBlockEndedAnalyzers, declaredNode, declaredSymbol,
                         executableCodeBlocks, analyzerOptions, semanticModel, addDiagnostic, continueOnAnalyzerException, getKind, cancellationToken, getAnalyzerKindsOfInterest);
                }
            }
        }

        private static void ExecuteCodeBlockAnalyzers(
            IEnumerable<ICodeBlockNestedAnalyzerFactory> codeBlockStartedAnalyzers,
            IEnumerable<ICodeBlockAnalyzer> codeBlockEndedAnalyzers,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            ImmutableArray<SyntaxNode> executableCodeBlocks,
            AnalyzerOptions analyzerOptions,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException,
            Func<SyntaxNode, TSyntaxKind> getKind,
            CancellationToken cancellationToken,
            Func<ISyntaxNodeAnalyzer<TSyntaxKind>, IEnumerable<TSyntaxKind>> getAnalyzerKindsOfInterest = null)
        {
            Debug.Assert(declaredNode != null);
            Debug.Assert(declaredSymbol != null);
            Debug.Assert(CanHaveExecutableCodeBlock(declaredSymbol));
            Debug.Assert(codeBlockStartedAnalyzers.Any() || codeBlockEndedAnalyzers.Any());

            // Compute the set of stateful code block analyzers.
            var endedAnalyzers = PooledHashSet<ICodeBlockAnalyzer>.GetInstance();
            var executableNodeAnalyzers = ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>.GetInstance();

            endedAnalyzers.AddAll(codeBlockEndedAnalyzers);
            foreach (var da in codeBlockStartedAnalyzers)
            {
                // Catch Exception from da.OnCodeBlockStarted
                ExecuteAndCatchIfThrows(da, addDiagnostic, continueOnAnalyzerException, cancellationToken, () =>
                {
                    var blockStatefulAnalyzer = da.CreateAnalyzerWithinCodeBlock(declaredNode, declaredSymbol, semanticModel, analyzerOptions, cancellationToken);
                    var endedAnalyzer = blockStatefulAnalyzer as ICodeBlockAnalyzer;
                    if (endedAnalyzer != null)
                    {
                        endedAnalyzers.Add(endedAnalyzer);
                    }

                    var executableNodeAnalyzer = blockStatefulAnalyzer as ISyntaxNodeAnalyzer<TSyntaxKind>;
                    if (executableNodeAnalyzer != null)
                    {
                        executableNodeAnalyzers.Add(executableNodeAnalyzer);
                    }
                });
            }

            // Execute stateful executable node analyzers, if any.
            if (executableNodeAnalyzers.Any())
            {
                var executableNodeAnalyzersByKind = GetNodeAnalyzersByKind(executableNodeAnalyzers, addDiagnostic, getAnalyzerKindsOfInterest);

                var nodesToAnalyze = executableCodeBlocks.SelectMany(cb => cb.DescendantNodesAndSelf());
                ExecuteSyntaxAnalyzers(nodesToAnalyze, executableNodeAnalyzersByKind, semanticModel, 
                    addDiagnostic, continueOnAnalyzerException, analyzerOptions, getKind, cancellationToken);

                foreach (var b in executableNodeAnalyzersByKind.Values)
                {
                    b.Free();
                }

                executableNodeAnalyzersByKind.Free();
            }

            // Execute code block analyzers.
            foreach (var a in endedAnalyzers)
            {
                // Catch Exception from a.OnCodeBlockEnded
                ExecuteAndCatchIfThrows(a, addDiagnostic, continueOnAnalyzerException, cancellationToken, () => a.AnalyzeCodeBlock(declaredNode, declaredSymbol, semanticModel, addDiagnostic, analyzerOptions, cancellationToken));
            }

            endedAnalyzers.Free();
            executableNodeAnalyzers.Free();
        }

        private static PooledDictionary<TSyntaxKind, ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>> GetNodeAnalyzersByKind(
            IEnumerable<ISyntaxNodeAnalyzer<TSyntaxKind>> nodeAnalyzers, 
            Action<Diagnostic> addDiagnostic,
            Func<ISyntaxNodeAnalyzer<TSyntaxKind>, IEnumerable<TSyntaxKind>> getAnalyzerKindsOfInterest = null)
        {
            Debug.Assert(nodeAnalyzers != null && nodeAnalyzers.Any());

            var nodeAnalyzersByKind = PooledDictionary<TSyntaxKind, ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>>.GetInstance();

            foreach (var nodeAnalyzer in nodeAnalyzers)
            {
                // Catch Exception from nodeAnalyzer.SyntaxKindsOfInterest
                try
                {
                    var kindsOfInterest = getAnalyzerKindsOfInterest != null ?
                        getAnalyzerKindsOfInterest(nodeAnalyzer) :
                        nodeAnalyzer.SyntaxKindsOfInterest;

                    foreach (var kind in kindsOfInterest)
                    {
                        ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>> analyzersForKind;
                        if (!nodeAnalyzersByKind.TryGetValue(kind, out analyzersForKind))
                        {
                            nodeAnalyzersByKind.Add(kind, analyzersForKind = ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>.GetInstance());
                        }
                        analyzersForKind.Add(nodeAnalyzer);
                    }
                }
                catch (Exception e)
                {
                    // Create a info diagnostic saying that the analyzer failed
                    addDiagnostic(GetAnalyzerDiagnostic(nodeAnalyzer, e));
                }
            }

            return nodeAnalyzersByKind;
        }

        private static void ExecuteSyntaxAnalyzers(
            IEnumerable<SyntaxNode> nodesToAnalyze,
            IDictionary<TSyntaxKind, ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>> nodeAnalyzersByKind,
            SemanticModel model,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, IDiagnosticAnalyzer, bool> continueOnAnalyzerException,
            AnalyzerOptions analyzerOptions,
            Func<SyntaxNode, TSyntaxKind> getKind,
            CancellationToken cancellationToken)
        {
            Debug.Assert(nodeAnalyzersByKind != null);
            Debug.Assert(nodeAnalyzersByKind.Any());

            foreach (var child in nodesToAnalyze)
            {
                ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>> analyzersForKind;
                if (nodeAnalyzersByKind.TryGetValue(getKind(child), out analyzersForKind))
                {
                    foreach (var analyzer in analyzersForKind)
                    {
                        // Catch Exception from analyzer.AnalyzeNode
                        ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnAnalyzerException, cancellationToken, () => analyzer.AnalyzeNode(child, model, addDiagnostic, analyzerOptions, cancellationToken));
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
