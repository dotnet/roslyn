// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal interface ICompilerDiagnosticExecutor : IHostSpecificService
    {
        Task<CompilerAnalysisResult> AnalyzeAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken);
    }
}
