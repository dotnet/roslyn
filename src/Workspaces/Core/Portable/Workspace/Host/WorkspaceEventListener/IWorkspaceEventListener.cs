// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// provide a way for features to lazily subscribe to workspace events
    /// </summary>
    internal interface IWorkspaceEventListener
    {
        void Listen(Workspace workspace);
        void Stop(Workspace workspace);
    }
}
