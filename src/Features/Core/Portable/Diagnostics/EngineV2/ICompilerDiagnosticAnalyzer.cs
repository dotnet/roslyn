// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal interface ICompilerDiagnosticAnalyzer : IWorkspaceService
    {
        Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken);
    }
}
