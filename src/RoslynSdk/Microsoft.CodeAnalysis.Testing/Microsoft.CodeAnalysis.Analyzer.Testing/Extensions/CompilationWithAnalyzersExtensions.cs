// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Reflection;
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
            Type? taskOfAnalysisResult;
            var methodInfo = typeof(CompilationWithAnalyzers).GetMethod(nameof(GetAllDiagnosticsAsync), new[] { typeof(CancellationToken) });
            if (methodInfo is not null)
            {
                s_getAllDiagnosticsAsync = (Func<CompilationWithAnalyzers, CancellationToken, Task<ImmutableArray<Diagnostic>>>)methodInfo.CreateDelegate(typeof(Func<CompilationWithAnalyzers, CancellationToken, Task<ImmutableArray<Diagnostic>>>), target: null);
            }
            else
            {
                s_getAllDiagnosticsAsync = (compilationWithAnalyzers, cancellationToken) => compilationWithAnalyzers.GetAllDiagnosticsAsync();
            }

            methodInfo = typeof(CompilationWithAnalyzers).GetMethod(nameof(GetAnalysisResultAsync), new[] { typeof(CancellationToken) });
            if (methodInfo is not null)
            {
                s_getAnalysisResultAsync = (Func<CompilationWithAnalyzers, CancellationToken, Task>)methodInfo.CreateDelegate(typeof(Func<CompilationWithAnalyzers, CancellationToken, Task>), target: null);

                // We know AnalysisResult exists because GetAnalysisResultAsync exists
                RoslynDebug.AssertNotNull(AnalysisResultWrapper.WrappedType);
                taskOfAnalysisResult = typeof(Task<>).MakeGenericType(AnalysisResultWrapper.WrappedType);
            }
            else
            {
                s_getAnalysisResultAsync = (compilationWithAnalyzers, cancellationToken) => throw new NotSupportedException();
                taskOfAnalysisResult = null;
            }

            var compilationWithAnalyzersOptionsType = typeof(CompilationWithAnalyzers).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.Diagnostics.CompilationWithAnalyzersOptions");
            var constructorInfo = compilationWithAnalyzersOptionsType is not null
                ? typeof(CompilationWithAnalyzers).GetConstructor(new[] { typeof(Compilation), typeof(ImmutableArray<DiagnosticAnalyzer>), compilationWithAnalyzersOptionsType })
                : null;
            if (constructorInfo is not null)
            {
                RoslynDebug.AssertNotNull(compilationWithAnalyzersOptionsType);
                s_createCompilationWithAnalyzers = (compilation, analyzers, options, cancellationToken) =>
                {
                    Action<Exception, DiagnosticAnalyzer, Diagnostic>? onAnalyzerException = null;
                    var concurrentAnalysis = true;
                    var logAnalyzerExecutionTime = true;
                    var reportSuppressedDiagnostics = true;
                    var analysisOptions = Activator.CreateInstance(compilationWithAnalyzersOptionsType, options, onAnalyzerException, concurrentAnalysis, logAnalyzerExecutionTime, reportSuppressedDiagnostics);
                    return (CompilationWithAnalyzers)Activator.CreateInstance(typeof(CompilationWithAnalyzers), compilation, analyzers, analysisOptions)!;
                };
            }
            else
            {
                s_createCompilationWithAnalyzers = (compilation, analyzers, options, cancellationToken) =>
                    compilation.WithAnalyzers(analyzers, options, cancellationToken);
            }

            s_getTaskOfAnalysisResultResult = LightupHelpers.CreatePropertyAccessor<Task, object>(taskOfAnalysisResult, nameof(Task<object>.Result), s_invalidResultSentinel);
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
