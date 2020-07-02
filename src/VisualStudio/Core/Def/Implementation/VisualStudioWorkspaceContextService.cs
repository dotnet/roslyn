// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class VisualStudioWorkspaceContextService : IWorkspaceContextService
    {
        /// <summary>
        /// Guid for UI context set by liveshare upon joining as a client to a session.
        /// </summary>
        private static readonly Guid s_sessionJoinedUIContextGuid = Guid.Parse("c6f0e3cb-a3c3-49bd-bad2-7aad8690c15b");
        private readonly UIContext _sessionJoinedUIContext;

        public VisualStudioWorkspaceContextService()
        {
            _sessionJoinedUIContext = UIContext.FromUIContextGuid(s_sessionJoinedUIContextGuid);
        }

        public bool IsInRemoteClientContext() => _sessionJoinedUIContext.IsActive;
    }

    [ExportWorkspaceServiceFactory(typeof(IWorkspaceContextService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioWorkspaceContextServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceContextServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices.Workspace.Kind == WorkspaceKind.AnyCodeRoslynWorkspace)
            {
                return new VisualStudioWorkspaceContextService();
            }

            return new DefaultWorkspaceContextService();
        }
    }
}
