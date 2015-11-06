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

        public Workspace Workspace { get { return _registeredWorkspace; } }
        public event EventHandler WorkspaceChanged;

        internal void SetWorkspaceAndRaiseEvents(Workspace workspace)
        {
            _registeredWorkspace = workspace;
            WorkspaceChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
