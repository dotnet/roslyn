// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    /// <summary>
    /// Interface to run DiagnosticAnalyzers. Implementation of this interface should be 
    /// able to run analyzers that can run in command line (Host agnostic DiagnosticAnalyzers)
    /// 
    /// How and where analyzers run depends on the implementation of this interface
    /// </summary>
    internal interface ICodeAnalysisDiagnosticAnalyzerExecutor : IWorkspaceService
    {
        Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken);
    }
}
