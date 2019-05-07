//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    [ExportLanguageService(typeof(INavigationBarItemService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspNavigationBarItemService : RoslynNavigationBarItemService
    {
        [ImportingConstructor]
        protected CSharpLspNavigationBarItemService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory)
            : base(roslynLSPClientServiceFactory)
        {
        }
    }

    [ExportLanguageService(typeof(INavigationBarItemService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspNavigationBarItemService : RoslynNavigationBarItemService
    {
        [ImportingConstructor]
        protected VBLspNavigationBarItemService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory)
            : base(roslynLSPClientServiceFactory)
        {
        }
    }

    [ExportLanguageService(typeof(INavigationBarItemService), StringConstants.TypeScriptLanguageName, WorkspaceKind.AnyCodeRoslynWorkspace), Shared]
    internal class TypeScriptLspNavigationBarItemService : RoslynNavigationBarItemService
    {
        [ImportingConstructor]
        protected TypeScriptLspNavigationBarItemService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory)
            : base(roslynLSPClientServiceFactory)
        {
        }
    }
}
