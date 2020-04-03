// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    internal partial class VisualStudioDiagnosticAnalyzerProvider
    {
        /// <summary>
        /// Loads VSIX analyzers into <see cref="VisualStudioWorkspace"/> when it's loaded.
        /// </summary>
        [Export]
        [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
        internal sealed class WorkspaceEventListener : IEventListener<object>
        {
            private readonly VisualStudioDiagnosticAnalyzerProvider _provider;

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public WorkspaceEventListener(Factory factory)
            {
                _provider = factory.Provider;
            }

            public void StartListening(Workspace workspace, object serviceOpt)
            {
                // fire and forget
                _ = Task.Run(() => InitializeHostAnalyzerReferences((VisualStudioWorkspaceImpl)workspace));
            }

            private void InitializeHostAnalyzerReferences(VisualStudioWorkspaceImpl workspace)
            {
                try
                {
                    var references = _provider.GetAnalyzerReferencesInExtensions();
                    LogWorkspaceAnalyzerCount(references.Length);

                    workspace.ApplyChangeToWorkspace(w =>
                        w.SetCurrentSolution(s => s.WithAnalyzerReferences(references), WorkspaceChangeKind.SolutionChanged));
                }
                catch (Exception e) when (FatalError.Report(e))
                {
                }
            }

            private static void LogWorkspaceAnalyzerCount(int analyzerCount)
            {
                Logger.Log(FunctionId.DiagnosticAnalyzerService_Analyzers, KeyValueLogMessage.Create(m => m["AnalyzerCount"] = analyzerCount));
            }
        }
    }
}
