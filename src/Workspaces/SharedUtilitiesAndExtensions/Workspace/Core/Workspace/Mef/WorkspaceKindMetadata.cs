// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            => this.WorkspaceKinds = (string[])data.GetValueOrDefault(nameof(WorkspaceKinds));

        public WorkspaceKindMetadata(params string[] workspaceKinds)
            => this.WorkspaceKinds = workspaceKinds;
    }
}
