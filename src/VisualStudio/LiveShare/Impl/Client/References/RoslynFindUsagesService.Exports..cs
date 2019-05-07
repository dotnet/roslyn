//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System.Composition;
using Microsoft.Cascade.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    [ExportLanguageService(typeof(IFindUsagesService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspFindUsagesService : RoslynFindUsagesService
    {
        [ImportingConstructor]
        public CSharpLspFindUsagesService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, RemoteLanguageServiceWorkspace remoteLanguageServiceWorkspace)
            : base(roslynLSPClientServiceFactory, remoteLanguageServiceWorkspace)
        {
        }
    }

    [ExportLanguageService(typeof(IFindUsagesService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspFindUsagesService : RoslynFindUsagesService
    {
        [ImportingConstructor]
        public VBLspFindUsagesService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, RemoteLanguageServiceWorkspace remoteLanguageServiceWorkspace)
            : base(roslynLSPClientServiceFactory, remoteLanguageServiceWorkspace)
        {
        }
    }

#if !VS_16_0
    [ExportLanguageService(typeof(IFindUsagesService), StringConstants.TypeScriptLanguageName, WorkspaceKind.AnyCodeRoslynWorkspace), Shared]
    internal class TypeScriptLspFindUsagesService : RoslynFindUsagesService
    {
        [ImportingConstructor]
        public TypeScriptLspFindUsagesService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory, RemoteLanguageServiceWorkspace remoteLanguageServiceWorkspace)
            : base(roslynLSPClientServiceFactory, remoteLanguageServiceWorkspace)
        {
        }
    }
#endif
}
