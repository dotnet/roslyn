// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.References
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
