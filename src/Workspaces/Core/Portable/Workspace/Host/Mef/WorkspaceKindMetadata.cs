// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// MEF metadata class used to find exports declared for a specific <see cref="WorkspaceKind"/>.
    /// </summary>
    internal class WorkspaceKindMetadata
    {
        public IReadOnlyCollection<string> WorkspaceKinds { get; }

        public WorkspaceKindMetadata(IDictionary<string, object> data)
        {
            this.WorkspaceKinds = (string[])data.GetValueOrDefault(nameof(WorkspaceKinds));
        }

        public WorkspaceKindMetadata(params string[] workspaceKinds)
        {
            this.WorkspaceKinds = workspaceKinds;
        }
    }
}
