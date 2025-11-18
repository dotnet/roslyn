// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;

internal sealed partial class VisualStudioDiagnosticAnalyzerProvider
{
    /// <summary>
    /// Loads VSIX analyzers into workspaces that provide <see cref="ISolutionAnalyzerSetterWorkspaceService"/> when they are loaded.
    /// </summary>
    [Export]
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host, WorkspaceKind.Interactive, WorkspaceKind.SemanticSearch), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class WorkspaceEventListener(
        IAsynchronousOperationListenerProvider listenerProvider,
        IVisualStudioDiagnosticAnalyzerProviderFactory providerFactory) : IEventListener
    {
        private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(nameof(Workspace));
        private readonly IVisualStudioDiagnosticAnalyzerProviderFactory _providerFactory = providerFactory;

        public void StartListening(Workspace workspace)
        {
            var setter = workspace.Services.GetService<ISolutionAnalyzerSetterWorkspaceService>();
            if (setter != null)
            {
                // fire and forget
                var token = _listener.BeginAsyncOperation(nameof(InitializeWorkspaceAsync));
                _ = Task.Run(() => InitializeWorkspaceAsync(setter)).CompletesAsyncOperation(token);
            }
        }

        public void StopListening(Workspace workspace)
        {
            // Nothing to do here.  We already kicked off the work to initialize the workspace.
        }

        private async Task InitializeWorkspaceAsync(ISolutionAnalyzerSetterWorkspaceService setter)
        {
            try
            {
                var provider = await _providerFactory.GetOrCreateProviderAsync(CancellationToken.None).ConfigureAwait(false);

                var references = provider.GetAnalyzerReferencesInExtensions();
                LogWorkspaceAnalyzerCount(references.Length);
                setter.SetAnalyzerReferences(references.SelectAsArray(referenceAndId => (AnalyzerReference)referenceAndId.reference));
            }
            catch (Exception e) when (FatalError.ReportAndPropagate(e, ErrorSeverity.Diagnostic))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private static void LogWorkspaceAnalyzerCount(int analyzerCount)
        {
            Logger.Log(FunctionId.DiagnosticAnalyzerService_Analyzers, KeyValueLogMessage.Create(
                static (m, analyzerCount) => m["AnalyzerCount"] = analyzerCount, analyzerCount, LogLevel.Debug));
        }
    }
}
