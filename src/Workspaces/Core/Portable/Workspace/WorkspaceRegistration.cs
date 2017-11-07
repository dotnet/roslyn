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

    public sealed class WorkspaceChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Point to old workspace it was associated with. it can be null
        /// if it was never associated with a workspace before
        /// </summary>
        public readonly Workspace OldWorkspace;

        /// <summary>
        /// Point to new workspace it is associated with. it can be null
        /// if it is being removed.
        /// </summary>
        public readonly Workspace NewWorkspace;

        internal WorkspaceChangedEventArgs(Workspace oldWorkspace, Workspace newWorkspace)
        {
            OldWorkspace = oldWorkspace;
            NewWorkspace = newWorkspace;
        }
    }
}
