// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Lightup;

namespace Microsoft.CodeAnalysis.Testing.Extensions
{
    internal static class CompilationWithAnalyzersExtensions
    {
        private static readonly Func<CompilationWithAnalyzers, CancellationToken, Task> s_getAnalysisResultAsync;
        private static readonly Func<Task, object> s_getTaskOfAnalysisResultResult;
        private static readonly object s_invalidResultSentinel = new object();
        private static readonly Type s_compilationType = typeof(CompilationWithAnalyzers);
        private static readonly Assembly s_frameworkAssembly = s_compilationType.GetTypeInfo().Assembly;
        private static readonly Type? s_optionsType = s_frameworkAssembly.DefinedTypes.FirstOrDefault(type => type.Name == "CompilationWithAnalyzersOptions")?.AsType();
        private static readonly Type? s_suppressorType = s_frameworkAssembly.DefinedTypes.FirstOrDefault(type => type.Name == "DiagnosticSuppressor")?.AsType();
        private static readonly int s_frameworkMajorVersion = s_frameworkAssembly.GetName().Version?.Major ?? 0;

        static CompilationWithAnalyzersExtensions()
        {
            Type? taskOfAnalysisResult;
            var methodInfo = typeof(CompilationWithAnalyzers).GetMethod(nameof(GetAnalysisResultAsync), new[] { typeof(CancellationToken) });
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

            s_getTaskOfAnalysisResultResult = LightupHelpers.CreatePropertyAccessor<Task, object>(taskOfAnalysisResult, nameof(Task<object>.Result), s_invalidResultSentinel);
        }

        public static async Task<AnalysisResultWrapper> GetAnalysisResultAsync(this CompilationWithAnalyzers compilationWithAnalyzers, CancellationToken cancellationToken)
        {
            var getAnalysisResultTask = s_getAnalysisResultAsync(compilationWithAnalyzers, cancellationToken);
            await getAnalysisResultTask.ConfigureAwait(false);
            return AnalysisResultWrapper.FromInstance(s_getTaskOfAnalysisResultResult(getAnalysisResultTask));
        }

        public static readonly Func<Compilation, ImmutableArray<DiagnosticAnalyzer>, AnalyzerOptions, bool, CancellationToken, CompilationWithAnalyzers> CreateCompilationWithAnalyzers = BuildCreateCompilationWithAnalyzersFunc();

        public static bool ContainsDiagnosticSuppressors(this ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            if (s_frameworkMajorVersion < 4 || s_suppressorType == null)
            {
                return false;
            }

            return analyzers.Any(analyzer => s_suppressorType.IsInstanceOfType(analyzer));
        }

        private static Func<Compilation, ImmutableArray<DiagnosticAnalyzer>, AnalyzerOptions, bool, CancellationToken, CompilationWithAnalyzers> BuildCreateCompilationWithAnalyzersFunc()
        {
            if (s_optionsType != null && s_frameworkMajorVersion >= 4)
            {
                return (compilation, analyzers, options, reportSuppressedDiagnostics, cancellationToken) =>
                {
                    // "https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnostics.compilationwithanalyzersoptions.-ctor?view=roslyn-dotnet-4.3.0#microsoft-codeanalysis-diagnostics-compilationwithanalyzersoptions-ctor(microsoft-codeanalysis-diagnostics-analyzeroptions-system-action((system-exception-microsoft-codeanalysis-diagnostics-diagnosticanalyzer-microsoft-codeanalysis-diagnostic))-system-boolean-system-boolean-system-boolean)"
                    var compilationWithAnalyzersOptions = Activator.CreateInstance(s_optionsType, options, null, true, false, reportSuppressedDiagnostics);

                    return (CompilationWithAnalyzers)Activator.CreateInstance(s_compilationType, compilation, analyzers, compilationWithAnalyzersOptions)!;
                };
            }

            return (compilation, analyzers, options, reportSuppressedDiagnostics, cancellationToken) => compilation.WithAnalyzers(analyzers, options, cancellationToken);
        }
    }
}
