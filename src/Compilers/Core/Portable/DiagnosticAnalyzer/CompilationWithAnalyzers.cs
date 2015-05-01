// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public class CompilationWithAnalyzers
    {
        private AnalyzerDriver _driver;
        private Compilation _compilation;
        private CancellationToken _cancellationToken;
        private ConcurrentSet<Diagnostic> _exceptionDiagnostics;
        private Dictionary<SyntaxTree, Task<ImmutableArray<Diagnostic>>> _documentTasks = new Dictionary<SyntaxTree, Task<ImmutableArray<Diagnostic>>>();
        private Task<ImmutableArray<Diagnostic>> _completeAnalysisTask;
        private Task _latestAnalysisTask;
        private readonly object _analysisLock = new object();

        public Compilation Compilation
        {
            get { return _compilation; }
        }
        
        /// <summary>
        /// Creates a new compilation by attaching diagnostic analyzers to an existing compilation.
        /// </summary>
        /// <param name="compilation">The original compilation.</param>
        /// <param name="analyzers">The set of analyzers to include in future analyses.</param>
        /// <param name="options">Options that are passed to analyzers.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to abort analysis.</param>
        public CompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            Initialize(compilation, analyzers, options, AddExceptionDiagnostic, cancellationToken);
        }

        /// <summary>
        /// Creates a new compilation by attaching diagnostic analyzers to an existing compilation.
        /// </summary>
        /// <param name="compilation">The original compilation.</param>
        /// <param name="analyzers">The set of analyzers to include in future analyses.</param>
        /// <param name="options">Options that are passed to analyzers.</param>
        /// <param name="onAnalyzerException">Action to invoke if an analyzer throws an exception.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to abort analysis.</param>
        internal CompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException, CancellationToken cancellationToken)
        {
            Initialize(compilation, analyzers, options, onAnalyzerException, cancellationToken);
        }

        private void Initialize(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _exceptionDiagnostics = new ConcurrentSet<Diagnostic>();
            _driver = AnalyzerDriver.Create(compilation, analyzers, options, AnalyzerManager.Instance, onAnalyzerException ?? AddExceptionDiagnostic, false, out _compilation, _cancellationToken);
        }

        private void AddExceptionDiagnostic(Exception exception, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
        {
            _exceptionDiagnostics.Add(diagnostic);
        }

        /// <summary>
        /// Returns diagnostics produced by diagnostic analyzers.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync()
        {
            while (true)
            {
                Task latestAnalysisTask = _latestAnalysisTask;
                if (latestAnalysisTask != null)
                {
                    await latestAnalysisTask.ConfigureAwait(false);
                }

                lock (_analysisLock)
                {
                    if (_completeAnalysisTask == null)
                    {
                        if (latestAnalysisTask != _latestAnalysisTask)
                        {
                            // Another analysis task has started. Wait to start this one.
                            // Analysis tasks are serialized because the driver can use all available cores
                            // in one analysis and swamping the system with tasks is not advantageous.
                            continue;
                        }

                        _completeAnalysisTask = Task.Run(async () =>
                        {
                            _driver.StartCompleteAnalysis(_cancellationToken);

                            // Invoke GetDiagnostics to populate the compilation's CompilationEvent queue.
                            // Discard the returned diagnostics.
                            _compilation.GetDiagnostics(_cancellationToken);

                            ImmutableArray<Diagnostic> analyzerDiagnostics = await _driver.GetDiagnosticsAsync().ConfigureAwait(false);
                            return analyzerDiagnostics;
                        }, _cancellationToken);

                        _latestAnalysisTask = _completeAnalysisTask;
                    }
                }

                return await _completeAnalysisTask.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns diagnostics produced by compilation and by diagnostic analyzers.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync()
        {
            ImmutableArray<Diagnostic> analyzerDiagnostics = await GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
            return _compilation.GetDiagnostics(_cancellationToken).AddRange(analyzerDiagnostics).AddRange(_exceptionDiagnostics);
        }

        /// <summary>
        /// Returns diagnostics produced by diagnostic analyzers from analyzing a single document.
        /// </summary>
        /// <param name="model">Semantic model for the document.</param>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsFromDocumentAsync(SemanticModel model)
        {
            SyntaxTree documentTree = model.SyntaxTree;
            Task<ImmutableArray<Diagnostic>> documentTask;

            while (true)
            {
                Task latestAnalysisTask = _latestAnalysisTask;
                if (latestAnalysisTask != null)
                {
                    await latestAnalysisTask.ConfigureAwait(false);
                }

                lock (_analysisLock)
                {
                    if (!_documentTasks.TryGetValue(documentTree, out documentTask))
                    {
                        if (_completeAnalysisTask != null)
                        {
                            // Once complete analysis has begun, it is too late to start analysis of an individual document.
                            return ImmutableArray<Diagnostic>.Empty;
                        }

                        if (latestAnalysisTask != _latestAnalysisTask)
                        {
                            // Another analysis task has started. Wait to start this one.
                            // Analysis tasks are serialized because the driver can use all available cores
                            // in one analysis and swamping the system with tasks is not advantageous.
                            continue;
                        }
                        
                        documentTask = Task.Run(async () =>
                        {
                            // Invoke GetDiagnostics to populate the compilation's CompilationEvent queue.
                            // Discard the returned diagnostics.
                            model.GetDiagnostics(null, _cancellationToken);

                            return await _driver.GetPartialDiagnosticsAsync(documentTree, _cancellationToken).ConfigureAwait(false);
                        }, _cancellationToken);

                        _documentTasks[documentTree] = documentTask;
                        _latestAnalysisTask = documentTask;
                    }
                }

                return await documentTask.ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns diagnostics produced compilation and by diagnostic analyzers from analyzing a single document.
        /// </summary>
        /// <param name="model">Semantic model for the document.</param>
        public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsFromDocumentAsync(SemanticModel model)
        {
            ImmutableArray<Diagnostic> analyzerDiagnostics = await GetAnalyzerDiagnosticsFromDocumentAsync(model).ConfigureAwait(false);
            return model.GetDiagnostics(null, _cancellationToken).AddRange(analyzerDiagnostics);
        }
        
        /// <summary>
        /// Returns diagnostics produced for exceptions thrown by analyzer actions.
        /// Diagnostics are produced for exceptions only if no onAnalyzerException action is supplied at construction.
        /// </summary>
        public ImmutableArray<Diagnostic> GetExceptionDiagnostics()
        {
            return _exceptionDiagnostics.ToImmutableArrayOrEmpty();
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

            var suppressMessageState = AnalyzerDriver.SuppressMessageStateByCompilation.GetValue(compilation, (c) => new SuppressMessageAttributeState(c));
            foreach (var diagnostic in diagnostics.ToImmutableArray())
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
        /// </summary>
        /// <param name="analyzer">Analyzer to be checked for suppression.</param>
        /// <param name="options">Compilation options.</param>
        /// <param name="onAnalyzerException">
        /// Optional delegate which is invoked when an analyzer throws an exception.
        /// Delegate can do custom tasks such as report the given analyzer exception diagnostic, report a non-fatal watson for the exception, etc.
        /// </param>
        public static bool IsDiagnosticAnalyzerSuppressed(DiagnosticAnalyzer analyzer, CompilationOptions options, Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
        {
            if (analyzer == null)
            {
                throw new ArgumentNullException(nameof(analyzer));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Action<Exception, DiagnosticAnalyzer, Diagnostic> voidHandler = (ex, a, diag) => { };
            onAnalyzerException = onAnalyzerException ?? voidHandler;
            var analyzerExecutor = AnalyzerExecutor.CreateForSupportedDiagnostics(onAnalyzerException, AnalyzerManager.Instance, CancellationToken.None);

            return AnalyzerDriver.IsDiagnosticAnalyzerSuppressed(analyzer, options, AnalyzerManager.Instance, analyzerExecutor);
        }
    }
}
