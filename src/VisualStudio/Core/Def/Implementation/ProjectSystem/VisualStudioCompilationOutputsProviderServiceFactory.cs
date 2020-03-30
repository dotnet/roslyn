// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioCompilationOutputsProviderServiceFactory(VisualStudioWorkspaceImpl workspace)
            => _workspace = workspace;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new VisualStudioCompilationOutputsProviderService(_workspace);
    }
}
