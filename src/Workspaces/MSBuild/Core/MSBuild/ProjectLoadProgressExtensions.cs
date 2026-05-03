// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

internal static class ProjectLoadProgressExtensions
{
    extension(IProgress<ProjectLoadProgress>? progress)
    {
        public async Task<TResult> DoOperationAndReportProgressAsync<TResult>(ProjectLoadOperation operation, string? projectPath, string? targetFramework, Func<Task<TResult>> doFunc)
        {
            var watch = progress != null
                ? Stopwatch.StartNew()
                : null;

            TResult result;
            try
            {
                result = await doFunc().ConfigureAwait(false);
            }
            finally
            {
                if (progress != null && watch != null)
                {
                    watch.Stop();
                    progress.Report(new ProjectLoadProgress(projectPath ?? string.Empty, operation, targetFramework, watch.Elapsed));
                }
            }

            return result;
        }
    }
}
