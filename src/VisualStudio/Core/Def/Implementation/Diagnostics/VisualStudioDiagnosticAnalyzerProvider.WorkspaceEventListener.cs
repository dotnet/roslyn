// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.ExtensionManager;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    internal partial class VisualStudioDiagnosticAnalyzerProvider
    {
        /// <summary>
        /// Loads VSIX analyzers into workspaces that provide <see cref="ISolutionAnalyzerSetterWorkspaceService"/> when they are loaded.
        /// </summary>
        [Export]
        [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host, WorkspaceKind.Interactive), Shared]
        internal sealed class WorkspaceEventListener : IEventListener<object>
        {
            private readonly IAsynchronousOperationListener _listener;
            private readonly IThreadingContext _threadingContext;
            private readonly IServiceProvider _serviceProvider;

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public WorkspaceEventListener(
                IThreadingContext threadingContext,
                Shell.SVsServiceProvider serviceProvider,
                IAsynchronousOperationListenerProvider listenerProvider)
            {
                _threadingContext = threadingContext;
                _serviceProvider = serviceProvider;
                _listener = listenerProvider.GetListener(nameof(Workspace));
            }

            public void StartListening(Workspace workspace, object serviceOpt)
            {
                var setter = workspace.Services.GetService<ISolutionAnalyzerSetterWorkspaceService>();
                if (setter != null)
                {
                    // fire and forget
                    var token = _listener.BeginAsyncOperation(nameof(InitializeWorkspaceAsync));
                    _ = Task.Run(() => InitializeWorkspaceAsync(setter)).CompletesAsyncOperation(token);
                }
            }

            private async Task InitializeWorkspaceAsync(ISolutionAnalyzerSetterWorkspaceService setter)
            {
                try
                {
                    var provider = await CreateProviderAsync().ConfigureAwait(false);

                    var references = provider.GetAnalyzerReferencesInExtensions();
                    LogWorkspaceAnalyzerCount(references.Length);
                    setter.SetAnalyzerReferences(references);
                }
                catch (Exception e) when (FatalError.ReportAndPropagate(e, ErrorSeverity.Diagnostic))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private async Task<VisualStudioDiagnosticAnalyzerProvider> CreateProviderAsync()
            {
                // the following code requires UI thread:
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                var extensionManager = (IVsExtensionManager)_serviceProvider.GetService(typeof(SVsExtensionManager));

                return new VisualStudioDiagnosticAnalyzerProvider(extensionManager);
            }

            private static void LogWorkspaceAnalyzerCount(int analyzerCount)
            {
                Logger.Log(FunctionId.DiagnosticAnalyzerService_Analyzers, KeyValueLogMessage.Create(m => m["AnalyzerCount"] = analyzerCount));
            }
        }
    }
}
