// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceService(typeof(IWorkspaceContextService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioWorkspaceContextService : IWorkspaceContextService
    {
        /// <summary>
        /// Guid for UI context set by liveshare upon joining as a client to a session.
        /// </summary>
        private static readonly Guid s_sessionJoinedUIContextGuid = Guid.Parse("c6f0e3cb-a3c3-49bd-bad2-7aad8690c15b");
        private static readonly UIContext s_sessionJoinedUIContext = UIContext.FromUIContextGuid(s_sessionJoinedUIContextGuid);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceContextService()
        {
        }

        public bool IsInRemoteClientContext()
        {
            return s_sessionJoinedUIContext.IsActive;
        }
    }
}
