// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices
{
    internal class WorkspaceFailureOutputPane : ForegroundThreadAffinitizedObject
    {
        private static readonly Guid s_workspacePaneGuid = new Guid("53D7CABD-085E-46AF-ACCA-EF5A640641CA");

        private readonly IServiceProvider _serviceProvider;
        private readonly Workspace _workspace;

        public WorkspaceFailureOutputPane(IServiceProvider serviceProvider, Workspace workspace)
        {
            _serviceProvider = serviceProvider;
            _workspace = workspace;
            _workspace.WorkspaceFailed += OnWorkspaceFailed;
        }

        private void OnWorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            InvokeBelowInputPriority(() =>
            {
                var outputPane = this.OutputPane;
                if (outputPane == null)
                {
                    return;
                }

                outputPane.OutputString(e.Diagnostic.ToString() + Environment.NewLine);
            });
        }

        private IVsOutputWindowPane _doNotAccessDirectlyOutputPane;

        private IVsOutputWindowPane OutputPane
        {
            get
            {
                AssertIsForeground();

                if (_doNotAccessDirectlyOutputPane == null)
                {
                    var outputWindow = (IVsOutputWindow)_serviceProvider.GetService(typeof(SVsOutputWindow));

                    // Try to get the workspace pane if it has already been registered
                    var workspacePaneGuid = s_workspacePaneGuid;
                    var hr = outputWindow.GetPane(ref workspacePaneGuid, out _doNotAccessDirectlyOutputPane);

                    // If the workspace pane has not been registered before, create it
                    if (_doNotAccessDirectlyOutputPane == null || hr != VSConstants.S_OK)
                    {
                        if (ErrorHandler.Failed(outputWindow.CreatePane(ref workspacePaneGuid, ServicesVSResources.IntelliSense, fInitVisible: 1, fClearWithSolution: 1)) ||
                            ErrorHandler.Failed(outputWindow.GetPane(ref workspacePaneGuid, out _doNotAccessDirectlyOutputPane)))
                        {
                            return null;
                        }

                        // Must activate the workspace pane for it to show up in the output window
                        _doNotAccessDirectlyOutputPane.Activate();
                    }
                }

                return _doNotAccessDirectlyOutputPane;
            }
        }
    }
}
