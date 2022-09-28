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

                var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));

                // Microsoft.VisualStudio.ExtensionManager is non-versioned, so we need to dynamically load it, depending on the version of VS we are running on
                // this will allow us to build once and deploy on different versions of VS SxS.
                var vsDteVersion = Version.Parse(dte.Version.Split(' ')[0]); // DTE.Version is in the format of D[D[.D[D]]][ (?+)], so we need to split out the version part and check for uninitialized Major/Minor below

                var assembly = Assembly.Load($"Microsoft.VisualStudio.ExtensionManager, Version={(vsDteVersion.Major == -1 ? 0 : vsDteVersion.Major)}.{(vsDteVersion.Minor == -1 ? 0 : vsDteVersion.Minor)}.0.0, PublicKeyToken=b03f5f7f11d50a3a");
                var typeIExtensionContent = assembly.GetType("Microsoft.VisualStudio.ExtensionManager.IExtensionContent");
                var type = assembly.GetType("Microsoft.VisualStudio.ExtensionManager.SVsExtensionManager");
                var extensionManager = _serviceProvider.GetService(type);

                return new VisualStudioDiagnosticAnalyzerProvider(extensionManager, typeIExtensionContent);
            }

            private static void LogWorkspaceAnalyzerCount(int analyzerCount)
            {
                Logger.Log(FunctionId.DiagnosticAnalyzerService_Analyzers, KeyValueLogMessage.Create(m => m["AnalyzerCount"] = analyzerCount));
            }
        }
    }
}
