// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
///  Helper struct for running project load operations and optionally reporting progress.
/// </summary>
internal readonly struct ProjectLoadOperationRunner(IProgress<ProjectLoadProgress>? progress)
{
    private readonly IProgress<ProjectLoadProgress>? _progress = progress;

    public Task<TResult> DoOperationAndReportProgressAsync<TResult>(
        ProjectLoadOperation operation,
        string? projectPath,
        string? targetFramework,
        Func<Task<TResult>> doFunc)
    {
        return _progress is { } progress
            ? DoOperationAndReportProgressCoreAsync(progress, operation, projectPath, targetFramework, doFunc)
            : doFunc();

        static async Task<TResult> DoOperationAndReportProgressCoreAsync(
            IProgress<ProjectLoadProgress> progress,
            ProjectLoadOperation operation,
            string? projectPath,
            string? targetFramework,
            Func<Task<TResult>> doFunc)
        {
            var watch = Stopwatch.StartNew();

            TResult result;
            try
            {
                result = await doFunc().ConfigureAwait(false);
            }
            finally
            {
                watch.Stop();
                progress.Report(new ProjectLoadProgress(projectPath ?? string.Empty, operation, targetFramework, watch.Elapsed));
            }

            return result;
        }
    }
}
