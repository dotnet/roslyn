﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public class CompilationWithAnalyzers
    {
        private readonly AnalyzerDriver _driver;
        private readonly Compilation _compilation;
        private readonly CancellationToken _cancellationToken;
        private readonly ConcurrentSet<Diagnostic> _exceptionDiagnostics;

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
            _cancellationToken = cancellationToken;
            _exceptionDiagnostics = new ConcurrentSet<Diagnostic>();
            _driver = AnalyzerDriver.Create(compilation, analyzers, options, AnalyzerManager.Instance, AddExceptionDiagnostic, out _compilation, _cancellationToken);
        }

        private void AddExceptionDiagnostic(Diagnostic diagnostic)
        {
            _exceptionDiagnostics.Add(diagnostic);
        }

        /// <summary>
        /// Returns diagnostics produced by diagnostic analyzers.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync()
        {
            // Invoke GetDiagnostics to populate the compilation's CompilationEvent queue.
            // Discard the returned diagnostics.
            _compilation.GetDiagnostics(_cancellationToken);

            return await _driver.GetDiagnosticsAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Returns diagnostics produced by compilation and by diagnostic analyzers.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync()
        {
            // Invoke GetDiagnostics to populate the compilation's CompilationEvent queue.
            ImmutableArray<Diagnostic> compilerDiagnostics = _compilation.GetDiagnostics(_cancellationToken);

            ImmutableArray<Diagnostic> analyzerDiagnostics = await _driver.GetDiagnosticsAsync().ConfigureAwait(false);
            return compilerDiagnostics.AddRange(analyzerDiagnostics).AddRange(_exceptionDiagnostics);
        }

        /// <summary>
        /// Given a set of compiler or <see cref="DiagnosticAnalyzer"/> generated <paramref name="diagnostics"/>, returns the effective diagnostics after applying the below filters:
        /// 1) <see cref="CompilationOptions.SpecificDiagnosticOptions"/> specified for the given <paramref name="compilation"/>.
        /// 2) <see cref="CompilationOptions.GeneralDiagnosticOption"/> specified for the given <paramref name="compilation"/>.
        /// 3) Diagnostic suppression through applied <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/>.
        /// 4) Pragma directives for the given <paramref name="compilation"/>.
        /// </summary>
        public static IEnumerable<Diagnostic> GetEffectiveDiagnostics(ImmutableArray<Diagnostic> diagnostics, Compilation compilation)
        {
            if (diagnostics == default(ImmutableArray<Diagnostic>))
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            var suppressMessageState = AnalyzerDriver.SuppressMessageStateByCompilation.GetValue(compilation, (c) => new SuppressMessageAttributeState(c));
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

            // TODO: Public API change to surface exception diagnostics?
            Action<Diagnostic> addExceptionDiagnostic = diagnostic => { };
            var analyzerExecutor = AnalyzerExecutor.CreateForSupportedDiagnostics(addExceptionDiagnostic, continueOnAnalyzerException, CancellationToken.None);

            return AnalyzerDriver.IsDiagnosticAnalyzerSuppressed(analyzer, options, AnalyzerManager.Instance, analyzerExecutor);
        }
    }
}
