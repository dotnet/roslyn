// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [ExportWorkspaceServiceFactory(typeof(ICompilationOutputsProviderService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioCompilationOutputsProviderServiceFactory : IWorkspaceServiceFactory
    {
        private readonly VisualStudioWorkspaceImpl _workspace;

        [ImportingConstructor]
        public VisualStudioCompilationOutputsProviderServiceFactory(VisualStudioWorkspaceImpl workspace)
        {
            _workspace = workspace;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new VisualStudioCompilationOutputsProviderService(_workspace);
    }
}
