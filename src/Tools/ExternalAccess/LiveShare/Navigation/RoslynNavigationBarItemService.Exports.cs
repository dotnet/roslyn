// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Navigation
{
    [ExportLanguageService(typeof(INavigationBarItemService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspNavigationBarItemService : RoslynNavigationBarItemService
    {
        [ImportingConstructor]
        protected CSharpLspNavigationBarItemService(CSharpLspClientServiceFactory csharpLspClientServiceFactory)
            : base(csharpLspClientServiceFactory)
        {
        }
    }

    [ExportLanguageService(typeof(INavigationBarItemService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspNavigationBarItemService : RoslynNavigationBarItemService
    {
        [ImportingConstructor]
        protected VBLspNavigationBarItemService(VisualBasicLspClientServiceFactory vbLspClientServiceFactory)
            : base(vbLspClientServiceFactory)
        {
        }
    }

    [ExportLanguageService(typeof(INavigationBarItemService), StringConstants.TypeScriptLanguageName, WorkspaceKind.AnyCodeRoslynWorkspace), Shared]
    internal class TypeScriptLspNavigationBarItemService : RoslynNavigationBarItemService
    {
        [ImportingConstructor]
        protected TypeScriptLspNavigationBarItemService(TypeScriptLspClientServiceFactory typeScriptLspClientServiceFactory)
            : base(typeScriptLspClientServiceFactory)
        {
        }
    }
}
