// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal class WorkspaceFailureOutputPane : ForegroundThreadAffinitizedObject
    {
        private static readonly Guid s_workspacePaneGuid = new Guid("53D7CABD-085E-46AF-ACCA-EF5A640641CA");

        private readonly IServiceProvider _serviceProvider;
        private readonly Workspace _workspace;

        public WorkspaceFailureOutputPane(IThreadingContext threadingContext, IServiceProvider serviceProvider, Workspace workspace)
            : base(threadingContext)
        {
            _serviceProvider = serviceProvider;
            _workspace = workspace;
            _workspace.WorkspaceFailed += OnWorkspaceFailed;
        }

        private void OnWorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            InvokeBelowInputPriorityAsync(() =>
            {
                this.OutputPaneOpt?.OutputString(e.Diagnostic.ToString() + Environment.NewLine);
            });
        }

        private IVsOutputWindowPane _doNotAccessDirectlyOutputPane;

        private IVsOutputWindowPane OutputPaneOpt
        {
            get
            {
                AssertIsForeground();

                if (_doNotAccessDirectlyOutputPane == null)
                {
                    var outputWindow = (IVsOutputWindow)_serviceProvider.GetService(typeof(SVsOutputWindow));

                    // This may run during the shutdown of Visual Studio and so we must be ready for the service
                    // not being available.
                    if (outputWindow == null)
                    {
                        return null;
                    }

                    // Output Window panes have two states; initialized and active. The former is used to indicate that the pane
                    // can be made active ("selected") by the user, the latter indicates that the pane is currently active.
                    // There's no way to only initialize a pane without also making it active so we remember the last active pane
                    // and reactivate it after we've created ourselves to avoid stealing focus away from it.
                    var lastActivePane = GetActivePane(outputWindow);

                    _doNotAccessDirectlyOutputPane = CreateOutputPane(outputWindow);

                    if (lastActivePane != Guid.Empty)
                    {
                        ActivatePane(outputWindow, lastActivePane);
                    }
                }

                return _doNotAccessDirectlyOutputPane;
            }
        }

        private IVsOutputWindowPane CreateOutputPane(IVsOutputWindow outputWindow)
        {
            AssertIsForeground();

            // Try to get the workspace pane if it has already been registered
            var workspacePaneGuid = s_workspacePaneGuid;

            // If the pane has already been created, CreatePane returns it
            if (ErrorHandler.Succeeded(outputWindow.CreatePane(ref workspacePaneGuid, ServicesVSResources.IntelliSense, fInitVisible: 1, fClearWithSolution: 1)) &&
                ErrorHandler.Succeeded(outputWindow.GetPane(ref workspacePaneGuid, out var pane)))
            {
                return pane;
            }

            return null;
        }

        private Guid GetActivePane(IVsOutputWindow outputWindow)
        {
            AssertIsForeground();

            if (outputWindow is IVsOutputWindow2 outputWindow2)
            {
                if (ErrorHandler.Succeeded(outputWindow2.GetActivePaneGUID(out var activePaneGuid)))
                {
                    return activePaneGuid;
                }
            }

            return Guid.Empty;
        }

        private void ActivatePane(IVsOutputWindow outputWindow, Guid paneGuid)
        {
            AssertIsForeground();

            if (ErrorHandler.Succeeded(outputWindow.GetPane(ref paneGuid, out var pane)))
            {
                pane.Activate();
            }
        }
    }
}
