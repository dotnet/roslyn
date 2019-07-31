// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.GotoDefinition
{

    [ExportLanguageService(typeof(IGoToDefinitionService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspGotoDefinitionService : RoslynGotoDefinitionService
    {
        [ImportingConstructor]
        public CSharpLspGotoDefinitionService(IStreamingFindUsagesPresenter streamingPresenter, CSharpLspClientServiceFactory csharpLspClientServiceFactory,
            RemoteLanguageServiceWorkspace remoteWorkspace, IThreadingContext threadingContext)
            : base(streamingPresenter, csharpLspClientServiceFactory, remoteWorkspace, threadingContext) { }
    }

    [ExportLanguageService(typeof(IGoToDefinitionService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspGotoDefinitionService : RoslynGotoDefinitionService
    {
        [ImportingConstructor]
        public VBLspGotoDefinitionService(IStreamingFindUsagesPresenter streamingPresenter, VisualBasicLspClientServiceFactory vbLspClientServiceFactory,
            RemoteLanguageServiceWorkspace remoteWorkspace, IThreadingContext threadingContext)
            : base(streamingPresenter, vbLspClientServiceFactory, remoteWorkspace, threadingContext) { }
    }

    [ExportLanguageService(typeof(IGoToDefinitionService), StringConstants.TypeScriptLanguageName, WorkspaceKind.AnyCodeRoslynWorkspace), Shared]
    internal class TypeScriptLspGotoDefinitionService : RoslynGotoDefinitionService
    {
        [ImportingConstructor]
        public TypeScriptLspGotoDefinitionService(IStreamingFindUsagesPresenter streamingPresenter, TypeScriptLspClientServiceFactory typeScriptLspClientServiceFactory,
            RemoteLanguageServiceWorkspace remoteWorkspace, IThreadingContext threadingContext)
            : base(streamingPresenter, typeScriptLspClientServiceFactory, remoteWorkspace, threadingContext)
        {
        }
    }
}
