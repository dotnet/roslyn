// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.References
{
    [ExportLanguageService(typeof(IFindUsagesService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspFindUsagesService : RoslynFindUsagesService
    {
        [ImportingConstructor]
        public CSharpLspFindUsagesService(CSharpLspClientServiceFactory csharpLspClientServiceFactory, RemoteLanguageServiceWorkspace remoteLanguageServiceWorkspace)
            : base(csharpLspClientServiceFactory, remoteLanguageServiceWorkspace)
        {
        }
    }

    [ExportLanguageService(typeof(IFindUsagesService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspFindUsagesService : RoslynFindUsagesService
    {
        [ImportingConstructor]
        public VBLspFindUsagesService(VisualBasicLspClientServiceFactory vbLspClientServiceFactory, RemoteLanguageServiceWorkspace remoteLanguageServiceWorkspace)
            : base(vbLspClientServiceFactory, remoteLanguageServiceWorkspace)
        {
        }
    }
}
