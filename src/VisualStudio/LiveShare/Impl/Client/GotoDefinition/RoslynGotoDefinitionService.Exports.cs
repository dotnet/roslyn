// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{

    [ExportLanguageService(typeof(IGoToDefinitionService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspGotoDefinitionService : RoslynGotoDefinitionService
    {
        [ImportingConstructor]
        public CSharpLspGotoDefinitionService(IStreamingFindUsagesPresenter streamingPresenter, RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, RemoteLanguageServiceWorkspace remoteWorkspace)
            : base(streamingPresenter, roslynLSPClientServiceFactory, remoteWorkspace) { }
    }

    [ExportLanguageService(typeof(IGoToDefinitionService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspGotoDefinitionService : RoslynGotoDefinitionService
    {
        [ImportingConstructor]
        public VBLspGotoDefinitionService(IStreamingFindUsagesPresenter streamingPresenter, RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, RemoteLanguageServiceWorkspace remoteWorkspace)
            : base(streamingPresenter, roslynLSPClientServiceFactory, remoteWorkspace) { }
    }

#if !VS_16_0
    [ExportLanguageService(typeof(IGoToDefinitionService), StringConstants.TypeScriptLanguageName, WorkspaceKind.AnyCodeRoslynWorkspace), Shared]
    internal class TypeScriptLspGotoDefinitionService : RoslynGotoDefinitionService
    {
        [ImportingConstructor]
        public TypeScriptLspGotoDefinitionService(IStreamingFindUsagesPresenter streamingPresenter, RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, RemoteLanguageServiceWorkspace remoteWorkspace)
            : base(streamingPresenter, roslynLSPClientServiceFactory, remoteWorkspace)
        { 
        }
    }
#endif
}
