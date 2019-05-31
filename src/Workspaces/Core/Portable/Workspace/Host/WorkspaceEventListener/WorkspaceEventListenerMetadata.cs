// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// MEF metadata class used to find exports declared for a specific file extensions.
    /// </summary>
    internal class WorkspaceEventListenerMetadata
    {
        public string[] WorkspaceKinds { get; }

        public WorkspaceEventListenerMetadata(IDictionary<string, object> data)
        {
            this.WorkspaceKinds = (string[])data.GetValueOrDefault("WorkspaceKinds");
        }

        public WorkspaceEventListenerMetadata(params string[] workspaceKinds)
        {
            if (workspaceKinds?.Length == 0)
            {
                throw new ArgumentException(nameof(workspaceKinds));
            }

            this.WorkspaceKinds = workspaceKinds;
        }
    }
}
