// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    public sealed class WorkspaceRegistration
    {
        private Workspace _registeredWorkspace;

        internal WorkspaceRegistration()
        {
        }

        public Workspace Workspace => _registeredWorkspace;
        public event EventHandler WorkspaceChanged;

        internal void SetWorkspace(Workspace workspace)
        {
            _registeredWorkspace = workspace;
        }

        internal void RaiseEvents(Workspace oldWorkspace, Workspace newWorkspace)
        {
            WorkspaceChanged?.Invoke(this, new WorkspaceChangedEventArgs(oldWorkspace, newWorkspace));
        }
    }

    internal sealed class WorkspaceChangedEventArgs : EventArgs
    {
        public readonly Workspace OldWorkspace;
        public readonly Workspace NewWorkspace;

        public WorkspaceChangedEventArgs(Workspace oldWorkspace, Workspace newWorkspace)
        {
            OldWorkspace = oldWorkspace;
            NewWorkspace = newWorkspace;
        }
    }
}
