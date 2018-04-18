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

                    _doNotAccessDirectlyOutputPane = CreateOutputPane(outputWindow);
                }

                return _doNotAccessDirectlyOutputPane;
            }
        }

        private IVsOutputWindowPane CreateOutputPane(IVsOutputWindow outputWindow)
        {
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
    }
}
