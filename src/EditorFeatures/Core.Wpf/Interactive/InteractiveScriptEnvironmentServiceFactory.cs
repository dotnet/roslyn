// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.Interactive;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting;

namespace Microsoft.CodeAnalysis.Editor.Interactive
{
    [ExportWorkspaceServiceFactory(typeof(IScriptEnvironmentService), WorkspaceKind.Interactive), Shared]
    internal sealed class InteractiveScriptEnvironmentServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InteractiveScriptEnvironmentServiceFactory()
        {
        }

        private sealed class Service : IScriptEnvironmentService
        {
            private readonly InteractiveWorkspace _workspace;

            public ImmutableArray<string> MetadataReferenceSearchPaths => _workspace.Evaluator.ReferenceSearchPaths;
            public ImmutableArray<string> SourceReferenceSearchPaths => _workspace.Evaluator.SourceSearchPaths;
            public string BaseDirectory => _workspace.Evaluator.WorkingDirectory;

            public Service(InteractiveWorkspace workspace)
                => _workspace = workspace;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices.Workspace is InteractiveWorkspace interactiveWorkspace)
            {
                return new Service(interactiveWorkspace);
            }

            // this service is not applicable to workspaces other than InteractiveWorkspace:
            return null;
        }
    }
}
