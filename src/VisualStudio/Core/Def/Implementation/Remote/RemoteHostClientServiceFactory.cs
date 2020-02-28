// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    [ExportWorkspaceServiceFactory(typeof(IRemoteHostClientService), layer: ServiceLayer.Host), Shared]
    internal partial class RemoteHostClientServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IDiagnosticAnalyzerService _analyzerService;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        public RemoteHostClientServiceFactory(
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider,
            IDiagnosticAnalyzerService analyzerService)
        {
            _threadingContext = threadingContext;
            _listener = listenerProvider.GetListener(FeatureAttribute.RemoteHostClient);
            _analyzerService = analyzerService;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new RemoteHostClientService(_threadingContext, _listener, workspaceServices.Workspace, _analyzerService.AnalyzerInfoCache);
        }
    }
}
