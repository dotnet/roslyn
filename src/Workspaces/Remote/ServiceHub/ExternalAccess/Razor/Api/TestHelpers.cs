// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Api
{
    internal static class TestHelpers
    {
        /// <summary>
        /// Allows tests to create a new remote workspace, rather than use RemoteWorkspaceManager.Default, so things can be isolated
        /// </summary>
        public static Workspace CreateTestWorkspace()
            => new RemoteWorkspaceManager(workspace => new SolutionAssetCache(workspace, cleanupInterval: TimeSpan.FromSeconds(30), purgeAfter: TimeSpan.FromMinutes(1))).GetWorkspace();
    }
}
