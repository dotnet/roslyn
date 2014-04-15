using Microsoft.CodeAnalysis.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// A proposed replacement for AnalyzerDriver that uses a <see cref="AsyncQueue{CompilationEvent}"/> to drive its analysis.
    /// </summary>
    public class AnalyzerDriver3<TSyntaxKind> : IDisposable
    {
        private const string DiagnosticId = "AnalyzerDriver";
        private readonly Action<Diagnostic> addDiagnostic;
        private Compilation Compilation;
        private bool continueOnError = true; // should be a parameter?
        private Task initialWorker;
        private ImmutableArray<Task> workers;

        // TODO: should these be made lazy?
        private ImmutableArray<IDiagnosticAnalyzer> Analyzers;
        private ImmutableArray<ICodeBlockStartedAnalyzer> BodyAnalyzers;
        private ImmutableArray<ISemanticModelAnalyzer> SemanticModelAnalyzers;
        private ImmutableArray<ImmutableArray<ISymbolAnalyzer>> DeclarationAnalyzersByKind; // indexed by symbol kind (of interest)
        private ImmutableArray<ICodeBlockStartedAnalyzer> CodeBlockStartedAnalyzers;
        private ImmutableArray<ICodeBlockEndedAnalyzer> CodeBlockEndedAnalyzers;
        private Func<SyntaxNode, TSyntaxKind> GetKind;

        /// <summary>
        /// Create an analyzer driver.
        /// </summary>
        /// <param name="analyzers">The set of analyzers to include in the analysis</param>
        /// <param name="getKind">A delegate that returns the language-specific kind for a given syntax node</param>
        /// <param name="cancellationToken">a cancellation token that can be used to abort analysis</param>
        public AnalyzerDriver3(IDiagnosticAnalyzer[] analyzers, Func<SyntaxNode, TSyntaxKind> getKind, CancellationToken cancellationToken)
        {
            CompilationEventQueue = new AsyncQueue<CompilationEvent>();
            DiagnosticQueue = new AsyncQueue<Diagnostic>();
            addDiagnostic = AddDiagnostic;
            GetKind = getKind;

            // start the first task to drain the event queue. The first compilation event is to be handled before
            // any other ones, so we cannot have more than one event processing task until the first event has been handled.
            if (analyzers != null && analyzers.Length != 0)
            {
                initialWorker = Task.Run(async () => {
                    try
                    {
                        await InitialWorker(analyzers, continueOnError, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // If creation is cancelled we had better not use the driver any longer
                        this.Dispose();
                    }
                });
            }
            else
            {
                initialWorker = Task.FromResult(true);
            }
        }

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
        /// <returns></returns>
        public AsyncQueue<Diagnostic> DiagnosticQueue
        {
            get; private set;
        }

        /// <summary>
        /// Returns all diagnostics computed by the analyzers since the last time this was invoked.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> DiagnosticsAsync()
        {
            var q = DiagnosticQueue;
            var allDiagnostics = DiagnosticBag.GetInstance();
            await q.WhenCompleted;
            Diagnostic d;
            while (q.TryDequeue(out d))
            {
                allDiagnostics.Add(d);
            }

            var filteredDiagnostics = DiagnosticBag.GetInstance();
            Compilation.FilterAndAppendAndFreeDiagnostics(filteredDiagnostics, ref allDiagnostics);
            return filteredDiagnostics.ToReadOnlyAndFree();
        }

        /// <summary>
        /// Return a task that completes when the driver is done producing diagnostics.
        /// </summary>
        /// <returns></returns>
        public async Task WhenCompleted()
        {
            await CompilationEventQueue.WhenCompleted;
            foreach (var worker in workers)
            {
                await worker;
            }
        }

        public void Dispose()
        {
            CompilationEventQueue.Complete();
            DiagnosticQueue.Complete();
        }

        private async Task InitialWorker(IDiagnosticAnalyzer[] analyzers, bool continueOnError, CancellationToken cancellationToken)
        {
            // Pull out the first event, which should be the "start compilation" event.
            var firstEvent = await CompilationEventQueue.DequeueAsync(/*cancellationToken*/);
            var startCompilation = firstEvent as CompilationEvent.CompilationStarted;
            if (startCompilation == null)
            {
                // The queue contents are ill formed, as they do not start with a CompilationStarted event.
                // Throwing an exception here won't do much good, as there is nothing higher on the call stack.
                // So we instead complete the queue so that the caller does not enqueue further data.
                CompilationEventQueue.Complete();
                DiagnosticQueue.Complete();
                while (CompilationEventQueue.Count != 0)
                {
                    var drainedEvent = await CompilationEventQueue.DequeueAsync();
                }
                throw new InvalidOperationException("First event must be CompilationEvent.CompilationStarted, not " + startCompilation.GetType().Name);
            }

            var compilation = startCompilation.Compilation;
            Interlocked.CompareExchange(ref Compilation, compilation, null);

            // Compute the set of effective analyzers based on suppression, and running the initial analyzers
            var effectiveAnalyzers = ArrayBuilder<IDiagnosticAnalyzer>.GetInstance();
            foreach (var analyzer in analyzers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsDiagnosticAnalyzerSuppressed(analyzer, compilation.Options, addDiagnostic, continueOnError, cancellationToken))
                {
                    effectiveAnalyzers.Add(analyzer);
                    var startAnalyzer = analyzer as ICompilationStartedAnalyzer;
                    if (startAnalyzer != null)
                    {
                        ExecuteAndCatchIfThrows(startAnalyzer, addDiagnostic, continueOnError, cancellationToken, () =>
                        {
                            var compilationAnalyzer = startAnalyzer.OnCompilationStarted(compilation, addDiagnostic, cancellationToken);
                            if (compilationAnalyzer != null) effectiveAnalyzers.Add(compilationAnalyzer);
                        });
                    }
                }
            }
            ImmutableInterlocked.InterlockedInitialize(ref Analyzers, effectiveAnalyzers.ToImmutableAndFree());
            ImmutableInterlocked.InterlockedInitialize(ref DeclarationAnalyzersByKind, MakeDeclarationAnalyzersByKind());
            ImmutableInterlocked.InterlockedInitialize(ref BodyAnalyzers, Analyzers.OfType<ICodeBlockStartedAnalyzer>().ToImmutableArray());
            ImmutableInterlocked.InterlockedInitialize(ref SemanticModelAnalyzers, Analyzers.OfType<ISemanticModelAnalyzer>().ToImmutableArray());
            ImmutableInterlocked.InterlockedInitialize(ref CodeBlockStartedAnalyzers, Analyzers.OfType<ICodeBlockStartedAnalyzer>().ToImmutableArray());
            ImmutableInterlocked.InterlockedInitialize(ref CodeBlockEndedAnalyzers, Analyzers.OfType<ICodeBlockEndedAnalyzer>().ToImmutableArray());

            // Invoke the syntax tree analyzers
            // TODO: How can the caller restrict this to one or a set of trees, or a span in a tree, rather than all trees in the compilation?
            foreach (var tree in compilation.SyntaxTrees)
            {
                foreach (var a in analyzers.OfType<ISyntaxTreeAnalyzer>())
                {
                    var runningAsynchronously = Task.Run(() =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        // Catch Exception from a.AnalyzeSyntaxTree
                        ExecuteAndCatchIfThrows(a, addDiagnostic, continueOnError, cancellationToken, () => { a.AnalyzeSyntaxTree(tree, addDiagnostic, cancellationToken); });
                    });
                }
            }

            // start some tasks to drain the event queue
            cancellationToken.ThrowIfCancellationRequested();
            const int nTasks = 1;
            var workers = ArrayBuilder<Task>.GetInstance();
            for (int i = 0; i < nTasks; i++)
            {
                workers.Add(Task.Run(() => ProcessCompilationEvents(cancellationToken)));
            }
            ImmutableInterlocked.InterlockedInitialize(ref this.workers, workers.ToImmutableAndFree());

            // TODO: Analyze nodes for those parts of each syntax tree that are not inside declarations that are analyzed.
            // For example, compilation units and namespaces, usings, etc. Perhaps those should be processed here?
        }

        private ImmutableArray<ImmutableArray<ISymbolAnalyzer>> MakeDeclarationAnalyzersByKind()
        {
            var analyzersByKind = new List<ArrayBuilder<ISymbolAnalyzer>>();
            foreach (var analyzer in Analyzers.OfType<ISymbolAnalyzer>())
            {
                // catch exceptions from SymbolKindsOfInterest
                ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnError, default(CancellationToken), () =>
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

        private async void ProcessCompilationEvents(CancellationToken cancellationToken)
        {
            try
            {
                await ProcessEvents(cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // when the queue is Completed the awaiting tasks get cancelled.
                // In that case we just return from this task
            }
        }

        private async Task ProcessEvents(CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    var e = await CompilationEventQueue.DequeueAsync(/*cancellationToken*/);
                    await ProcessEvent(e, cancellationToken);
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
            }
        }

        private async Task ProcessEvent(CompilationEvent e, CancellationToken cancellationToken)
        {
            var symbolEvent = e as CompilationEvent.SymbolDeclared;
            if (symbolEvent != null)
            {
                await ProcessSymbolDeclared(symbolEvent, cancellationToken);
                return;
            }

            var completedEvent = e as CompilationEvent.CompilationUnitCompleted;
            if (completedEvent != null)
            {
                await ProcessCompilationUnitCompleted(completedEvent, cancellationToken);
                return;
            }

            var endEvent = e as CompilationEvent.CompilationCompleted;
            if (endEvent != null)
            {
                await ProcessCompilationCompleted(endEvent, cancellationToken);
                return;
            }

            throw new InvalidOperationException("Unexpected compilation event of type " + e.GetType().Name);
        }

        private Task ProcessSymbolDeclared(CompilationEvent.SymbolDeclared symbolEvent, CancellationToken cancellationToken)
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

        private Task AnalyzeSymbol(CompilationEvent.SymbolDeclared symbolEvent, CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            Action<Diagnostic> addDiagnostic = diagnostic => AddDiagnostic(diagnostic, symbol);
            var tasks = ArrayBuilder<Task>.GetInstance();
            if ((int)symbol.Kind < DeclarationAnalyzersByKind.Length)
            {
                foreach (var da in DeclarationAnalyzersByKind[(int)symbol.Kind])
                {
                    // TODO: is the overhead of creating tasks here too high compared to the cost of running them sequentially?
                    tasks.Add(Task.Run(() =>
                    {
                        // Catch Exception from da.AnalyzeSymbol
                        ExecuteAndCatchIfThrows(da, addDiagnostic, continueOnError, cancellationToken, () =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            da.AnalyzeSymbol(symbol, Compilation, addDiagnostic, cancellationToken);
                        });
                    }));
                }
            }

            // TODO: what about syntax references elsewhere, for example in a class base clause?
            switch (symbol.Kind)
            {
                // TODO: what about other syntax, such as base clauses, using directives, top-level attributes, etc?
                case SymbolKind.Method:
                case SymbolKind.Field:
                case SymbolKind.Event: // TODO: should this be restricted to field-like events?
                    foreach (var decl in symbol.DeclaringSyntaxReferences)
                    {
                        tasks.Add(AnalyzeDeclaringReference(symbolEvent, decl, AddDiagnostic, cancellationToken));
                    }
                    break;
            }

            return Task.WhenAll(tasks.ToImmutableAndFree());
        }

        private async Task AnalyzeDeclaringReference(CompilationEvent.SymbolDeclared symbolEvent, SyntaxReference decl, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            var syntax = await decl.GetSyntaxAsync();
            var endedAnalyzers = ArrayBuilder<ICodeBlockEndedAnalyzer>.GetInstance();
            endedAnalyzers.AddRange(CodeBlockEndedAnalyzers);
            var nodeAnalyzers = ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>.GetInstance();
            nodeAnalyzers.AddRange(Analyzers.OfType<ISyntaxNodeAnalyzer<TSyntaxKind>>());
            foreach (var da in CodeBlockStartedAnalyzers)
            {
                // Catch Exception from da.OnCodeBlockStarted
                ExecuteAndCatchIfThrows(da, addDiagnostic, continueOnError, cancellationToken, () =>
                {
                    var blockStatefulAnalyzer = da.OnCodeBlockStarted(syntax, symbol, symbolEvent.SemanticModel(decl), addDiagnostic, cancellationToken);
                    var endedAnalyzer = blockStatefulAnalyzer as ICodeBlockEndedAnalyzer;
                    if (endedAnalyzer != null)
                    {
                        endedAnalyzers.Add(endedAnalyzer);
                    }
                    var nodeAnalyzer = blockStatefulAnalyzer as ISyntaxNodeAnalyzer<TSyntaxKind>;
                    if (nodeAnalyzer != null)
                    {
                        nodeAnalyzers.Add(nodeAnalyzer);
                    }
                });
            }

            PooledDictionary<TSyntaxKind, ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>> nodeAnalyzersByKind = null;
            foreach (var nodeAnalyzer in nodeAnalyzers)
            {
                // Catch Exception from  nodeAnalyzer.SyntaxKindsOfInterest
                try
                {
                    foreach (var kind in nodeAnalyzer.SyntaxKindsOfInterest)
                    {
                        if (nodeAnalyzersByKind == null) nodeAnalyzersByKind = PooledDictionary<TSyntaxKind, ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>>>.GetInstance();
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
            nodeAnalyzers.Free();

            SemanticModel semanticModel = (nodeAnalyzersByKind != null || endedAnalyzers.Any()) ? symbolEvent.SemanticModel(decl) : null;
           if (nodeAnalyzersByKind != null)
            {
                semanticModel = symbolEvent.SemanticModel(decl);
                foreach (var child in syntax.DescendantNodesAndSelf())
                {
                    ArrayBuilder<ISyntaxNodeAnalyzer<TSyntaxKind>> analyzersForKind;
                    if (nodeAnalyzersByKind.TryGetValue(GetKind(child), out analyzersForKind))
                    {
                        foreach (var analyzer in analyzersForKind)
                        {
                            // Catch Exception from analyzer.AnalyzeNode
                            ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnError, cancellationToken, () => { analyzer.AnalyzeNode(child, semanticModel, addDiagnostic, cancellationToken); });
                        }
                    }
                }

                foreach (var b in nodeAnalyzersByKind.Values)
                {
                    b.Free();
                }
                nodeAnalyzersByKind.Free();
            }

            foreach (var a in endedAnalyzers)
            {
                // Catch Exception from a.OnCodeBlockEnded
                ExecuteAndCatchIfThrows(a, addDiagnostic, continueOnError, cancellationToken, () => { a.OnCodeBlockEnded(syntax, symbol, semanticModel, addDiagnostic, cancellationToken); });
            }
            endedAnalyzers.Free();
        }

        private Task ProcessCompilationUnitCompleted(CompilationEvent.CompilationUnitCompleted completedEvent, CancellationToken cancellationToken)
        {
            // When the compiler is finished with a compilation unit, we can run user diagnostics which
            // might want to ask the compiler for all the diagnostics in the source file, for example
            // to get information about unnecessary usings.

            try
            {
                var tasks = ArrayBuilder<Task>.GetInstance();
                var semanticModel = completedEvent.SemanticModel;
                foreach (var da in SemanticModelAnalyzers)
                {
                    // TODO: is the overhead of creating tasks here too high compared to the cost of running them sequentially?
                    tasks.Add(Task.Run(() =>
                    {
                        // Catch Exception from da.AnalyzeSemanticModel
                        ExecuteAndCatchIfThrows(da, addDiagnostic, continueOnError, cancellationToken, () =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            da.AnalyzeSemanticModel(semanticModel, addDiagnostic, cancellationToken);
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

        private async Task ProcessCompilationCompleted(CompilationEvent.CompilationCompleted endEvent, CancellationToken cancellationToken)
        {
            var tasks = ArrayBuilder<Task>.GetInstance();
            foreach (var da in Analyzers.OfType<ICompilationEndedAnalyzer>())
            {
                // TODO: is the overhead of creating tasks here too high compared to the cost of running them sequentially?
                tasks.Add(Task.Run(() =>
                {
                    // Catch Exception from da.OnCompilationEnded
                    ExecuteAndCatchIfThrows(da, addDiagnostic, continueOnError, cancellationToken, () =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        da.OnCompilationEnded(Compilation, AddDiagnostic, cancellationToken);
                    });
                }));
            }

            foreach (var task in tasks)
            {
                await task;
            }

            DiagnosticQueue.Complete();
        }

        private void AddDiagnostic(Diagnostic diagnostic)
        {
            var d = Compilation.FilterDiagnostic(diagnostic);
            if (d != null)
            {
                DiagnosticQueue.Enqueue(diagnostic);
            }
        }

        private void AddDiagnostic(Diagnostic diagnostic, ISymbol container)
        {
            // TODO: this should apply the symbol filter before enqueueing the diagnostic
            AddDiagnostic(diagnostic);
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// </summary>
        private static bool IsDiagnosticAnalyzerSuppressed(IDiagnosticAnalyzer analyzer, CompilationOptions options, Action<Diagnostic> addDiagnostic, bool continueOnError, CancellationToken cancellationToken)
        {
            var supportedDiagnostics = ImmutableArray<DiagnosticDescriptor>.Empty;

            // Catch Exception from analyzer.SupportedDiagnostics
            ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnError, cancellationToken, () => { supportedDiagnostics = analyzer.SupportedDiagnostics; });

            var diagnosticOptions = options.SpecificDiagnosticOptions;

            foreach (var diag in supportedDiagnostics)
            {
                if (diagnosticOptions.ContainsKey(diag.Id))
                {
                    if (diagnosticOptions[diag.Id] == ReportDiagnostic.Suppress)
                    {
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static void ExecuteAndCatchIfThrows(IDiagnosticAnalyzer a, Action<Diagnostic> addDiagnostic, bool continueOnError, CancellationToken cancellationToken, Action analyze)
        {
            try
            {
                analyze();
            }
            catch (OperationCanceledException oce) if (continueOnError)
            {
                if (oce.CancellationToken != cancellationToken)
                {
                    // Create a info diagnostic saying that the analyzer failed
                    addDiagnostic(GetAnalyzerDiagnostic(a, oce));
                }
            }
            catch (Exception e) if (continueOnError)
            {
                // Create a info diagnostic saying that the analyzer failed
                addDiagnostic(GetAnalyzerDiagnostic(a, e));
            }
        }

        private static Diagnostic GetAnalyzerDiagnostic(IDiagnosticAnalyzer analyzer, Exception e)
        {
            return Diagnostic.Create(GetDiagnosticDescriptor(analyzer.GetType().ToString(), e.Message), Location.None);
        }

        private static DiagnosticDescriptor GetDiagnosticDescriptor(string analyzerName, string message)
        {
            return new DiagnosticDescriptor(DiagnosticId,
                CodeAnalysisResources.CompilerAnalyzerFailure,
                string.Format(CodeAnalysisResources.CompilerAnalyzerThrows, analyzerName, message),
                category: Diagnostic.CompilerDiagnosticCategory,
                defaultSeverity: DiagnosticSeverity.Info);
        }
    }
}
