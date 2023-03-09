// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    }
}
