// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    [ExportWorkspaceServiceFactory(typeof(IRemoteHostClientService), layer: ServiceLayer.Host), Shared]
    internal partial class RemoteHostClientServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IDiagnosticAnalyzerService _analyzerService;
        private readonly IEditorOptionsFactoryService _editorOptionService;

        [ImportingConstructor]
        public RemoteHostClientServiceFactory(
            IDiagnosticAnalyzerService analyzerService,
            IEditorOptionsFactoryService editorOptionService)
        {
            _analyzerService = analyzerService;
            _editorOptionService = editorOptionService;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new RemoteHostClientService(workspaceServices.Workspace, _analyzerService, _editorOptionService.GlobalOptions);
        }
    }
}
