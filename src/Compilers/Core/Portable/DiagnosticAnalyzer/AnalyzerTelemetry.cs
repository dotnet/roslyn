// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics.Telemetry
{
    public static partial class AnalyzerTelemetry
    {
        /// <summary>
        /// Gets the count of registered actions for the analyzer for the given <see cref="CompilationWithAnalyzers"/>.
        /// </summary>
        public static async Task<ActionCounts> GetAnalyzerActionCountsAsync(this CompilationWithAnalyzers compilationWithAnalyzers, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            return await compilationWithAnalyzers.GetAnalyzerActionCountsAsync(analyzer, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the execution time for the given analyzer for the given <see cref="CompilationWithAnalyzers"/>.
        /// </summary>
        public static TimeSpan GetAnalyzerExecutionTime(this CompilationWithAnalyzers compilationWithAnalyzers, DiagnosticAnalyzer analyzer)
        {
            return compilationWithAnalyzers.GetAnalyzerExecutionTime(analyzer);
        }
    }
}
