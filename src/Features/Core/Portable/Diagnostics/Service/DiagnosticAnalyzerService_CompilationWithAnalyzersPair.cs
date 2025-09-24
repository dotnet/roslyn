// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    /// <summary>
    /// Cached data from a <see cref="Project"/> to the <see cref="CompilationWithAnalyzers"/>s we've created for it.
    /// Note: the CompilationWithAnalyzersPair instance is dependent on the set of <see cref="DiagnosticAnalyzer"/>s
    /// passed along with the project.  It is important to be associated with the project as the <see
    /// cref="CompilationWithAnalyzers"/> will use the <see cref="Compilation"/> it produces, and must see agree on that
    /// for correctness.  By sharing the same compilations, we ensure also that all syntax trees in that shared
    /// compilation are consistent with the trees retrieved from this project's documents.
    /// <para/>
    /// The value of the table is a SmallDictionary that maps from the 
    /// <see cref="Project"/> checksum the set of <see cref="DiagnosticAnalyzer"/>s being requested.
    /// Note: this dictionary must be locked with <see cref="s_gate"/> before accessing it.  A 
    /// small dictionary is chosen as this will normally only have one item in it (the current project
    /// and all its analyzers).  Occasionally it will have more, if (for example) a request to run
    /// a single analyzer is performed.
    /// </summary>
    private static readonly ConditionalWeakTable<
        Project,
        SmallDictionary<
            ImmutableArray<DiagnosticAnalyzer>,
            AsyncLazy<CompilationWithAnalyzers?>>> s_projectToCompilationWithAnalyzers = new();

    /// <summary> 
    /// Protection around the SmallDictionary in <see cref="s_projectToCompilationWithAnalyzers"/>.
    /// </summary>
    private static readonly SemaphoreSlim s_gate = new(initialCount: 1);

    private static async Task<CompilationWithAnalyzers?> GetOrCreateCompilationWithAnalyzers_OnlyCallInProcessAsync(
        Project project,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        HostAnalyzerInfo hostAnalyzerInfo,
        bool crashOnAnalyzerException,
        CancellationToken cancellationToken)
    {
        if (!project.SupportsCompilation)
            return null;

        // Make sure the cached pair was computed with the same state sets we're asking about.  if not,
        // recompute and cache with the new state sets.
        var map = s_projectToCompilationWithAnalyzers.GetValue(
            project, static _ => new(AnalyzersEqualityComparer.Instance));

        AsyncLazy<CompilationWithAnalyzers?>? lazy;
        using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!map.TryGetValue(analyzers, out lazy))
            {
                lazy = AsyncLazy.Create(
                    asynchronousComputeFunction: CreateCompilationWithAnalyzersAsync,
                    arg: (project, analyzers, hostAnalyzerInfo, crashOnAnalyzerException));
                map.Add(analyzers, lazy);
            }
        }

        return await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);

        // <summary>
        // Should only be called on a <see cref="Project"/> that <see cref="Project.SupportsCompilation"/>.
        // </summary>
        static async Task<CompilationWithAnalyzers?> CreateCompilationWithAnalyzersAsync(
            (Project project,
             ImmutableArray<DiagnosticAnalyzer> analyzers,
             HostAnalyzerInfo hostAnalyzerInfo,
             bool crashOnAnalyzerException) tuple,
            CancellationToken cancellationToken)
        {
            var (project, analyzers, hostAnalyzerInfo, crashOnAnalyzerException) = tuple;

            // Ensure we filter out DocumentDiagnosticAnalyzers (they're used to get diagnostics, without involving a
            // compilation), and also ensure the list has no duplicates.
            analyzers = analyzers.WhereAsArray(static a => !a.IsWorkspaceDiagnosticAnalyzer()).Distinct();

            // PERF: there is no analyzers for this compilation.
            //       compilationWithAnalyzer will throw if it is created with no analyzers which is perf optimization.
            if (analyzers.IsEmpty)
                return null;

            var (sharedOptions, analyzerSpecificOptionsFactory) = GetOptions();

            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            return compilation.WithAnalyzers(
                analyzers,
                new CompilationWithAnalyzersOptions(
                    options: sharedOptions,
                    onAnalyzerException: null,
                    // in IDE, we always set concurrentAnalysis == false otherwise, we can get into thread starvation due to
                    // async being used with synchronous blocking concurrency.
                    concurrentAnalysis: false,
                    logAnalyzerExecutionTime: true,
                    reportSuppressedDiagnostics: true,
                    getAnalyzerConfigOptionsProvider: analyzerSpecificOptionsFactory,
                    analyzerExceptionFilter: ex =>
                    {
                        if (ex is not OperationCanceledException && crashOnAnalyzerException)
                        {
                            // report telemetry
                            FatalError.ReportAndPropagate(ex);

                            // force fail fast (the host might not crash when reporting telemetry):
                            FailFast.OnFatalException(ex);
                        }

                        return true;
                    }));

            (AnalyzerOptions sharedOptions, Func<DiagnosticAnalyzer, AnalyzerConfigOptionsProvider>? analyzerSpecificOptionsFactory) GetOptions()
            {
                var projectAnalyzers = analyzers.Where(a => !hostAnalyzerInfo.IsHostAnalyzer(a)).ToSet();

                // If we're all host analyzers and no project analyzers (which we can check if we just have 0 project
                // analyzers), we can just return the options for host analyzers and not need any special logic.
                //
                // We want to do this (as opposed to passing back the lambda below in either of these cases) as
                // the compiler optimizes this in src\Compilers\Core\Portable\DiagnosticAnalyzer\AnalyzerExecutor.cs
                // to effectively no-op this and only add the cost of a null-check (which will then should be
                // optimized out by branch 
                if (projectAnalyzers.Count == 0)
                    return (project.State.HostAnalyzerOptions, null);

                // Similarly, If we're all project analyzers and no host analyzers, then just return the project
                // analyzer specific options.
                if (projectAnalyzers.Count == analyzers.Length)
                    return (project.State.ProjectAnalyzerOptions, null);

                // Ok, we have both host analyzers and project analyzers.  in that case, we want to provide
                // specific options for the project analyzers. Specifically, these options will be whatever
                // is in EditorConfig for the project, *without* falling back to host options.  That way
                // they don't accidentally pick up options users set for their VS instance for other solutions.
                // instead, they'll only get what is in editorconfig for the project, which is what the command
                // line will do as well.
                return (
                    project.State.HostAnalyzerOptions,
                    analyzer => projectAnalyzers.Contains(analyzer)
                        ? project.State.ProjectAnalyzerOptions.AnalyzerConfigOptionsProvider
                        : project.State.HostAnalyzerOptions.AnalyzerConfigOptionsProvider);
            }
        }
    }
}
