//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    [ExportLanguageService(typeof(IRemoteDiagnosticsService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpLspRemoteDiagnosticsService : RoslynRemoteDiagnosticsService
    {
        [ImportingConstructor]
        public CSharpLspRemoteDiagnosticsService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory)
            : base(roslynLSPClientServiceFactory)
        {
        }
    }

    [ExportLanguageService(typeof(IRemoteDiagnosticsService), StringConstants.VBLspLanguageName), Shared]
    internal class VBLspRemoteDiagnosticsService : RoslynRemoteDiagnosticsService
    {
        [ImportingConstructor]
        public VBLspRemoteDiagnosticsService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory)
            : base(roslynLSPClientServiceFactory)
        {
        }
    }

#if !VS_16_0
    [ExportLanguageService(typeof(IRemoteDiagnosticsService), StringConstants.TypeScriptLanguageName, WorkspaceKind.AnyCodeRoslynWorkspace), Shared]
    internal class TypeScriptLspRemoteDiagnosticsService : RoslynRemoteDiagnosticsService
    {
        [ImportingConstructor]
        public TypeScriptLspRemoteDiagnosticsService(RoslynLSPClientServiceFactory roslynLSPClientServiceFactory)
            : base(roslynLSPClientServiceFactory)
        {
        }
    }
#endif
}
