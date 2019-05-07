// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
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
}
