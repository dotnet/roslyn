// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
    /// Cached data from a <see cref="ProjectState"/> to the <see cref="CompilationWithAnalyzersPair"/>s
    /// we've created for it.  Note: the CompilationWithAnalyzersPair instance is dependent on the set of <see
    /// cref="DiagnosticAnalyzer"/>s passed along with the project.
    /// <para/>
    /// The value of the table is a SmallDictionary that maps from the 
    /// <see cref="Project"/> checksum the set of <see cref="DiagnosticAnalyzer"/>s being requested.
    /// Note: this dictionary must be locked with <see cref="s_gate"/> before accessing it.  A 
    /// small dictionary is chosen as this will normally only have one item in it (the current project
    /// and all its analyzers).  Occasionally it will have more, if (for example) a request to run
    /// a single analyzer is performed.
    /// </summary>
    private static readonly ConditionalWeakTable<
        ProjectState,
        SmallDictionary<
            (Checksum checksum, ImmutableArray<DiagnosticAnalyzer> analyzers),
            AsyncLazy<CompilationWithAnalyzersPair?>>> s_projectToCompilationWithAnalyzers = new();

    /// <summary>
    /// Protection around the SmallDictionary in <see cref="s_projectToCompilationWithAnalyzers"/>.
    /// </summary>
    private static readonly SemaphoreSlim s_gate = new(initialCount: 1);

    private static async Task<CompilationWithAnalyzersPair?> GetOrCreateCompilationWithAnalyzersAsync(
        Project project,
        ImmutableArray<DiagnosticAnalyzer> analyzers,
        HostAnalyzerInfo hostAnalyzerInfo,
        bool crashOnAnalyzerException,
        CancellationToken cancellationToken)
    {
        if (!project.SupportsCompilation)
            return null;

        var checksum = await project.GetDiagnosticChecksumAsync(cancellationToken).ConfigureAwait(false);

        // Make sure the cached pair was computed with the same state sets we're asking about.  if not,
        // recompute and cache with the new state sets.
        var map = s_projectToCompilationWithAnalyzers.GetValue(
            project.State, static _ => new(ChecksumAndAnalyzersEqualityComparer.Instance));

        AsyncLazy<CompilationWithAnalyzersPair?>? lazy;
        using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            var checksumAndAnalyzers = (checksum, analyzers);
            if (!map.TryGetValue(checksumAndAnalyzers, out lazy))
            {
                lazy = AsyncLazy.Create(
                    asynchronousComputeFunction: CreateCompilationWithAnalyzersAsync,
                    arg: (project, analyzers, hostAnalyzerInfo, crashOnAnalyzerException));
                map.Add(checksumAndAnalyzers, lazy);
            }
        }

        return await lazy.GetValueAsync(cancellationToken).ConfigureAwait(false);

        // <summary>
        // Should only be called on a <see cref="Project"/> that <see cref="Project.SupportsCompilation"/>.
        // </summary>
        static async Task<CompilationWithAnalyzersPair?> CreateCompilationWithAnalyzersAsync(
            (Project project,
             ImmutableArray<DiagnosticAnalyzer> analyzers,
             HostAnalyzerInfo hostAnalyzerInfo,
             bool crashOnAnalyzerException) tuple,
            CancellationToken cancellationToken)
        {
            var (project, analyzers, hostAnalyzerInfo, crashOnAnalyzerException) = tuple;

            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            var projectAnalyzers = analyzers.WhereAsArray(static (s, info) => !info.IsHostAnalyzer(s), hostAnalyzerInfo);
            var hostAnalyzers = analyzers.WhereAsArray(static (s, info) => info.IsHostAnalyzer(s), hostAnalyzerInfo);

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

            var exceptionFilter = (Exception ex) =>
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

            // Create driver that holds onto compilation and associated analyzers
            return new(
                CreateCompilationWithAnalyzers(compilation, filteredProjectAnalyzers, project.State.ProjectAnalyzerOptions, exceptionFilter),
                CreateCompilationWithAnalyzers(compilation, filteredHostAnalyzers, project.HostAnalyzerOptions, exceptionFilter));
        }

        static CompilationWithAnalyzers? CreateCompilationWithAnalyzers(
            Compilation compilation,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalyzerOptions? options,
            Func<Exception, bool> exceptionFilter)
        {
            if (analyzers.Length == 0)
                return null;

            return compilation.WithAnalyzers(analyzers, new CompilationWithAnalyzersOptions(
                options: options,
                onAnalyzerException: null,
                analyzerExceptionFilter: exceptionFilter,
                // in IDE, we always set concurrentAnalysis == false otherwise, we can get into thread starvation due to
                // async being used with synchronous blocking concurrency.
                concurrentAnalysis: false,
                logAnalyzerExecutionTime: true,
                reportSuppressedDiagnostics: true));
        }
    }
}
