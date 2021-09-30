// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    /// <summary>
    /// Allows workspaces to register themselves to be considered when LSP requests come in. Any workspace
    /// registered will be probed for a matching document/solution which can be given to the request handler
    /// to operate on.
    /// </summary>
    internal interface ILspWorkspaceRegistrationService
    {
        /// <summary>
        /// Get all current registered <see cref="Workspace"/>s.  Used to find the appropriate workspace
        /// corresponding to a particular <see cref="Document"/> request.
        /// </summary>
        ImmutableArray<Workspace> GetAllRegistrations();

        /// <summary>
        /// Returns the host/primary <see cref="Workspace"/> used for global operations associated
        /// with the entirety of the user's code (for example 'diagnostics' or 'search').
        /// </summary>
        Workspace? TryGetHostWorkspace();

        /// <summary>
        /// Register the specified workspace for consideration for LSP requests.
        /// </summary>
        void Register(Workspace workspace);
    }
}
