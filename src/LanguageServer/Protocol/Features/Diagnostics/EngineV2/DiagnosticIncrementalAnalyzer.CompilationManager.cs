// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    /// <summary>
    /// Cached data from a <see cref="Project"/> to the last <see cref="CompilationWithAnalyzersPair"/> instance created
    /// for it.  Note: the CompilationWithAnalyzersPair instance is dependent on the set of <see
    /// cref="DiagnosticAnalyzer"/>s passed along with the project.  As such, we might not be able to use a prior cached
    /// value if the set of analyzers changes.  In that case, a new instance will be created and will be cached for the
    /// next caller.
    /// </summary>
    private static readonly ConditionalWeakTable<Project, StrongBox<(ImmutableArray<DiagnosticAnalyzer> analyzers, CompilationWithAnalyzersPair? compilationWithAnalyzersPair)>> s_projectToCompilationWithAnalyzers = new();

    private static async Task<CompilationWithAnalyzersPair?> GetOrCreateCompilationWithAnalyzersAsync(
        Project project,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        HostAnalyzerInfo hostAnalyzerInfo,
        bool crashOnAnalyzerException,
        CancellationToken cancellationToken)
    {
        if (!project.SupportsCompilation)
            return null;

        // Make sure the cached pair was computed with at least the same state sets we're asking about.  if not,
        // recompute and cache with the new state sets.
        if (!s_projectToCompilationWithAnalyzers.TryGetValue(project, out var tupleBox) ||
            !analyzers.IsSubsetOf(tupleBox.Value.analyzers))
        {
            var compilationWithAnalyzersPair = await CreateCompilationWithAnalyzersAsync().ConfigureAwait(false);
            tupleBox = new((analyzers, compilationWithAnalyzersPair));

            // Make a best effort attempt to store the latest computed value against these state sets. If this
            // fails (because another thread interleaves with this), that's ok.  We still return the pair we 
            // computed, so our caller will still see the right data
            s_projectToCompilationWithAnalyzers.Remove(project);

            // Intentionally ignore the result of this.  We still want to use the value we computed above, even if
            // another thread interleaves and sets a different value.
            s_projectToCompilationWithAnalyzers.GetValue(project, _ => tupleBox);
        }

        return tupleBox.Value.compilationWithAnalyzersPair;

        // <summary>
        // Should only be called on a <see cref="Project"/> that <see cref="Project.SupportsCompilation"/>.
        // </summary>
        async Task<CompilationWithAnalyzersPair?> CreateCompilationWithAnalyzersAsync()
        {
            var projectAnalyzers = analyzers.WhereAsArray(static (s, info) => !info.IsHostAnalyzer(s), hostAnalyzerInfo);
            var hostAnalyzers = analyzers.WhereAsArray(static (s, info) => info.IsHostAnalyzer(s), hostAnalyzerInfo);

            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            // Create driver that holds onto compilation and associated analyzers
            var filteredProjectAnalyzers = projectAnalyzers.WhereAsArray(static a => !a.IsWorkspaceDiagnosticAnalyzer());
            var filteredHostAnalyzers = hostAnalyzers.WhereAsArray(static a => !a.IsWorkspaceDiagnosticAnalyzer());
            var filteredProjectSuppressors = filteredProjectAnalyzers.WhereAsArray(static a => a is DiagnosticSuppressor);
            filteredHostAnalyzers = filteredHostAnalyzers.AddRange(filteredProjectSuppressors);

            // PERF: there is no analyzers for this compilation.
            //       compilationWithAnalyzer will throw if it is created with no analyzers which is perf optimization.
            if (filteredProjectAnalyzers.IsEmpty && filteredHostAnalyzers.IsEmpty)
            {
                return null;
            }

            Contract.ThrowIfFalse(project.SupportsCompilation);
            AssertCompilation(project, compilation);

            // in IDE, we always set concurrentAnalysis == false otherwise, we can get into thread starvation due to
            // async being used with synchronous blocking concurrency.
            var projectAnalyzerOptions = new CompilationWithAnalyzersOptions(
                options: project.AnalyzerOptions,
                onAnalyzerException: null,
                analyzerExceptionFilter: GetAnalyzerExceptionFilter(),
                concurrentAnalysis: false,
                logAnalyzerExecutionTime: true,
                reportSuppressedDiagnostics: true);
            var hostAnalyzerOptions = new CompilationWithAnalyzersOptions(
                options: project.HostAnalyzerOptions,
                onAnalyzerException: null,
                analyzerExceptionFilter: GetAnalyzerExceptionFilter(),
                concurrentAnalysis: false,
                logAnalyzerExecutionTime: true,
                reportSuppressedDiagnostics: true);

            // Create driver that holds onto compilation and associated analyzers
            return new CompilationWithAnalyzersPair(
                filteredProjectAnalyzers.Any() ? compilation.WithAnalyzers(filteredProjectAnalyzers, projectAnalyzerOptions) : null,
                filteredHostAnalyzers.Any() ? compilation.WithAnalyzers(filteredHostAnalyzers, hostAnalyzerOptions) : null);

            Func<Exception, bool> GetAnalyzerExceptionFilter()
            {
                return ex =>
                {
                    if (ex is not OperationCanceledException && crashOnAnalyzerException)
                    {
                        // report telemetry
                        FatalError.ReportAndPropagate(ex);

                        // force fail fast (the host might not crash when reporting telemetry):
                        FailFast.OnFatalException(ex);
                    }

                    return true;
                };
            }
        }
    }

    [Conditional("DEBUG")]
    private static void AssertCompilation(Project project, Compilation compilation1)
    {
        // given compilation must be from given project.
        Contract.ThrowIfFalse(project.TryGetCompilation(out var compilation2));
        Contract.ThrowIfFalse(compilation1 == compilation2);
    }
}
