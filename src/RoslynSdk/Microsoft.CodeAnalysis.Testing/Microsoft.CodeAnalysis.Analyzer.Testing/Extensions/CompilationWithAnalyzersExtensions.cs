// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Lightup;

namespace Microsoft.CodeAnalysis.Testing.Extensions
{
    internal static class CompilationWithAnalyzersExtensions
    {
        private static readonly Func<CompilationWithAnalyzers, CancellationToken, Task<ImmutableArray<Diagnostic>>> s_getAllDiagnosticsAsync;
        private static readonly Func<CompilationWithAnalyzers, CancellationToken, Task> s_getAnalysisResultAsync;
        private static readonly Func<Compilation, ImmutableArray<DiagnosticAnalyzer>, AnalyzerOptions, CancellationToken, CompilationWithAnalyzers> s_createCompilationWithAnalyzers;
        private static readonly Func<Task, object> s_getTaskOfAnalysisResultResult;
        private static readonly object s_invalidResultSentinel = new object();

        static CompilationWithAnalyzersExtensions()
        {
            s_getAllDiagnosticsAsync = (compilationWithAnalyzers, cancellationToken) =>
                compilationWithAnalyzers.GetAllDiagnosticsAsync(cancellationToken);
            s_getAnalysisResultAsync = (compilationWithAnalyzers, cancellationToken) =>
                compilationWithAnalyzers.GetAnalysisResultAsync(cancellationToken);
            s_createCompilationWithAnalyzers = (compilation, analyzers, options, cancellationToken) =>
            {
                var analysisOptions = new CompilationWithAnalyzersOptions(
                    options,
                    onAnalyzerException: null,
                    concurrentAnalysis: true,
                    logAnalyzerExecutionTime: true,
                    reportSuppressedDiagnostics: true);
                return compilation.WithAnalyzers(analyzers, analysisOptions);
            };

            s_getTaskOfAnalysisResultResult = LightupHelpers.CreatePropertyAccessor<Task, object>(
                typeof(Task<AnalysisResult>),
                nameof(Task<object>.Result),
                s_invalidResultSentinel);
        }

        public static CompilationWithAnalyzers Create(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, CancellationToken cancellationToken)
            => s_createCompilationWithAnalyzers(compilation, analyzers, options, cancellationToken);

        public static Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync(this CompilationWithAnalyzers compilationWithAnalyzers, CancellationToken cancellationToken)
            => s_getAllDiagnosticsAsync(compilationWithAnalyzers, cancellationToken);

        public static async Task<AnalysisResultWrapper> GetAnalysisResultAsync(this CompilationWithAnalyzers compilationWithAnalyzers, CancellationToken cancellationToken)
        {
            var getAnalysisResultTask = s_getAnalysisResultAsync(compilationWithAnalyzers, cancellationToken);
            await getAnalysisResultTask.ConfigureAwait(false);
            return AnalysisResultWrapper.FromInstance(s_getTaskOfAnalysisResultResult(getAnalysisResultTask));
        }
    }
}
