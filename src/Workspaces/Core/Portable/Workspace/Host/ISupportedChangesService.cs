// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis
{
    internal interface ISupportedChangesService : IWorkspaceService
    {
        bool CanApplyChange(ApplyChangesKind kind);
    }

    [ExportWorkspaceServiceFactory(typeof(ISupportedChangesService)), Shared]
    internal sealed class DefaultSupportedChangesServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultSupportedChangesServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new DefaultSupportedChangesService(workspaceServices.Workspace);

        private sealed class DefaultSupportedChangesService : ISupportedChangesService
        {
            private readonly Workspace _workspace;

            public DefaultSupportedChangesService(Workspace workspace)
            {
                _workspace = workspace;
            }

            public bool CanApplyChange(ApplyChangesKind kind)
                => _workspace.CanApplyChange(kind);
        }
    }

    internal static class SupportedChangesServiceExtensions
    {
        public static bool CanApplyChange(this Solution solution, ApplyChangesKind kind)
            => solution.Services.GetRequiredService<ISupportedChangesService>().CanApplyChange(kind);
    }
}
