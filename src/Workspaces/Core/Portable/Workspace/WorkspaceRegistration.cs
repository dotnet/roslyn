// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    public sealed class WorkspaceRegistration
    {
        private readonly object _gate = new object();

        internal WorkspaceRegistration()
        {
        }

        public Workspace Workspace { get; private set; }

        public event EventHandler WorkspaceChanged;

        internal void SetWorkspaceAndRaiseEvents(Workspace workspace)
        {
            SetWorkspace(workspace);
            RaiseEvents();
        }

        internal void SetWorkspace(Workspace workspace)
        {
            Workspace = workspace;
        }

        internal void RaiseEvents()
        {
            lock (_gate)
            {
                // this is a workaround for https://devdiv.visualstudio.com/DevDiv/_workitems/edit/744145
                // for preview 2.
                //
                // it is a workaround since we are calling event handler under a lock to make sure
                // we don't raise event concurrently
                //
                // we have this issue to track proper fix for preview 3 or later
                // https://github.com/dotnet/roslyn/issues/32551
                //
                // issue we are working around is the fact this event can happen concurrently
                // if RegisteryText happens and then UnregisterText happens in perfect timing, 
                // RegisterText got slightly delayed since it is async event, and UnregisterText happens 
                // at the same time since it is a synchronous event, and they my happens in 2 different threads, 
                // cause this event to be raised concurrently.
                // that can cause some listener to mess up its internal state like the issue linked above
                WorkspaceChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
