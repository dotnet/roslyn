// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// <see cref="VSWorkspaceKind" /> represents the various kinds of known workspaces.
    /// </summary>
    internal enum VSWorkspaceKind
    {
        /// <summary>
        /// Host Workspace
        /// </summary>
        Host = 1,

        /// <summary>
        /// Miscellaneous Files Workspace
        /// </summary>
        MiscellaneousFiles = 2,
    }
}
