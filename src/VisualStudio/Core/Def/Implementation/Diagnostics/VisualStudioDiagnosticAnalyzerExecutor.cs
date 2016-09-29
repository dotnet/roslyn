// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    [ExportWorkspaceService(typeof(ICodeAnalysisDiagnosticAnalyzerExecutor), layer: ServiceLayer.Host), Shared]
    internal class VisualStudioDiagnosticAnalyzerExecutor : ICodeAnalysisDiagnosticAnalyzerExecutor
    {
        public async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
        {
            var remoteHostClient = await project.Solution.Workspace.Services.GetService<IRemoteHostClientService>().GetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (remoteHostClient == null)
            {
                // remote host is not running. this can happen if remote host is disabled.
                return await InProcCodeAnalysisDiagnosticAnalyzerExecutor.Instance.AnalyzeAsync(analyzerDriver, project, cancellationToken).ConfigureAwait(false);
            }

            // this will let VS.Next dll to be loaded
            var executor = project.Solution.Workspace.Services.GetService<IRemoteHostDiagnosticAnalyzerExecutor>();
            return await executor.AnalyzeAsync(analyzerDriver, project, cancellationToken).ConfigureAwait(false);
        }
    }
}
