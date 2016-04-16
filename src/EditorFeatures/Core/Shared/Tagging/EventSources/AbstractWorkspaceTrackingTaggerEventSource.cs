// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Tagging
{
    /// <summary>
    /// An abstract implementation of a tagger event source that takes a buffer and tracks
    /// the workspace that it's attached to.
    /// </summary>
    internal abstract class AbstractWorkspaceTrackingTaggerEventSource : AbstractTaggerEventSource
    {
        private readonly WorkspaceRegistration _workspaceRegistration;

        protected ITextBuffer SubjectBuffer { get; }
        protected Workspace CurrentWorkspace { get; private set; }

        protected AbstractWorkspaceTrackingTaggerEventSource(ITextBuffer subjectBuffer, TaggerDelay delay) : base(delay)
        {
            this.SubjectBuffer = subjectBuffer;
            _workspaceRegistration = Workspace.GetWorkspaceRegistration(subjectBuffer.AsTextContainer());
        }

        protected abstract void ConnectToWorkspace(Workspace workspace);
        protected abstract void DisconnectFromWorkspace(Workspace workspace);

        public override void Connect()
        {
            this.CurrentWorkspace = _workspaceRegistration.Workspace;
            _workspaceRegistration.WorkspaceChanged += OnWorkspaceRegistrationChanged;

            if (this.CurrentWorkspace != null)
            {
                ConnectToWorkspace(this.CurrentWorkspace);
            }
        }

        private void OnWorkspaceRegistrationChanged(object sender, EventArgs e)
        {
            if (this.CurrentWorkspace != null)
            {
                DisconnectFromWorkspace(this.CurrentWorkspace);
            }

            this.CurrentWorkspace = _workspaceRegistration.Workspace;

            if (this.CurrentWorkspace != null)
            {
                ConnectToWorkspace(this.CurrentWorkspace);
            }
        }

        public override void Disconnect()
        {
            if (this.CurrentWorkspace != null)
            {
                DisconnectFromWorkspace(this.CurrentWorkspace);
                this.CurrentWorkspace = null;
            }

            _workspaceRegistration.WorkspaceChanged -= OnWorkspaceRegistrationChanged;
        }
    }
}
