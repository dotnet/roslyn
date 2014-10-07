// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    public sealed class WorkspaceRegistration
    {
        private Workspace registeredWorkspace;

        internal WorkspaceRegistration()
        {
        }

        public Workspace Workspace { get { return registeredWorkspace; } }
        public event EventHandler WorkspaceChanged;

        internal void SetWorkspaceAndRaiseEvents(Workspace workspace)
        {
            registeredWorkspace = workspace;

            var workspaceChanged = WorkspaceChanged;
            if (workspaceChanged != null)
            {
                workspaceChanged(this, EventArgs.Empty);
            }
        }
    }
}
